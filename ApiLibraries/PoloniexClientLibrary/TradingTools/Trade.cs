/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using Newtonsoft.Json;

namespace PoloniexAPI.TradingTools
{
    public class Trade : Order
    {
        [JsonProperty("date")]
        private string TimeInternal { set { Time = Helper.ParseDateTime(value); } }
        public System.DateTime Time { get; private set; }
    }
}
