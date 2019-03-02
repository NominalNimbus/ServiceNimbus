/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using CommonObjects;
using System.Collections.Generic;

namespace Server.Interfaces
{
    internal interface IRealTimeWorker
    {
        void Level1Subscribe(Security instrument, string userId);
        void Level1UnSubscribe(string sessionID, IEnumerable<Security> instruments);
    }
}
