/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using Newtonsoft.Json;

namespace PoloniexAPI.MarketTools
{
    public class Trade
    {
        [JsonProperty("date")]
        private string TimeInternal { set { Time = Helper.ParseDateTime(value); } }
        public DateTime Time { get; private set; }

        [JsonProperty("type")]
        private string TypeInternal { set { Type = value.ToOrderSide(); } }
        public OrderSide Type { get; private set; }

        [JsonProperty("rate")]
        public double PricePerCoin { get; private set; }

        [JsonProperty("amount")]
        public double AmountQuote { get; private set; }

        [JsonProperty("total")]
        public double AmountBase { get; private set; }
    }
}
