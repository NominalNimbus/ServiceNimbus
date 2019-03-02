/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using CommonObjects;
using ServerCommonObjects.Classes;
using ServerCommonObjects.Enums;
using ServerCommonObjects.Interfaces;

// ReSharper disable once CheckNamespace
namespace ServerCommonObjects
{
    public abstract class AbstractBroker : MarshalByRefObject, IBroker
    {

        #region Fields&Consts

        protected const StringComparison IgnoreCase = StringComparison.OrdinalIgnoreCase;
        private readonly Dictionary<string, Tuple<decimal, decimal>> _stops;  //orderId -> SL, TP
        private readonly List<string> _ordersForUpdate;
        private readonly Dictionary<string, decimal> _ticks;
        

        #endregion

        #region Properties

        public abstract string Name { get; }
        public AccountInfo AccountInfo { get; protected set; }
        public string DataFeedName { get; protected set; }
        public bool IsStarted { get; protected set; }
        public List<Order> Orders { get; private set; }
        public List<Position> Positions { get; private set; }
        public List<Security> Securities { get; private set; }

        #endregion
        
        #region Events

        public event EventHandler<EventArgs> AccountStateChanged;
        public event EventHandler<EventArgs<string>> Error;
        public event EventHandler<EventArgs<Order, Status>> NewHistoricalOrder;
        public event EventHandler<OrderRejectedEventArgs> OrderRejected;
        public event EventHandler<EventArgs<List<Order>>> OrdersChanged;
        public event EventHandler<EventArgs<List<Order>>> OrdersUpdated;
        public event EventHandler<EventArgs<Position>> PositionUpdated;
        public event EventHandler<EventArgs<List<Position>>> PositionsChanged;

        #endregion
        
        protected AbstractBroker(IDataFeed datafeed)
        {
            DataFeedName = datafeed.Name;
            AccountInfo = new AccountInfo { DataFeedName = datafeed.Name, BalanceDecimals = datafeed.BalanceDecimals };
            Positions = new List<Position>();
            Orders = new List<Order>();
            Securities = new List<Security>(datafeed.Securities);
            _ticks = new Dictionary<string, decimal>();
            _stops = new Dictionary<string, Tuple<decimal, decimal>>();
            _ordersForUpdate = new List<string>();

            datafeed.NewTick += OnNewTick;
        }

        #region IBroker Methods

        public abstract void Login(AccountInfo account);

        public virtual void Start()
        {
            IsStarted = true;
        }

        public virtual void Stop()
        {
            IsStarted = false;
            lock (_stops)
                _stops.Clear();

            lock (Positions)
                Positions.Clear();

            _ordersForUpdate.Clear();
        }

        public virtual void PlaceOrder(Order order)
        {
            var orderError = ValidateOrder(order);
            if (orderError != null)
            {
                OnOrderRejected(order, orderError);
                return;
            }

            var shares = GetOrderShares(order.Symbol, order.Quantity, order.OrderSide);
            order.OpeningQty = shares.Item1;
            order.ClosingQty = shares.Item2;

            if (order.OrderType == OrderType.Market)
            {
                if (order.ServerSide && (order.SLOffset.HasValue || order.TPOffset.HasValue))
                {
                    lock (_stops)
                    {
                        _stops[order.UserID] = new Tuple<decimal, decimal>(order.SLOffset ?? 0m, order.TPOffset ?? 0m);
                        order.SLOffset = null;
                        order.TPOffset = null;
                    }
                }

                PlaceMarketOrder(order);
            }
            else
            {
                if (order.ServerSide)
                {
                    var copy = order.Clone();
                    copy.OpenDate = DateTime.UtcNow;
                    copy.CancelledQuantity = 0;
                    copy.OpenQuantity = 0;
                    copy.FilledQuantity = 0;
                    copy.BrokerName = Name;
                    copy.BrokerID = DateTime.UtcNow.Ticks.ToString();
                    copy.AccountId = AccountInfo.ID;

                    AddOrder(copy);
                    OnOrdersChanged(Orders);
                }
                else
                {
                    PlaceLimitStopOrder(order);
                }
            }
        }

