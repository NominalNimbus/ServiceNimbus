/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonObjects;
using ServerCommonObjects;
using ServerCommonObjects.Classes;
using ServerCommonObjects.Interfaces;
using PoloniexAPI;
using PoloniexAPI.MarketTools;
using System.Configuration;
using System.Reflection;
using System.IO;

namespace PoloniexDataFeed
{
    public class PoloniexDataFeed : IDataFeed
    {

        #region Private members

        private string PublicKey { get; }
        private string PrivateKey { get; }
        private string Url { get; }

        private const int MaxLevel = 20;
        private PoloniexClient _api;
        private readonly Dictionary<CurrencyPair, Security> _securities;
        private readonly Dictionary<int, Security> _securitiesById;

        private readonly Dictionary<CurrencyPair, OrderBook> _orderBooks;
        private readonly Dictionary<string, int> _subscribedSymbols;
        private readonly Dictionary<string, DateTime> _tickTimestamps;

        private TimeZoneInfo TimeZoneInfo { get; }

        #endregion

        #region Public properties and events

        public string Name => "Poloniex";

        public int BalanceDecimals => 8;

        public bool IsStarted { get; private set; }
        public List<Security> Securities { get; set; }

        public event NewTickHandler NewTick;
        public event NewSecurityHandler NewSecurity;

        #endregion

        #region Constructor

        public PoloniexDataFeed()
        {
            var config = new LocalConfigHelper(Assembly.GetExecutingAssembly().Location, "Poloniex Datafeed");

            PublicKey = config.GetString(nameof(PublicKey));
            PrivateKey = config.GetString(nameof(PrivateKey));
            Url = config.GetString(nameof(Url));

            TimeZoneInfo = TimeZoneInfo.Utc;
            Securities = new List<Security>();
            _securities = new Dictionary<CurrencyPair, Security>();
            _securitiesById = new Dictionary<int, Security>();
            _orderBooks = new Dictionary<CurrencyPair, OrderBook>();
            _subscribedSymbols = new Dictionary<string, int>();
            _tickTimestamps = new Dictionary<string, DateTime>();
        }

        #endregion

        #region IDataFeed implementation

        public void Start()
        {
            //start live stream
            _api = new PoloniexClient(Url, PublicKey, PrivateKey);

            //get latest quotes to populate securities list
            List<Quote> quotes = null;
            Task.Run(async () => quotes = await _api.Markets.GetSummary()).Wait();
            if (quotes != null && quotes.Count > 0)
            {
                foreach (var q in quotes)
                {
                    if (Securities.All(i => i.Symbol != q.Symbol.ToString()))
                    {
                        var security = GetSecurityFromCurrencyPair(q.Symbol);
                        Securities.Add(security);
                        NewSecurity?.Invoke(security);
                        _securities[q.Symbol] = security;
                        _securitiesById[q.Symbol.Id] = security;
                        Subscribe(security);
                    }
                }
            }
            else
            {
                throw new Exception($"No securities defined for {Name} data feed");
            }

            _api.Live.OnOrderBookUpdate += Api_OnOrderBookUpdate;
            _api.Live.OnTickerUpdate += Api_OnTickerUpdate;
            _api.Live.OnSessionError += Api_OnSessionError;
            _api.Live.Start();
            IsStarted = true;

            Task.Run(async () =>
            {
                var books = await _api.Markets.GetOrderBooks();
                lock (_orderBooks)
                {
                    _orderBooks.Clear();
                    foreach (var book in books)
                        _orderBooks.Add(book.Key, book.Value);
                }
            });
        }

        public void Stop()
        {
            IsStarted = false;
            _api.Live.OnOrderBookUpdate -= Api_OnOrderBookUpdate;
            _api.Live.OnSessionError -= Api_OnSessionError;

            try { _api.Live.Stop(); }
            catch (Exception e) { Logger.Error(Name + " stop exception", e); }

            lock (Securities)
                Securities.Clear();

            lock (_subscribedSymbols)
                _subscribedSymbols.Clear();
        }

