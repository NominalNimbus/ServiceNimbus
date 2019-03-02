/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using Newtonsoft.Json;

namespace PoloniexAPI.WalletTools
{
    public class Balance
    {
        [JsonIgnore]
        public string Currency { get; set; }

        [JsonIgnore]
        public string AccountType { get; set; }

        [JsonProperty("available")]
        public decimal QuoteAvailable { get; private set; }  //quantity available for sell

        [JsonProperty("onOrders")]
        public decimal QuoteOnOrders { get; private set; }  //quantity on pending orders (waiting to be sold)

        [JsonProperty("btcValue")]
        public decimal BitcoinValue { get; private set; }

        public Balance()
        {
        }

        public Balance(string currency, string accType, decimal available, decimal onOrders, decimal btcValue)
        {
            Currency = currency;
            AccountType = accType;
            QuoteAvailable = available;
            QuoteOnOrders = onOrders;
            BitcoinValue = btcValue;
        }
    }
}
