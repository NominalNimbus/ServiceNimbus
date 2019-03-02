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
using ServerCommonObjects.Classes;
using ServerCommonObjects.Enums;

namespace ServerCommonObjects.Interfaces
{
    public interface IBroker
    {
        string Name { get; }
        string DataFeedName { get; }
        bool IsStarted { get; }
        AccountInfo AccountInfo { get; }
        List<Security> Securities { get; }
        List<Order> Orders { get; }
        List<Position> Positions { get; }

        event EventHandler<EventArgs<Order, Status>> NewHistoricalOrder;
        event EventHandler<OrderRejectedEventArgs> OrderRejected;
        event EventHandler<EventArgs<List<Order>>> OrdersUpdated;
        event EventHandler<EventArgs<List<Order>>> OrdersChanged;
        event EventHandler<EventArgs<List<Position>>> PositionsChanged;
        event EventHandler<EventArgs<Position>> PositionUpdated;
        event EventHandler<EventArgs> AccountStateChanged;
        event EventHandler<EventArgs<string>> Error;

        void Login(AccountInfo account);
        void Start();
        void Stop();
        void CancelOrder(Order order);
        void PlaceOrder(Order order);
        void ModifyOrder(Order order, decimal? sl, decimal? tp, bool isServerSide = false);
        void ClosePosition(string symbol);
        void CloseAllPositions();
    }
}
