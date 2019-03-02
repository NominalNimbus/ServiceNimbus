/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using Newtonsoft.Json;

namespace PoloniexAPI.MarketTools
{
    public class Quote
    {
        public CurrencyPair Symbol { get; set; }

        [JsonProperty("id")]
        public int Id { get; internal set; }

        [JsonProperty("last")]
        public decimal Last { get; internal set; }

        [JsonProperty("percentChange")]
        public decimal PercentChange { get; internal set; }

        [JsonProperty("baseVolume")]
        public decimal BaseVolume { get; internal set; }

        [JsonProperty("quoteVolume")]
        public decimal Volume { get; internal set; }

        [JsonProperty("highestBid")]
        public decimal Bid { get; internal set; }

        [JsonProperty("lowestAsk")]
        public decimal Ask { get; internal set; }

        [JsonProperty("isFrozen")]
        internal byte IsFrozenValue { set { IsFrozen = value != 0; } }
        public bool IsFrozen { get; private set; }

        public decimal Spread => Ask - Bid;

        public decimal SpreadPercentage => Ask / Bid - 1m;
    }
}
