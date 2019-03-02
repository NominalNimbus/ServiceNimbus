/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using System;
using System.Linq;
using System.Threading.Tasks;
using ServerCommonObjects;
using Server.Interfaces;

namespace Server.Commands
{
    internal sealed class BrokerLoginCommand : CommandBase<BrokersLoginRequest>
    {
        #region Constructors

        public BrokerLoginCommand(ICore core, IPusher pusher) : base(core, pusher)
        {
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(BrokersLoginRequest request)
        {
            var user = request.User;
            Core.OMS.AddTrader(user, request.Accounts, out var accErrors);
            var gotErrors = accErrors != null && accErrors.Count > 0;
            var errors = gotErrors ? string.Join(Environment.NewLine, accErrors.Select(i => i.Value)) : null;
            var accounts = gotErrors ? user.Accounts.Where(a => !accErrors.ContainsKey(a)).ToList() : user.Accounts;

            PushResponse(new BrokerLoginResponse
            {
                User = request.User,
                Accounts = accounts,
                Error = errors
            });

            foreach (var account in accounts)
            {
                Task.Run(() =>
                {
                    var response = new BrokersAvailableSecuritiesResponse
                    {
                        User = user,
                        Securities = Core.OMS.GetAvailableSecurities(account),
                        BrokerId = account.ID
                    };
                    PushResponse(response);
                });
            }
        }

        #endregion // CommandBase
    }
}
