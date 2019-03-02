/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using ServerCommonObjects;
using ServerCommonObjects.Classes;
using ServerCommonObjects.ServerClasses;
using RabbitMQServiceHost.Commands;
using RabbitMQServiceHost.Interfaces;
using WebSocketServiceHost;
using CommandType = ServerCommonObjects.CommandType;
using System;
using System.Configuration;
using System.Reflection;
using System.IO;

namespace RabbitMQServiceHost.Core
{
    internal class ServiceHost : IServerServiceHost
    {

        #region Fields
        
        private readonly IHostCommandManager _commandManager;
        private readonly IRabbitMQServer _rabbitMQServer;
        private readonly HostCore _core;

        #endregion

        #region Properties

        public string Name => "RabbitMQ";

        private string Username { get; }
        private string Password { get; }
        private string VirtualHost { get; }
        private string HostName { get; }

        #endregion

        #region Constructor

        public ServiceHost()
        {
            var config = new LocalConfigHelper(Assembly.GetExecutingAssembly().Location, "RabbitMQ Service");

            Username = config.GetString(nameof(Username));
            Password = config.GetString(nameof(Password));
            VirtualHost = config.GetString(nameof(VirtualHost));
            HostName = config.GetString(nameof(HostName));

            _core = new HostCore();
            _commandManager = _core.CommandManager;
            _rabbitMQServer = _core.RabbitMQServer;

            RegisterCommands();
        }

        #endregion

        #region IServerServiceHost

        public void Start() => 
            _rabbitMQServer.Start(Username, Password, HostName, VirtualHost);

        public void Stop() => 
            _rabbitMQServer.Stop();

        #endregion

        #region Private

        private void RegisterCommands()
        {
            _commandManager.RegisterCommand(CommandType.Login, new LoginCommand(_core));
            _commandManager.RegisterCommand(CommandType.Logout, new LogoutCommand(_core));
            _commandManager.RegisterCommand(CommandType.ProcessRequest, new ProcessRequestCommand(_core));
        }

        #endregion

    }
}