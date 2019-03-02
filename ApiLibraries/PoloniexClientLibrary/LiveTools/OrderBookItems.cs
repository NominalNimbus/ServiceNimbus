/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using Newtonsoft.Json;

namespace PoloniexAPI.LiveTools
{
    public class OrderBookItem
    {
        public CurrencyPair Symbol { get; set; }

        public OrderBookItemType Type { get; set; }

        [JsonProperty("rate")]
        public decimal Price { get; private set; }

        [JsonProperty("type")]
        private string SideString { set { Side = (OrderBookItemSide)System.Enum.Parse(typeof(OrderBookItemSide), value, true); } }
        public OrderBookItemSide Side { get; private set; }

        [JsonProperty("amount")]
        public decimal Quantity { get; private set; }  //absent for OrderBookItemType.OrderBookRemove
    }

    public class OrderBookTrade : OrderBookItem
    {
        [JsonProperty("tradeID")]
        public int TradeID { get; private set; }

        [JsonProperty("date")]
        public System.DateTime TradeTime { get; private set; }

        [JsonProperty("total")]
        public decimal TradeBaseQuantity { get; private set; }
    }
}
