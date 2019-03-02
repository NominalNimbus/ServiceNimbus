/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace PoloniexAPI.MarketTools
{
    public class OrderBook
    {
        [JsonProperty("bids")]
        private List<string[]> BidValues { set { Bids = ParseOrders(value); } }
        public Dictionary<decimal, decimal> Bids { get; private set; }

        [JsonProperty("asks")]
        private List<string[]> AskValues { set { Asks = ParseOrders(value); } }
        public Dictionary<decimal, decimal> Asks { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Dictionary<decimal, decimal> ParseOrders(IList<string[]> orders)
        {
            var result = new Dictionary<decimal, decimal>(orders.Count);
            for (var i = 0; i < orders.Count; i++)
                result.Add(orders[i][0].ToDecimal(), orders[i][1].ToDecimal());
            return result;
        }
    }
}
