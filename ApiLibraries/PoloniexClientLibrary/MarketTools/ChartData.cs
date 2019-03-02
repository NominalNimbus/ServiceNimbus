/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using Newtonsoft.Json;
using System;

namespace PoloniexAPI.MarketTools
{
    public class ChartData
    {
        [JsonProperty("date")]
        private ulong TimeInternal { set { Time = Helper.UnixTimeStampToDateTime(value); } }
        public DateTime Time { get; private set; }

        [JsonProperty("open")]
        public decimal Open { get; private set; }

        [JsonProperty("close")]
        public decimal Close { get; private set; }

        [JsonProperty("high")]
        public decimal High { get; private set; }

        [JsonProperty("low")]
        public decimal Low { get; private set; }

        [JsonProperty("volume")]
        public decimal VolumeBase { get; private set; }

        [JsonProperty("quoteVolume")]
        public decimal Volume { get; private set; }

        [JsonProperty("weightedAverage")]
        public decimal WeightedAverage { get; private set; }
    }
}
