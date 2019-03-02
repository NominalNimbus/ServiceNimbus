/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PoloniexAPI.WalletTools
{
    public class Wallet
    {
        private readonly ApiWebClient _apiWebClient;

        internal Wallet(ApiWebClient apiWebClient)
        {
            _apiWebClient = apiWebClient;
        }

        public async Task<AccountSummary> GetMarginAccountSummary()
        {
            return await PostData<AccountSummary>("returnMarginAccountSummary", new Dictionary<string, object>());
        }

        public async Task<List<Balance>> GetBalances(bool includeAll = false)
        {
            var result = new List<Balance>();
            if (includeAll)
            {
                var data = await PostData<Dictionary<string, Dictionary<string, Balance>>>("returnCompleteBalances",
                    new Dictionary<string, object> { ["account"] = "all" });
                if (data != null && data.Count > 0)
                {
                    //result.Capacity = data.Where(a => a.Value != null).Sum(a => a.Value.Count);
                    foreach (var acct in data)
                    {
                        if (acct.Value == null || acct.Value.Count == 0)
                            continue;

                        foreach (var item in acct.Value)
                        {
                            result.Add(new Balance(item.Key, acct.Key,
                                item.Value.QuoteAvailable, item.Value.QuoteOnOrders, item.Value.BitcoinValue));
                        }
                    }
                }
            }
            else  //only exchange account
            {
                var data = await PostData<Dictionary<string, Balance>>("returnCompleteBalances", 
                    new Dictionary<string, object>(0));
                if (data != null && data.Count > 0)
                {
                    result.Capacity = data.Count;
                    foreach (var item in data)
                    {
                        result.Add(new Balance(item.Key, "exchange", 
                            item.Value.QuoteAvailable, item.Value.QuoteOnOrders, item.Value.BitcoinValue));
                    }
                }
            }

            return result;
        }

        public async Task<Dictionary<string, Dictionary<string, decimal>>> GetAvailableBalances()
        {
            return await PostData<Dictionary<string, 
                Dictionary<string, decimal>>>("returnAvailableAccountBalances", new Dictionary<string, object>(0));
        }

        public async Task<Dictionary<string, string>> GetDepositAddresses()
        {
            return await PostData<Dictionary<string, string>>("returnDepositAddresses", 
                new Dictionary<string, object>(0));
        }

        public async Task<DepositWithdrawalList> GetDepositsAndWithdrawals(DateTime startTime, DateTime endTime)
        {
            var postData = new Dictionary<string, object>
            {
                ["start"] = Helper.DateTimeToUnixTimeStamp(startTime),
                ["end"] = Helper.DateTimeToUnixTimeStamp(endTime)
            };

            return await PostData<DepositWithdrawalList>("returnDepositsWithdrawals", postData);
        }

        public async Task<DepositWithdrawalList> GetDepositsAndWithdrawals()
        {
            return await GetDepositsAndWithdrawals(Helper.DateTimeUnixEpochStart, DateTime.UtcNow);
        }

        public async Task<GeneratedDepositAddress> PostGenerateNewDepositAddress(string currency)
        {
            var postData = new Dictionary<string, object> { ["currency"] = currency };
            return await PostData<GeneratedDepositAddress>("generateNewAddress", postData);
        }

        public async Task<GeneratedDepositAddress> PostWithdrawal(string currency, 
            double amount, string address, string paymentId = null)
        {
            var postData = new Dictionary<string, object>
            {
                ["currency"] = currency,
                ["amount"] = amount.ToStringNormalized(),
                ["address"] = address
            };

            if (!String.IsNullOrWhiteSpace(paymentId))
                postData.Add("paymentId", paymentId);

            return await PostData<GeneratedDepositAddress>("withdraw", postData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task<T> PostData<T>(string command, Dictionary<string, object> postData)
        {
            return await _apiWebClient.PostData<T>(command, postData);
        }
    }
}
