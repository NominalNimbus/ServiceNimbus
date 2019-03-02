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
    public class Deposit
    {
        [JsonProperty("currency")]
        public string Currency { get; private set; }

        [JsonProperty("address")]
        public string Address { get; private set; }

        [JsonProperty("amount")]
        public double Amount { get; private set; }

        [JsonProperty("timestamp")]
        private ulong TimeInternal { set { Time = Helper.UnixTimeStampToDateTime(value); } }
        public System.DateTime Time { get; private set; }

        [JsonProperty("txid")]
        public string TransactionId { get; private set; }

        [JsonProperty("confirmations")]
        public uint Confirmations { get; private set; }

        [JsonProperty("status")]
        public string Status { get; private set; }
    }
}
