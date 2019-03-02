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
    internal sealed class CoreServiceHost : ICoreServiceHost
    {
        #region Properties

        public IMessageManager MessageManager { get; }

        public ISender<ResponseMessage> WebSocketSender { get; }

        public IHostCommandManager CommandManager { get; }

        public IWebSocketServer WebSocketServer { get; }

        #endregion // Properties

        #region Constructors

        public CoreServiceHost()
        {
            MessageManager = new MessageManager();
            CommandManager = new CommandManager();
            var webSocketServer = new WebSocketServer(CommandManager);
            WebSocketSender = webSocketServer;
            WebSocketServer = webSocketServer;
        }

        #endregion // Constructors
    }
}
