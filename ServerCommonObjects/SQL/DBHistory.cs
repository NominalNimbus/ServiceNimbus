/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using CommonObjects;

namespace ServerCommonObjects.SQL
{
    public class DBHistory
    {
        #region Members

        private string _connectionString;
        private DateTime _sqlMinDate;
        private SqlBulkCopy _ticksBulkCopy;
        private readonly DataTable _ticksDataTable;
        private readonly DataTable _ticksL2DataTable;
        private readonly object _tickBulkLock;
        private readonly Dictionary<string, object> _minutesLockObjects;


        #endregion //Members

        #region Properties

        public List<Security> AvailableSymbols { get; private set; }

        #endregion //Properties

        #region Constructor

        public DBHistory()
        {
            _sqlMinDate = new DateTime(1900, 1, 1);
            _tickBulkLock = new object();
            _minutesLockObjects = new Dictionary<string, object>();

            _ticksDataTable = new DataTable("Ticks");
            _ticksDataTable.Columns.Add("Timestamp", typeof(DateTime));
            _ticksDataTable.Columns.Add("Ask", typeof(decimal));
            _ticksDataTable.Columns.Add("Bid", typeof(decimal));
            _ticksDataTable.Columns.Add("AskSize", typeof(decimal));
            _ticksDataTable.Columns.Add("BidSize", typeof(decimal));

            _ticksL2DataTable = new DataTable("TicksLevel2");
            _ticksL2DataTable.Columns.Add("Timestamp", typeof(DateTime));
            _ticksL2DataTable.Columns.Add("Level", typeof(byte));
            _ticksL2DataTable.Columns.Add("Ask", typeof(decimal));
            _ticksL2DataTable.Columns.Add("Bid", typeof(decimal));
            _ticksL2DataTable.Columns.Add("AskSize", typeof(decimal));
            _ticksL2DataTable.Columns.Add("BidSize", typeof(decimal));
        }

        #endregion //Constructor

        #region Start/Stop

        public void Start(string connectionString)
        {
            _connectionString = connectionString;
            _ticksBulkCopy = new SqlBulkCopy(_connectionString, SqlBulkCopyOptions.CheckConstraints) { BulkCopyTimeout = 30 };

            AvailableSymbols = GetSecurities();
        }

        public void Stop()
        {
            AvailableSymbols.Clear();
            lock (_tickBulkLock)
            {
                _ticksDataTable.Rows.Clear();
                _ticksL2DataTable.Rows.Clear();
            }

            try
            {
                if (_ticksBulkCopy != null)
                {
                    lock (_tickBulkLock)
                    {
                        _ticksBulkCopy.Close();
                        ((IDisposable)_ticksBulkCopy).Dispose();
                    }
                    _ticksBulkCopy = null;

                }
            }
            catch (Exception e)
            {
                Logger.Warning("Failed to dispose of ticks BulkCopy object", e);
            }
        }

        #endregion //Start/Stop

        #region Public Add Methods

        public void AddMinRecordsToDb(List<Bar> bars, string symbol, string dataFeed)
        {
            if (bars == null || bars.Count == 0)
                return;

            var key = $"{symbol}__{dataFeed}";
            object lockObject = null;
            lock (_minutesLockObjects)
            {
                if (!_minutesLockObjects.TryGetValue(key, out lockObject))
                {
                    lockObject = new object();
                    _minutesLockObjects.Add(key, lockObject);
                }
            }

            lock (lockObject)
            {
                var latestStored = GetUtmostMinuteTimestamp(symbol, dataFeed, false);
                var barsToStore = new List<BarWithInstrument>(bars.Count);
                foreach (var bar in bars)
                {
                    if (bar.Date > latestStored)
                        barsToStore.Add(new BarWithInstrument(bar, symbol, dataFeed));
                }

                if (barsToStore.Count > 0)
                {
                    using (var loader = new SqlBulkCopy(_connectionString, SqlBulkCopyOptions.CheckConstraints))
                    {
                        try
                        {
                            loader.DestinationTableName = "BarsMinute";
                            loader.WriteToServer(new SqlBulkBarCopy(barsToStore));
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Failed to write data to BarsMinute table", ex);
                        }
                    }
                }
            }
        }

        public void AddDayRecordsToDb(List<Bar> bars, string symbol, string dataFeed)
        {
            if (bars == null || bars.Count == 0)
                return;

            var latestStored = GetUtmostDayDate(symbol, dataFeed, false);
            var barsToStore = new List<BarWithInstrument>(bars.Count);
            for (int i = 0; i < bars.Count; i++)
            {
                if (bars[i].Date.Date > latestStored)
                {
                    latestStored = bars[i].Date.Date;
                    barsToStore.Add(new BarWithInstrument(bars[i], bars[i].Date.Date, symbol, dataFeed));
                }
            }

            using (var loader = new SqlBulkCopy(_connectionString, SqlBulkCopyOptions.CheckConstraints))
            {
                try
                {
                    loader.DestinationTableName = "BarsDaily";
                    loader.WriteToServer(new SqlBulkBarCopy(barsToStore));
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to write data to BarsDaily table", ex);
                }
            }
        }

