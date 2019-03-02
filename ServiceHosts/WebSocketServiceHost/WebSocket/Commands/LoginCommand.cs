/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using ServerCommonObjects;

namespace WebSocketServiceHost
{
    internal sealed class LoginCommand : CommandBase<LoginRequest>
    {
        #region Construcors

        public LoginCommand(ICoreServiceHost core) : base(core)
        {

        }

        #endregion // Construcors

        #region CommandBase

        public override void ExecuteComamnd(string sessionId, LoginRequest request)
        {
            var isValid = Core.MessageManager.ValidateCredentials(request);
            if (isValid)
            {
                var userInfo = new UserInfo(Core, sessionId, request.Login);
                Core.MessageManager.AddSession(userInfo);
            }
        }

        #endregion // CommandBase
    }
}