        public virtual void CancelOrder(Order order)
        {
            RemoveOrder(order);
            OnOrdersChanged(Orders);
        }

        public virtual void ModifyOrder(Order order, decimal? sl, decimal? tp, bool isServerSide = false)
        {
            if (isServerSide)
            {
                bool exists;
                lock (Orders)
                    exists = Orders.Contains(order);

                if (exists && order.ServerSide)
                {
                    order.SLOffset = sl;
                    order.TPOffset = tp;

                    if (!order.SLOffset.HasValue && !order.TPOffset.HasValue)
                        order.ServerSide = false;

                    OnOrdersUpdated(new List<Order> { order });
                }
                else if (exists && !order.ServerSide)
                {
                    if (order.OpenQuantity > 0)
                    {
                        if (order.SLOffset.HasValue || order.TPOffset.HasValue)
                        {
                            lock (_stops)
                            {
                                if (_stops.ContainsKey(order.UserID))
                                    _stops.Remove(order.UserID);

                                if (sl.HasValue || tp.HasValue)
                                    _stops.Add(order.UserID, new Tuple<decimal, decimal>(sl ?? 0m, tp ?? 0m));
                            }
                        }
                        else
                        {
                            order.SLOffset = sl;
                            order.TPOffset = tp;
                            order.ServerSide = sl.HasValue || tp.HasValue;
                            OnOrdersUpdated(new List<Order> { order });
                        }
                    }
                    else if (order.OrderType != OrderType.Market)
                    {
                        CancelOrder(order);
                        var copy = order.Clone();
                        copy.ServerSide = true;
                        copy.SLOffset = sl;
                        copy.TPOffset = tp;
                        PlaceOrder(copy);
                    }
                }
            }
            else  //not isServerSide
            {
                if (order.OpenQuantity > 0)
                {
                    order.ServerSide = false;
                    if (!sl.HasValue && !tp.HasValue)
                    {
                        order.SLOffset = null;
                        order.TPOffset = null;
                        OnOrdersUpdated(new List<Order> { order });
                    }
                }
                else
                {
                    CancelOrder(order);
                    order.ServerSide = false;
                    order.SLOffset = sl;
                    order.TPOffset = tp;
                    PlaceOrder(order.Clone());
                }
            }
        }
        public void ClosePosition(string symbol)
        {
            var positions = Positions.Where(p => p.Symbol == symbol);

            foreach (var position in positions)
            {
                if (position.Quantity <= 0) continue;
                var order = new Order
                {
                    Symbol = position.Symbol,
                    AccountId = position.AccountId,
                    OrderSide = position.PositionSide == Side.Buy ? Side.Sell : Side.Buy,
                    Quantity = position.Quantity,
                    TimeInForce = TimeInForce.FillOrKill,
                    OrderType = OrderType.Market
                };

                PlaceOrder(order);
            }
        }

        public void CloseAllPositions()
        {
            IEnumerable<Position> closedPositions = null;
            lock (Positions)
            {
                closedPositions = Positions.ToList();
            }

            foreach (var position in closedPositions)
            {
                if (position.Quantity == 0) continue;
                var order = new Order
                {
                    Symbol = position.Symbol,
                    AccountId = position.AccountId,
                    OrderSide = position.PositionSide == Side.Buy ? Side.Sell : Side.Buy,
                    Quantity = Math.Abs(position.Quantity),
                    TimeInForce = TimeInForce.FillOrKill,
                    OrderType = OrderType.Market
                };

                PlaceOrder(order);
            }
        }

        #endregion
        
        #region Proccess Helper Methods

        private void CheckOrders()
        {
            lock (Orders)
            {
                if (Orders.Count == 0) return;
                Orders.RemoveAll(o => !o.ServerSide && o.FilledQuantity == o.Quantity);
            }
        }

        protected abstract void PlaceMarketOrder(Order order);

        protected abstract void PlaceLimitStopOrder(Order order);

