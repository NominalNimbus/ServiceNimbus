/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoloniexAPI.LiveTools
{
    internal class MessageIn
    {
        [JsonProperty("command")]
        public string Command { get; set; }

        [JsonProperty("channel")]
        public string Channel { get; set; }

        internal MessageIn(string command, string channel)
        {
            Command = command;
            Channel = channel;
        }

        internal MessageIn(string command, int channel) :
            this(command, channel.ToString())
        {

        }
    }
}
