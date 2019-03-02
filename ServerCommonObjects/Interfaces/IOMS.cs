/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using CommonObjects;
using ServerCommonObjects;
using ServerCommonObjects.Classes;
using ServerCommonObjects.Interfaces;

namespace OMS
{
    public interface IOMS
    {
        event EventHandler<EventArgs<IUserInfo, AccountInfo>> AccountStateChanged;
        event EventHandler<EventArgs<UserAccount, List<Order>>> OrdersChanged;
        event EventHandler<EventArgs<UserAccount, List<Order>>> OrdersUpdated;
        event EventHandler<EventArgs<UserAccount, Order>> HistoricalOrderAdded;
        event EventHandler<EventArgs<UserAccount, Position>> PositionUpdated;
        event EventHandler<EventArgs<UserAccount, List<Position>>> PositionsChanged;
        event EventHandler<UserOrderRejectedEventArgs> OrderRejected;
        bool IsStarted { get; }

        /// <summary>
        /// Throw exception in case of invalid credentials or some other errors
        /// </summary>
        /// <param name="user"></param>
        void AddTrader(IUserInfo userInfo, List<AccountInfo> accounts,
            out Dictionary<AccountInfo, string> failedAccounts);

        void AddActiveSignal(IUserInfo user, string path);
        void RemoveActiveSignal(IUserInfo user, string path);
        AccountInfo GetAccountById(IUserInfo user, string accountId);

        void BrokerAccountsLogout(IUserInfo info, List<AccountInfo> accounts);
        List<Security> GetAvailableSecurities(AccountInfo account);
        void Start(List<IDataFeed> dataFeeds, string connectionString);
        void Stop();
        IUserInfo GetUserByName(string name);
        List<Order> GetOrders(IUserInfo userInfo);
        List<Order> GetOrdersHistory(IUserInfo userInfo, int count, int skip);
        void PlaceOrder(Order order, IUserInfo info, AccountInfo account);
        void CancelOrder(string orderId, IUserInfo info, AccountInfo account);
        void ModifyOrder(string orderId, IUserInfo info, AccountInfo account, decimal? SL, decimal? TP, bool isServerSide);
        List<Position> GetPositions(AccountInfo account);
        List<Position> GetPositions(AccountInfo account, string symbol);
        void ClosePosition(AccountInfo account, string symbol);
        void CloseAllPositions(AccountInfo account);
    }
}