        private void CalculateProfits(Security security, decimal bid, decimal ask)
        {
            var currencyBase = GetCurrencyRate(security.BaseCurrency, AccountInfo.Currency);
            List<Order> orders;
            lock (Orders)
                orders = Orders.Where(q => q.Symbol.Equals(security.Symbol, IgnoreCase)).ToList();

            if (orders.Count > 0)
            {
                var passedCount = 0;
                foreach (var order in orders)
                {
                    CalculateOrderUpl(order, security.ContractSize, currencyBase, bid, ask);
                    if (EvaluatePending(order))
                        passedCount++;
                    EvaluateStopProfits(order);
                }

                if (passedCount > 0)
                    OnOrdersChanged(Orders);

                OnOrdersUpdated(orders);
            }

            Position position;
            lock (Positions)
            {
                position = Positions.FirstOrDefault(q => q.Symbol.Equals(security.Symbol, IgnoreCase));
                if (position != null)
                    CalculatePositionUpl(position, security.ContractSize, currencyBase, bid, ask, security.MarginRate);
            }

            if (position != null)
                OnPositionUpdated(position);

            UpdateAccount();
        }

        protected virtual void UpdateAccount()
        {
            lock (Positions)
            {
                AccountInfo.Margin = Positions.Sum(p => p.Margin);
                AccountInfo.Profit = Positions.Sum(p => p.Profit);
            }
            AccountInfo.Equity = AccountInfo.Balance - AccountInfo.Margin + AccountInfo.Profit;

            OnAccountStateChanged();
        }

        private bool EvaluatePending(Order order)
        {
            if (!order.ServerSide || order.OrderType == OrderType.Market)
                return false;

            //check TimeInForce
            if(order.TimeInForce == TimeInForce.GoodForDay && order.OpenDate.Date != DateTime.UtcNow.Date)
            {
                RemoveOrder(order);
                OnOrderRejected(order, "Order is expired");
                return true;
            }

            var evaluated = false;
            if (order.OrderSide == Side.Buy)
            {
                if (order.OrderType == OrderType.Limit)
                    evaluated = order.Price >= order.CurrentPrice;
                if (order.OrderType == OrderType.Stop)
                    evaluated = order.Price <= order.CurrentPrice;
            }
            else
            {
                if (order.OrderType == OrderType.Limit)
                    evaluated = order.Price <= order.CurrentPrice;
                if (order.OrderType == OrderType.Stop)
                    evaluated = order.Price >= order.CurrentPrice;
            }

            if (!evaluated)
                return false;

            var tmp = order.Clone();
            tmp.OrderType = OrderType.Market;

            RemoveOrder(order);

            PlaceOrder(tmp);
            return true;
        }

        private void EvaluateStopProfits(Order order)
        {
            if (!order.ServerSide || order.Quantity != order.FilledQuantity + order.CancelledQuantity)
                return;

            var evaluated = false;
            if (order.SLOffset.HasValue && order.SLOffset.Value > 0)
            {
                if (order.OrderSide == Side.Buy)
                {
                    if (order.AvgFillPrice - order.SLOffset >= order.CurrentPrice)
                        evaluated = true;
                }
                else
                {
                    if (order.AvgFillPrice + order.SLOffset <= order.CurrentPrice)
                        evaluated = true;
                }
            }

            if (!evaluated && order.TPOffset.HasValue && order.TPOffset.Value > 0)
            {
                if (order.OrderSide == Side.Buy)
                {
                    if (order.AvgFillPrice + order.TPOffset <= order.CurrentPrice)
                        evaluated = true;

                }
                else
                {
                    if (order.AvgFillPrice - order.TPOffset >= order.CurrentPrice)
                        evaluated = true;
                }
            }

            if (evaluated)
            {
                var opposite = new Order(DateTime.UtcNow.Ticks.ToString(), order.Symbol)
                {
                    BrokerName = Name,
                    OrderSide = order.OrderSide == Side.Buy ? Side.Sell : Side.Buy,
                    Quantity = order.OpenQuantity,
                    TimeInForce = TimeInForce.FillOrKill,
                    OrderType = OrderType.Market,
                    OpenDate = DateTime.UtcNow
                };

                order.ServerSide = false;
                order.SLOffset = null;
                order.TPOffset = null;

                OnOrdersUpdated(new List<Order> { order });
                PlaceOrder(opposite);
            }
        }

