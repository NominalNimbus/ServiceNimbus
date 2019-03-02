/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using Com.Lmax.Api;
using Com.Lmax.Api.MarketData;
using Com.Lmax.Api.OrderBook;
using CommonObjects;
using ServerCommonObjects;
using ServerCommonObjects.Classes;
using ServerCommonObjects.Interfaces;
using Instrument = Com.Lmax.Api.OrderBook.Instrument;

namespace LmaxDataFeed
{
    public class LMAXDataFeed : IDataFeed
    {

        #region Private members

        private readonly ProductType _productType;

        private ISession _session;
        private DateTime _prevSessionBreak;
        private LmaxApi _api;
        private int _id;
        private readonly Dictionary<int, Security> _securities;
        private readonly Dictionary<int, int> _subscribedSymbols;
        private readonly Dictionary<int, HistoryAnswerHandler> _historyRequestHandlers;
        private readonly Dictionary<int, Selection> _originalHistoryRequestParameters;
        private readonly ConcurrentDictionary<int, List<Bar>> _bidAskCache;
        
        private string Username { get; }
        private string Password { get; }
        private string Url { get; }

        #endregion

        #region Public members

        public string Name => "LMAX";

        public int BalanceDecimals => 2;

        private TimeZoneInfo TimeZoneInfo { get; }
        public List<Security> Securities { get; set; }
        public bool IsStarted { get; private set; }

        public event NewTickHandler NewTick;
        public event NewSecurityHandler NewSecurity;

        #endregion

        #region Constructor

        public LMAXDataFeed()
        {
            var config = new LocalConfigHelper(Assembly.GetExecutingAssembly().Location, "Lmax Datafeed");

            Username = config.GetString(nameof(Username));
            Password = config.GetString(nameof(Password));
            Url = config.GetString(nameof(Url));

            TimeZoneInfo = TimeZoneInfo.Utc;
            Securities = new List<Security>();

            _productType = ProductType.CFD_LIVE;

            _securities = new Dictionary<int, Security>();
            _subscribedSymbols = new Dictionary<int, int>();
            _historyRequestHandlers = new Dictionary<int, HistoryAnswerHandler>();
            _originalHistoryRequestParameters = new Dictionary<int, Selection>();
            _bidAskCache = new ConcurrentDictionary<int, List<Bar>>();
        }

        #endregion

        #region IDataFeed implementation

        public void Start()
        {
            if (_api == null)
                _api = new LmaxApi(Url);

            _api.Login(new Com.Lmax.Api.LoginRequest(Username, Password, _productType), LoginCallback, FailureLoginCallback);
        }

        public void Stop()
        {
            IsStarted = false;
            _session.MarketDataChanged -= MarketDataUpdate;
            _session.HistoricMarketDataReceived -= SessionOnHistoricMarketDataReceived;
            _session.EventStreamSessionDisconnected -= NotifySessionDisconnect;
            _session.Logout(() => { }, FailureCallback);

            lock (Securities)
            {
                Securities.Clear();
                _securities.Clear();
            }

            lock (_originalHistoryRequestParameters)
                _originalHistoryRequestParameters.Clear();

            lock (_historyRequestHandlers)
                _historyRequestHandlers.Clear();

            lock (_subscribedSymbols)
                _subscribedSymbols.Clear();

            lock (_bidAskCache)
                _bidAskCache.Clear();

            try
            {
                _session.Stop();
                _session = null;
            }
            catch (Exception ex)
            {
                Logger.Error("LMAX stop exception.", ex);
            }
        }

        public void Subscribe(Security security)
        {
            if (_session == null)
                throw new ApplicationException($"Can't subscribe to symbol: {Name} feed is not connected");

            _securities.TryGetValue(security.SecurityId, out var instrument);
            if (instrument == null)
            {
                Logger.Warning("Invalid symbol passed for subscription over "
                    + $"{Name} feed: {security.Symbol}, #{security.SecurityId}");
                return;
            }

            lock (_subscribedSymbols)
            {
                if (!_subscribedSymbols.ContainsKey(instrument.SecurityId))
                {
                    _subscribedSymbols.Add(instrument.SecurityId, 1);
                    _session.Subscribe(new OrderBookSubscriptionRequest(instrument.SecurityId), () => { }, FailureCallback);
                }
                else
                {
                    _subscribedSymbols[instrument.SecurityId]++;
                }
            }
        }

