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
    public class Order
    {
        [JsonIgnore]
        public CurrencyPair CurrencyPair { get; set; }

        [JsonProperty("orderNumber")]
        public ulong Id { get; set; }

        [JsonProperty("type")]
        private string TypeInternal { set { Side = value.ToOrderSide(); } }
        public OrderSide Side { get; set; }

        [JsonProperty("rate")]
        public decimal PricePerCoin { get; set; }

        [JsonProperty("amount")]
        public decimal Quantity { get; set; }

        [JsonProperty("total")]
        public decimal Value { get; set; }
    }
}
