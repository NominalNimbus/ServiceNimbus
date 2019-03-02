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
using System.Runtime.Remoting.Lifetime;
using CommonObjects;
using ServerCommonObjects;
using ServerCommonObjects.Classes;
using ServerCommonObjects.Enums;
using ServerCommonObjects.Interfaces;
using ServerCommonObjects.SQL;

namespace OMS
{
    public class OrderManager : MarshalByRefObject, IOMS
    {
        private readonly DBOrders _dbOrders;
        private readonly List<IUserInfo> _users;
        private readonly List<IBroker> _brokers;
        private readonly List<IDataFeed> _dataFeeds;
        private readonly Dictionary<string, List<string>> _userActiveSignals;
        private readonly Dictionary<string, string> _ordersFromSignals;

        public bool IsStarted { get; private set; }

        public event EventHandler<EventArgs<IUserInfo, AccountInfo>> AccountStateChanged;
        public event EventHandler<EventArgs<UserAccount, List<Order>>> OrdersChanged;
        public event EventHandler<EventArgs<UserAccount, List<Order>>> OrdersUpdated;
        public event EventHandler<EventArgs<UserAccount, Order>> HistoricalOrderAdded;
        public event EventHandler<EventArgs<UserAccount, Position>> PositionUpdated;
        public event EventHandler<EventArgs<UserAccount, List<Position>>> PositionsChanged;
        public event EventHandler<UserOrderRejectedEventArgs> OrderRejected;

        public OrderManager()
        {
            _dbOrders = new DBOrders();
            _brokers = new List<IBroker>();
            _users = new List<IUserInfo>();
            _dataFeeds = new List<IDataFeed>();
            _userActiveSignals = new Dictionary<string, List<string>>();
            _ordersFromSignals = new Dictionary<string, string>();
        }

        #region Public Methods

        public void Start(List<IDataFeed> dataFeeds, string connectionString)
        {
            IsStarted = true;
            _dataFeeds.Clear();
            _dataFeeds.AddRange(dataFeeds);
            _dbOrders.Start(connectionString);
        }

        public void Stop()
        {
            _dataFeeds.Clear();

            lock (_brokers)
            {
                foreach (var broker in _brokers)
                {
                    UnsubscribeBrokerEvents(broker);
                    broker.Stop();
                }

                _brokers.Clear();
            }

            lock (_users)
                _users.Clear();

            _dbOrders.Stop();
            IsStarted = false;
        }

        public void AddTrader(IUserInfo userInfo, List<AccountInfo> accounts,
            out Dictionary<AccountInfo, string> failedAccounts)
        {
            failedAccounts = new Dictionary<AccountInfo, string>();  //account -> description

            lock (_brokers)
            {
                var accountsToAdd = new List<AccountInfo>();
                var tmpCreatedBrokers = new Dictionary<IBroker, AccountInfo>();
                var tmpLoggedIn = new Dictionary<IBroker, AccountInfo>();
                var activeBrokers = new List<IBroker>();

                foreach (var account in accounts)
                {
                    var broker = GetBroker(account);
                    if (broker == null)
                    {
                        broker = BrokerFactory.CreateBrokerInstance(account.BrokerName, account.DataFeedName, userInfo.Login,  _dataFeeds);
                        if (broker == null)
                        {
                            failedAccounts[account]
                                = $"Failed to initialize {account.BrokerName} broker for {account.UserName} user";
                            continue;
                        }

                        account.DataFeedName = broker.DataFeedName;
                        account.BalanceDecimals = broker.AccountInfo.BalanceDecimals;
                        tmpCreatedBrokers.Add(broker, account);
                    }
                    else
                    {
                        account.DataFeedName = broker.DataFeedName;
                        account.BalanceDecimals = broker.AccountInfo.BalanceDecimals;
                        accountsToAdd.Add(broker.AccountInfo);
                        activeBrokers.Add(broker);
                    }
                }

                foreach (var broker in tmpCreatedBrokers)
                {
                    if (failedAccounts.ContainsKey(broker.Value))
                        continue;

                    try
                    {
                        broker.Key.Login(broker.Value);
                    }
                    catch (Exception e)
                    {
                        var msg = e.Message;
                        System.Diagnostics.Trace.TraceInformation(msg + ": " + e.Message);
                        failedAccounts[broker.Value] = msg;
                        continue;
                    }

                    tmpLoggedIn.Add(broker.Key, broker.Value);
                }

                lock (_users)
                {
                    if (!_users.Contains(userInfo))
                    {
                        _users.Add(userInfo);
                    }
                    else
                    {
                        var indexOf = _users.IndexOf(userInfo);
                        if (indexOf != -1)
                        {
                            if (!ReferenceEquals(_users[indexOf], userInfo))
                            {
                                _users.RemoveAt(indexOf);
                                _users.Add(userInfo);
                            }
                        }
                    }
                }

                foreach (var brokerAccountPair in tmpLoggedIn)
                {
                    if (!userInfo.Accounts.Contains(brokerAccountPair.Key.AccountInfo))
                        userInfo.Accounts.Add(brokerAccountPair.Key.AccountInfo);
                    _brokers.Add(brokerAccountPair.Key);
                    SubscribeBrokerEvents(brokerAccountPair.Key);
                    brokerAccountPair.Key.Start();
                }

                foreach (var acc in accountsToAdd)
                {
                    if (!userInfo.Accounts.Contains(acc))
                        userInfo.Accounts.Add(acc);
                }

                _dbOrders.AddUserActivity(userInfo);

                foreach (var broker in activeBrokers.Concat(tmpLoggedIn.Keys))
                {
                    if (broker.Orders.Count > 0)
                        RaiseOrdersChanged(userInfo, broker.AccountInfo, broker.Orders);
                    if (broker.Positions.Count > 0)
                        RaisePositionsChanged(userInfo, broker.AccountInfo, broker.Positions);
                }
            }
        }

