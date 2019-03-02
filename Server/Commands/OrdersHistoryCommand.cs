/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using System;
using ServerCommonObjects;
using Server.Interfaces;

namespace Server.Commands
{
    internal sealed class OrdersHistoryCommand : CommandBase<OrdersListRequest>
    {
        #region Constructors

        public OrdersHistoryCommand(ICore core, IPusher pusher) : base(core, pusher)
        {
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(OrdersListRequest request)
        {
            var count = Math.Max(Math.Min(request.CountPerSymbol, 10000), 1);
            var response = new HistoricalOrdersListResponse
            {
                HistoricalOrders = Core.OMS.GetOrdersHistory(request.User, count, request.Skip),
                User = request.User
            };

            PushResponse(response);
        }

        #endregion // CommandBase
    }
}
