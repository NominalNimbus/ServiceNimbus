/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using ServerCommonObjects;
using System;
using System.Collections.Generic;

namespace WebSocketServiceHost
{
    internal sealed class CommandManager : IHostCommandManager
    {
        #region Fields

        private Dictionary<string, IHostCommand> _commands;

        #endregion // Fields

        #region Constructors

        public CommandManager()
        {
            _commands = new Dictionary<string, IHostCommand>();
        }

        #endregion // Constructors

        #region ICommandManager

        public void RegisterCommand(string commandType, IHostCommand command)
        {
            if (string.IsNullOrEmpty(commandType))
                throw new ArgumentNullException("Command type is null or empty.");

            if (command == null)
                throw new ArgumentNullException("Command is null");

            if (_commands.ContainsKey(commandType))
                return;

            _commands.Add(commandType, command);
        }

        public void OnNewRequest(string sessionId, RequestMessage request)
        {
            if (request == null)
                return;

            var commandType = CommandType.GetCommandType(request);
            if (commandType == string.Empty)
                return;

            if (_commands.TryGetValue(commandType, out IHostCommand command))
                command.Execute(sessionId, request);
        }

        #endregion // ICommandManager
    }
}
