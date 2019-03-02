/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using ServerCommonObjects;
using RabbitMQServiceHost.Commands;
using RabbitMQServiceHost.Interfaces;
using RabbitMQServiceHost.Messaging;
using RabbitMQServiceHost.RabbitMQ;

namespace RabbitMQServiceHost.Core
{
    internal class HostCore
    {
        public IMessageManager MessageManager { get; }
        public IHostCommandManager CommandManager { get; }
        public IRabbitMQServer RabbitMQServer { get; }

        public HostCore()
        {
            MessageManager = new MessageManager();
            CommandManager = new CommandManager();
            RabbitMQServer = new RabbitMQServer(CommandManager);
        }
    }
}
