/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

namespace PoloniexAPI
{
    public class Authenticator
    {
        private readonly ApiWebClient _apiWebClient;

        public string PublicKey { get; private set; }
        public string PrivateKey { get; private set; }

        internal Authenticator(ApiWebClient apiWebClient, string publicKey, string privateKey)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey;
            _apiWebClient = apiWebClient;
            _apiWebClient.SetAuthenticator(this);
        }
    }
}