        public void AddTicksToDb(List<Tick> ticks)
        {
            if (_ticksBulkCopy == null || ticks == null || ticks.Count == 0)
                return;

            lock (_tickBulkLock)
            {
                foreach (var symbols in ticks.GroupBy(t => t.Symbol))
                {
                    //top of the book ticks
                    _ticksDataTable.Rows.Clear();
                    _ticksBulkCopy.DestinationTableName = DBMaintenance
                        .GetSecurityTableName(symbols.Key.Symbol, symbols.Key.DataFeed);
                    foreach (var item in symbols)
                        _ticksDataTable.Rows.Add(item.Date, item.Ask, item.Bid, item.AskSize, item.BidSize);

                    try
                    {
                        _ticksBulkCopy.WriteToServer(_ticksDataTable);
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"Failed to store {symbols.Count()} {symbols.Key.Symbol} ticks to DB: {e.Message}");
                    }

                    if (!symbols.Any(s => s.Level2 != null && s.Level2.Count > 0))
                        continue;

                    //level 2 ticks
                    _ticksL2DataTable.Rows.Clear();
                    _ticksBulkCopy.DestinationTableName = DBMaintenance
                        .GetSecurityTableName(symbols.Key.Symbol, symbols.Key.DataFeed, 2);
                    foreach (var container in symbols)
                        foreach (var item in container.Level2)
                        {
                            _ticksL2DataTable.Rows.Add(container.Date, item.DomLevel,
                                item.AskPrice, item.BidPrice, item.AskSize, item.BidSize);
                        }

                    try
                    {
                        _ticksBulkCopy.WriteToServer(_ticksL2DataTable);
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"Failed to store {symbols.Count()} {symbols.Key.Symbol} L2 ticks to DB: {e.Message}");
                    }
                }
            }
        }

        public void AddSecurity(Security item)
        {
            DBMaintenance.CreateSecurityTickTable(_connectionString, item.Symbol, item.DataFeed);
            DBMaintenance.CreateSecurityTickTable(_connectionString, item.Symbol, item.DataFeed, 2);

            if (AvailableSymbols.Any(s => s.Symbol == item.Symbol && s.DataFeed == item.DataFeed))
                return;

            using (var aConnection = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("INSERT INTO [dbo].[Securities]"
                    + "([Id],[Symbol],[Name],[DataFeed],[PriceIncrement],[QtyIncrement],[Digit],[AssetClass],[BaseCurrency],"
                    + "[UnitOfMeasure],[MarginRate],[MaxPosition],[UnitPrice],[ContractSize],[MarketOpen],[MarketClose])"
                    + "VALUES(@id, @symbol, @name, @df, @priceIncr, @qtyIncr, @digits, @asset, @currency, @measure, "
                    + "@margin, @maxPos, @untiPrice, @contract, @open, @close)", aConnection);

                cmd.Parameters.AddWithValue("id", item.SecurityId);
                cmd.Parameters.AddWithValue("symbol", item.Symbol);
                cmd.Parameters.AddWithValue("name", item.Name);
                cmd.Parameters.AddWithValue("df", item.DataFeed);
                cmd.Parameters.AddWithValue("priceIncr", item.PriceIncrement);
                cmd.Parameters.AddWithValue("qtyIncr", item.QtyIncrement);
                cmd.Parameters.AddWithValue("digits", item.Digit);
                cmd.Parameters.AddWithValue("asset", item.AssetClass);
                cmd.Parameters.AddWithValue("currency", item.BaseCurrency);
                cmd.Parameters.AddWithValue("measure", item.UnitOfMeasure);
                cmd.Parameters.AddWithValue("margin", item.MarginRate);
                cmd.Parameters.AddWithValue("maxPos", item.MaxPosition);
                cmd.Parameters.AddWithValue("untiPrice", item.UnitPrice);
                cmd.Parameters.AddWithValue("contract", item.ContractSize);
                cmd.Parameters.AddWithValue("open", item.MarketOpen);
                cmd.Parameters.AddWithValue("close", item.MarketClose);

                SqlTransaction transaction = null;

                try
                {
                    aConnection.Open();
                    transaction = aConnection.BeginTransaction();
                    cmd.Transaction = transaction;

                    var value = cmd.ExecuteNonQuery();

                    if (value > 0)
                        AvailableSymbols.Add(item);

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to insert security.", e);
                    transaction?.Rollback();
                }
            }
        }

        #endregion //Public Add Methods

        #region Public Get Methods

        public List<Bar> GetDayHistory(Selection parameters)
        {
            var count = (parameters.From > _sqlMinDate && parameters.To > parameters.From)
                ? Int32.MaxValue : parameters.BarCount;

            var result = new List<Bar>();
            using (var aConnection = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT TOP(@recCount) * FROM [BarsDaily] WHERE [Symbol]=@symbol AND [DataFeed]=@dataFeed"
                    + " AND [Timestamp] BETWEEN @fromDate AND @toDate ORDER BY [Timestamp] DESC", aConnection);
                cmd.Parameters.AddWithValue("recCount", count > 0 ? count : Int32.MaxValue);
                cmd.Parameters.AddWithValue("symbol", parameters.Symbol);
                cmd.Parameters.AddWithValue("dataFeed", parameters.DataFeed);
                cmd.Parameters.AddWithValue("fromDate", parameters.From < _sqlMinDate ? _sqlMinDate : parameters.From);
                cmd.Parameters.AddWithValue("toDate", parameters.To < _sqlMinDate ? DateTime.UtcNow : parameters.To);

                SqlTransaction transaction = null;

                try
                {
                    aConnection.Open();
                    transaction = aConnection.BeginTransaction(IsolationLevel.ReadUncommitted, "DayHistoryRequest");
                    cmd.Transaction = transaction;

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                            return result;

                        while (reader.Read())
                        {
                            try
                            {
                                result.Add(new Bar
                                {
                                    Date = (DateTime)reader["Timestamp"],
                                    OpenBid = (decimal)reader["OpenBid"],
                                    OpenAsk = (decimal)reader["OpenAsk"],
                                    HighBid = (decimal)reader["HighBid"],
                                    HighAsk = (decimal)reader["HighAsk"],
                                    LowBid = (decimal)reader["LowBid"],
                                    LowAsk = (decimal)reader["LowAsk"],
                                    CloseBid = (decimal)reader["CloseBid"],
                                    CloseAsk = (decimal)reader["CloseAsk"],
                                    VolumeBid = (decimal)(float)reader["VolumeBid"],
                                    VolumeAsk = (decimal)(float)reader["VolumeAsk"]
                                });
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Failed to parse day bar", ex);
                            }
                        }
                    }

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to load EOD history", e);

                    transaction?.Rollback();

                    return result;
                }
            }
            return result;
        }

        public List<Bar> GetMinHistory(Selection parameters)
        {
            var sw = new Stopwatch();
            sw.Start();

            var count = parameters.From > _sqlMinDate && parameters.To > parameters.From
                ? int.MaxValue : parameters.BarCount;

            var result = new List<Bar>();
            SqlTransaction transaction = null;
            using (var aConnection = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT TOP(@recCount) * FROM [BarsMinute] "
                    + "WHERE [Symbol]=@symbol AND [DataFeed]=@dataFeed "
                    + "AND [Timestamp] BETWEEN @fromDate AND @toDate ORDER BY [Timestamp] DESC", aConnection);
                cmd.Parameters.AddWithValue("recCount", count > 0 ? count : int.MaxValue);
                cmd.Parameters.AddWithValue("symbol", parameters.Symbol);
                cmd.Parameters.AddWithValue("dataFeed", parameters.DataFeed);
                cmd.Parameters.AddWithValue("fromDate", parameters.From < _sqlMinDate ? _sqlMinDate : parameters.From);
                cmd.Parameters.AddWithValue("toDate", parameters.To < _sqlMinDate ? DateTime.UtcNow : parameters.To);

                try
                {
                    aConnection.Open();
                    transaction = aConnection.BeginTransaction(IsolationLevel.ReadUncommitted, "MinHistoryRequest");
                    cmd.Transaction = transaction;

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                try
                                {
                                    result.Add(new Bar
                                    {
                                        Date = (DateTime)reader["Timestamp"],
                                        OpenBid = (decimal)reader["OpenBid"],
                                        OpenAsk = (decimal)reader["OpenAsk"],
                                        HighBid = (decimal)reader["HighBid"],
                                        HighAsk = (decimal)reader["HighAsk"],
                                        LowBid = (decimal)reader["LowBid"],
                                        LowAsk = (decimal)reader["LowAsk"],
                                        CloseBid = (decimal)reader["CloseBid"],
                                        CloseAsk = (decimal)reader["CloseAsk"],
                                        VolumeBid = (decimal)(float)reader["VolumeBid"],
                                        VolumeAsk = (decimal)(float)reader["VolumeAsk"]
                                    });
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("Failed to parse a bar", ex);
                                }
                            }
                        }
                    }

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to load min history", e);
                    transaction?.Rollback();
                    return result;
                }
            }

            #region Request missing data from Archive table

            string addDataCmd = null;
            if (count > 0 && count != int.MaxValue)
            {
                if (result.Count < count)
                {
                    addDataCmd = $"SELECT TOP({count - result.Count}) * FROM [BarsMinute_Archive] "
                        + $"WHERE [Symbol]='{parameters.Symbol}' AND [DataFeed]='{parameters.DataFeed}' "
                        + "ORDER BY [Timestamp] DESC";
                }
            }
            else
            {
                if (result.Count == 0 || result.Min(i => i.Date) > parameters.From)
                {
                    var from = parameters.From < _sqlMinDate ? _sqlMinDate : parameters.From;
                    var to = parameters.To <= from ? DateTime.UtcNow : parameters.To;
                    addDataCmd = "SELECT * FROM [BarsMinute_Archive] "
                        + $"WHERE [Symbol]='{parameters.Symbol}' AND [DataFeed]='{parameters.DataFeed}' "
                        + $"AND [Timestamp] BETWEEN '{from:yyyy-MM-dd HH:mm:ss}' AND '{to:yyyy-MM-dd HH:mm:ss}' "
                        + "ORDER BY [Timestamp] DESC";
                }
            }

            if (addDataCmd == null)
                return result;

            try
            {
                transaction = null;
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted, "MinArchiveHistoryRequest");
                    var cmd = new SqlCommand(addDataCmd, connection);
                    cmd.Transaction = transaction;

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                try
                                {
                                    result.Add(new Bar
                                    {
                                        Date = (DateTime)reader["Timestamp"],
                                        OpenBid = (decimal)reader["OpenBid"],
                                        OpenAsk = (decimal)reader["OpenAsk"],
                                        HighBid = (decimal)reader["HighBid"],
                                        HighAsk = (decimal)reader["HighAsk"],
                                        LowBid = (decimal)reader["LowBid"],
                                        LowAsk = (decimal)reader["LowAsk"],
                                        CloseBid = (decimal)reader["CloseBid"],
                                        CloseAsk = (decimal)reader["CloseAsk"],
                                        VolumeBid = (decimal)(float)reader["VolumeBid"],
                                        VolumeAsk = (decimal)(float)reader["VolumeAsk"]
                                    });
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("Failed to parse a bar", ex);
                                }
                            }
                        }
                    }

                    transaction.Commit();
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to load additional min history", e);
                if (transaction != null)
                    transaction.Rollback();
            }

            #endregion

            sw.Stop();
            Debug.WriteLine(sw.Elapsed);

            return result;
        }

        public List<Bar> GetTickHistory(Selection parameters)
        {
            int count = (parameters.From > _sqlMinDate && parameters.To > parameters.From)
                ? int.MaxValue : parameters.BarCount;

            string tableName = string.Empty;
            var result = new List<Bar>();
            SqlTransaction transaction = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = null;
                tableName = DBMaintenance.GetSecurityTableName(parameters.Symbol, parameters.DataFeed, parameters.Level);
                if (parameters.Level == 0)
                {
                    cmd = new SqlCommand("SELECT TOP(@recCount) * FROM [" + tableName + "] "
                        + "WHERE [Timestamp] BETWEEN @fromDate AND @toDate ORDER BY [Timestamp] DESC",
                        connection);
                }
                else
                {
                    cmd = new SqlCommand("SELECT TOP(@recCount) * FROM [" + tableName + "] "
                        + "WHERE [Level] = @level AND [Timestamp] BETWEEN @fromDate AND @toDate ORDER BY [Timestamp] DESC",
                        connection);
                }

                cmd.Parameters.AddWithValue("recCount", count > 0 ? count : Int32.MaxValue);
                cmd.Parameters.AddWithValue("fromDate", parameters.From < _sqlMinDate ? _sqlMinDate : parameters.From);
                cmd.Parameters.AddWithValue("toDate", parameters.To < _sqlMinDate ? DateTime.UtcNow : parameters.To);
                if (parameters.Level != 0)
                    cmd.Parameters.AddWithValue("level", parameters.Level);

                try
                {
                    connection.Open();
                    transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted, "TickHistorySelect");
                    cmd.Transaction = transaction;

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                var bid = (decimal)reader["Bid"];
                                var ask = (decimal)reader["Ask"];
                                var bidSize = (decimal)reader["BidSize"];
                                var askSize = (decimal)reader["AskSize"];
                                result.Add(new Bar
                                {
                                    Date = (DateTime)reader["Timestamp"],
                                    OpenBid = bid,
                                    OpenAsk = ask,
                                    HighBid = bid,
                                    HighAsk = ask,
                                    LowBid = bid,
                                    LowAsk = ask,
                                    CloseBid = bid,
                                    CloseAsk = ask,
                                    VolumeBid = bidSize,
                                    VolumeAsk = askSize
                                });
                            }
                        }
                    }

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to load tick history", e);
                    if (transaction != null)
                        transaction.Rollback();
                    return result;
                }
            }

            #region Request missing data from Archive table
            string addDataCmd = null;
            tableName += "_Archive";
            if (count > 0 && count != Int32.MaxValue)
            {
                if (result.Count < count)
                {
                    if (parameters.Level == 0)
                    {
                        addDataCmd = $"SELECT TOP({count - result.Count}) * FROM [{tableName}] "
                            + "ORDER BY [Timestamp] DESC";
                    }
                    else
                    {
                        addDataCmd = $"SELECT TOP({count - result.Count}) * FROM [{tableName}] "
                            + $"WHERE [Level] = {parameters.Level} ORDER BY [Timestamp] DESC";
                    }
                }
            }
            else
            {
                if (result.Count == 0 || result.Min(i => i.Date) > parameters.From)
                {
                    var from = parameters.From < _sqlMinDate ? _sqlMinDate : parameters.From;
                    var to = parameters.To <= from ? DateTime.UtcNow : parameters.To;
                    if (parameters.Level == 0)
                    {
                        addDataCmd = $"SELECT * FROM [{tableName}] WHERE [Timestamp] BETWEEN '{from:yyyy-MM-dd HH:mm:ss}' "
                            + $"AND '{to:yyyy-MM-dd HH:mm:ss}' ORDER BY [Timestamp] DESC";
                    }
                    else
                    {
                        addDataCmd = $"SELECT * FROM [{tableName}] WHERE [Level] = {parameters.Level} "
                            + $"AND [Timestamp] BETWEEN '{from:yyyy-MM-dd HH:mm:ss}' AND '{to:yyyy-MM-dd HH:mm:ss}' "
                            + "ORDER BY [Timestamp] DESC";
                    }
                }
            }

            if (addDataCmd == null)
                return result;

            try
            {
                transaction = null;
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    if (IsExistingTable(connection, tableName) != true)
                        return result;

                    transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted,
                        "TickArchiveHistorySelect");
                    var cmd = new SqlCommand(addDataCmd, connection);
                    cmd.Transaction = transaction;
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                var bid = (decimal)reader["Bid"];
                                var ask = (decimal)reader["Ask"];
                                var bidSize = (decimal)reader["BidSize"];
                                var askSize = (decimal)reader["AskSize"];
                                result.Add(new Bar
                                {
                                    Date = (DateTime)reader["Timestamp"],
                                    OpenBid = bid,
                                    OpenAsk = ask,
                                    HighBid = bid,
                                    HighAsk = ask,
                                    LowBid = bid,
                                    LowAsk = ask,
                                    CloseBid = bid,
                                    CloseAsk = ask,
                                    VolumeBid = bidSize,
                                    VolumeAsk = askSize
                                });
                            }
                        }
                    }

                    transaction.Commit();
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to load archived ticks", e);
                if (transaction != null)
                    transaction.Rollback();
            }
            #endregion

            return result;
        }

        public List<Bar> GetDayHistoryL2(Selection parameters)
        {
            //calculate start date for ticks based on bar size
            if (parameters.BarCount > 0 && parameters.BarCount != Int32.MaxValue)
            {
                var from = GetStartingDate(parameters.Timeframe, parameters.TimeFactor, parameters.BarCount);
                if (parameters.From < _sqlMinDate || (parameters.From > from && from > _sqlMinDate))
                    parameters.From = from;
            }

            if (parameters.To < parameters.From)
                parameters.To = DateTime.UtcNow;

            var tickSelection = (Selection)parameters.Clone();
            tickSelection.BarCount = 0;
            tickSelection.Timeframe = Timeframe.Tick;
            tickSelection.TimeFactor = 1;
            var ticks = GetTickHistory(tickSelection);
            if (ticks == null || ticks.Count == 0)
                return new List<Bar>(0);

            ticks.Reverse();  //sort ticks by date in ascending order
            DateTime date = ticks[0].Date;
            Bar bar = new Bar(date, ticks[0].CloseBid, ticks[0].CloseAsk, 0, 0);
            var result = new List<Bar>();
            foreach (var tick in ticks)
            {
                if (tick.Date < date.AddDays(1))
                {
                    bar.AppendTick(tick.CloseBid, tick.CloseAsk, tick.VolumeBid, tick.VolumeAsk);
                }
                else
                {
                    result.Add(new Bar(bar, date));
                    date = tick.Date.Date;
                    bar.Date = date;
                    bar.OpenBid = tick.CloseBid;
                    bar.OpenAsk = tick.CloseAsk;
                    bar.HighBid = tick.CloseBid;
                    bar.HighAsk = tick.CloseAsk;
                    bar.LowBid = tick.CloseBid;
                    bar.LowAsk = tick.CloseAsk;
                    bar.CloseBid = tick.CloseBid;
                    bar.CloseAsk = tick.CloseAsk;
                    bar.VolumeBid = tick.VolumeBid;
                    bar.VolumeAsk = tick.VolumeAsk;
                }
            }

            if (bar != null && result.Count > 0 && !result.Any(p => p.Date == bar.Date))
                result.Add(new Bar(bar));

            result.Reverse();  //sort results by date in descending order
            return result;
        }

        public List<Bar> GetMinHistoryL2(Selection parameters)
        {
            //calculate start date for ticks based on bar size
            if (parameters.BarCount > 0 && parameters.BarCount != Int32.MaxValue)
            {
                var from = GetStartingDate(parameters.Timeframe, parameters.TimeFactor, parameters.BarCount);
                if (parameters.From < _sqlMinDate || (parameters.From > from && from > _sqlMinDate))
                    parameters.From = from;
            }

            if (parameters.To < parameters.From)
                parameters.To = DateTime.UtcNow;

            var tickSelection = (Selection)parameters.Clone();
            tickSelection.BarCount = 0;
            tickSelection.Timeframe = Timeframe.Tick;
            tickSelection.TimeFactor = 1;
            var ticks = GetTickHistory(tickSelection);
            if (ticks == null || ticks.Count == 0)
                return new List<Bar>(0);

            ticks.Reverse();  //sort ticks by date in ascending order
            DateTime date = CommonHelper.GetTimeRoundToMinute(ticks[0].Date);
            Bar bar = new Bar(date, ticks[0].CloseBid, ticks[0].CloseAsk, 0, 0);
            var result = new List<Bar>();
            foreach (var tick in ticks)
            {
                if (tick.Date < date.AddMinutes(1))
                {
                    bar.AppendTick(tick.CloseBid, tick.CloseAsk, tick.VolumeBid, tick.VolumeAsk);
                }
                else
                {
                    result.Add(new Bar(bar));
                    date = CommonHelper.GetTimeRoundToMinute(tick.Date);
                    bar.Date = date;
                    bar.OpenBid = tick.CloseBid;
                    bar.OpenAsk = tick.CloseAsk;
                    bar.HighBid = tick.CloseBid;
                    bar.HighAsk = tick.CloseAsk;
                    bar.LowBid = tick.CloseBid;
                    bar.LowAsk = tick.CloseAsk;
                    bar.CloseBid = tick.CloseBid;
                    bar.CloseAsk = tick.CloseAsk;
                    bar.VolumeBid = tick.VolumeBid;
                    bar.VolumeAsk = tick.VolumeAsk;
                }
            }

            if (bar != null && result.Count > 0 && !result.Any(p => p.Date == bar.Date))
                result.Add(new Bar(bar));

            result.Reverse();  //sort results by date in descending order
            return result;
        }

        public Tick GetTick(string symbol, string dataFeed, DateTime timestamp, bool useArchiveData = false)
        {
            var securityId = GetSecurityId(symbol, dataFeed);
            if (securityId < 1)
                return null;

            Tick tick = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                SqlTransaction transaction = null;
                string table = DBMaintenance.GetSecurityTableName(symbol, dataFeed);
                if (useArchiveData)
                    table += "_Archive";

                var cmd = new SqlCommand("SELECT TOP 1 [Timestamp], [Ask], [Bid], [AskSize], [BidSize] FROM ["
                    + table + "] WHERE [Timestamp] <= @time ORDER BY [Timestamp] DESC", connection);
                cmd.Parameters.AddWithValue("time", timestamp);

                try
                {
                    connection.Open();
                    if (IsExistingTable(connection, table) != true)
                        return tick;

                    transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted, "TickRequest");
                    cmd.Transaction = transaction;
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows && reader.Read())
                        {
                            var ask = (decimal)reader["Ask"];
                            var bid = (decimal)reader["Bid"];
                            tick = new Tick
                            {
                                Symbol = new Security { Symbol = symbol, DataFeed = dataFeed, SecurityId = securityId },
                                Date = (DateTime)reader["Timestamp"],
                                Bid = bid,
                                Ask = ask,
                                BidSize = (int)reader["BidSize"],
                                AskSize = (int)reader["AskSize"],
                                Price = (bid + ask) / 2M
                            };
                        }
                        else if (!reader.HasRows && !useArchiveData)
                        {
                            return GetTick(symbol, dataFeed, timestamp, true);
                        }
                    }

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to retrieve tick for " + symbol, e);
                    if (transaction != null)
                        transaction.Rollback();
                }
            }

            return tick;
        }

        public IDictionary<int, decimal> GetDailyTotalVolume(string symbol, string dataFeed, IEnumerable<int> levels, PriceType priceType)
        {
            var totalVolume = new Dictionary<int, decimal>();
            if (priceType != PriceType.Bid && priceType != PriceType.Ask)
                return totalVolume;

            var priceName = priceType + "Size";
            var tableName = DBMaintenance.GetSecurityTableName(symbol, dataFeed, 2);
            using (var connection = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand($"SELECT [Level], SUM([{priceName}]) FROM [{tableName}] "
                    + "where [Timestamp] >= @today GROUP BY [Level]", connection);
                cmd.Parameters.AddWithValue("today", DateTime.UtcNow.Date);

                try
                {
                    connection.Open();
                    if (IsExistingTable(connection, tableName) == true)
                    {

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.HasRows)
                                return totalVolume;

                            while (reader.Read())
                            {
                                totalVolume[reader.GetByte(0)] = reader.GetInt32(1);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Error($"Failed to retrieve total {priceType} volume for {symbol}", e);
                }
            }

            return totalVolume;
        }

        public decimal GetDailyTotalVolume(string symbol, string dataFeed, byte level, PriceType priceType)
        {
            if (priceType != PriceType.Bid && priceType != PriceType.Ask)
                return 0M;

            var priceName = priceType + "Size";
            var tableName = DBMaintenance.GetSecurityTableName(symbol, dataFeed, level);
            using (var connection = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = null;
                if (level == 0)
                {
                    cmd = new SqlCommand($"SELECT SUM([{priceName}]) FROM [{tableName}] "
                        + "WHERE [Timestamp] >= @today", connection);
                    cmd.Parameters.AddWithValue("today", DateTime.UtcNow.Date);
                }
                else
                {
                    cmd = new SqlCommand($"SELECT SUM([{priceName}]) FROM [{tableName}] "
                        + "WHERE [Level] = @level AND [Timestamp] >= @today", connection);
                    cmd.Parameters.AddWithValue("level", level);
                    cmd.Parameters.AddWithValue("today", DateTime.UtcNow.Date);
                }

                try
                {
                    connection.Open();
                    if (IsExistingTable(connection, tableName) == true)
                    {
                        var result = cmd.ExecuteScalar();
                        return (result == null || result is DBNull) ? 0M : Convert.ToDecimal(result);//(decimal)result;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error($"Failed to retrieve total {priceType} volume for {symbol}", e);
                }
            }

            return 0M;
        }
           
        public DateTime GetUtmostDayDate(string symbol, string dataFeed, bool earliest)
        {
            using (var aConnection = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT " + (earliest ? "MIN" : "MAX")
                    + "([Timestamp]) FROM [BarsDaily] WHERE [Symbol]=@symbol AND [DataFeed]=@dataFeed", aConnection);
                cmd.Parameters.AddWithValue("symbol", symbol);
                cmd.Parameters.AddWithValue("dataFeed", dataFeed);
                try
                {
                    aConnection.Open();
                    var result = cmd.ExecuteScalar();
                    return (result != null && !(result is DBNull)) ? (DateTime)result : DateTime.MinValue;
                }
                catch (Exception e)
                {
                    string side = earliest ? "earliest" : "latest";
                    Logger.Error("Failed to retrieve " + side + " day for EOD data", e);
                    return DateTime.MinValue;
                }
            }
        }

        public DateTime GetUtmostMinuteTimestamp(string symbol, string dataFeed, bool earliest)
        {
            using (var aConnection = new SqlConnection(_connectionString))
            {
                var tableName = earliest ? "BarsMinute_Archive" : "BarsMinute";
                var cmd = new SqlCommand("SELECT " + (earliest ? "MIN" : "MAX") + "([Timestamp]) FROM " + tableName
                    + " WHERE [Symbol]=@symbol AND [DataFeed]=@dataFeed", aConnection);
                cmd.Parameters.AddWithValue("symbol", symbol);
                cmd.Parameters.AddWithValue("dataFeed", dataFeed);

                try
                {
                    aConnection.Open();
                    var result = cmd.ExecuteScalar();
                    if (result != null && result is DateTime)
                    {
                        return (DateTime)result;
                    }
                    else if (earliest)
                    {
                        cmd = new SqlCommand("SELECT MIN([Timestamp]) FROM [BarsMinute] "
                            + "WHERE [Symbol]=@symbol AND [DataFeed]=@dataFeed", aConnection);
                        cmd.Parameters.AddWithValue("symbol", symbol);
                        cmd.Parameters.AddWithValue("dataFeed", dataFeed);
                        result = cmd.ExecuteScalar();
                        return (result != null && result is DateTime) ? (DateTime)result : DateTime.MinValue;
                    }

                }
                catch (Exception e)
                {
                    string side = earliest ? "earliest" : "latest";
                    Logger.Error("Failed to retrieve " + side + " minute timestamp for security", e);
                }
            }

            return DateTime.MinValue;
        }

        public List<Security> GetDatafeedSecurities(string dataFeed)
        {
            var result = new List<Security>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT * FROM [Securities] "
                    + "WHERE [DataFeed]=@dataFeed ORDER BY [Symbol]", connection);
                cmd.Parameters.AddWithValue("dataFeed", dataFeed);
                SqlTransaction transaction = null;
                try
                {
                    connection.Open();
                    transaction = connection.BeginTransaction();
                    cmd.Transaction = transaction;

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                            return result;

                        while (reader.Read())
                        {
                            result.Add(new Security
                            {
                                SecurityId = (int)reader["Id"],
                                Symbol = ((string)reader["Symbol"]).Trim(),
                                Name = ((string)reader["Name"]).Trim(),
                                DataFeed = ((string)reader["DataFeed"]).Trim(),
                                AssetClass = ((string)reader["AssetClass"]).Trim(),
                                Digit = (int)reader["Digit"],
                                PriceIncrement = (decimal)reader["PriceIncrement"],
                                BaseCurrency = ((string)reader["BaseCurrency"]).Trim(),
                                MarginRate = (decimal)reader["MarginRate"],
                                ContractSize = (decimal)reader["ContractSize"],
                                MaxPosition = (decimal)reader["MaxPosition"],
                                UnitPrice = (decimal)reader["UnitPrice"],
                                QtyIncrement = (decimal)reader["QtyIncrement"],
                                UnitOfMeasure = ((string)reader["UnitOfMeasure"]).Trim(),
                                MarketClose = (TimeSpan)reader["MarketClose"],
                                MarketOpen = (TimeSpan)reader["MarketOpen"]
                            });
                        }

                    }

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    Logger.Error($"Failed to retrieve {dataFeed} securities", e);
                    if (transaction != null)
                        transaction.Rollback();
                }
            }
            return result;
        }

        public int GetSecurityId(string symbol, string datafeed)
        {
            return AvailableSymbols
                .Where(i => i.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)
                    && i.DataFeed.Equals(datafeed, StringComparison.OrdinalIgnoreCase))
                .Select(i => i.SecurityId).FirstOrDefault();

            ////optional: try DB query
            //using (var connection = new SqlConnection(_connectionString))
            //{
            //    var cmd = new SqlCommand("SELECT [Id] FROM [Securities] WHERE [DataFeed]=@dataFeed AND [Symbol]=@symbol", connection);
            //    cmd.Parameters.AddWithValue("dataFeed", datafeed);
            //    cmd.Parameters.AddWithValue("symbol", symbol);

            //    try
            //    {
            //        connection.Open();
            //        return (int)cmd.ExecuteScalar();
            //    }
            //    catch (Exception e)
            //    {
            //        Logger.Error($"Failed to retrieve security ID for {symbol} from {datafeed}", e);
            //    }
            //}
            //return -1;
        }

        #endregion //Public Get Methods

        #region Private Methods

        private List<Security> GetSecurities()
        {
            List<Security> securities = new List<Security>();
            using (var aConnection = new SqlConnection(_connectionString))
            {
                SqlTransaction transaction = null;

                try
                {
                    aConnection.Open();
                    transaction = aConnection.BeginTransaction();
                    var cmd = new SqlCommand("SELECT * FROM [Securities]", aConnection, transaction);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                            return securities;

                        while (reader.Read())
                        {
                            securities.Add(new Security
                            {
                                SecurityId = (int)reader["Id"],
                                Symbol = ((string)reader["Symbol"]).Trim(),
                                Name = ((string)reader["Name"]).Trim(),
                                DataFeed = ((string)reader["DataFeed"]).Trim(),
                                AssetClass = ((string)reader["AssetClass"]).Trim(),
                                Digit = (int)reader["Digit"],
                                PriceIncrement = (decimal)reader["PriceIncrement"],
                                BaseCurrency = ((string)reader["BaseCurrency"]).Trim(),
                                MarginRate = (decimal)reader["MarginRate"],
                                ContractSize = (decimal)reader["ContractSize"],
                                MaxPosition = (decimal)reader["MaxPosition"],
                                UnitPrice = (decimal)reader["UnitPrice"],
                                QtyIncrement = (decimal)reader["QtyIncrement"],
                                UnitOfMeasure = ((string)reader["UnitOfMeasure"]).Trim(),
                                MarketClose = (TimeSpan)reader["MarketClose"],
                                MarketOpen = (TimeSpan)reader["MarketOpen"]
                            });
                        }
                    }

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to load securities", e);
                    if (transaction != null)
                        transaction.Rollback();
                }
            }

            return securities;
        }

        private bool? IsExistingTable(SqlConnection connection, string tableName)
        {
            try
            {
                var cmd = new SqlCommand("SELECT 1 FROM INFORMATION_SCHEMA.TABLES "
                    + $"WHERE TABLE_NAME = '{tableName}'", connection);
                return cmd.ExecuteScalar() != null;
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to check if '{tableName}' exists", e);
                return null;
            }
        }

        private static DateTime GetStartingDate(Timeframe tf, int barSize, int barCount)
        {
            switch (tf)
            {
                case Timeframe.Minute: return DateTime.UtcNow.AddMinutes(barSize * barCount);
                case Timeframe.Hour: return DateTime.UtcNow.AddHours(barSize * barCount);
                case Timeframe.Day: return DateTime.UtcNow.AddDays(barSize * barCount);
                case Timeframe.Month: return DateTime.UtcNow.AddMonths(barSize * barCount);
                default: throw new ArgumentException($"Timeframe parameter '{tf}' is not supported");
            }
        }

        #endregion //Private Methods
    }
}