        public void BrokerAccountsLogout(IUserInfo user, List<AccountInfo> accounts)
        {
            if (_userActiveSignals.ContainsKey(user.Login) && _userActiveSignals[user.Login].Any())
                return;

            lock (_users)
            {
                if (_users.Contains(user) && user.Accounts.All(accounts.Contains))
                    _users.Remove(user);
            }

            foreach (var account in accounts)
            {
                var broker = GetBroker(account);
                if (broker == null)
                    continue;

                //user.Accounts.Remove(account);
                user.Accounts.Remove(broker.AccountInfo);

                var isInUse = _users.Any(p => p.Accounts.Contains(account))
                    || broker.Orders.Any(p => p.ServerSide);

                if (!isInUse)
                {
                    lock (_brokers)
                    {
                        UnsubscribeBrokerEvents(broker);
                        broker.Stop();
                        _brokers.Remove(broker);
                    }
                }
            }
        }

        public void AddActiveSignal(IUserInfo user, string path)
        {
            if (!_userActiveSignals.ContainsKey(user.Login))
                _userActiveSignals.Add(user.Login, new List<string>());

            if (!_userActiveSignals[user.Login].Contains(path))
                _userActiveSignals[user.Login].Add(path);
        }

        public void RemoveActiveSignal(IUserInfo user, string path)
        {
            if (_userActiveSignals.ContainsKey(user.Login))
            {
                _userActiveSignals[user.Login].Remove(path);
                if (!_userActiveSignals[user.Login].Any())
                    _userActiveSignals.Remove(user.Login);
            }
        }

        public AccountInfo GetAccountById(IUserInfo user, string accountId)
        {
            if (_users == null || user == null || string.IsNullOrEmpty(accountId))
                return null;

            lock (_users)
            {
                var idx = _users.IndexOf(user);
                var u = idx >= 0 ? _users[idx] : null;
                if (u?.Accounts != null)
                    return u.Accounts.FirstOrDefault(i => i.ID == accountId);
            }

            return null;
        }

        public List<Security> GetAvailableSecurities(AccountInfo account)
        {
            var broker = GetBroker(account);
            return broker != null && broker.IsStarted ? broker.Securities.ToList() : new List<Security>(0);
        }

        public IUserInfo GetUserByName(string name)
        {
            if (_users == null || string.IsNullOrEmpty(name))
                return null;

            lock (_users)
                return _users.FirstOrDefault(i => i.Login == name);
        }

