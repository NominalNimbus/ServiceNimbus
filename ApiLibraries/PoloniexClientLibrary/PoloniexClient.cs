/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

namespace PoloniexAPI
{
    public sealed class PoloniexClient
    {
        public Authenticator Authenticator { get; private set; }
        public MarketTools.Markets Markets { get; private set; }
        public TradingTools.Trading Trading { get; private set; }
        public WalletTools.Wallet Wallet { get; private set; }
        //public LiveTools.Live Live { get; private set; }
        public LiveTools.LiveWebSocket Live { get; private set; }

        public PoloniexClient(string apiBaseUrl, string publicApiKey, string privateApiKey)
        {
            if (apiBaseUrl != null && apiBaseUrl.Length > 1 && apiBaseUrl[apiBaseUrl.Length - 1] != '/')
                apiBaseUrl += "/";

            var apiWebClient = new ApiWebClient(apiBaseUrl);
            Authenticator = new Authenticator(apiWebClient, publicApiKey, privateApiKey);

            Markets = new MarketTools.Markets(apiWebClient);
            Trading = new TradingTools.Trading(apiWebClient);
            Wallet = new WalletTools.Wallet(apiWebClient);
            Live = new LiveTools.LiveWebSocket();
        }
    }
}