        public void Subscribe(Security security)
        {
            if (Securities.Any(i => i.Symbol == security.Symbol))
            {
                lock (_subscribedSymbols)
                {
                    if (!_subscribedSymbols.ContainsKey(security.Symbol))
                    {
                        _subscribedSymbols.Add(security.Symbol, 1);
                        Task.Run(() =>
                        {
                            try { _api.Live.SubscribeOrderBook(security.Symbol); }
                            catch (Exception e) { Logger.Error($"Failed to subscribe {security.Symbol}: {e.Message}"); }
                        });
                    }
                    else
                    {
                        _subscribedSymbols[security.Symbol]++;
                    }
                }
            }
            else
            {
                Logger.Warning($"Invalid symbol passed for subscription over {Name} feed: {security.Symbol}", null);
            }
        }

        public void UnSubscribe(Security security)
        {
            if (_api?.Live == null)
                return;

            if (!_api.Live.IsConnected)
                throw new ApplicationException($"Can't unsubscribe from symbol. {Name} data feed is not connected.");

            if (Securities.Any(i => i.Symbol == security.Symbol))
            {
                lock (_subscribedSymbols)
                {
                    if (_subscribedSymbols.ContainsKey(security.Symbol))
                    {
                        if (_subscribedSymbols[security.Symbol] <= 1)
                        {
                            _subscribedSymbols.Remove(security.Symbol);
                            Task.Run(() => _api.Live.UnsubscribeOrderBook(security.Symbol));
                        }
                        else
                        {
                            _subscribedSymbols[security.Symbol]--;
                        }
                    }
                }
            }
            else
            {
                Logger.Warning($"Invalid symbol passed for unsubscription over {Name} feed: {security.Symbol}", null);
            }
        }

        public void GetHistory(Selection parameters, HistoryAnswerHandler callback)
        {
            if (parameters.Timeframe < Timeframe.Minute)
            {
                Logger.Warning($"{parameters.Timeframe} time frame is not supported by {Name} data feed");
                callback(parameters, new List<Bar>(0));
                return;
            }

            var symbol = parameters.Symbol.ToUpper();
            var instrument = Securities.FirstOrDefault(i => i.Symbol == symbol);
            var currencyPair = _securities.FirstOrDefault(i => i.Value.Symbol == symbol).Key;
            if (instrument == null || currencyPair == null)
            {
                Logger.Warning($"Invalid symbol {parameters.Symbol} passed for history request over {Name} feed", null);
                callback(parameters, new List<Bar>(0));
                return;
            }

            if (parameters.To == DateTime.MinValue || parameters.To > DateTime.UtcNow)
                parameters.To = DateTime.UtcNow;

            //calculate start time
            if (parameters.From == DateTime.MinValue && parameters.BarCount != Int32.MaxValue)
            {
                if (parameters.BarCount < 3)
                {
                    callback(parameters, new List<Bar>(0));
                    return;
                }

                if (parameters.Timeframe == Timeframe.Minute)
                    parameters.From = parameters.To.AddMinutes(-1 * 3 * parameters.BarCount * parameters.TimeFactor);
                else if (parameters.Timeframe == Timeframe.Hour)
                    parameters.From = parameters.To.AddHours(-1 * 3 * parameters.BarCount * parameters.TimeFactor);
                else if (parameters.Timeframe == Timeframe.Day)
                    parameters.From = parameters.To.AddDays(-1 * 2 * parameters.BarCount * parameters.TimeFactor);
                else if (parameters.Timeframe == Timeframe.Month)
                    parameters.From = parameters.To.AddDays(-1 * parameters.BarCount * parameters.TimeFactor * 31);
            }

            var barSize = GetBarSize(parameters.Timeframe, parameters.TimeFactor);
            Task.Run(async () =>
            {
                var data = await _api.Markets.GetChartData(currencyPair, barSize, parameters.From, parameters.To);
                if (data != null && data.Count > 0)
                {
                    var bars = data.Select(i => new Bar(i.Time, i.Open, i.High, i.Low, i.Close, i.Volume)).ToList();
                    callback(parameters, bars);
                }
                else
                {
                    callback(parameters, new List<Bar>(0));
                }
            });
        }

