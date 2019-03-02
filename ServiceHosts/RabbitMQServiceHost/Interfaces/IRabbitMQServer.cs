/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using ServerCommonObjects;
using RabbitMQServiceHost.RabbitMQ;

namespace RabbitMQServiceHost.Interfaces
{
    public interface IRabbitMQServer
    {
        void Start(string username, string password, string hostName, string virtualHost);
        void Stop();
        void Send(ResponseMessage message, string queueName);
    }
}