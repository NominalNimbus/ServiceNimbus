/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using CommonObjects;
using ServerCommonObjects;
using ServerCommonObjects.Interfaces;
using ServerCommonObjects.SQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Server
{
    internal class HistoryDataStoreCache : IDataCacheManager
    {
        #region Members and Events

        private readonly Dictionary<string, List<Tick>> _tickBySecurity;
        private readonly List<Tick> _tickForDb;
        private readonly Dictionary<string, Bar> _minuteBars;
        private readonly Dictionary<string, Bar> _dailyBars;

        private readonly Dictionary<string, Tuple<Dictionary<int, decimal>,DateTime>> _totalDailyBidVolume;
        private readonly Dictionary<string, Tuple<Dictionary<int, decimal>, DateTime>> _totalDailyAskVolume;

        private readonly Dictionary<string, bool> _minDataCached;
        private readonly Dictionary<string, bool> _dayDataCached;

        private readonly List<string> _manuallyRequestedMinData;
        private readonly List<string> _manuallyRequestedDayData;

        private readonly List<IDataFeed> _dataFeeds;
        private readonly DBHistory _dbHistory;
        private string _connectionString;
        private volatile bool _isRunning;

        private readonly Timer _minCacheRequestTimer;
        private readonly Timer _dayCacheRequestTimer;
        private readonly Timer _writeTicksToDB;

        public event EventHandler<EventArgs<Security>> NewMinBar;
        public event EventHandler<EventArgs<string, double>> CachedDataProgress;

        private readonly Queue<Tick> _tickMessages;

        #endregion

        #region Constructor

        public HistoryDataStoreCache()
        {
            _tickBySecurity = new Dictionary<string, List<Tick>>();
            _minuteBars = new Dictionary<string, Bar>();
            _dailyBars = new Dictionary<string, Bar>();
            _minDataCached = new Dictionary<string, bool>();
            _dayDataCached = new Dictionary<string, bool>();
            _dataFeeds = new List<IDataFeed>();
            _dbHistory = new DBHistory();
            _tickForDb = new List<Tick>();
            _totalDailyAskVolume = new Dictionary<string, Tuple<Dictionary<int, decimal>, DateTime>>();
            _totalDailyBidVolume = new Dictionary<string, Tuple<Dictionary<int, decimal>, DateTime>>();
            _manuallyRequestedMinData = new List<string>();
            _manuallyRequestedDayData = new List<string>();

            _minCacheRequestTimer = new Timer(2 * 60 * 1000);
            _dayCacheRequestTimer = new Timer(2 * 60 * 1000);
            _writeTicksToDB = new Timer(30 * 1000);

            _tickMessages = new Queue<Tick>();

            _minCacheRequestTimer.Elapsed += delegate
            {
                var items = _minDataCached.Where(p => !p.Value).Select(p => p.Key).ToList();
                lock (_minDataCached)
                {
                    foreach (var item in items)
                        _minDataCached.Remove(item);
                }
            };

            _dayCacheRequestTimer.Elapsed += delegate
            {
                var items = _dayDataCached.Where(p => !p.Value).Select(p => p.Key).ToList();
                lock (_dayDataCached)
                {
                    foreach (var item in items)
                        _dayDataCached.Remove(item);
                }
            };

            _writeTicksToDB.Elapsed += delegate
            {
                AddTicksToDB();
            };
        }

        #endregion

        #region IDataCacheManager Methods

        public void Start(IEnumerable<IDataFeed> dataFeeds, string connectionString)
        {
            _isRunning = true;
            _connectionString = connectionString;
            _dbHistory.Start(_connectionString);
            _dataFeeds.AddRange(dataFeeds);

            foreach (var dataFeedItem in _dataFeeds)
            {
                dataFeedItem.NewSecurity += security => _dbHistory.AddSecurity(security);

                foreach (var security in dataFeedItem.Securities)
                    _dbHistory.AddSecurity(security);
            }

            //foreach (var dataFeed in _dataFeeds.Where(f => f.IsStarted))
            foreach (var dataFeed in _dataFeeds)
            {
                dataFeed.NewTick += OnNewTick;

                foreach (var symbol in dataFeed.Securities)
                    dataFeed.Subscribe(symbol);

                //new Thread(() => FillDayCache(dataFeed)) { IsBackground = true }.Start();
                //new Thread(() => FillMinuteCache(dataFeed)) { IsBackground = true }.Start();
            }

            _writeTicksToDB.Start();
        }

        public void Stop()
        {
            _isRunning = false;
            foreach (var dataFeed in _dataFeeds)
            {
                if (dataFeed.IsStarted)
                {
                    foreach (var symbol in dataFeed.Securities)
                        dataFeed.UnSubscribe(symbol);
                    dataFeed.NewTick -= OnNewTick;
                }
            }

            lock (_minDataCached)
            {
                _manuallyRequestedMinData.Clear();
                _minDataCached.Clear();
            }

            lock (_dayDataCached)
            {
                _dayDataCached.Clear();
                _manuallyRequestedDayData.Clear();
            }

            lock (_tickBySecurity)
                _tickBySecurity.Clear();

            lock (_tickForDb)
                _tickForDb.Clear();

            lock (_minuteBars)
            {
                _minuteBars.Clear();
                _dailyBars.Clear();
            }

            lock (_totalDailyBidVolume)
                _totalDailyBidVolume.Clear();

            lock (_totalDailyAskVolume)
                _totalDailyAskVolume.Clear();

            _dataFeeds.Clear();

            _minCacheRequestTimer.Stop();
            _dayCacheRequestTimer.Stop();
            _writeTicksToDB.Stop();
            AddTicksToDB();
            _dbHistory.Stop();
        }

        public decimal GetTotalDailyAskVolume(string symbol, string dataFeed, int level)
        {
            var key = GetSymbolKey(symbol, dataFeed);
            lock (_totalDailyAskVolume)
            {
                return (_totalDailyAskVolume.ContainsKey(key) &&
                    _totalDailyAskVolume[key].Item1.ContainsKey(level))
                    ? _totalDailyAskVolume[key].Item1[level] : 0M;
            }
        }

        public decimal GetTotalDailyBidVolume(string symbol, string dataFeed, int level)
        {
            var key = GetSymbolKey(symbol, dataFeed);
            lock (_totalDailyBidVolume)
            {
                return (_totalDailyBidVolume.ContainsKey(key) &&
                    _totalDailyBidVolume[key].Item1.ContainsKey(level))
                    ? _totalDailyBidVolume[key].Item1[level] : 0M;
            }
        }

        public void GetHistory(Selection sel, IDataFeed dataFeed, HistoryAnswerHandler callback)
        {
            var p = (Selection)sel.Clone();
            
            var requireGetLatestHistory = true;
            var key = GetSymbolKey(sel.Symbol, sel.DataFeed);
            if (p.From == DateTime.MinValue) //use bar count (and end date if specified)
            {
                p.To = p.To.Year > 1970 ? p.To : DateTime.MaxValue;
                switch (p.Timeframe)
                {
                    case Timeframe.Tick:
                        p.BarCount = p.BarCount * p.TimeFactor + 50;
                        break;
                    case Timeframe.Minute:
                        p.BarCount = p.BarCount * p.TimeFactor + 60;
                        requireGetLatestHistory = !_minuteBars.ContainsKey(key);
                        break;
                    case Timeframe.Hour:
                        p.BarCount = p.BarCount * p.TimeFactor * 60 + 8 * 60;
                        requireGetLatestHistory = !_minuteBars.ContainsKey(key);
                        break;
                    case Timeframe.Day:
                        p.BarCount = p.BarCount * p.TimeFactor + 7;
                        requireGetLatestHistory = !_dailyBars.ContainsKey(key);
                        break;
                    case Timeframe.Month:
                        p.BarCount = p.BarCount * p.TimeFactor * 31 + 31;
                        requireGetLatestHistory = !_dailyBars.ContainsKey(key);
                        break;
                    default:
                        System.Diagnostics.Debug.Assert(false);
                        break;
                }
            }

            var expectedFromDate = (p.Timeframe == Timeframe.Day || p.Timeframe == Timeframe.Month) ?
                DateTime.UtcNow.AddDays(-p.BarCount) : DateTime.UtcNow.AddMinutes(-p.BarCount);
            p.TimeFactor = 1;
            var cache = GetHistoryFromDB(p);
            if(cache?.Count > 0 && cache.First().Date > cache.Last().Date)
            {
                cache.Reverse();
            }
            
            if (!requireGetLatestHistory && cache != null && p.IsEnoughData(cache))
            {
                cache = CommonHelper.AdjustRawBars(sel.Timeframe, sel.TimeFactor, cache);
                callback(sel, cache);
                return;
            }

            //
            var lastDateFromDB = GetUtmostStoreDate(p, false); 
            var firstDateFromDB = GetUtmostStoreDate(p, true);
            if (lastDateFromDB != DateTime.MinValue &&
                (p.From == DateTime.MinValue || p.From > lastDateFromDB) &&
                (firstDateFromDB != DateTime.MinValue && firstDateFromDB < expectedFromDate))

            {
                p.From = lastDateFromDB;
            }
            //

            RequestHistoryFromFeed(p, dataFeed, (@params, data) =>
            {
                if (data != null && data.Count > 1)
                {
                    StoreToDB(p, data);
                }
                var combineBars = CommonHelper.CombineBars(data, cache);
                var compressedBars = CommonHelper.CompressBars(combineBars, sel);//convert to timeframe
                callback(sel, compressedBars);

            });

        }
     
        public Tick GetTick(string symbol, string dataFeed, DateTime timestamp) =>
            _dbHistory.GetTick(symbol, dataFeed, timestamp);

        public List<Security> GetDatafeedSecurities(string name) =>
            _dbHistory.GetDatafeedSecurities(name);


        #endregion IDataCacheManager Methods

        #region Private history members

        private void StoreToDB(Selection sel, List<Bar> bars)
        {
            if (bars.Count == 0)
                return;

            var key = GetSymbolKey(sel.Symbol, sel.DataFeed);

            switch (sel.Timeframe)
            {
                case Timeframe.Minute:
                case Timeframe.Hour:
                    _dbHistory.AddMinRecordsToDb(bars, sel.Symbol, sel.DataFeed);
                    if (!_minuteBars.ContainsKey(key))
                    {
                        _minuteBars[key] = bars.Last();
                    }
                    break;
                case Timeframe.Day:
                case Timeframe.Month:
                    _dbHistory.AddDayRecordsToDb(bars, sel.Symbol, sel.DataFeed);
                    if (!_dailyBars.ContainsKey(key))
                    {
                        _dailyBars[key] = bars.Last();
                    }
                    break;
            }
        }

        private DateTime GetUtmostStoreDate(Selection p, bool earliest = false)
        {
            if (p.Level == 0)
            {
                switch (p.Timeframe)
                {
                    case Timeframe.Tick:
                        //need to implement later
                        break;
                    case Timeframe.Minute:
                    case Timeframe.Hour:
                        return _dbHistory.GetUtmostMinuteTimestamp(p.Symbol, p.DataFeed, earliest);
                    case Timeframe.Day:
                    case Timeframe.Month:
                        return _dbHistory.GetUtmostDayDate(p.Symbol, p.DataFeed, earliest);
                }
            }

            return DateTime.MinValue;
        }

        private List<Bar> GetHistoryFromDB(Selection p)
        {
            switch (p.Timeframe)
            {
                case Timeframe.Tick:
                    return GetTickHistory(p);
                case Timeframe.Minute:
                case Timeframe.Hour:
                    return p.Level < 1 ? GetMinHistory(p) : GetMinHistoryL2(p);
                case Timeframe.Day:
                case Timeframe.Month:
                    return p.Level < 1 ? GetDayHistory(p) : GetDayHistoryL2(p);
                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }
            return new List<Bar>();
        }

        private List<Bar> GetTickHistory(Selection parameters) =>
            _dbHistory.GetTickHistory(parameters);

        private List<Bar> GetMinHistory(Selection parameters)
        {
            var res = _dbHistory.GetMinHistory(parameters);
            var key = GetSymbolKey(parameters.Symbol, parameters.DataFeed);
            if (parameters.To == DateTime.MinValue || parameters.To >= DateTime.UtcNow.AddMinutes(-5))
            {
                lock (_minuteBars)
                {
                    if (_minuteBars.ContainsKey(key))
                        res.Insert(0, _minuteBars[key]);
                }
            }

            return res;
        }

        private List<Bar> GetDayHistory(Selection parameters)
        {
            var res = _dbHistory.GetDayHistory(parameters);
            var key = GetSymbolKey(parameters.Symbol, parameters.DataFeed);
            lock (_dailyBars)
            {
                if (_dailyBars.ContainsKey(key))
                    res.Insert(0, _dailyBars[key]);
            }

            return res;
        }

        private List<Bar> GetMinHistoryL2(Selection parameters) =>
            _dbHistory.GetMinHistoryL2(parameters);

        private List<Bar> GetDayHistoryL2(Selection parameters) =>
            _dbHistory.GetDayHistoryL2(parameters);

        private void RequestHistoryFromFeed(Selection parameters, IDataFeed dataFeed, HistoryAnswerHandler callback)
        {
            if (parameters.Timeframe < Timeframe.Minute)
                callback(parameters, new List<Bar>(0));
            else
                dataFeed.GetHistory(parameters, callback);
        }

        #endregion //Private history members

        #region Private Methods

        private void ProcessTick(Tick tick)
        {
            lock (_tickForDb)
                _tickForDb.Add(tick);

            var key = GetSymbolKey(tick.Symbol.Symbol, tick.Symbol.DataFeed);
            if (tick.Level2.Count > 0)
            {
                ProccessTickLevel2Volume(key, tick, PriceType.Bid);
                ProccessTickLevel2Volume(key, tick, PriceType.Ask);
            }

            lock (_tickBySecurity)
            {
                if (!_tickBySecurity.TryGetValue(key, out var ticks))
                {
                    ticks = new List<Tick>();
                    _tickBySecurity.Add(key, ticks);
                }

                ticks.Add(tick);

                if (ticks.Count > 20)
                {
                    lock (_minuteBars)
                        CreateBarCacheFromTicks(tick.Symbol.Symbol, tick.Symbol.DataFeed);
                }
            }
        }

        private void ProccessTickLevel2Volume(string key, Tick tick, PriceType priceType)
        {
            var volumeList = priceType == PriceType.Ask ? _totalDailyAskVolume : _totalDailyBidVolume;
            lock (volumeList)
            {
                if (!volumeList.TryGetValue(key, out var dailyBidVolume) || dailyBidVolume.Item2 != tick.Date.Date)
                {
                    dailyBidVolume = new Tuple<Dictionary<int, decimal>, DateTime>(new Dictionary<int, decimal>(), tick.Date.Date);
                    var existingVolume = _dbHistory.GetDailyTotalVolume(tick.Symbol.Symbol, tick.Symbol.DataFeed, tick.Level2.Select(t => t.DomLevel), priceType);
                    foreach (var level2 in tick.Level2)
                    {
                        dailyBidVolume.Item1[level2.DomLevel] = (existingVolume.TryGetValue(level2.DomLevel, out var volume) ? volume : 0) +
                            (priceType == PriceType.Ask ? level2.AskSize : level2.BidSize);
                    }
                    volumeList[key] = dailyBidVolume;
                }
                else
                {
                    foreach (var level2 in tick.Level2)
                    {
                        if (!dailyBidVolume.Item1.ContainsKey(level2.DomLevel))
                        {
                            dailyBidVolume.Item1[level2.DomLevel] = _dbHistory.GetDailyTotalVolume(tick.Symbol.Symbol, tick.Symbol.DataFeed, (byte)level2.DomLevel, priceType);
                        }
                        dailyBidVolume.Item1[level2.DomLevel] += priceType == PriceType.Ask ? level2.AskSize : level2.BidSize;
                    }
                }
            }
        }

        private void CreateBarCacheFromTicks(string symbol, string dataFeed)
        {
            var key = GetSymbolKey(symbol, dataFeed);
            if (!_minuteBars.ContainsKey(key) && !_dailyBars.ContainsKey(key))
            {
                lock (_tickBySecurity)
                {
                    _tickBySecurity[key].Clear();
                }
                return;
            }

            List<Tick> tmpList;
            lock (_tickBySecurity)
            {
                tmpList = _tickBySecurity[key].ToList();
                _tickBySecurity[key].Clear();
            }

            if (_minuteBars.ContainsKey(key))
            {
                foreach (var item in tmpList)
                {
                    if (IsNextBar(_minuteBars[key].Date, item.Date, Timeframe.Minute))
                    {
                        _dbHistory.AddMinRecordsToDb(new List<Bar> { new Bar(_minuteBars[key]) }, symbol, dataFeed);
                        _minuteBars[key] = new Bar(item, CommonHelper.GetTimeRoundToMinute(item.Date));

                        var security = _dbHistory.AvailableSymbols
                            .FirstOrDefault(i => i.Symbol == symbol && i.DataFeed == dataFeed);
                        if (security != null)
                            NewMinBar?.Invoke(this, new EventArgs<Security>(security));
                    }
                    else
                    {
                        _minuteBars[key].AppendTick(item.Bid, item.Ask, item.BidSize, item.AskSize);
                    }
                }
            }

            if (_dailyBars.ContainsKey(key))
            {
                foreach (var item in tmpList)
                {
                    if (IsNextBar(_dailyBars[key].Date, item.Date, Timeframe.Day))
                    {
                        _dbHistory.AddDayRecordsToDb(new List<Bar> { new Bar(_dailyBars[key]) }, symbol, dataFeed);
                        _dailyBars[key] = new Bar(item, item.Date.Date);
                    }
                    else
                    {
                        _dailyBars[key].AppendTick(item.Bid, item.Ask, item.BidSize, item.AskSize);
                    }
                }
            }
        }

        private void CheckMinHistoryCache(Tick tick, string dataFeedName)
        {
            var dataFeed = _dataFeeds.FirstOrDefault(p => p.Name.Equals(dataFeedName));
            if (dataFeed == null)
                return;

            var key = GetSymbolKey(tick.Symbol.Symbol, tick.Symbol.DataFeed);
            var startTime = _manuallyRequestedMinData.Contains(key) ? DateTime.MinValue
                : _dbHistory.GetUtmostMinuteTimestamp(tick.Symbol.Symbol, tick.Symbol.DataFeed, true);
            RequestMinHistory(tick.Symbol, dataFeed, startTime);
        }

        private void CheckDayHistoryCache(Tick tick, string dataFeedName)
        {
            var dataFeed = _dataFeeds.FirstOrDefault(p => p.Name.Equals(dataFeedName));
            if (dataFeed == null)
                return;

            var key = GetSymbolKey(tick.Symbol.Symbol, tick.Symbol.DataFeed);
            var startDate = _manuallyRequestedDayData.Contains(key) ? DateTime.MinValue
                : _dbHistory.GetUtmostDayDate(tick.Symbol.Symbol, tick.Symbol.DataFeed, true);
            RequestDayHistory(tick.Symbol, dataFeed, startDate);
        }

        private void FillMinuteCache(IDataFeed feed)
        {
            Parallel.ForEach(feed.Securities, (item, state) =>
            {
                if (!_isRunning)
                {
                    if (!state.IsStopped)
                        state.Stop();
                    return;
                }

                var key = GetSymbolKey(item.Symbol, item.DataFeed);
                lock (_minDataCached)
                    _minDataCached[key] = false;

                RequestMinHistory(item, feed, _dbHistory.GetUtmostMinuteTimestamp(item.Symbol, item.DataFeed, true));
            });
        }

        private void FillDayCache(IDataFeed feed)
        {
            Parallel.ForEach(feed.Securities, (item, state) =>
            {
                if (!_isRunning)
                {
                    if (!state.IsStopped)
                        state.Stop();
                    return;
                }

                var key = GetSymbolKey(item.Symbol, item.DataFeed);
                lock (_dayDataCached)
                    _dayDataCached[key] = false;

                RequestDayHistory(item, feed, _dbHistory.GetUtmostDayDate(item.Symbol, item.DataFeed, true));
            });
        }

        private void RequestMinHistory(Security security, IDataFeed dataFeed, DateTime startRequestDate)
        {
            _minCacheRequestTimer.Start();

            var defaultStartTime = DateTime.UtcNow.Date.AddDays(-7);
            if (startRequestDate < defaultStartTime)  //optional: limit requested data range
                startRequestDate = defaultStartTime;

            var endDate = DateTime.MaxValue;
            var key = GetSymbolKey(security.Symbol, security.DataFeed);
            lock (_manuallyRequestedMinData)
            {
                if (_manuallyRequestedMinData.Contains(key))
                    endDate = _dbHistory.GetUtmostMinuteTimestamp(security.Symbol, security.DataFeed, false);

                if (endDate <= startRequestDate)
                {
                    lock (_minDataCached)
                        _minDataCached[key] = true;

                    _minCacheRequestTimer.Stop();

                    if (_manuallyRequestedMinData.Contains(key))
                        _manuallyRequestedMinData.Remove(key);

                    if (CachedDataProgress != null)
                    {
                        double count = _dataFeeds.Where(i => i.IsStarted).Sum(df => df.Securities.Count);
                        var progress = (_minDataCached.Count(i => i.Value) / count) * 100.0;
                        CachedDataProgress(this, new EventArgs<string, double>("minute bars", progress));
                    }
                    return;
                }
            }

            dataFeed.GetHistory(new Selection
            {
                From = startRequestDate,
                To = endDate,
                Timeframe = Timeframe.Minute,
                BarCount = Int32.MaxValue,
                Symbol = security.Symbol,
                DataFeed = security.DataFeed
            }, DataFeedMinHistoryReceived);
        }

        private void RequestDayHistory(Security security, IDataFeed dataFeed, DateTime startRequestDate)
        {
            _dayCacheRequestTimer.Start();

            var defaultStartDate = DateTime.UtcNow.Date.AddYears(-15);
            if (startRequestDate < defaultStartDate)  //optional: limit requested data range
                startRequestDate = defaultStartDate;

            var endDate = DateTime.MaxValue;
            var key = GetSymbolKey(security.Symbol, security.DataFeed);
            lock (_manuallyRequestedDayData)
            {
                if (_manuallyRequestedDayData.Contains(key))
                    endDate = _dbHistory.GetUtmostDayDate(security.Symbol, security.DataFeed, false);

                if (endDate <= startRequestDate)
                {
                    lock (_dayDataCached)
                        _dayDataCached[key] = true;

                    _dayCacheRequestTimer.Stop();

                    if (_manuallyRequestedDayData.Contains(key))
                        _manuallyRequestedDayData.Remove(key);

                    if (CachedDataProgress != null)
                    {
                        double count = _dataFeeds.Where(i => i.IsStarted).Sum(df => df.Securities.Count);
                        var progress = (_dayDataCached.Count(i => i.Value) / count) * 100.0;
                        CachedDataProgress(this, new EventArgs<string, double>("EOD bars", progress));
                    }

                    return;
                }
            }

            dataFeed.GetHistory(new Selection
            {
                From = startRequestDate,
                To = endDate,
                Timeframe = Timeframe.Day,
                BarCount = Int32.MaxValue,
                Symbol = security.Symbol,
                DataFeed = security.DataFeed
            }, DataFeedDayHistoryReceived);
        }

        private bool _isWritingToDB = false;

        private void AddTicksToDB()
        {
            if (_isWritingToDB)
                return;

            _isWritingToDB = true;
            List<Tick> tmp;

            lock (_tickForDb)
            {
                tmp = _tickForDb.ToList();
                _tickForDb.Clear();
            }

            _dbHistory.AddTicksToDb(tmp);
            _isWritingToDB = false;
        }

        private bool IsNextBar(DateTime prevTime, DateTime currentTime, Timeframe periodicity)
        {
            if (periodicity == Timeframe.Minute)
            {
                if (currentTime.Minute > prevTime.Minute)
                    return true;
                if (prevTime.Minute == 59 && currentTime.Minute == 0)
                    return true;
                if ((prevTime - currentTime) > new TimeSpan(0, 1, 0))
                    return true;

                return false;
            }
            if (periodicity == Timeframe.Day)
            {
                if (currentTime.Day > prevTime.Day)
                    return true;
                if (prevTime.Day == 6 && currentTime.Day == 0)
                    return true;
                if ((prevTime - currentTime) > new TimeSpan(1, 0, 0, 0))
                    return true;

                return false;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetSymbolKey(string symbol, string dataFeed) =>
            symbol + "@" + dataFeed;

        #endregion

        #region Events and Callbacks

        private void OnNewTick(Tick tick)
        {
            if (_isRunning)
            {
                lock (_tickMessages)
                {
                    _tickMessages.Enqueue(tick);
                    if (_tickMessages.Count == 1)
                        Task.Run(() => TickHandler());
                }
            }
        }

        private void TickHandler()
        {
            while (true)
            {
                var tick = default(Tick);
                lock (_tickMessages)
                {
                    if (_tickMessages.Count == 0)
                        break;

                    tick = _tickMessages.Peek();
                }

                ProcessTick(tick);

                lock (_tickMessages)
                {
                    _tickMessages.Dequeue();
                    if (_tickMessages.Count == 0)
                        break;
                }
            }
        }

        private void DataFeedMinHistoryReceived(Selection parameters, List<Bar> bars)
        {
            try { _dbHistory.AddMinRecordsToDb(bars, parameters.Symbol, parameters.DataFeed); }
            catch (Exception ex) { Logger.Error("Failed to flush minute data to DB", ex); }

            var key = GetSymbolKey(parameters.Symbol, parameters.DataFeed);
            lock (_minDataCached)
            {
                _minDataCached[key] = true;
                _minCacheRequestTimer.Stop();

                if (_manuallyRequestedMinData.Contains(key))
                    _manuallyRequestedMinData.Remove(key);

                if (CachedDataProgress != null)
                {
                    double count = _dataFeeds.Where(i => i.IsStarted).Sum(df => df.Securities.Count);
                    var progress = (_minDataCached.Count(p => p.Value) / count) * 100.0;
                    CachedDataProgress(this, new EventArgs<string, double>("minute bars", progress));
                }
            }
        }

        private void DataFeedDayHistoryReceived(Selection parameters, List<Bar> bars)
        {
            try { _dbHistory.AddDayRecordsToDb(bars, parameters.Symbol, parameters.DataFeed); }
            catch (Exception ex) { Logger.Error("Failed to flush EOD data to DB", ex); }

            var key = GetSymbolKey(parameters.Symbol, parameters.DataFeed);
            lock (_dayDataCached)
            {
                _dayDataCached[key] = true;
                _dayCacheRequestTimer.Stop();

                if (_manuallyRequestedDayData.Contains(key))
                    _manuallyRequestedDayData.Remove(key);

                if (CachedDataProgress != null)
                {
                    double count = _dataFeeds.SelectMany(s => s.Securities).Count();//_dataFeeds.Where(i => i.IsStarted).Sum(df => df.Securities.Count);
                    var progress = (_dayDataCached.Count(p => p.Value) / count) * 100.0;
                    CachedDataProgress(this, new EventArgs<string, double>("EOD bars", progress));
                }
            }
        }

        #endregion

    }
}
