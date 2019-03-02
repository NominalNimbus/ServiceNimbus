/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoloniexAPI.LiveTools
{
    internal class MessageHelper
    {
        public const int AccountNotifications = 1000;
        public const int TickerData = 1002;
        public const int ExchangeVolume24Hours = 1003;
        public const int Heartbeat = 1010;

        public const string Subscribe = "subscribe";
        public const string Unsubscribe = "unsubscribe";
    }
}