        protected virtual string ValidateOrder(Order order)
        {
            if (!IsStarted)
                return "Trading is not available";

            if (order == null)
                return "Order is empty";

            if (string.IsNullOrWhiteSpace(order.Symbol))
                return "Symbol is empty";

            if (order.Quantity == 0M)
                return "Quantity is zero";

            if ((order.OrderType == OrderType.Limit || order.OrderType == OrderType.Stop)
                && order.Price == 0M)
            {
                return "Invalid stop/limit price";
            }

            if (!Securities.Any(p => p.Symbol.Equals(order.Symbol, IgnoreCase)))
                return "Invalid or not supported order symbol";

            return null;
        }

        protected decimal GetCurrencyRate(Security security) =>
            security == null ? 1m : GetCurrencyRate(security.BaseCurrency, AccountInfo.Currency);
        
        protected decimal GetCurrencyRate(string from, string to)
        {
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
                return 1m;

            from = from.ToUpper();
            to = to.ToUpper();

            if (from == to)
                return 1m;

            lock (_ticks)
            {
                if (_ticks.TryGetValue(from + "/" + to, out var price) && price > 0m)
                    return price;

                if (_ticks.TryGetValue(to + "/" + from, out price) && price > 0m)
                    return 1m / price;
            }

            if (to != "USD" && to != "GBP" && to != "EUR")
            {
                var rate = GetCurrencyRate(from, "USD");
                var price = GetPrice(to + "/USD");
                if (rate != 1m && price > 0m)
                    return price / rate;
            }

            if (to != "USD" && to != "GBP" && to != "EUR")
            {
                var rate = GetCurrencyRate(from, "GBP");
                var price = GetPrice("GBP/" + to);
                if (rate != 1m && price > 0m)
                    return price / rate;
            }

            if (to != "USD" && to != "GBP" && to != "EUR")
            {
                var rate = GetCurrencyRate(from, "EUR");
                var price = GetPrice("EUR/" + to);
                if (rate != 1m && price != 0m)
                    return price / rate;
            }

            return 1m;
        }

        private void AddOrder(Order order)
        {
            lock (Orders)
            {
                if (order == null || Orders.Contains(order))
                    return;

                Orders.Add(order);
                Orders.RemoveAll(o => !o.ServerSide && o.FilledQuantity == o.Quantity);
            }
        }
        
        private Tuple<decimal, decimal> GetOrderShares(string symbol, decimal quantity, Side side)
        {
            var posSize = 0m;
            var isShortPosition = false;
            lock (Positions)
            {
                var pos = Positions.FirstOrDefault(p => p.Symbol == symbol);
                if (pos != null)
                {
                    posSize = Math.Abs(pos.Quantity);
                    isShortPosition = pos.PositionSide == Side.Sell;
                }
            }

            var qty = Math.Abs(quantity);
            if (posSize == 0)
                return new Tuple<decimal, decimal>(qty, 0M);

            var absDiff = posSize < qty ? Math.Abs(posSize - qty) : 0M;
            if ((!isShortPosition && side == Side.Sell) || (isShortPosition && side == Side.Buy))  //opposite order
                return new Tuple<decimal, decimal>(absDiff, qty - absDiff);
            else
                return new Tuple<decimal, decimal>(qty, 0M);
        }

