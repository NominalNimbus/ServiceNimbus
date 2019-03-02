/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.IO;
using System.Net;
using System.Text;

namespace Com.Lmax.Api.Internal
{
    public class Connection : IConnection
    {
        private readonly WebRequest _webRequest;
        private readonly WebResponse _webResponse;

        public Connection(WebRequest webRequest, WebResponse webResponse)
        {
            _webRequest = webRequest;
            _webResponse = webResponse;
        }

        public TextReader GetTextReader()
        {
            return new StreamReader(_webResponse.GetResponseStream(), new UTF8Encoding());
        }

        public BinaryReader GetBinaryReader()
        {
            return new BinaryReader(_webResponse.GetResponseStream());
        }

        public void Abort()
        {
            _webRequest.Abort();
        }

        public void Close()
        {
            _webResponse.Close();
        }
    }
}