        #endregion

        #region API events

        private void Api_OnSessionError(object sender, string e)
        {
            if (e.StartsWith("Reconnecting"))
                Logger.Info(e);
            else
                Logger.Error(e);
        }

        private void Api_OnOrderBookUpdate(object sender, PoloniexAPI.LiveTools.OrderBookItem e)
        {
            if (NewTick == null || (e.Type != OrderBookItemType.OrderBookModify && e.Type != OrderBookItemType.OrderBookRemove))
                return;

            _securities.TryGetValue(e.Symbol, out var instrument);
            if (instrument == null)
            {
                Logger.Warning($"OrderBookUpdate: available {Name} instruments list does not contain {e.Symbol}", null);
                return;
            }

            lock (_orderBooks)
            {
                if (!_orderBooks.ContainsKey(e.Symbol))
                    return;

                var book = _orderBooks[e.Symbol];

                if (e.Type == OrderBookItemType.OrderBookModify)
                {
                    var updatedLevel = UpdateOrderBookItem(e.Symbol,
                        e.Price, e.Quantity, e.Side == OrderBookItemSide.Bid);
                    if (updatedLevel >= 0 && updatedLevel <= MaxLevel)
                    {
                        var time = DateTime.UtcNow;
                        var prevTime = DateTime.MinValue;
                        if (!_tickTimestamps.TryGetValue(instrument.Symbol, out prevTime) || prevTime < time)
                        {
                            _tickTimestamps[instrument.Symbol] = time;
                            NewTick(GetCombinedTick(instrument, book.Bids.ToArray(), book.Asks.ToArray(),
                                time, e.Price, MaxLevel));
                        }
                    }
                }
            }
        }

        private void Api_OnTickerUpdate(object sender, Quote e)
        {
            if (NewTick == null)
                return;

            _securitiesById.TryGetValue(e.Id, out var instrument);
            if (instrument == null)
            {
                Logger.Warning($"TickerUpdate: available {Name} instruments list does not contain {e.Id}", null);
                return;
            }

            var time = DateTime.UtcNow;
            var prevTime = DateTime.MinValue;
            if (!_tickTimestamps.TryGetValue(instrument.Symbol, out prevTime) || prevTime < time)
            {
                _tickTimestamps[instrument.Symbol] = time;
                NewTick?.Invoke(GetCombinedTick(instrument, e, time));
            }
        }
        
        #endregion

        #region Helpers

        private int UpdateOrderBookItem(CurrencyPair symbol, decimal price, decimal size, bool isBid)
        {
            var level = -1;  //updated order book level, zero-based
            var book = _orderBooks[symbol];
            if (isBid)
            {
                if (book.Bids.ContainsKey(price))
                {
                    level = 0;
                    book.Bids[price] = size;
                    foreach (var item in book.Bids)
                    {
                        if (item.Key == price)
                            break;
                        else
                            level++;
                    }
                }
                else
                {
                    book.Bids[price] = size;
                    var items = book.Bids.ToList();
                    if (price < items[items.Count - 1].Key)
                    {
                        items.Add(new KeyValuePair<decimal, decimal>(price, size));
                        level = items.Count - 1;
                    }
                    else
                    {
                        for (var i = 0; i < items.Count; i++)
                        {
                            if (price > items[i].Key)
                            {
                                items.Insert(i, new KeyValuePair<decimal, decimal>(price, size));
                                level = i;
                                break;
                            }
                        }
                    }

                    book.Bids.Clear();
                    for (var i = 0; i < items.Count; i++)
                        book.Bids[items[i].Key] = items[i].Value;
                }
            }
            else  //asks
            {
                if (book.Asks.ContainsKey(price))
                {
                    level = 0;
                    book.Asks[price] = size;
                    foreach (var item in book.Asks)
                    {
                        if (item.Key == price)
                            break;
                        else
                            level++;
                    }
                }
                else
                {
                    book.Asks[price] = size;
                    var items = book.Asks.ToList();
                    if (price > items[items.Count - 1].Key)
                    {
                        items.Add(new KeyValuePair<decimal, decimal>(price, size));
                        level = items.Count - 1;
                    }
                    else
                    {
                        for (var i = 0; i < items.Count; i++)
                        {
                            if (price < items[i].Key)
                            {
                                items.Insert(i, new KeyValuePair<decimal, decimal>(price, size));
                                level = i;
                                break;
                            }
                        }
                    }

                    book.Asks.Clear();

                    foreach (var item in items)
                        book.Asks[item.Key] = item.Value;
                }
            }

            return level;
        }

