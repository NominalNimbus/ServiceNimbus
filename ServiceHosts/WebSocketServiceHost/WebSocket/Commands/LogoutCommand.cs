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
    internal sealed class LogoutCommand : CommandBase<LogoutRequest>
    {
        #region Constructors

        public LogoutCommand(ICoreServiceHost core) : base(core)
        {
        }

        #endregion // Constructors

        #region CommandBase

        public override void ExecuteComamnd(string sessionId, LogoutRequest request)
        {
            var userInfo = Core.MessageManager.RemoveSession(sessionId);
        }

        #endregion // CommandBase
    }
}
