/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using ServerCommonObjects;
using RabbitMQServiceHost.Core;

namespace RabbitMQServiceHost.Commands
{
    internal sealed class LogoutCommand : CommandBase<LogoutRequest>
    {

        #region Constructors

        public LogoutCommand(HostCore core) : base(core)
        {
        }

        #endregion

        #region CommandBase

        protected override void ExecuteCommand(string sessionId, LogoutRequest request)
        {
            Core.MessageManager.RemoveSession(sessionId);
        }

        #endregion

    }
}