        private void ProcessOrderThroughFifoSystem(Order order)
        {
            List<Order> oppositeOrders;
            lock (Orders)
            {
                oppositeOrders = Orders.Where(p => p.Symbol.Equals(order.Symbol, IgnoreCase)
                    && p.OrderSide != order.OrderSide && p.OpenQuantity > 0).ToList();
            }

            if (oppositeOrders.Count == 0)
            {
                OnOrdersChanged(Orders);
                return;
            }

            var totalQty = Math.Abs(order.OpenQuantity);
            oppositeOrders.Sort((a, b) => a.OpenDate.CompareTo(b.OpenDate));

            if (_ordersForUpdate.Count > 0)
            {
                var tmp = oppositeOrders.Where(o => _ordersForUpdate.Contains(o.BrokerID)).ToList();
                tmp.AddRange(oppositeOrders.Where(o => !_ordersForUpdate.Contains(o.BrokerID)));
                oppositeOrders.Clear();
                oppositeOrders = tmp;
            }

            foreach (var opposite in oppositeOrders)
            {
                if (totalQty <= 0)
                    break;

                if (totalQty >= opposite.OpenQuantity)
                {
                    totalQty -= opposite.OpenQuantity;

                    opposite.OpenQuantity = 0;

                    if (!opposite.IsActive)
                    {
                        RemoveOrder(opposite);
                    }
                }
                else
                {
                    var leftProfit = opposite.Profit * ((opposite.OpenQuantity - totalQty) / opposite.OpenQuantity);
                    opposite.Commission = opposite.Commission * ((opposite.OpenQuantity - totalQty) / opposite.OpenQuantity);
                    opposite.Profit = leftProfit;
                    opposite.OpenQuantity -= totalQty;
                    totalQty = 0;
                }
            }

            if (totalQty > 0)
            {
                order.OpenQuantity = totalQty;
            }
            else
            {
                order.OpenQuantity = 0;
                if (!order.IsActive)
                {
                    RemoveOrder(order);
                }
            }

            OnOrdersChanged(Orders);
        }

        private void OnNewTick(Tick tick)
        {
            if (IsStarted)
            {
                lock (_ticks)
                    _ticks[tick.Symbol.Symbol] = tick.Ask;

                ThreadPool.QueueUserWorkItem(state => CalculateProfits(tick.Symbol, tick.Bid, tick.Ask));
            }
        }

        private static decimal GetPositionQuantity(Position pos)
        {
            var qty = pos.Quantity;
            if (pos.PositionSide == Side.Buy && qty < 0M || pos.PositionSide == Side.Sell && qty > 0M)
                qty = -qty;
            return qty;
        }
        
        #endregion //Proccess Helper Methods

        #region Processing Status Changes

        protected void ProcessOrderUpdate(Order order)
        {
            lock (Orders)
            {
                var existingOrder = Orders.FirstOrDefault(i => i.BrokerID == order.BrokerID);
                if (existingOrder == null)
                {
                    if (order.IsActive)
                    {
                        order.OpenDate = DateTime.UtcNow;
                        Orders.Add(order);
                        OnOrdersChanged(Orders);
                    }
                }
                else
                {
                    if (existingOrder.Quantity != order.Quantity
                        || existingOrder.FilledQuantity != order.FilledQuantity
                        || existingOrder.CancelledQuantity != order.CancelledQuantity
                        || existingOrder.OpenQuantity != order.OpenQuantity)
                    {
                        Orders.Remove(existingOrder);
                        if (order.IsActive)
                            Orders.Add(order);
                        OnOrdersChanged(Orders);
                    }
                }
            }

            if (!order.IsActive)
                OnNewHistoricalOrder(order, order.CancelledQuantity > 0 ? Status.Cancelled : Status.Filled);
        }

