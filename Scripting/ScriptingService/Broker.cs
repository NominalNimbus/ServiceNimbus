/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonObjects;
using Scripting;
using System.Linq;

namespace ScriptingService
{
    public class Broker : MarshalByRefObject, IBroker
    {

        #region Members

        private readonly string _username;
        private readonly TimeSpan _taskTimeOut;
        private readonly Connector _connector;
        private readonly SignalBase _signal;

        public List<PortfolioAccount> AccountInfos { get; set; }
        public List<Portfolio> Portfolios { get; private set; } = new List<Portfolio>();
        public List<AccountInfo> AvailableAccounts { get; private set; } = new List<AccountInfo>();

        #endregion

        #region StartUp

        public Broker(Connector connector, string username, List<PortfolioAccount> accountInfos, SignalBase signal)
        {
            _signal = signal;
            _username = username;
            _connector = connector;
            _taskTimeOut = TimeSpan.FromSeconds(5);
            AccountInfos = accountInfos;

            GetPortfolios();
            GetAccounts();
        }

        private void GetPortfolios()
        {
            var portfoliosTask = _connector.GetPortfolios(_username);
            portfoliosTask.Wait(_taskTimeOut);
            Portfolios = portfoliosTask.Status == TaskStatus.RanToCompletion ? portfoliosTask.Result : new List<Portfolio>();
        }

        private void GetAccounts()
        {
            var accountTask = _connector.GetAccounts(_username);
            accountTask.Wait(_taskTimeOut);

            var accounts = new List<AccountInfo>();
            if(accountTask.Status == TaskStatus.RanToCompletion)
            {
                foreach (var item in AccountInfos)
                {
                    var account = accountTask.Result.FirstOrDefault(a => a.BrokerName == item.BrokerName && 
                                                      a.UserName == item.UserName && a.Account == item.Account);
                    if (account != null)
                        accounts.Add(account);
                }
            }

            AvailableAccounts = accounts;
        }

        #endregion

        #region IBroker

        public List<Security> GetAvailableSecurities(AccountInfo account)
        {
            var securitiesTask = _connector.GetAvailableSecurities(account);
            securitiesTask.Wait(_taskTimeOut);
            return securitiesTask.Status == TaskStatus.RanToCompletion ? securitiesTask.Result : new List<Security>();
        }

        public List<Order> GetOrders(AccountInfo account)
        {
            var ordersTask = _connector.GetOrders(_username, account.ID);
            ordersTask.Wait(_taskTimeOut);
            return ordersTask.Status == TaskStatus.RanToCompletion ? ordersTask.Result : new List<Order>();
        }

        public Order GetOrder(string orderId, AccountInfo account)
        {
            var ordersTask = _connector.GetOrder(_username, account.ID, orderId);
            ordersTask.Wait(_taskTimeOut);
            return ordersTask.Status == TaskStatus.RanToCompletion ? ordersTask.Result : null;
        }

        public void PlaceOrder(OrderParams order, AccountInfo account)
        {
            if (account == null || string.IsNullOrEmpty(order.UserID) || string.IsNullOrEmpty(order.Symbol))
                return;

            if (order.OrderType != OrderType.Market && order.Price <= 0M)
                return;

            switch (_signal.State)
            {
                case SignalState.Running:
                    string origin = null;
                    if (_signal.StrategyParameters != null && _signal.StrategyParameters.StrategyID > 0)
                        origin = _signal.StrategyParameters.StrategyID + "/" + _signal.ShortName;

                    _connector.PlaceOrder(new Order(order.UserID, order.Symbol)
                    {
                        Quantity = order.Quantity,
                        SignalID = order.SignalId,
                        OrderType = order.OrderType,
                        OrderSide = order.OrderSide,
                        ServerSide = order.ServerSide,
                        Price = order.Price,
                        SLOffset = order.SLOffset,
                        TPOffset = order.TPOffset,
                        TimeInForce = order.TimeInForce,
                        Origin = origin,
                        BrokerName = account.BrokerName,
                        AccountId = account.ID
                    }, account, _username);
                    break;
                case SignalState.RunningSimulated:
                    _signal.Alert($"PLACE ORDER: {order}, {_signal.Name} signal "
                                  + $"(broker: {account.BrokerName}, account: {account.UserName})");
                    break;
            }
        }

        public void CancelOrder(string orderId, AccountInfo account)
        {
            if (account == null || string.IsNullOrEmpty(orderId))
                return;

            switch (_signal.State)
            {
                case SignalState.Running:
                    _connector.CancelOrder(orderId, account, _username);
                    break;
                case SignalState.RunningSimulated:
                    _signal.Alert($"CANCEL ORDER #{orderId} of {_signal.Name} signal "
                                  + $"(broker: {account.BrokerName}, account: {account.UserName})");
                    break;
            }
        }

        public void Modify(string orderId, decimal? sl, decimal? tp, bool isServerSide, AccountInfo account)
        {
            if (account == null || string.IsNullOrEmpty(orderId))
                return;

            switch (_signal.State)
            {
                case SignalState.Running:
                    _connector.ModifyOrder(orderId, sl, tp, isServerSide, account, _username);
                    break;
                case SignalState.RunningSimulated:
                    _signal.Alert($"MODIFY ORDER #{orderId} of {_signal.Name} signal "
                                  + $"(broker: {account.BrokerName}, account: {account.UserName})");
                    break;
            }
        }

        public List<Position> GetPositions(AccountInfo account, string symbol)
        {
            var positionsTask = _connector.GetPositions(account, symbol);
            positionsTask.Wait(_taskTimeOut);
            return positionsTask.Status == TaskStatus.RanToCompletion ? positionsTask.Result : new List<Position>();
        }

        public List<Position> GetPositions(AccountInfo account) => GetPositions(account, string.Empty);

        public void ClosePosition(AccountInfo account, string symbol) =>
            _connector.ClosePosition(symbol, account);

        public void CloseAllPositions(AccountInfo account) => ClosePosition(account, string.Empty);

        public override object InitializeLifetimeService()
        {
            var lease = (System.Runtime.Remoting.Lifetime.ILease)base.InitializeLifetimeService();
            if (lease != null && lease.CurrentState == System.Runtime.Remoting.Lifetime.LeaseState.Initial)
                lease.InitialLeaseTime = TimeSpan.FromDays(256);
            return lease;
        }

        public void Dispose()
        {
        }

        #endregion

    }
}