        public List<Order> GetOrders(IUserInfo user)
        {
            if (user?.Accounts == null || user.Accounts.Count == 0)
                return new List<Order>(0);

            var result = new List<Order>();
            foreach (var account in user.Accounts)
            {
                var broker = GetBroker(account);
                if (broker != null)
                    result.AddRange(broker.Orders);
            }

            return result;
        }

        public List<Order> GetOrdersHistory(IUserInfo userInfo, int countPerSymbol, int skip)
        {
            var result = new List<Order>();
            foreach (var account in userInfo.Accounts.ToList())
            {
                var broker = GetBroker(account);
                if (broker != null)
                    result.AddRange(_dbOrders.GetOrderHistory(broker.AccountInfo, countPerSymbol, skip));
            }
            return result;
        }

        public void PlaceOrder(Order order, IUserInfo user, AccountInfo account)
        {
            if (account == null)
            {
                RaiseOrderRejected(user, order, "Can't place an order: account is NULL");
                return;
            }

            order.AccountId = account.ID;

            var broker = GetBroker(account);
            if (broker == null)
                return;

            if (!broker.IsStarted)
            {
                RaiseOrderRejected(user, order, "Trading is not available now.");
                return;
            }

            var instrument =
                broker.Securities.FirstOrDefault(
                  p => p.Symbol.Equals(order.Symbol, StringComparison.InvariantCultureIgnoreCase));

            if (instrument == null)
            {
                RaiseOrderRejected(user, order, "Internal server error.");
                return;
            }

            if (order.SLOffset.HasValue && order.SLOffset.Value < instrument.PriceIncrement * 10
                || order.TPOffset.HasValue && order.TPOffset.Value < instrument.PriceIncrement * 10)
            {
                RaiseOrderRejected(user, order, "Invalid SL or TP value.");
                return;
            }

            if (!order.SLOffset.HasValue && !order.TPOffset.HasValue && order.OrderType == OrderType.Market && order.ServerSide)
            {
                RaiseOrderRejected(user, order, "You can't specify server side execution for this type of order.");
                return;
            }

            if (order.OrderType != OrderType.Market && order.ServerSide && order.Price == 0)
            {
                RaiseOrderRejected(user, order, "You need to specify price for this type of order.");
                return;
            }

            if (!string.IsNullOrEmpty(order.Origin))
            {
                lock (_ordersFromSignals)
                    _ordersFromSignals[order.UniqueUserId] = order.Origin;
            }

            broker.PlaceOrder(order);
        }

        public void CancelOrder(string orderId, IUserInfo user, AccountInfo account)
        {
            if (account == null)
            {
                RaiseOrderRejected(user, new Order { UserID = orderId }, "Can't cancel an order: account is NULL");
                return;
            }

            var broker = GetBroker(account);
            if (broker == null)
                return;

            if (!broker.IsStarted)
            {
                RaiseOrderRejected(user, new Order { UserID = orderId }, "Trading is not available now.");
                return;
            }

            var order = broker.Orders.FirstOrDefault(p => p.UserID.Equals(orderId));
            if (order != null)
                broker.CancelOrder(order);
        }

        public void ModifyOrder(string orderId, IUserInfo user, AccountInfo account, decimal? SL, decimal? TP, bool isServerSide)
        {
            if (account == null)
            {
                RaiseOrderRejected(user, new Order { UserID = orderId }, "Can't modify an order: account is NULL");
                return;
            }

            var broker = GetBroker(account);
            if (broker == null)
                return;

            if (!broker.IsStarted)
            {
                RaiseOrderRejected(user, new Order { UserID = orderId }, "Trading is not available now.");
                return;
            }

            var order = broker.Orders.FirstOrDefault(p => p.UserID.Equals(orderId));
            if (order == null)
                return;

            var instrument = broker.Securities.FirstOrDefault(
                p => p.Symbol.Equals(order.Symbol, StringComparison.InvariantCultureIgnoreCase));
            if (instrument == null)
            {
                RaiseOrderRejected(user, new Order { UserID = orderId }, "Internal server error.");
                return;
            }

            if (SL.HasValue && SL.Value < instrument.PriceIncrement * 10
                || TP.HasValue && TP.Value < instrument.PriceIncrement * 10)
            {
                RaiseOrderRejected(user, new Order { UserID = orderId }, "Invalid SL or TP.");
                return;
            }

            broker.ModifyOrder(order, SL, TP, isServerSide);
        }