        protected void ProcessOrderExecution(Order trade)
        {
            if (string.IsNullOrEmpty(trade.BrokerID))
                trade.BrokerID = trade.UserID;

            Order existingOrder = GetOrder(trade.BrokerID);

            var security = Securities.FirstOrDefault(i => i.Symbol.Equals(trade.Symbol, IgnoreCase));
            var currencyBase = GetCurrencyRate(security?.BaseCurrency, AccountInfo.Currency);

            if (existingOrder == null)
            {
                if (security == null)
                {
                    Logger.Warning($"Unable to process #{trade.BrokerID} order execution: no security for {trade.Symbol}");
                    return;
                }

                existingOrder = trade.Clone();
                existingOrder.SetAbsValuesForQuantities();

                existingOrder.OpenDate = long.TryParse(trade.UserID, out var value) 
                    ? new DateTime(value) //M4 generates Order.UserID using DateTime.Now.Ticks
                    : DateTime.Now; //Generated from other source (Broker's side etc.)

                existingOrder.Commission *= currencyBase;
                existingOrder.AccountId = AccountInfo.ID;
                existingOrder.BrokerName = AccountInfo.BrokerName;
                existingOrder.DataFeedName = DataFeedName;

                var shares = GetOrderShares(security.Symbol, trade.Quantity, trade.OrderSide);
                existingOrder.OpeningQty = shares.Item1;
                existingOrder.ClosingQty = shares.Item2;

                lock (_stops)
                {
                    if (_stops.ContainsKey(existingOrder.UserID))
                    {
                        existingOrder.ServerSide = true;
                        var stop = _stops[existingOrder.UserID];
                        existingOrder.SLOffset = stop.Item1;
                        existingOrder.TPOffset = stop.Item2;
                        _stops.Remove(existingOrder.UserID);
                    }
                }

                AddOrder(existingOrder);
            }
            else  //update existing order
            {
                if (trade.Quantity != 0)  //! current trade quantity (not total order quantity)
                {
                    if (security != null)
                    {
                        var shares = GetOrderShares(security.Symbol, trade.Quantity, trade.OrderSide);
                        existingOrder.OpeningQty += shares.Item1;
                        existingOrder.ClosingQty += shares.Item2;
                    }

                    existingOrder.AvgFillPrice = (existingOrder.FilledQuantity * existingOrder.AvgFillPrice
                        + Math.Abs(trade.Quantity) * trade.Price)
                        / (existingOrder.FilledQuantity + Math.Abs(trade.Quantity));
                    existingOrder.Quantity += Math.Abs(trade.Quantity);
                    existingOrder.FilledQuantity = Math.Abs(trade.FilledQuantity);
                    existingOrder.OpenQuantity += Math.Abs(trade.OpenQuantity);
                    existingOrder.Commission = trade.Commission * currencyBase;
                }
                else if (trade.CancelledQuantity != 0)
                {
                    existingOrder.CancelledQuantity = trade.CancelledQuantity;
                    var cancelled = existingOrder.Clone();
                    cancelled.CancelledQuantity = Math.Abs(trade.CancelledQuantity);
                    cancelled.OpenDate = DateTime.UtcNow;
                    cancelled.FilledQuantity = 0;
                    cancelled.AvgFillPrice = 0;
                    OnNewHistoricalOrder(cancelled, Status.Cancelled);
                }
            }

            if (trade.FilledQuantity != 0)
            {
                var filled = existingOrder.Clone();
                filled.FilledQuantity = Math.Abs(trade.FilledQuantity);
                filled.OpenDate = DateTime.UtcNow;
                filled.AvgFillPrice = trade.AvgFillPrice;
                filled.CancelledQuantity = 0;
                OnNewHistoricalOrder(filled, Status.Filled);
                ProcessOrderThroughFifoSystem(existingOrder);
            }
            else if (!existingOrder.IsActive)
            {
                RemoveOrder(existingOrder);
                OnOrdersChanged(Orders);
            }

            _ordersForUpdate.Clear();
        }

        protected void ProcessOrderChange(Order orderChangeDetails)
        {
            Order existingOrder = GetOrder(orderChangeDetails.BrokerID);

            if (existingOrder == null)
            {
                if (orderChangeDetails.OrderType != OrderType.Market)
                {
                    var security = Securities.FirstOrDefault(i => i.Symbol.Equals(orderChangeDetails.Symbol, IgnoreCase));
                    var currencyBase = GetCurrencyRate(security?.BaseCurrency, AccountInfo.Currency);

                    var newOrder = orderChangeDetails.Clone();
                    newOrder.SetAbsValuesForQuantities();
                    newOrder.Commission *= currencyBase;
                    newOrder.AccountId = AccountInfo.ID;
                    newOrder.BrokerName = AccountInfo.BrokerName;
                    newOrder.DataFeedName = DataFeedName;

                    if (security != null)
                    {
                        var shares = GetOrderShares(security.Symbol, orderChangeDetails.Quantity, orderChangeDetails.OrderSide);
                        newOrder.OpeningQty = shares.Item1;
                        newOrder.ClosingQty = shares.Item2;
                    }

                    AddOrder(newOrder);
                    ProcessOrderThroughFifoSystem(newOrder);
                }
            }
            else
            {
                if (existingOrder.TPOffset != orderChangeDetails.TPOffset
                    || existingOrder.SLOffset != orderChangeDetails.SLOffset)
                {
                    lock (_stops)
                    {
                        if (_stops.ContainsKey(existingOrder.UserID))
                        {
                            existingOrder.ServerSide = true;
                            existingOrder.SLOffset = _stops[existingOrder.UserID].Item1;
                            existingOrder.TPOffset = _stops[existingOrder.UserID].Item2;
                            _stops.Remove(existingOrder.UserID);
                        }
                        else
                        {
                            existingOrder.SLOffset = orderChangeDetails.SLOffset;
                            existingOrder.TPOffset = orderChangeDetails.TPOffset;
                            existingOrder.ServerSide = false;
                        }
                    }

                    OnOrdersUpdated(new List<Order> { existingOrder });
                }
                else
                {
                    _ordersForUpdate.Add(orderChangeDetails.BrokerID);
                }
            }
        }