        private Tick GetCombinedTick(Security security, KeyValuePair<decimal, decimal>[] bids,
            KeyValuePair<decimal, decimal>[] asks, DateTime time, decimal price = 0M, int maxLevel = 0)
        {
            var level2Count = Math.Min(Math.Min(bids.Length, asks.Length), maxLevel);
            var level2 = new List<MarketLevel2>(level2Count);
            for (var i = 0; i < level2Count; i++)
            {
                level2.Add(new MarketLevel2
                {
                    BidPrice = bids[i].Key,
                    BidSize = bids[i].Value,
                    AskPrice = asks[i].Key,
                    AskSize = asks[i].Value,
                    DomLevel = i + 1
                });
            }

            return new Tick
            {
                DataFeed = Name,
                Ask = asks[0].Key,
                Bid = bids[0].Key,
                AskSize = (long)asks[0].Value,
                BidSize = (long)bids[0].Value,
                Date = time,
                Price = price > 0M ? price : (bids[0].Key + asks[0].Key) / 2M,
                Symbol = security,
                Volume = asks[0].Value + bids[0].Value,
                Level2 = level2
            };
        }

        private Tick GetCombinedTick(Security security, Quote quote, DateTime time)
        {
            return new Tick
            {
                DataFeed = Name,
                Ask = quote.Ask,
                Bid = quote.Bid,
                AskSize = 0,
                BidSize = 0,
                Date = time,
                Price = quote.Last > 0M ? quote.Last : (quote.Ask + quote.Bid) / 2M,
                Symbol = security,
                Volume = quote.BaseVolume, //not sure
                Level2 = new List<MarketLevel2>()
            };

        }

        private Security GetSecurityFromCurrencyPair(CurrencyPair pair)
        {
            return new Security
            {
                AssetClass = "CURRENCY",
                BaseCurrency = pair.BaseCurrency,
                ContractSize = 1,  //! unknown
                DataFeed = Name,
                Digit = 8,
                MarginRate = 1,  //! unknown
                MarketOpen = TimeSpan.Zero,  //! unknown
                MarketClose = TimeSpan.Zero, //! unknown
                MaxPosition = 100000,  //! unknown
                Name = pair.ToString(),
                PriceIncrement = 0.00000001M,  //! unknown
                QtyIncrement = 0.0001M,  //! unknown
                SecurityId = pair.Id,
                Symbol = pair.ToString(),
                UnitOfMeasure = pair.QuoteCurrency,
                UnitPrice = 1  //! unknown
            };
        }

        private static BarSize GetBarSize(Timeframe timeframe, int barSize)
        {
            if (timeframe < Timeframe.Minute)
                throw new ArgumentException(timeframe + " time frame is not supported");

            if (timeframe == Timeframe.Minute)
            {
                switch (barSize)
                {
                    case 15: return BarSize.M15;
                    case 30: return BarSize.M30;
                    case 120: return BarSize.H2;
                    case 240: return BarSize.H4;
                    default: return BarSize.M5;
                }
            }

            if (timeframe == Timeframe.Hour)
            {
                switch (barSize)
                {
                    case 2: return BarSize.H2;
                    case 4: return BarSize.H4;
                    default: return BarSize.M30;
                }
            }

            return BarSize.Day;
        }

        #endregion

    }
}
