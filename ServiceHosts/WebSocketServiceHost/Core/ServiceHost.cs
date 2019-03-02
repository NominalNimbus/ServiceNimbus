/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using ServerCommonObjects;
using ServerCommonObjects.ServerClasses;
using System.Collections.Generic;
using ServerCommonObjects.Classes;
using System.Configuration;
using System.Reflection;
using System;
using System.IO;

namespace WebSocketServiceHost
{
    internal sealed class ServiceHost : IServerServiceHost
    {

        #region Fields

        private string IP { get; }
        private int Port { get; }

        private readonly IWebSocketServer _webSocketServer;
        private readonly IHostCommandManager _commandManager;
        private readonly ICoreServiceHost _core;

        #endregion // Fields

        #region Properties

        public string Name => "WebSocket";

        #endregion // Properties

        #region Constructors

        public ServiceHost()
        {
            var config = new LocalConfigHelper(Assembly.GetExecutingAssembly().Location, " WebSocket Service");

            IP = config.GetString(nameof(IP));
            Port = config.GetInt(nameof(Port));

            var core = new CoreServiceHost();
            _commandManager = core.CommandManager;
            _webSocketServer = core.WebSocketServer;
            _core = core;

            RegsterCommands();
        }

        #endregion // Constructors

        #region Private

        private void RegsterCommands()
        {
            _commandManager.RegisterCommand(CommandType.Login, new LoginCommand(_core));
            _commandManager.RegisterCommand(CommandType.Logout, new LogoutCommand(_core));
            _commandManager.RegisterCommand(CommandType.ProcessRequest, new ProcessRequestCommand(_core));
        }

        #endregion // Private

        #region IServerServiceHost

        public void Start() => 
            _webSocketServer.Start(IP, Port);

        public void Stop() => 
            _webSocketServer.Stop();

        #endregion // IServerServiceHost

    }
}
