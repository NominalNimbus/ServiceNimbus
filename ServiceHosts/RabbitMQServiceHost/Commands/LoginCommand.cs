/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using CommonObjects;
using ServerCommonObjects;
using RabbitMQServiceHost.Core;

namespace RabbitMQServiceHost.Commands
{
    internal sealed class LoginCommand : CommandBase<LoginRequest>
    {

        #region Constructor

        public LoginCommand(HostCore core) : base(core)
        {
        }

        #endregion
        
        #region ICommand

        protected override void ExecuteCommand(string sessionID, LoginRequest request)
        {
            var loginResponse = new LoginResponse();

            var isValid = Core.MessageManager.ValidateCredentials(request);
            if (!isValid)
            {
                loginResponse.State = AuthorizationState.Faulted;

                Core.RabbitMQServer.Send(loginResponse, sessionID);
                Logger.Warning($"Login error: user = '{request.Login}'");
                return;
            }

            loginResponse.State = AuthorizationState.Authorized;
            var userInfo = new UserInfo(Core, sessionID, request.Login);
            Core.MessageManager.AddSession(userInfo);
            Core.RabbitMQServer.Send(loginResponse, sessionID);
        }

        #endregion

    }
}