        protected void ProcessPositionUpdate(Position positionDetails)
        {
            Position existingPosition;
            lock (Positions)
                existingPosition = Positions.FirstOrDefault(p => p.Symbol.Equals(positionDetails.Symbol, IgnoreCase));

            if (existingPosition == null)
            {
                existingPosition = positionDetails.Clone();
                existingPosition.AccountId = AccountInfo.ID;
                existingPosition.BrokerName = AccountInfo.BrokerName;
                existingPosition.DataFeedName = DataFeedName;
                if (existingPosition.Quantity != 0)
                {
                    lock (Positions)
                        Positions.Add(existingPosition);
                }
                else
                {
                    return;
                }
            }
            else
            {
                existingPosition.Quantity = GetPositionQuantity(positionDetails);
                existingPosition.PositionSide = positionDetails.PositionSide;
                existingPosition.Price = positionDetails.Price;

                if (existingPosition.Quantity == 0)
                {
                    lock (Positions)
                        Positions.Remove(existingPosition);

                    List<Order> orders;
                    lock (Orders)
                    {
                        orders = Orders.Where(p => p.Symbol.Equals(existingPosition.Symbol, IgnoreCase)
                            && p.OpenQuantity > 0).ToList();
                    }

                    if (orders.Count > 0)
                    {
                        foreach (var order in orders)
                        {
                            order.OpenQuantity = 0;
                            if (!order.IsActive)
                            {
                                RemoveOrder(order);
                            }
                        }

                        OnOrdersChanged(Orders);
                    }
                }
                List<Position> positionList;
                lock (Positions)
                {
                    positionList = Positions.ToList();
                }
                OnPositionsChanged(positionList);

                return;
            }

            var posQty = GetPositionQuantity(existingPosition);
            decimal ordersQty;
            lock (Orders)
            {
                ordersQty = Orders
                    .Where(p => p.Symbol.Equals(existingPosition.Symbol, IgnoreCase))
                    .Sum(p => (p.OpenQuantity * (p.OrderSide == Side.Buy ? 1 : -1)));
            }

            if (posQty != ordersQty)
            {
                var diff = posQty - ordersQty;
                var side = diff > 0 ? Side.Buy : Side.Sell;
                var shares = GetOrderShares(existingPosition.Symbol, diff, side);
                var order = new Order(DateTime.UtcNow.Ticks.ToString(), existingPosition.Symbol)
                {
                    AccountId = AccountInfo.ID,
                    BrokerName = AccountInfo.BrokerName,
                    OrderSide = side,
                    Quantity = Math.Abs(diff),
                    OpenQuantity = Math.Abs(diff),
                    FilledQuantity = Math.Abs(diff),
                    CancelledQuantity = 0,
                    OpeningQty = shares.Item1,
                    ClosingQty = shares.Item2
                };

                AddOrder(order);
                ProcessOrderThroughFifoSystem(order);
            }

            List<Position> positionList1;
            lock (Positions)
            {
                positionList1 = Positions.ToList();
            }
            OnPositionsChanged(positionList1);
        }

        #endregion

        #region Static Calculations

