/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using Newtonsoft.Json;
using System.Collections.Generic;

namespace PoloniexAPI.WalletTools
{
    public class DepositWithdrawalList
    {
        [JsonProperty("deposits")]
        public List<Deposit> Deposits { get; private set; }

        [JsonProperty("withdrawals")]
        public List<Withdrawal> Withdrawals { get; private set; }
    }
}
