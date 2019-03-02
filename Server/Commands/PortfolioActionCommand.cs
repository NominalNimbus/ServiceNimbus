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
    internal sealed class PortfolioActionCommand : CommandBase<PortfolioActionRequest>
    {
        #region Constructors
        public PortfolioActionCommand(ICore core, IPusher pusher) : base(core, pusher)
        {
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(PortfolioActionRequest request)
        {
            if (request.Action == PortfolioAction.Add)
            {
                var res = Core.AddPortfolio(request.Portfolio, request.User.Login);
                Core.AddUserFiles(request.User.Login, request.Portfolio.Name, null);

                PushResponse(new PortfolioActionResponse
                {
                    User = request.User,
                    Portfolio = request.Portfolio,
                    Action = request.Action,
                    Error = res > 0 ? string.Empty : "Failed to create portfolio"
                });
            }

            if (request.Action == PortfolioAction.Remove)
            {
                var res = Core.RemovePortfolio(request.Portfolio);
                Core.DeleteUserFiles(request.User.Login, new[] { request.Portfolio.Name });

                PushResponse(new PortfolioActionResponse
                {
                    User = request.User,
                    Portfolio = request.Portfolio,
                    Action = request.Action,
                    Error = res ? string.Empty : "Failed to remove portfolio"
                });
            }

            if (request.Action == PortfolioAction.Edit)
            {
                var res = Core.UpdatePortfolio(request.User, request.Portfolio);

                PushResponse(new PortfolioActionResponse
                {
                    User = request.User,
                    Portfolio = request.Portfolio,
                    Action = request.Action,
                    Error = res ? string.Empty : "Failed to update portfolio"
                });
            }
        }

        #endregion // CommandBase
    }
}
