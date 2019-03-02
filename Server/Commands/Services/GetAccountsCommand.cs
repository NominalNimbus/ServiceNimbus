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
    internal sealed class GetAccountsCommand : CommandBase<GetAccountsRequest>
    {
        #region Constructors

        public GetAccountsCommand(ICore core, IPusher pusher) : base(core, pusher)
        {
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(GetAccountsRequest request)
        {
            var user = Core.GetUser(request.Username);
            PushToProcessor(new GetAccountsResponse
            {
                ID = request.ID,
                Accounts = user?.Accounts
            }, request.Processor);
        }

        #endregion // CommandBase
    }
}
