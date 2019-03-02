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
    public class Position
    {
        [JsonIgnore]
        public CurrencyPair CurrencyPair { get; set; }

        [JsonProperty("amount")]
        public decimal Size { get; set; }

        [JsonProperty("total")]
        public decimal Total { get; private set; }  //= Size * BasePrice

        [JsonProperty("basePrice")]
        public decimal BasePrice { get; private set; }

        [JsonProperty("liquidationPrice")]
        public decimal LiquidationPrice { get; private set; }

        [JsonProperty("pl")]
        public decimal PL { get; private set; }

        [JsonProperty("lendingFees")]
        public decimal LendingFees { get; private set; }

        [JsonProperty("type")]
        private string TypeInternal { set { Side = value.ToPositionSide(); } }
        public PositionSide Side { get; set; }
    }
}
