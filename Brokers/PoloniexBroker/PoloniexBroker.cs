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
using ServerCommonObjects.Interfaces;
using PoloniexAPI;
using ApiPosition = PoloniexAPI.TradingTools.Position;
using ApiOrder = PoloniexAPI.TradingTools.Order;
using OrderTrade = PoloniexAPI.TradingTools.OrderTrade;
using Trade = PoloniexAPI.TradingTools.Trade;
using System.Security.Authentication;

namespace Brokers
{
    public sealed class PoloniexBroker : AbstractBroker
    {
        #region members

        private readonly Dictionary<ulong, ApiOrder> _orders;
        private readonly Dictionary<CurrencyPair, ApiPosition> _positions;
        private readonly Dictionary<UpdateDataType, int> _updateIntervals;
        private readonly Dictionary<UpdateDataType, DateTime> _previousUpdates;
        private readonly System.Timers.Timer _tmrUpdates;

        private PoloniexClient _api;
        private bool _isStarted;
        private bool? _isMarginAccount;

        #endregion members

        #region Constants

        public const string BrokerName = "Poloniex Live";
        public const string DefaultDataFeedName = "Poloniex";
        private const string Url = "https://poloniex.com/";
        private const string Currency = "BTC";

        #endregion //Constants

        public override string Name => BrokerName;

        public PoloniexBroker(IDataFeed datafeed) : base(datafeed)
        {
            DataFeedName = datafeed.Name;
            AccountInfo = new AccountInfo
            {
                DataFeedName = datafeed.Name,
                Currency = Currency,
                BalanceDecimals = datafeed.BalanceDecimals
            };
            _orders = new Dictionary<ulong, ApiOrder>();
            _positions = new Dictionary<CurrencyPair, ApiPosition>();

            _updateIntervals = new Dictionary<UpdateDataType, int>
            {
                [UpdateDataType.AccSummary] = 5000,
                [UpdateDataType.OpenOrders] = 3000,
                [UpdateDataType.Positions]  = 4000
            };

            _previousUpdates = new Dictionary<UpdateDataType, DateTime>(_updateIntervals.Count);
            foreach (var item in _updateIntervals)
                _previousUpdates[item.Key] = DateTime.UtcNow.Date;

            _tmrUpdates = new System.Timers.Timer(1000);
            _tmrUpdates.Elapsed += UpdatesTimer_Elapsed;
        }

        public static AvailableBrokerInfo BrokerInfo(string user) =>
            AvailableBrokerInfo.CreateLiveBroker(BrokerName, DefaultDataFeedName, Url);

        #region IBroker Implementation

        public override void Login(AccountInfo account)
        {
            AccountInfo = account;
            if (String.IsNullOrEmpty(account.ID))
            {
                account.ID = account.UserName;
            }
            account.Currency = Currency;

            _api = new PoloniexClient(Url, AccountInfo.UserName, AccountInfo.Password);
            //TODO: (optionally) explicitly set this flag on client side, like _isMarginAccount = account.IsMargin;
            try
            {
                var loginTask = _api.Wallet.GetAvailableBalances();
                var getIsMarginValTask = IsMarginAccount();
                Task.WaitAll(loginTask, getIsMarginValTask);
                account.IsMarginAccount = getIsMarginValTask.Result == true;
            }
            catch
            {
                throw new InvalidCredentialException($"Login failed for {AccountInfo.UserName} ({Name} broker)");
            }

        }

        public override void Start()
        {
            if (_api == null)
                return;

            base.Start();
            _isStarted = true;
            _tmrUpdates.Start();
        }

        public override void Stop()
        {
            _isStarted = false;
            _tmrUpdates.Stop();

            base.Stop();
        }

        public override void PlaceOrder(Order order)
        {
            if (!_isStarted)
                OnOrderRejected(order, "Broker is not started or not connected");
            else
                base.PlaceOrder(order);
        }

        public override void CancelOrder(Order order)
        {
            if (!_isStarted)
            {
                OnOrderRejected(order, "Broker is not started or not connected");
                return;
            }

            if (order.ServerSide)
            {
                base.CancelOrder(order);
            }
            else
            {
                var currencyPair = CurrencyPair.Parse(order.Symbol);
                if (ulong.TryParse(order.BrokerID, out var id))
                    Task.Run(async () => await _api.Trading.CancelOrder(currencyPair, id));
                else
                    OnOrderRejected(order, $"Can't cancel an order #{order.BrokerID}: invalid ID");
            }
        }

