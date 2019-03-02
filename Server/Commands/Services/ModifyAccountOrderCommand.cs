/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using ServerCommonObjects;
using Server.Interfaces;

namespace Server.Commands.Services
{
    internal sealed class ModifyAccountOrderCommand : CommandBase<ModifyAccountOrderRequest>
    {
        #region Constructors

        public ModifyAccountOrderCommand(ICore core, IPusher pusher) : base(core, pusher)
        {
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(ModifyAccountOrderRequest request)
        {
            var account = Core.GetUser(request.Username);
            Core.OMS.ModifyOrder(request.OrderId, account, request.Account, request.Sl, request.Tp, request.IsServerSide);
        }

        #endregion // CommandBase
    }
}
