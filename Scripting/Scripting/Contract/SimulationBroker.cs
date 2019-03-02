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
using CommonObjects;
using Scripting;

namespace ScriptingManager
{

    sealed class AccountSymbolKey : IEquatable<AccountSymbolKey>
    {
        public AccountInfo AccountInfo { get; }

        public string Symbol { get; }

        public AccountSymbolKey(AccountInfo accountInfo, string symbol)
        {
            AccountInfo = accountInfo;
            Symbol = symbol;
        }

        public override int GetHashCode()
        {
            return AccountInfo.GetHashCode() ^ Symbol.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is AccountSymbolKey other)
                return Equals(other);

            return false;
        }

        public bool Equals(AccountSymbolKey other)
        {
            return other.AccountInfo.ID == AccountInfo.ID && other.Symbol == Symbol;
        }

        public bool SymbolEquals(string otherSymbol)
        {
            return Symbol == otherSymbol;
        }

        public bool AccountInfoEquals(AccountInfo otherAccountInfo)
        {
            return AccountInfo.ID == otherAccountInfo.ID;
        }
    }

    sealed class OrderData
    {
        public Order Order;

        public AccountInfo AccountInfo { get; set; }

        public bool IsPriceHitted { get; set; }

        public AccountSymbolKey PositionKey { get; set; }

        public bool IsLong => Order.OrderSide == Side.Buy;
        public bool IsShort => Order.OrderSide == Side.Sell;

        public OrderData(Order order)
        {
            Order = (Order)order.Clone();
        }
    }

    public static class BarExtensions
    {
        public static IEnumerable<Tick> ToSimulationTicks(this Bar bar, string symbol)
        {
            yield return CreateTick(symbol, bar, bar.OpenAsk, bar.OpenBid);
            yield return CreateTick(symbol, bar, bar.CloseAsk >= bar.OpenAsk ? bar.LowAsk : bar.HighAsk, bar.CloseBid >= bar.OpenBid ? bar.LowBid : bar.HighBid);
            yield return CreateTick(symbol, bar, bar.CloseAsk >= bar.OpenAsk ? bar.HighAsk : bar.LowAsk, bar.CloseBid >= bar.OpenBid ? bar.HighBid : bar.LowBid);
            yield return CreateTick(symbol, bar, bar.CloseAsk, bar.CloseBid);
        }

        private static Tick CreateTick(string symbol, Bar bar, decimal priceAsk, decimal priceBid)
        {
            var volume = (bar.VolumeAsk + bar.VolumeBid) / 8;

            return new Tick
            {
                Date = bar.Date,
                Ask = priceAsk,
                AskSize = (long)volume,
                Bid = priceBid,
                BidSize = (long)volume,
                Volume = volume,
            };
        }
    }

    public class SimulationBroker : ISimulationBroker
    {
        #region Fields

        private Dictionary<AccountSymbolKey, List<OrderData>> _orders = new Dictionary<AccountSymbolKey, List<OrderData>>();

        private Dictionary<AccountSymbolKey, Position> _positions = new Dictionary<AccountSymbolKey, Position>();

        private AccountSymbolKey CreateKey(AccountInfo accountInfo, string symbol) => new AccountSymbolKey(accountInfo, symbol);

        private List<AccountInfo> _availableAccounts;

        private List<Portfolio> _availablePortfolios;

        private readonly List<string> _activityLog;

        #endregion //Fields

        #region Properties

        public IEnumerable<Order> Orders => _orders.SelectMany(s => s.Value).Select(s => s.Order);

        public IEnumerable<Position> Positions => _positions.Values;

        public IEnumerable<string> ActivityLog => _activityLog;

        public List<AccountInfo> AvailableAccounts => _availableAccounts;

        public List<Portfolio> Portfolios => _availablePortfolios;

        #endregion Properties

        public SimulationBroker(List<AccountInfo> availableAccounts, List<Portfolio> availablePortfolios)
        {
            _availableAccounts = availableAccounts;
            _availablePortfolios = availablePortfolios;

            _activityLog = new List<string>();
        }

        public SimulationBroker(IEnumerable<SimulationAccount> simulationAccount)
        {
            _availableAccounts = new List<AccountInfo>(simulationAccount.Select(a => new AccountInfo
            {
                ID = a.ID,
                Balance = a.Balance,
                Currency = a.Currency,
                UserName = a.UserName,
                Password = a.Password
            }));

            _activityLog = new List<string>();
        }

        #region Orders Stuff

        public Order GetOrder(string orderId, AccountInfo account)
        {
            return _orders.Values.SelectMany(s => s).FirstOrDefault(s => s.Order.UserID == orderId)?.Order;
        }

        public void CancelOrder(string orderId, AccountInfo accountInfo)
        {
            _activityLog.Add($"Order CANCELED. ID: {orderId}, Broker: {accountInfo.BrokerName}, Username: {accountInfo.UserName} ");
        }

        public List<Security> GetAvailableSecurities(AccountInfo account)
        {
            return new List<Security>(0);
        }

        public List<Order> GetOrders(AccountInfo account)
        {
            return _orders.Where(s => s.Key.AccountInfoEquals(account)).SelectMany(s => s.Value).Select(s => s.Order).ToList();
        }

        public void PlaceOrder(OrderParams orderParams, AccountInfo accountInfo) => 
            PlaceOrder(orderParams, accountInfo, false);

        public void Modify(string orderId, decimal? sl, decimal? tp, bool isServerSide, AccountInfo account)
        {
            var order = _orders.SelectMany(s => s.Value).FirstOrDefault(s => s.Order.UserID == orderId);

            if (order != null && order.Order.FilledQuantity == 0)
            {
                var key = CreateKey(account, order.Order.Symbol);
                CancelUnfilledOrdersForPosition(key);
                ReplaceSLTPOrders(order, key);
            }
        }

        public void ProcessBar(string symbol, Bar bar)
        {
            var orders = _orders.Where(s => s.Key.SymbolEquals(symbol)).SelectMany(s => s.Value);

            var ticks = bar.ToSimulationTicks(symbol).ToList();

            foreach (var orderData in orders)
            {
                foreach (var tick in ticks)
                {
                    ProcessOrderWithTick(orderData, tick);
                }
            }

            var positions = _positions.Where(s => s.Key.SymbolEquals(symbol)).Select(s => s.Value);
            foreach (var position in positions)
            {
                foreach (var tick in ticks)
                {
                    position.CurrentPrice = tick.Price * position.Quantity;
                    position.Profit = position.CurrentPrice - position.Price;
                }
            }
        }

        private void ProcessOrderWithTick(OrderData orderData, Tick tick)
        {
            var order = orderData.Order;

            if (!orderData.IsPriceHitted)
            {

                var isHitted = IsPriceHitted(orderData, tick);
                if (!isHitted)
                    return;
            }
            else
            {
                return;
            }

            orderData.IsPriceHitted = true;
            var fillPrice = GetFillPrice(orderData, tick.Ask, tick.Bid);

            order.FilledQuantity = order.Quantity;
     
            var key = CreateKey(orderData.AccountInfo, order.Symbol);

            if (_positions.TryGetValue(key, out Position position))
            {
                UpdatePosition(orderData, position);
            }
            else
            {
                CreateNewPosition(orderData, order.Symbol);
            }
        }

        private string PlaceOrder(OrderParams orderParams, AccountInfo accountInfo, bool isPositionOrder = false)
        {
            var order = new Order(orderParams.UserID, orderParams.Symbol)
            {
                Quantity = orderParams.Quantity,
                OrderType = orderParams.OrderType,
                OrderSide = orderParams.OrderSide,
                SignalID = orderParams.SignalId,
                Price = orderParams.Price,
                SLOffset = orderParams.SLOffset,
                TPOffset = orderParams.TPOffset,
                TimeInForce = orderParams.TimeInForce,
                BrokerID = DateTime.UtcNow.Ticks.ToString(),
                BrokerName = accountInfo.BrokerName,
                DataFeedName = accountInfo.DataFeedName,
                OpenDate = DateTime.UtcNow,
                OpenQuantity = orderParams.Quantity,
                AccountId = accountInfo.ID,
            };
            var key = CreateKey(accountInfo, order.Symbol);
            var orderData = new OrderData(order) { AccountInfo = accountInfo };

            if (isPositionOrder)
                orderData.PositionKey = key;

            if (_orders.TryGetValue(key, out List<OrderData> orders))
            {
                orders.Add(orderData);
            }
            else
            {
                _orders.Add(key, new List<OrderData> { orderData });
            }

            _activityLog.Add($"Order PLACED. ID: {orderParams.UserID}, Broker: {accountInfo.BrokerName}, Username: {accountInfo.UserName} ");

            return String.Empty;
        }


        private bool IsPriceHitted(OrderData orderData, Tick tick, decimal offset = 0M)
        {
            var hitPrice = 0M;

            if (orderData.IsLong)
            {
                hitPrice = orderData.Order.OrderType == OrderType.Stop ? tick.Ask : tick.Bid;
            }
            else
            {
                hitPrice = orderData.Order.OrderType == OrderType.Stop ? tick.Bid : tick.Ask;
            }

            //current Order Price contains only offset added by user
            if (orderData.PositionKey != null)
                offset = hitPrice;

            switch (orderData.Order.OrderType)
            {
                case OrderType.Market:
                    return true;
                case OrderType.Stop:

                    return orderData.IsLong
                        ? orderData.Order.Price + offset <= hitPrice
                        : orderData.Order.Price + offset >= hitPrice;

                case OrderType.Limit:
                    return orderData.IsLong
                        ? orderData.Order.Price + offset >= hitPrice
                        : orderData.Order.Price + offset <= hitPrice;

                default:
                    return false;
            }
        }

        private decimal GetFillPrice(OrderData orderData, decimal ask, decimal bid)
        {
            if (orderData.IsLong)
            {
                return orderData.Order.OrderType == OrderType.Market || orderData.Order.OrderType == OrderType.Stop
                    ? ask
                    : bid;
            }

            return orderData.Order.OrderType == OrderType.Market || orderData.Order.OrderType == OrderType.Stop
                ? bid
                : ask;
        }

        #endregion //Orders stuff

        #region Positions stuff

        public void ClosePosition(AccountInfo account, string symbol)
        {
            var orderParams = CreatePositionClosingOrderParams(account, symbol);
            if (orderParams != null)
            {
                _activityLog.Add($"Order for Position Closing Created, Position: {symbol}, UserName: {account.UserName}");

                PlaceOrder(orderParams, account);
            }
        }

        public void CloseAllPositions(AccountInfo account)
        {
            var positions = _positions.Where(p => p.Key.AccountInfoEquals(account)).Select(s => s.Value);
            _activityLog.Add($"Account All Positions closing started, UserName: {account.UserName}, Count: {positions?.Count()}");

            foreach (var position in positions)
            {
                var orderParams = CreatePositionClosingOrderParams(account, position.Symbol);
                if (orderParams != null)
                {
                    PlaceOrder(orderParams, account);
                }
            }
        }

        public List<Position> GetPositions(AccountInfo account)
        {
            return _positions.Where(p => p.Key.AccountInfoEquals(account)).Select(s => s.Value).ToList();
        }

        public List<Position> GetPositions(AccountInfo account, string symbol)
        {
            var key = CreateKey(account, symbol);
            return _positions.Where(p => p.Equals(p)).Select(s => s.Value).ToList();
        }

        private void CreateNewPosition(OrderData orderData, string symbol)
        {
            var key = CreateKey(orderData.AccountInfo, symbol);
            _activityLog.Add($"Position Created, Symbol: {symbol}, UserName: {orderData.AccountInfo.UserName}");

            var position = new Position
            {
                Symbol = symbol,
                AccountId = orderData.AccountInfo.ID,
                PositionSide = orderData.Order.OrderSide,
                Price = orderData.Order.AvgFillPrice,
                Quantity = orderData.Order.FilledQuantity
                //Timestamp = order.Timestamp,
            };

            _positions.Add(key, position);

            PlaceSLTPOrders(orderData);
        }

        private void UpdatePosition(OrderData orderData, Position position)
        {
            var isSameDirection = orderData.Order.OrderSide == position.PositionSide;

            if (isSameDirection)
                ExtendPosition(orderData, position);
            else
                DecreasePosition(orderData, position);
            _activityLog.Add($"Position Updated, Symbol: {position.Symbol}, Qty: {position.Quantity}, Side: {position.Quantity}, UserName: {orderData.AccountInfo.UserName}");
        }

        private void ExtendPosition(OrderData orderData, Position position)
        {
            position.Price = Average(
                position.Price, position.Quantity,
                orderData.Order.AvgFillPrice, orderData.Order.FilledQuantity,
                orderData.Order.Commission);

            position.Quantity += orderData.Order.FilledQuantity;

            var key = CreateKey(orderData.AccountInfo, position.Symbol);
            ReplaceSLTPOrders(orderData, key);
        }

        private void DecreasePosition(OrderData orderData, Position position)
        {
            var filledQuantity = Math.Min(orderData.Order.FilledQuantity, position.Quantity);

            position.Quantity -= filledQuantity;

            var key = CreateKey(orderData.AccountInfo, position.Symbol);
            var remainingQuantity = orderData.Order.Quantity - filledQuantity;

            if (remainingQuantity > 0)
            {
                var newPosition = new Position
                {
                    Symbol = position.Symbol,
                    AccountId = orderData.AccountInfo.ID,
                    PositionSide = orderData.Order.OrderSide,
                    Price = orderData.Order.AvgFillPrice,
                    Quantity = remainingQuantity,
                    //Timestamp = order.Timestamp,
                };
                _positions[key] = newPosition;
                ReplaceSLTPOrders(orderData, key);
            }
            else
            {
                _positions.Remove(key);
                CancelUnfilledOrdersForPosition(key);
            }
        }

        public decimal Average(decimal price, decimal quantity, decimal filledPrice, decimal filledQuantity, decimal commission)
        {
            var totalQuantity = quantity + filledQuantity;

            return (quantity * price + filledQuantity * filledPrice - commission) / totalQuantity;
        }

        private OrderParams CreatePositionClosingOrderParams(AccountInfo accountInfo, string symbol, OrderType orderType, decimal price)
        {
            var key = CreateKey(accountInfo, symbol);
            if (_positions.TryGetValue(key, out Position position))
            {
                var orderParams = new OrderParams
                {
                    UserID = Guid.NewGuid().ToString(),
                    Symbol = symbol,
                    OrderSide = position.PositionSide == Side.Buy ? Side.Sell : Side.Buy,
                    OrderType = orderType,
                    TimeInForce = TimeInForce.GoodForDay,
                    Price = price,
                    Quantity = position.Quantity
                };

                return orderParams;
            }
            return null;
        }

        private OrderParams CreatePositionClosingOrderParams(AccountInfo accountInfo, string symbol)
        {
            return CreatePositionClosingOrderParams(accountInfo, symbol, OrderType.Market, 0);
        }

        private void PlaceSLTPOrders(OrderData orderData)
        {
            if (orderData.Order.TPOffset.HasValue)
            {
                var tpOrderParams = CreatePositionClosingOrderParams(orderData.AccountInfo, orderData.Order.Symbol, OrderType.Limit, orderData.Order.TPOffset.Value);
                PlaceOrder(tpOrderParams, orderData.AccountInfo, true);
            }
            if (orderData.Order.SLOffset.HasValue)
            {
                var slOrderParams = CreatePositionClosingOrderParams(orderData.AccountInfo, orderData.Order.Symbol, OrderType.Stop, orderData.Order.SLOffset.Value);
                PlaceOrder(slOrderParams, orderData.AccountInfo, true);
            }
        }

        /// <summary>
        /// removes TakeProfit and StopLoss target orders only
        /// </summary>
        private void CancelUnfilledOrdersForPosition(AccountSymbolKey positionKey)
        {
            _orders[positionKey]?.RemoveAll(r => r.PositionKey?.Equals(positionKey) ?? false);
        }

        private void ReplaceSLTPOrders(OrderData orderData, AccountSymbolKey positionKey)
        {
            CancelUnfilledOrdersForPosition(positionKey);
            PlaceSLTPOrders(orderData);
        }

        #endregion //Positions stuff

        public void Dispose()
        {
            
        }
    }
}
