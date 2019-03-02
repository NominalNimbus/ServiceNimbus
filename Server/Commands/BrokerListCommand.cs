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

namespace Server.Commands
{
    internal sealed class BrokerListCommand : CommandBase<TradingInfoRequest>
    {
        #region Constructors

        public BrokerListCommand(ICore core, IPusher pusher) : base(core, pusher)
        {
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(TradingInfoRequest request)
        {
            PushResponse(new TradingInfoResponse
            {
                User = request.User,
                Brokers = BrokerFactory.GetAvailableBrokers(request.User.Login),
                Portfolios = Core.GetPortfolios(request.User)
            });
        }

        #endregion // CommandBase
    }
}
