/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using ServerCommonObjects;
using RabbitMQServiceHost.Commands;
using RabbitMQServiceHost.Core;

namespace WebSocketServiceHost
{
    internal sealed class ProcessRequestCommand : CommandBase<RequestMessage>
    {

        #region Constructors

        public ProcessRequestCommand(HostCore core) : base(core)
        {
        }

        #endregion

        #region CommandBase

        protected override void ExecuteCommand(string sessionId, RequestMessage request)
        {
            var isConnected = Core.MessageManager.IsUserConnected(sessionId);
            if (!isConnected)
                return;

            Core.MessageManager.SendRequest(request);
        }

        #endregion

    }
}
