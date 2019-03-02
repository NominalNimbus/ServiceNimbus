/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

namespace WebSocketServiceHost
{
    internal interface IWebSocketServer
    {
        void Start(string ip, int port);
        void Stop();
        void Disconect(string sessinId);
    }
}
