/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PoloniexAPI
{
    sealed class ApiWebClient
    {
        private Authenticator _authenticator;
        private HMACSHA512 _encryptor;

        public string BaseUrl { get; private set; }

        private static readonly JsonSerializer JsonSerializer = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore };
        public static readonly Encoding Encoding = Encoding.ASCII;

        public ApiWebClient(string baseUrl)
        {
            BaseUrl = baseUrl;
            _encryptor = new HMACSHA512();
        }

        public void SetAuthenticator(Authenticator auth)
        {
            _authenticator = auth;
            _encryptor.Key = Encoding.GetBytes(auth.PrivateKey);
        }

        public async Task<T> GetData<T>(string command, params object[] parameters)
        {
            var relativeUrl = CreateRelativeUrl(command, parameters);
            var jsonString = await QueryString(relativeUrl);
            return JsonSerializer.DeserializeObject<T>(jsonString);
        }

        public async Task<T> PostData<T>(string command, Dictionary<string, object> postData)
        {
            postData.Add("command", command);
            postData.Add("nonce", Helper.GetCurrentHttpPostNonce());

            var jsonString = await PostString(Helper.ApiUrlHttpsRelativeTrading, postData.ToHttpPostString());

            try
            {
                return JsonSerializer.DeserializeObject<T>(jsonString);
            }
            catch (Exception e)
            {
                if (JObject.Parse(jsonString).First.Path == "error")
                    throw new Exception(JObject.Parse(jsonString).First.First.ToString());
                else
                    throw e;
            }
        }

        private async Task<string> QueryString(string relativeUrl)
        {
            var request = CreateHttpWebRequest("GET", relativeUrl);
            return await request.GetResponseString();
        }

        private async Task<string> PostString(string relativeUrl, string postData)
        {
            var request = CreateHttpWebRequest("POST", relativeUrl);
            request.ContentType = "application/x-www-form-urlencoded";

            var postBytes = Encoding.GetBytes(postData);
            request.ContentLength = postBytes.Length;
            request.Headers["Key"] = _authenticator.PublicKey;
            request.Headers["Sign"] = _encryptor.ComputeHash(postBytes).ToStringHex();

            using (var requestStream = request.GetRequestStream())
                requestStream.Write(postBytes, 0, postBytes.Length);

            return await request.GetResponseString();
        }

        private static string CreateRelativeUrl(string command, object[] parameters)
        {
            var relativeUrl = command;
            if (parameters.Length != 0)
                relativeUrl += "&" + String.Join("&", parameters);
            return relativeUrl;
        }

        private HttpWebRequest CreateHttpWebRequest(string method, string relativeUrl)
        {
            var request = WebRequest.CreateHttp(BaseUrl + relativeUrl);
            request.Method = method;
            request.UserAgent = "Poloniex API .NET v" + Helper.AssemblyVersionString;
            request.Timeout = Timeout.Infinite;
            request.Headers[HttpRequestHeader.AcceptEncoding] = "gzip,deflate";
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            return request;
        }
    }
}