        public List<Position> GetPositions(AccountInfo account)
        {
            var broker = GetBroker(account);
            return broker == null ? new List<Position>(0) : broker.Positions;
        }

        public List<Position> GetPositions(AccountInfo account, string symbol)
        {
            return GetPositions(account).Where(p => p.Symbol == symbol).ToList();
        }

        public void ClosePosition(AccountInfo account, string symbol)
        {
            var broker = GetBroker(account);
            broker.ClosePosition(symbol);
        }

        public void CloseAllPositions(AccountInfo account)
        {
            var broker = GetBroker(account);
            broker.CloseAllPositions();
        }

        public override object InitializeLifetimeService()
        {
            var lease = (ILease)base.InitializeLifetimeService();
            if (lease != null && lease.CurrentState == LeaseState.Initial)
                lease.InitialLeaseTime = TimeSpan.FromDays(256);

            return lease;
        }

        #endregion

        #region Event Invocators

        private void RaiseOrderRejected(IUserInfo user, Order order, string msg)
        {
            if (user != null)
                OrderRejected?.Invoke(this, new UserOrderRejectedEventArgs(user, order, msg));
        }

        private void RaiseOrdersChanged(IUserInfo user, AccountInfo account, List<Order> orders)
        {
            if (user != null)
                OrdersChanged?.Invoke(this, new EventArgs<UserAccount, List<Order>>(new UserAccount(user, account), orders));
        }

        private void RaisePositionUpdated(IUserInfo user, AccountInfo account, Position pos)
        {
            if (user != null)
                PositionUpdated?.Invoke(this, new EventArgs<UserAccount, Position>(new UserAccount(user, account), pos));
        }

        private void RaiseOrdersUpdated(IUserInfo user, AccountInfo account, List<Order> orders)
        {
            if (user != null)
                OrdersUpdated?.Invoke(this, new EventArgs<UserAccount, List<Order>>(new UserAccount(user, account), orders));
        }

        private void RaisePositionsChanged(IUserInfo user, AccountInfo account, List<Position> positions)
        {
            if (user != null)
                PositionsChanged?.Invoke(this, new EventArgs<UserAccount, List<Position>>(new UserAccount(user, account), positions));
        }

        private void RaiseAccountStateChanged(IUserInfo user, AccountInfo account)
        {
            if (user != null)
                AccountStateChanged?.Invoke(this, new EventArgs<IUserInfo, AccountInfo>(user, account));
        }

        private void RaiseHistoricalOrder(IUserInfo user, AccountInfo account, Order order)
        {
            if (user != null)
                HistoricalOrderAdded?.Invoke(this, new EventArgs<UserAccount, Order>(new UserAccount(user, account), order));
        }

        #endregion

        #region Private Methods

        private void BrokerOnOnAccountStateChanged(object sender, EventArgs eventArgs)
        {
            if (sender is IBroker broker)
            {
                foreach (var user in GetUserInfo(broker))
                    RaiseAccountStateChanged(user, broker.AccountInfo);
            }
        }

        private void BrokerOnOrderRejected(object sender, OrderRejectedEventArgs args)
        {
            if (sender is IBroker broker)
            {
                lock (_ordersFromSignals)
                {
                    var id = args.Order.UniqueUserId;
                    if (_ordersFromSignals.ContainsKey(id))
                    {
                        if (string.IsNullOrEmpty(args.Order.Origin))
                            args.Order.Origin = _ordersFromSignals[id];
                        _ordersFromSignals.Remove(id);
                    }
                }

                foreach (var userInfo in GetUserInfo(broker))
                    RaiseOrderRejected(userInfo, args.Order, args.Message);
            }
        }

        private void BrokerOnPositionsChanged(object sender, EventArgs<List<Position>> eventArgs)
        {
            if (sender is IBroker broker)
            {
                foreach (var userInfo in GetUserInfo(broker))
                    RaisePositionsChanged(userInfo, broker.AccountInfo, eventArgs.Value);
            }
        }

        private void BrokerOnPositionUpdated(object sender, EventArgs<Position> args)
        {
            if (sender is IBroker broker)
            {
                foreach (var userInfo in GetUserInfo(broker))
                    RaisePositionUpdated(userInfo, broker.AccountInfo, args.Value);

                CheckBroker(broker);
            }
        }

