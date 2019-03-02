/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using System.Linq;
using ServerCommonObjects;
using Server.Interfaces;

namespace Server.Commands.Services
{
    internal sealed class GetOrderCommand : CommandBase<GetOrderRequest>
    {
        #region Constructors

        public GetOrderCommand(ICore core, IPusher pusher) : base(core, pusher)
        {
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(GetOrderRequest request)
        {
            var user = Core.GetUser(request.Username);
            var order = Core.OMS.GetOrders(user).FirstOrDefault(i => i.UserID == request.OrderId && i.AccountId == request.AccountID);
            PushToProcessor(new GetOrderResponse
            {
                ID = request.ID,
                Order = order
            }, request.Processor);
        }

        #endregion // CommandBase
    }
}