        public override void ModifyOrder(Order order, decimal? sl, decimal? tp, bool isServerSide = false)
        {
            if (!_isStarted)
            {
                OnOrderRejected(order, "Broker is not started or not connected");
                return;
            }

            if (!isServerSide && (sl.HasValue || tp.HasValue))
            {
                OnOrderRejected(order, $"{Name} broker doesn't support SL/TP order features");
                return;
            }

            base.ModifyOrder(order, sl, tp, isServerSide);  //will cancel+place an order if necessary
        }

        protected override void PlaceLimitStopOrder(Order order)
        {
            if (order.SLOffset.HasValue || order.TPOffset.HasValue)
            {
                OnOrderRejected(order, $"{Name} broker doesn't support SL/TP order features");
                return;
            }

            if (_isStarted)
            {
                if (order.OrderType == OrderType.Limit)
                    Task.Run(async () => await SubmitOrder(order, isMarginOrder: _isMarginAccount ?? false));
                else
                    OnOrderRejected(order, $"{order.OrderType} orders are not supported");
            }
        }

        protected override void PlaceMarketOrder(Order order)
        {
            OnOrderRejected(order, "Market orders are not supported");
        }

        protected override void UpdateAccount()
        {
            lock (Positions)
            {
                AccountInfo.Margin = Positions.Sum(p => p.Margin);
                AccountInfo.Profit = Positions.Sum(p => p.Profit);
            }

            OnAccountStateChanged();
        }

        #endregion

        #region Data Updates (on Timer)

        private async void UpdatesTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!_isStarted)
                return;

            _tmrUpdates.Stop();

            if (!_isMarginAccount.HasValue)
                _isMarginAccount = await IsMarginAccount();

            if (IsTimeToUpdate(UpdateDataType.OpenOrders))
                await UpdateOrders();

            if (IsTimeToUpdate(UpdateDataType.Positions))
                await UpdatePositions();

            if (_isMarginAccount == true && IsTimeToUpdate(UpdateDataType.AccSummary))
                await UpdateMarginAccountSummary();

