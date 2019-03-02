/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using ServerCommonObjects;
using OMS;
using Server.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Commands
{
    internal sealed class CreateSimulatedBrokerAccountCommand : CommandBase<CreateSimulatedBrokerAccountRequest>
    {
        #region Constructors

        public CreateSimulatedBrokerAccountCommand(ICore core, IPusher pusher) : base(core, pusher)
        {
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(CreateSimulatedBrokerAccountRequest request)
        {
            var error = BrokerFactory.CreateSimulatedAccount(request.User.Login, request.Account);
            
            PushResponse(new CreateSimulatedBrokerAccountResponse
            {
                User = request.User,
                Account = request.Account,
                Error = error
            });
        }

        #endregion // CommandBase
    }
}
