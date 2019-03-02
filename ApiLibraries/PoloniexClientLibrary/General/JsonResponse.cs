/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using Newtonsoft.Json;
using System.Net;

namespace PoloniexAPI
{
    class JsonResponse<T>
    {
        [JsonProperty("status")]
        private string Status { get; set; }
        [JsonProperty("message")]
        private string Message { get; set; }

        private T _data;
        [JsonProperty("data")]
        internal T Data {
            get { return _data; }

            private set {
                CheckStatus();
                _data = value;
            }
        }

        internal void CheckStatus()
        {
            if (Status != "success") {
                if (string.IsNullOrWhiteSpace(Message)) throw new WebException("Could not parse data from the server.", WebExceptionStatus.UnknownError);
                throw new WebException("Could not parse data from the server: " + Message, WebExceptionStatus.UnknownError);
            }
        }
    }
}
