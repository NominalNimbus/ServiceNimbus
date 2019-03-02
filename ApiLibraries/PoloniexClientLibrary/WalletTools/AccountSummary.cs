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
    public class AccountSummary
    {
        [JsonProperty("totalValue")]
        public decimal Value { get; private set; }

        [JsonProperty("pl")]
        public decimal PL { get; private set; }

        [JsonProperty("lendingFees")]
        public decimal LendingFees { get; private set; }

        [JsonProperty("netValue")]
        public decimal NetValue { get; private set; }  //Value + PL

        [JsonProperty("totalBorrowedValue")]
        public decimal BorrowedValue { get; private set; }

        [JsonProperty("currentMargin")]
        public decimal CurrentMargin { get; private set; }
    }
}