        private static void CalculateOrderUpl(Order order, decimal contractSize, decimal currencyBase, decimal bid, decimal ask)
        {
            if (order.OpenQuantity > 0)
            {
                if (order.OrderSide == Side.Buy)
                {
                    order.Profit = (bid - order.AvgFillPrice) * contractSize * order.OpenQuantity * currencyBase;
                    order.PipProfit = (bid - order.AvgFillPrice) * contractSize * order.OpenQuantity * 10;
                    order.CurrentPrice = bid;
                }
                else
                {
                    order.Profit = (order.AvgFillPrice - ask) * contractSize * order.OpenQuantity * currencyBase;
                    order.PipProfit = (order.AvgFillPrice - ask) * contractSize * order.OpenQuantity * 10;
                    order.CurrentPrice = ask;
                }
            }
            else
            {
                order.CurrentPrice = order.OrderSide == Side.Buy ? ask : bid;
            }
        }

        private static void CalculatePositionUpl(Position position, decimal contractSize, decimal currencyBase, decimal bid, decimal ask, decimal securityMargin)
        {
            var absPosQty = Math.Abs(position.Quantity);
            if (position.PositionSide == Side.Buy)
            {
                position.Profit = (bid - position.Price) * contractSize * absPosQty * currencyBase;
                position.PipProfit = (bid - position.Price) * contractSize * absPosQty * 10;
                position.CurrentPrice = bid;
            }
            else
            {
                position.Profit = (position.Price - ask) * contractSize * absPosQty * currencyBase;
                position.PipProfit = (position.Price - ask) * contractSize * absPosQty * 10;
                position.CurrentPrice = ask;
            }
            position.Margin = CalculateMargin( position.Quantity, position.CurrentPrice,  contractSize , currencyBase ,  securityMargin);
        }

        protected static decimal CalculateMargin(decimal quantity, decimal price, decimal contractSize, decimal currencyBase, decimal symbolMargin) =>
            Math.Abs(quantity) * price * contractSize * currencyBase * symbolMargin * 0.01M;  //0.01M - convert margin from percent

        #endregion

        #region Event Invokators

        protected void OnAccountStateChanged() =>
            AccountStateChanged?.Invoke(this, EventArgs.Empty);

        protected void OnError(string error) =>
            Error?.Invoke(this, new EventArgs<string>(error));

        protected void OnNewHistoricalOrder(Order order, Status status) =>
            NewHistoricalOrder?.Invoke(this, new EventArgs<Order, Status>(order, status));

        protected void OnOrderRejected(Order order, string message = "") =>
            OrderRejected?.Invoke(this, new OrderRejectedEventArgs(order, message));

        protected void OnOrdersChanged(List<Order> orders) =>
            OrdersChanged?.Invoke(this, new EventArgs<List<Order>>(orders));

        protected void OnOrdersUpdated(List<Order> orders) =>
            OrdersUpdated?.Invoke(this, new EventArgs<List<Order>>(orders));

        protected void OnPositionUpdated(Position position) =>
            PositionUpdated?.Invoke(this, new EventArgs<Position>(position));

        protected void OnPositionsChanged(List<Position> positions) =>
            PositionsChanged?.Invoke(this, new EventArgs<List<Position>>(positions));

        #endregion

        #region Helper Methods

        protected Order GetOrder(string brokerID)
        {
            lock (Orders)
                return Orders.FirstOrDefault(i => i.BrokerID == brokerID);
        }

        protected void RemoveOrder(Order order)
        {
            lock (Orders)
                Orders.Remove(order);
        }

        protected Position GetPosition(string symbol)
        {
            lock (Positions)
                return Positions.FirstOrDefault(i => i.Symbol.Equals(symbol, IgnoreCase));
        }

        protected Position GetOrCreatePosition(Order order)
        {
            return GetPosition(order.Symbol) ?? new Position
            {
                AccountId = order.AccountId,
                BrokerName = Name,
                PositionSide = order.OrderSide,
                Symbol = order.Symbol
            };
        }

        protected Security GetSecurity(string symbol) =>
            Securities.FirstOrDefault(i => i.Symbol.Equals(symbol, IgnoreCase));


        protected decimal GetPrice(string symbol)
        {
            lock (_ticks)
                return _ticks.ContainsKey(symbol) ? _ticks[symbol] : 0m;
        }

        #endregion //Helper Methods
    }
}