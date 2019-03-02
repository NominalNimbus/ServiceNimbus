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
    public class OrderTrade
    {
        [JsonProperty("tradeID")]
        public ulong Id { get; private set; }

        [JsonProperty("currencyPair")]
        private string CurrencyPairInternal { set { CurrencyPair = CurrencyPair.Parse(value); } }
        public CurrencyPair CurrencyPair { get; private set; }

        [JsonProperty("date")]
        private string TimeInternal { set { Time = Helper.ParseDateTime(value); } }
        public System.DateTime Time { get; private set; }

        [JsonProperty("type")]
        private string TypeInternal { set { Side = value.ToOrderSide(); } }
        public OrderSide Side { get; private set; }

        [JsonProperty("amount")]
        public decimal Quantity { get; private set; }

        [JsonProperty("total")]
        public decimal Value { get; private set; }

        [JsonProperty("rate")]
        public decimal PricePerCoin { get; private set; }  //= Quantity / Value

        [JsonProperty("fee")]
        public decimal Comission { get; private set; }

        public OrderTrade()
        {
        }

        public OrderTrade(decimal quantity)
        {
            Quantity = quantity;
        }
    }
}