        private void BrokerOnOrdersUpdated(object sender, EventArgs<List<Order>> eventArgs)
        {
            if (sender is IBroker broker)
            {
                foreach (var userInfo in GetUserInfo(broker))
                    RaiseOrdersUpdated(userInfo, broker.AccountInfo, eventArgs.Value);
            }
        }

        private void BrokerOnOrdersChanged(object sender, EventArgs<List<Order>> eventArgs)
        {
            if (sender is IBroker broker)
            {
                foreach (var userInfo in GetUserInfo(broker))
                    RaiseOrdersChanged(userInfo, broker.AccountInfo, eventArgs.Value);

                CheckBroker(broker);
            }
        }

        private void BrokerOnNewHistoricalOrder(object sender, EventArgs<Order, Status> eventArgs)
        {
            if (sender is IBroker broker)
            {
                var order = eventArgs.Value1;
                lock (_ordersFromSignals)
                {
                    var id = order.UniqueUserId;
                    if (_ordersFromSignals.ContainsKey(id))
                    {
                        if (string.IsNullOrEmpty(order.Origin))
                            order.Origin = _ordersFromSignals[id];
                        if (order.FilledQuantity == order.Quantity || order.CancelledQuantity == order.Quantity)
                            _ordersFromSignals.Remove(id);
                    }
                }

                if (order.PlacedDate != DateTime.MinValue)
                    _dbOrders.AddHistoricalOrder(order, broker.AccountInfo, eventArgs.Value2, true);

                foreach (var userInfo in GetUserInfo(broker))
                    RaiseHistoricalOrder(userInfo, broker.AccountInfo, order);
            }
        }

        private void SubscribeBrokerEvents(IBroker broker)
        {
            if (broker != null)
            {
                broker.AccountStateChanged += BrokerOnOnAccountStateChanged;
                broker.OrderRejected += BrokerOnOrderRejected;
                broker.PositionsChanged += BrokerOnPositionsChanged;
                broker.PositionUpdated += BrokerOnPositionUpdated;
                broker.OrdersChanged += BrokerOnOrdersChanged;
                broker.OrdersUpdated += BrokerOnOrdersUpdated;
                broker.NewHistoricalOrder += BrokerOnNewHistoricalOrder;
            }
        }

        private void UnsubscribeBrokerEvents(IBroker broker)
        {
            if (broker != null)
            {
                broker.AccountStateChanged -= BrokerOnOnAccountStateChanged;
                broker.OrderRejected -= BrokerOnOrderRejected;
                broker.PositionsChanged -= BrokerOnPositionsChanged;
                broker.PositionUpdated -= BrokerOnPositionUpdated;
                broker.OrdersChanged -= BrokerOnOrdersChanged;
                broker.OrdersUpdated -= BrokerOnOrdersUpdated;
                broker.NewHistoricalOrder -= BrokerOnNewHistoricalOrder;
            }
        }

        private IBroker GetBroker(AccountInfo account)
        {
            lock (_brokers)
            {
                return _brokers.FirstOrDefault(p => 
                    p.AccountInfo.BrokerName.Equals(account.BrokerName)
                    && p.AccountInfo.DataFeedName.Equals(account.DataFeedName)
                    && p.AccountInfo.Password.Equals(account.Password)
                    && p.AccountInfo.UserName.Equals(account.UserName)
                    && p.AccountInfo.Account.Equals(account.Account)
                    && p.AccountInfo.Uri.Equals(account.Uri));
            }
        }

        private List<IUserInfo> GetUserInfo(IBroker broker)
        {
            lock (_users)
                return _users.Where(p => p.Accounts.Any(q => q.Equals(broker.AccountInfo))).ToList();
        }

        private void CheckBroker(IBroker broker)
        {
            var users = GetUserInfo(broker);
            if (users.Count > 0 || broker.Orders.Any(p => p.ServerSide))
                return;

            foreach (var user in users)
            {
                if (_userActiveSignals.ContainsKey(user.Login) && _userActiveSignals[user.Login].Any())
                    return;
            }

            try
            {
                lock (_brokers)
                {
                    UnsubscribeBrokerEvents(broker);
                    broker.Stop();
                    _brokers.Remove(broker);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to log broker out", ex);
            }
        }

        #endregion
    }
}