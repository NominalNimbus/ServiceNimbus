/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using ServerCommonObjects;
using Server.Interfaces;

namespace Server.Commands
{
    internal sealed class CancelOrderCommand : CommandBase<CancelOrderRequest>
    {
        #region Constructors

        public CancelOrderCommand(ICore core, IPusher pusher) : base(core, pusher)
        {
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(CancelOrderRequest request)
        {
            var account = Core.OMS.GetAccountById(request.User, request.AccountId);
            Core.OMS.CancelOrder(request.OrderID, request.User, account);
        }

        #endregion // CommandBase
    }
}
