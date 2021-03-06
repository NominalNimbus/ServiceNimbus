﻿/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.Net;

namespace Com.Lmax.Api.Internal
{
    public class Response
    {
        private readonly HttpStatusCode _statusCode;

        public Response(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        public bool IsOk
        {
            get { return _statusCode == HttpStatusCode.OK; }
        }

        public int Status
        {
            get { return (int) _statusCode; }
        }
    }
}
