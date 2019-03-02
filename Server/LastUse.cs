/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using System;

namespace Server
{
    internal sealed class LastUse
    {
        public string Key { get; }
        public DateTime Time { get; set; }

        public LastUse(string key, DateTime time)
        {
            Key = key;
            Time = time;
        }
    }
}