            if (_isStarted)
                _tmrUpdates.Start();
        }

        private async Task UpdateMarginAccountSummary()
        {
            if (_isMarginAccount != true)
                return;

            PoloniexAPI.WalletTools.AccountSummary summary = null;
            try { summary = await _api.Wallet.GetMarginAccountSummary(); }
            catch (Exception e)
            {
                var error = $"Failed to retrieve account summary from {Name}: {e.Message}";
                System.Diagnostics.Trace.TraceError(error);  //OnError(error);
            }

            _previousUpdates[UpdateDataType.AccSummary] = DateTime.UtcNow;
            if (summary != null)
            {
                bool summaryUpdated = AccountInfo.Balance != summary.Value
                    || AccountInfo.Equity != summary.NetValue
                    || AccountInfo.Margin != summary.CurrentMargin
                    || AccountInfo.Profit != summary.PL;

                if (summaryUpdated)
                {
                    if (String.IsNullOrEmpty(AccountInfo.Currency))
                        AccountInfo.Currency = Currency;
                    AccountInfo.Balance = summary.Value;
                    AccountInfo.Equity = summary.NetValue;
                    AccountInfo.Margin = summary.CurrentMargin;
                    AccountInfo.Profit = summary.PL;

                    OnAccountStateChanged();
                }
            }
        }

        private async Task UpdateOrders()
        {
            List<ApiOrder> openOrders = null;
            try { openOrders = await _api.Trading.GetOpenOrders(); }
            catch (Exception e)
            {
                var error = $"Failed to retrieve open orders from {Name}: {e.Message}";
                System.Diagnostics.Trace.TraceError(error);  //OnError(error);
                _previousUpdates[UpdateDataType.OpenOrders] = DateTime.UtcNow;
                return;
            }

            _previousUpdates[UpdateDataType.OpenOrders] = DateTime.UtcNow;

            //update open orders (new/existing)
            if (openOrders == null)
                openOrders = new List<ApiOrder>(0);
            foreach (var order in openOrders)
            {
                if (!_orders.ContainsKey(order.Id) || IsOrderChanged(_orders[order.Id], order))
                    await ProcessOpenOrder(order);
            }

            //update closed orders (filled/cancelled)
            var closedOrders = new List<ApiOrder>();
            foreach (var order in _orders)
            {
                if (!openOrders.Any(i => i.Id == order.Key))
                    closedOrders.Add(order.Value);
            }
            foreach (var order in closedOrders)
                await ProcessClosedOrder(order);
        }

        private async Task UpdatePositions()
        {
            bool anyPosUpdated = false;
            var closedPositions = new List<CurrencyPair>();
            if (_isMarginAccount == true)
            {
                List<ApiPosition> positions = null;
                try { positions = await _api.Trading.GetMarginPositions(); }
                catch (Exception e)
                {
                    var error = $"Failed to retrieve positions from {Name}: {e.Message}";
                    System.Diagnostics.Trace.TraceError(error);  //OnError(error);
                }

                _previousUpdates[UpdateDataType.Positions] = DateTime.UtcNow;
                if (positions != null && positions.Count > 0)
                {
                    //add or update existing positions
                    foreach (var pos in positions)
                    {
                        if (!_positions.ContainsKey(pos.CurrencyPair) 
                            || _positions[pos.CurrencyPair].Size != pos.Size)
                        {
                            _positions[pos.CurrencyPair] = pos;
                            anyPosUpdated = true;
                        }
                    }

                    //mark closed positions
                    foreach (var item in _positions)
                    {
                        if (!positions.Any(p => p.CurrencyPair == item.Key))
                            closedPositions.Add(item.Key);
                    }
                }
            }
            else  //optional: simply retrieve currency balances and process them as positions
            {
                List<PoloniexAPI.WalletTools.Balance> balances = null;
                try { balances = await _api.Wallet.GetBalances(); }
                catch { }

                if (balances != null && balances.Count > 0)
                {
                    var baseCur = AccountInfo?.Currency ?? Currency;
                    //add or update existing positions
                    foreach (var b in balances)
                    {
                        if (baseCur == b.Currency)  //that would be 'balance'
                        {
                            AccountInfo.Balance = b.QuoteAvailable;
                            AccountInfo.Equity = 0;
                            AccountInfo.Margin = 0;
                            AccountInfo.Profit = 0;
                            OnAccountStateChanged();
                            continue;
                        }

                        if (b.QuoteAvailable <= 0M)
                            continue;
                                                
                        var key = new CurrencyPair(baseCur, b.Currency);
                        if (!_positions.ContainsKey(key) || _positions[key].Size != b.QuoteAvailable)
                        {
                            _positions[key] = new ApiPosition
                            {
                                CurrencyPair = key,
                                Side = PositionSide.Long,
                                Size = b.QuoteAvailable
                            };
                            anyPosUpdated = true;
                        }
                    }

                    //mark closed positions
                    foreach (var item in _positions)
                    {
                        var b = balances.FirstOrDefault(i => i.Currency == item.Key.QuoteCurrency);
                        if (b == null || b.QuoteAvailable <= 0M)
                            closedPositions.Add(item.Key);
                    }
                }
            }

            //remove closed positions
            foreach (var symbol in closedPositions)
                _positions.Remove(symbol);

            if (anyPosUpdated || closedPositions.Count > 0)
                OnPositionsChanged(_positions.Select(p => ToCommonPosition(p.Value)).ToList());
        }

        #endregion

        #region Helpers

        private async Task SubmitOrder(Order order, bool isMarginOrder)
        {
            try
            {
                var currency = CurrencyPair.Parse(order.Symbol);
                var side = order.OrderSide == Side.Sell ? OrderSide.Sell : OrderSide.Buy;
                var id = await _api.Trading.PlaceOrder(currency, side, 
                    order.Price, order.Quantity, isMarginOrder);
                order.BrokerID = id.ToString();
                await ProcessOpenOrder(ToApiOrder(order));
            }
            catch (Exception e)
            {
                string error = e.Message;
                string key = "Not enough ";
                if (e.Message.StartsWith(key) && order.OrderSide == Side.Sell)
                {
                    //optional: request available quantity to notify client
                    var currency = e.Message.Substring(key.Length);
                    if (currency.Contains("."))
                        currency = currency.Remove(currency.IndexOf('.'));
                    List<PoloniexAPI.WalletTools.Balance> balances = null;
                    try { balances = await _api.Wallet.GetBalances(); }
                    catch { }
                    var available = balances?.FirstOrDefault(b => b.Currency == currency);
                    if (available != null)
                        error = $"{key}{currency} (available quantity: {available.QuoteAvailable:0.########})";
                }
                if(error.Contains("(422)") && order.Price * order.Quantity < 0.0001M)
                {
                    error += "Total (Price * Quantity) must be at least 0.0001";
                }


                OnError($"Failed to submit a {order.Symbol} order: {error}");
                OnOrderRejected(order, error);
            }
        }

        private async Task ProcessOpenOrder(ApiOrder order)
        {
            if (order != null && order.Id > 0)
            {
                var trades = new List<OrderTrade>();
                try { trades = await _api.Trading.GetOrderTrades(order.Id); }
                catch { }
                _orders[order.Id] = order;
                ProcessOrderUpdate(ToCommonOrder(order, false, trades));
            }
        }

        private async Task ProcessClosedOrder(ApiOrder order)
        {
            if (order != null && order.Id > 0)
            {
                _orders.Remove(order.Id);
                var trades = new List<OrderTrade>();
                try { trades = await _api.Trading.GetOrderTrades(order.Id); }
                catch { }
                ProcessOrderUpdate(ToCommonOrder(order, true, trades));
            }
        }

        private async Task<bool?> IsMarginAccount()
        {
            try
            {
                var balances = await _api.Wallet.GetAvailableBalances();
                if (balances != null)
                    return balances.ContainsKey("margin");
            }
            catch { }

            return null;
        }

        private bool IsTimeToUpdate(UpdateDataType type)
        {
            _updateIntervals.TryGetValue(type, out var interval);
            if (interval < 1)
                return false;

            _previousUpdates.TryGetValue(type, out var prevUpdate);
            return prevUpdate != DateTime.MinValue && (DateTime.UtcNow - prevUpdate).TotalMilliseconds >= interval;
        }

        private static bool IsOrderChanged(ApiOrder existing, ApiOrder updated)
        {
            //? not sure if API updates quantity on partial fill
            return existing == null || existing.Quantity != updated.Quantity;
        }

        #endregion

        #region Converters

        private ApiOrder ToApiOrder(Order order)
        {
            ulong.TryParse(order.BrokerID, out var id);
            var currencyPir = CurrencyPair.Parse(order.Symbol);
            return new ApiOrder
            {
                Id = id,
                CurrencyPair = currencyPir,
                PricePerCoin = order.Price,
                Quantity = order.Quantity,
                Side = order.OrderSide == Side.Sell ? OrderSide.Sell : OrderSide.Buy,
                Value = order.Price * order.Quantity
            };
        }

        private Order ToCommonOrder(ApiOrder order, bool isClosed, List<OrderTrade> trades = null)
        {
            var result = new Order
            {
                AccountId = AccountInfo.ID,
                UserID = order.Id.ToString(),
                BrokerID = order.Id.ToString(),
                BrokerName = Name,
                DataFeedName = Name,
                Symbol = order.CurrencyPair.ToString(),
                OrderType = OrderType.Limit,
                OrderSide = ToCommonOrderSide(order.Side),
                Price = order.PricePerCoin,
                Quantity = order.Quantity,
                PlacedDate = DateTime.UtcNow,
                FilledDate = DateTime.UtcNow
                //OpenQuantity = unknown
                //CancelledQuantity = unknown
                //AvgFillPrice = unknown
                //FilledQuantity = unknown
                //OpeningQty = unknown
                //ClosingQty = unknown
            };

            if (trades != null && trades.Count > 0)
            {
                result.FilledDate = trades[0].Time;//not sure
                result.FilledQuantity = trades.Sum(t => t.Quantity);
                if (result.FilledQuantity > 0)
                    result.AvgFillPrice = trades.Sum(t => t.Value) / result.FilledQuantity;
            }

            if (result.Quantity < result.FilledQuantity)
                result.Quantity = result.FilledQuantity;
            if (result.Quantity < result.FilledQuantity + result.CancelledQuantity + result.OpenQuantity)
                result.Quantity = result.FilledQuantity + result.CancelledQuantity + result.OpenQuantity;

            if (isClosed)
            {
                result.OpenQuantity = 0;
                if (result.FilledQuantity < result.Quantity)
                    result.CancelledQuantity = result.Quantity - result.FilledQuantity;
            }
            else
            {
                if (result.OpenQuantity == 0)
                    result.OpenQuantity = result.Quantity - result.FilledQuantity - result.CancelledQuantity;
            }

            return result;
        }

        private Position ToCommonPosition(ApiPosition position)
        {
            return new Position
            {
                AccountId = AccountInfo.ID,
                BrokerName = Name,
                DataFeedName = Name,
                Symbol = position.CurrencyPair.ToString(),
                Quantity = position.Size,
                Profit = position.PL,
                PositionSide = ToCommonPositionSide(position.Side),
                CurrentPrice = position.BasePrice,  //price per coin
                Price = position.Total  //position value (price * quantity)
            };
        }

        private static Side ToCommonOrderSide(OrderSide side)
        {
            switch (side)
            {
                case OrderSide.Buy: return Side.Buy;
                case OrderSide.Sell: return Side.Sell;
                default: throw new ArgumentException("Unsupported order side", nameof(side));
            }
        }

        private static Side ToCommonPositionSide(PositionSide side)
        {
            switch (side)
            {
                case PositionSide.Long: return Side.Buy;
                case PositionSide.Short: return Side.Sell;
                default: throw new ArgumentException("Unsupported position side", nameof(side));
            }
        }

        #endregion
    }
}
