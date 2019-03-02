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
    internal sealed class ModifyOrderCommand : CommandBase<ModifyOrderRequest>
    {
        #region Constructors

        public ModifyOrderCommand(ICore core, IPusher pusher) : base(core, pusher)
        {
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(ModifyOrderRequest request)
        {
            var account = Core.OMS.GetAccountById(request.User, request.AccountId);
            Core.OMS.ModifyOrder(request.OrderID, request.User, account, request.SL, request.TP, request.IsServerSide);
        }

        #endregion // CommandBase
    }
}
