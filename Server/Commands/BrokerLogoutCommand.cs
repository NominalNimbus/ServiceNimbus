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
    internal sealed class BrokerLogoutCommand : CommandBase<BrokerLogoutRequest>
    {
        #region Constructors

        public BrokerLogoutCommand(ICore core, IPusher pusher) : base(core, pusher)
        {
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(BrokerLogoutRequest request)
        {
            try
            {
                var user = request.User;
                Core.OMS.BrokerAccountsLogout(user, request.Accounts);
                PushResponse(new BrokerLogoutResponse
                {
                    User = request.User
                });
            }
            catch (Exception ex)
            {
                PushResponse(new BrokerLogoutResponse
                {
                    User = request.User,
                    Error = ex.Message
                });
            }
        }

        #endregion // CommandBase
    }
}