        public void UnSubscribe(Security security)
        {
            if (_session == null)
                return; //TODO find out
                        //throw new ApplicationException($"Can't unsubscribe from symbol. {Name} data feed is not connected.");

            _securities.TryGetValue(security.SecurityId, out var instrument);
            if (instrument == null)
            {
                Logger.Warning($"Invalid symbol passed for unsubscription over {Name} feed: {security.Symbol}, #{security.SecurityId}");
                return;
            }

            lock (_subscribedSymbols)
            {
                if (_subscribedSymbols.ContainsKey(instrument.SecurityId))
                {
                    if (_subscribedSymbols[instrument.SecurityId] <= 1)
                        _subscribedSymbols.Remove(instrument.SecurityId);
                    else
                        _subscribedSymbols[instrument.SecurityId]--;
                }
            }
        }

        public void GetHistory(Selection parameters, HistoryAnswerHandler callback)
        {
            if (_session == null)
                throw new ApplicationException("Can't load history. LMAX data feed is not connected.");

            var symbol = parameters.Symbol.ToUpper();
            var instrument = Securities.FirstOrDefault(i => i.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
            if (instrument == null)
            {
                Logger.Warning($"Invalid symbol {parameters.Symbol} passed for history request over {Name} feed");
                return;
            }

            lock (_historyRequestHandlers)
            {
                if (parameters.To == DateTime.MinValue || parameters.To > DateTime.UtcNow)
                    parameters.To = DateTime.UtcNow;

                //calculate start time
                if (parameters.From == DateTime.MinValue && parameters.BarCount != int.MaxValue)
                {
                    if (parameters.BarCount < 3)
                    {
                        callback(parameters, new List<Bar>());
                        return;
                    }

                    if (parameters.To > DateTime.UtcNow)
                        parameters.To = DateTime.UtcNow;

                    if (parameters.Timeframe == Timeframe.Minute)
                        parameters.From = parameters.To.AddMinutes(-1 * 3 * parameters.BarCount * parameters.TimeFactor);
                    else if (parameters.Timeframe == Timeframe.Hour)
                        parameters.From = parameters.To.AddHours(-1 * 3 * parameters.BarCount * parameters.TimeFactor);
                    else if (parameters.Timeframe == Timeframe.Day)
                        parameters.From = parameters.To.AddDays(-1 * 2 * parameters.BarCount * parameters.TimeFactor);
                    else if (parameters.Timeframe == Timeframe.Month)
                        parameters.From = parameters.To.AddDays(-1 * parameters.BarCount * parameters.TimeFactor * 31);
                }

                parameters.From = TimeZoneInfo.ConvertTimeFromUtc(parameters.From, TimeZoneInfo);
                parameters.To = TimeZoneInfo.ConvertTimeFromUtc(parameters.To, TimeZoneInfo);

                var bidParams = (Selection)parameters.Clone();
                bidParams.BidAsk = PriceType.Bid;
                var id = ++_id;
                _historyRequestHandlers.Add(id, callback);
                _originalHistoryRequestParameters.Add(id, bidParams);

                _session?.RequestHistoricMarketData(new AggregateHistoricMarketDataRequest(id, instrument.SecurityId, bidParams.From, bidParams.To,
                        FromPeriodToResolution(bidParams.Timeframe), Format.Csv, Option.Bid), () => { }, FailureCallback);

                var askParams = (Selection)parameters.Clone();
                askParams.BidAsk = PriceType.Ask;
                id = ++_id;
                _historyRequestHandlers.Add(id, callback);
                _originalHistoryRequestParameters.Add(id, askParams);

                _session?.RequestHistoricMarketData(new AggregateHistoricMarketDataRequest(id, instrument.SecurityId, askParams.From, askParams.To,
                        FromPeriodToResolution(askParams.Timeframe), Format.Csv, Option.Ask), () => { }, FailureCallback);

                //
                /*var askHistParams = (Selection)parameters.Clone();
                askHistParams.BidAsk = PriceType.Unspecified;
                id = ++_id;
                _historyRequestHandlers.Add(id, callback);
                _originalHistoryRequestParameters.Add(id, askHistParams);

                _session?.RequestHistoricMarketData(new TopOfBookHistoricMarketDataRequest(id, instrument.SecurityId, askParams.From, askParams.To, Format.Csv),
                    () => { }, FailureCallback);*/
            }
        }

        #endregion

        #region Private Methods

        private void FailureCallback(FailureResponse response)
        {
            if (!IsStarted)
                return;

            Logger.Warning($"Failure Callback. Message: {response.Message}; Description: {response.Description}", response.Exception);

            if (response.Message.Contains("(403) Forbidden"))
            {
                if ((DateTime.Now - _prevSessionBreak).TotalMinutes >= 1)
                {
                    _prevSessionBreak = DateTime.Now;
                    NotifySessionDisconnect();
                }
            }
        }

        private void LoginCallback(ISession session)
        {
            _session = session;
            _session.MarketDataChanged += MarketDataUpdate;
            _session.HistoricMarketDataReceived += SessionOnHistoricMarketDataReceived;
            _session.EventStreamSessionDisconnected += NotifySessionDisconnect;

            _session.SearchInstruments(new SearchRequest(""), SearchCallback, FailureCallback);
            _session.Subscribe(new HistoricMarketDataSubscriptionRequest(), () => { }, FailureCallback);

            ThreadPool.QueueUserWorkItem(p =>
            {
                try
                {
                    IsStarted = true;
                    _session.Start();
                }
                catch (Exception ex)
                {
                    Logger.Error("LMAX start exception.", ex);
                }
            });
        }

        private void ReLoginCallback(ISession session)
        {
            _session = session;
            _session.MarketDataChanged += MarketDataUpdate;
            _session.HistoricMarketDataReceived += SessionOnHistoricMarketDataReceived;
            _session.EventStreamSessionDisconnected += NotifySessionDisconnect;

            _session.Subscribe(new HistoricMarketDataSubscriptionRequest(), () => { }, FailureCallback);

            ThreadPool.QueueUserWorkItem(o =>
            {
                Thread.Sleep(1000 * 10);

                lock (_subscribedSymbols)
                {
                    foreach (var item in _subscribedSymbols)
                        _session.Subscribe(new OrderBookSubscriptionRequest(item.Key), () => { }, FailureCallback);
                }
                lock (Securities)
                {
                    foreach (var security in Securities)
                    {
                        _session.Subscribe(new OrderBookSubscriptionRequest(security.SecurityId),
                            () => { }, FailureCallback);
                        _session.Subscribe(new OrderBookStatusSubscriptionRequest(security.SecurityId),
                            () => { }, FailureCallback);
                    }
                }
                lock (_originalHistoryRequestParameters)
                {
                    var tmpRequests = _originalHistoryRequestParameters.ToDictionary(p => p.Key, p => p.Value);
                    var tmpHandlers = _historyRequestHandlers.ToDictionary(p => p.Key, p => p.Value);

                    _originalHistoryRequestParameters.Clear();
                    _historyRequestHandlers.Clear();
                    _bidAskCache.Clear();

                    foreach (var item in tmpRequests)
                    {
                        if (!tmpHandlers.ContainsKey(item.Key))
                            continue;

                        GetHistory(item.Value, tmpHandlers[item.Key]);
                    }
                }
            });

            ThreadPool.QueueUserWorkItem(p =>
            {
                try
                {
                    IsStarted = true;
                    _session.Start();
                }
                catch (Exception ex)
                {
                    Logger.Error($"{Name} data feed start failure", ex);
                }
            });
        }

        private void NotifySessionDisconnect()
        {
            IsStarted = false;

            if (_session != null)
            {
                _session.MarketDataChanged -= MarketDataUpdate;
                _session.HistoricMarketDataReceived -= SessionOnHistoricMarketDataReceived;
                _session.EventStreamSessionDisconnected -= NotifySessionDisconnect;
                _session.Logout(() => { }, FailureCallback);

                try
                {
                    _session.Stop();
                }
                catch (Exception ex)
                {
                    Logger.Error("LMAX stop exception", ex);
                }

                _session = null;
            }

            Logger.Error("Reconnecting LMAX feed...");
            Reconnect();
        }

        private void Reconnect()
        {
            if (_api == null)
                _api = new LmaxApi(Url);

            _api.Login(new Com.Lmax.Api.LoginRequest(Username, Password, _productType), ReLoginCallback, ReconnectFailureCallback);
        }

        private void ReconnectFailureCallback(FailureResponse response)
        {
            Logger.Warning($"Broker Reconnect Failure Callback. Message: {response.Message}; Description: {response.Description}", response.Exception);

            ThreadPool.QueueUserWorkItem(a =>
            {
                Thread.Sleep(1000 * 15);
                Reconnect();
            });
        }

        private void SearchCallback(List<Instrument> instruments, bool hasMoreResults)
        {
            var lastAddedInstrumentId = 0;
            lock (Securities)
            {
                foreach (var instrument in instruments)
                {
                    if (_securities.ContainsKey((int)instrument.Id) || instrument.Name.Contains("weekend"))
                        continue;

                    var security = new Security
                    {
                        DataFeed = Name,
                        Symbol = instrument.Underlying.Symbol,
                        Name = instrument.Name,
                        SecurityId = (int)instrument.Id,
                        AssetClass = instrument.Underlying.AssetClass,
                        BaseCurrency = instrument.Contract.Currency,
                        UnitOfMeasure = instrument.Contract.UnitOfMeasure,
                        ContractSize = instrument.Contract.ContractSize,
                        Digit = Math.Max(instrument.OrderBook.PriceIncrement.ToString().Length - 2, 1),
                        PriceIncrement = instrument.OrderBook.PriceIncrement,
                        MarginRate = instrument.Risk.MarginRate,
                        MaxPosition = instrument.Risk.MaximumPosition,
                        UnitPrice = instrument.Contract.UnitPrice,
                        QtyIncrement = instrument.OrderBook.QuantityIncrement,
                        MarketOpen = instrument.Calendar.Open,
                        MarketClose = instrument.Calendar.Close
                    };

                    Securities.Add(security);
                    NewSecurity?.Invoke(security);
                    _securities[security.SecurityId] = security;
                    Subscribe(security);
                    lastAddedInstrumentId = security.SecurityId;

                    _session.Subscribe(new OrderBookSubscriptionRequest(instrument.Id), () => { }, FailureCallback);
                    _session.Subscribe(new OrderBookStatusSubscriptionRequest(instrument.Id), () => { }, FailureCallback);
                }
            }

            if (hasMoreResults && lastAddedInstrumentId > 0)
                _session.SearchInstruments(new SearchRequest("", lastAddedInstrumentId), SearchCallback, FailureCallback);
        }

        private void SessionOnHistoricMarketDataReceived(string idString, List<Uri> uris)
        {
            if (!int.TryParse(idString, out var id))
                throw new ArgumentException($"{Name} feed: invalid historical data id ({idString})");

            lock (_historyRequestHandlers)
            {
                if (!_historyRequestHandlers.ContainsKey(id))
                {
                    Logger.Warning($"{Name} history request callback error: no entry with ID #{id}");
                    return;
                }

                if (!_originalHistoryRequestParameters.ContainsKey(id))
                {
                    Logger.Warning($"{Name} history request callback error: no original entry with ID #{id}");
                    return;
                }

                var response = new StringBuilder();
                foreach (var uri in uris)
                {
                    _session.OpenUri(uri, (responseUri, reader) =>
                    {
                        using (var stream = new GZipStream(reader.BaseStream, CompressionMode.Decompress))
                        {
                            const int size = 8184;
                            var buffer = new byte[size];
                            try
                            {
                                var numBytes = stream.Read(buffer, 0, size);
                                while (numBytes > 0)
                                {
                                    response.Append(Encoding.UTF8.GetString(buffer, 0, numBytes));
                                    numBytes = stream.Read(buffer, 0, size);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Warning("Failed to read historical response", ex);
                            }
                        }
                    }, FailureCallback);
                }

                var callback = _historyRequestHandlers[id];
                var originalRequestParameters = _originalHistoryRequestParameters[id];
                _originalHistoryRequestParameters.Remove(id);
                _historyRequestHandlers.Remove(id);

                ThreadPool.QueueUserWorkItem(o => ProcessHistoryResponseAsync(response.ToString(), id, originalRequestParameters, callback));
            }
        }

        private void ProcessHistoryResponseAsync(string response, int requestId, Selection originalRequestParameters, HistoryAnswerHandler callback)
        {
            var data = ParseHistoricalResponse(response, originalRequestParameters.BidAsk == PriceType.Ask);
            var neededCount = originalRequestParameters.BarCount;
            if (originalRequestParameters.Timeframe == Timeframe.Minute)
                neededCount = neededCount * originalRequestParameters.TimeFactor;
            if (originalRequestParameters.Timeframe == Timeframe.Hour)
                neededCount = neededCount * originalRequestParameters.TimeFactor * 60;
            if (originalRequestParameters.Timeframe == Timeframe.Day)
                neededCount = neededCount * originalRequestParameters.TimeFactor;
            if (originalRequestParameters.Timeframe == Timeframe.Month)
                neededCount = neededCount * originalRequestParameters.TimeFactor * 30;
            if (data.Count < neededCount && originalRequestParameters.From > new DateTime(1970, 1, 1)
                && originalRequestParameters.BarCount != int.MaxValue)
            {
                Logger.Warning($"Received less bars ({data.Count}) than expected ({neededCount}) from {Name} for "
                    + originalRequestParameters.Symbol);
            }

            if (originalRequestParameters.BidAsk == PriceType.Bid)
            {
                var asksId = requestId + 1;
                if (_bidAskCache.ContainsKey(asksId))
                {
                    var merged = MergeBidAskData(data, _bidAskCache[asksId]);
                    _bidAskCache.TryRemove(asksId, out var removedBars);
                    callback(originalRequestParameters, merged);
                }
                else
                {
                    _bidAskCache[requestId] = data;  //hold bids and wait for asks
                }
            }
            else if (originalRequestParameters.BidAsk == PriceType.Ask)
            {
                var bidsId = requestId - 1;
                if (_bidAskCache.ContainsKey(bidsId))
                {
                    var merged = MergeBidAskData(_bidAskCache[bidsId], data);
                    _bidAskCache.TryRemove(bidsId, out var removedBars);
                    callback(originalRequestParameters, merged);
                }
                else
                {
                    _bidAskCache[requestId] = data;  //hold asks and wait for bids
                }
            }
        }

        private static List<Bar> MergeBidAskData(List<Bar> bids, List<Bar> asks)
        {
            if (bids.IsEmpty())
                return asks;

            if (asks.IsEmpty())
                return bids;

            //! bars are supposed to be sorted by date in descending order
            var count = Math.Max(bids.Count, asks.Count);
            var merged = new List<Bar>(count);
            for (int a = 0, b = 0; a < count && b < count; a++, b++)
            {
                Bar bid;
                Bar ask;
                if (a >= asks.Count || (b < bids.Count && bids[b].Date > asks[a].Date))
                {
                    bid = bids[b];
                    ask = a < asks.Count ? new Bar(asks[a]) {Date = bid.Date} : new Bar(bid.Date, 0, 0, 0, 0);
                    a--;
                }
                else if (b >= bids.Count || asks[a].Date > bids[b].Date)
                {
                    ask = asks[a];
                    bid = b < bids.Count ? new Bar(bids[b]) {Date = ask.Date} : new Bar(ask.Date, 0, 0, 0, 0);
                    b--;
                }
                else
                {
                    bid = bids[b];
                    ask = asks[a];
                }

                merged.Add(new Bar
                {
                    Date = bid.Date,
                    OpenBid = bid.OpenBid,
                    OpenAsk = ask.OpenAsk,
                    HighBid = bid.HighBid,
                    HighAsk = ask.HighAsk,
                    LowBid = bid.LowBid,
                    LowAsk = ask.LowAsk,
                    CloseBid = bid.CloseBid,
                    CloseAsk = ask.CloseAsk,
                    VolumeBid = bid.VolumeBid,
                    VolumeAsk = ask.VolumeAsk
                });
            }

            return merged;
        }

        private void MarketDataUpdate(OrderBookEvent data)
        {
            _securities.TryGetValue((int)data.InstrumentId, out var instrument);
            if (instrument == null)
            {
                Logger.Warning($"Available {Name} instruments does not contain instrument #{data.InstrumentId} passed to MarketDataUpdate");
                return;
            }

            if (NewTick != null && data.BidPrices.Any() && data.AskPrices.Any())
            {
                var date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(data.Timestamp);
                var bids = data.BidPrices;
                var asks = data.AskPrices;
                var level2 = new List<MarketLevel2>();

                if (bids.Count > 1)
                {
                    for (var i = 0; i < bids.Count && i < asks.Count; i++)
                    {
                        level2.Add(new MarketLevel2
                        {
                            DomLevel = i + 1,
                            AskPrice = asks[i].Price,
                            AskSize = asks[i].Quantity,
                            BidPrice = bids[i].Price,
                            BidSize = bids[i].Quantity
                        });
                    }
                }

                NewTick(new Tick
                {
                    DataFeed = Name,
                    Ask = asks[0].Price,
                    Bid = bids[0].Price,
                    AskSize = (long)asks[0].Quantity,
                    BidSize = (long)bids[0].Quantity,
                    Date = TimeZoneInfo.ConvertTimeToUtc(date, TimeZoneInfo),
                    Price = bids[0].Price,
                    Symbol = instrument,
                    Volume = asks[0].Quantity + bids[0].Quantity,
                    Level2 = level2
                });
            }
        }

        #endregion

        #region Static Helpers

        private static Resolution FromPeriodToResolution(Timeframe periodicity)
        {
            switch (periodicity)
            {
                case Timeframe.Minute:
                case Timeframe.Hour:
                    return Resolution.Minute;
                case Timeframe.Day:
                case Timeframe.Month:
                    return Resolution.Day;
                default:
                    Logger.Warning("Invalid periodicity passed: " + periodicity);
                    return Resolution.Minute;
            }
        }

        private static List<Bar> ParseHistoricalResponse(string response, bool isAskData)
        {
            var unixStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var result = new List<Bar>();
            foreach (var row in response.Split('\n'))
            {
                var data = row.Split(',');
                if (data.Length < 8)
                    continue;

                if (long.TryParse(data[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var timeSpan)
                    && decimal.TryParse(data[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var open)
                    && decimal.TryParse(data[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var high)
                    && decimal.TryParse(data[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var low)
                    && decimal.TryParse(data[4], NumberStyles.Any, CultureInfo.InvariantCulture, out var close)
                    && decimal.TryParse(data[5], NumberStyles.Any, CultureInfo.InvariantCulture, out var upVolume)
                    && decimal.TryParse(data[6], NumberStyles.Any, CultureInfo.InvariantCulture, out var downVolume)
                    && decimal.TryParse(data[7], NumberStyles.Any, CultureInfo.InvariantCulture, out var sameVolume))
                {
                    if (isAskData)
                    {
                        result.Add(new Bar
                        {
                            Date = unixStart.AddMilliseconds(timeSpan),
                            OpenAsk = open,
                            HighAsk = high,
                            LowAsk = low,
                            CloseAsk = close,
                            VolumeAsk = upVolume + downVolume + sameVolume
                        });
                    }
                    else
                    {
                        result.Add(new Bar
                        {
                            Date = unixStart.AddMilliseconds(timeSpan),
                            OpenBid = open,
                            HighBid = high,
                            LowBid = low,
                            CloseBid = close,
                            VolumeBid = upVolume + downVolume + sameVolume
                        });
                    }
                }
            }

            result.Reverse();

            return result;
        }

        private static void FailureLoginCallback(FailureResponse args) => 
            throw new InvalidCredentialException(args.Message, args.Exception);

        #endregion

    }
}
