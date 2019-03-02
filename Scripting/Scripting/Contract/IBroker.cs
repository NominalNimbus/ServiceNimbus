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

namespace Scripting
{
    public interface IBroker : IDisposable
    {
        List<AccountInfo> AvailableAccounts { get; }

        List<Portfolio> Portfolios { get; }

        List<Security> GetAvailableSecurities(AccountInfo account);
        List<Order> GetOrders(AccountInfo account);
        Order GetOrder(string orderId, AccountInfo account);
        void CancelOrder(string orderId, AccountInfo account);
        void PlaceOrder(OrderParams order, AccountInfo account);
        void Modify(string orderId, decimal? sl, decimal? tp, bool isServerSide, AccountInfo account);
        List<Position> GetPositions(AccountInfo account);
        List<Position> GetPositions(AccountInfo account, string symbol);
        void ClosePosition(AccountInfo account, string symbol);
        void CloseAllPositions(AccountInfo account);
    }
}