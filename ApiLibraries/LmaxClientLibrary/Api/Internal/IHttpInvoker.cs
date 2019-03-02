/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;

namespace Com.Lmax.Api.Internal
{
    public interface IHttpInvoker
    {
        Response Invoke(string baseUri, IRequest request, IXmlParser xmlParser, Handler handler, out string sessionId);

        Response PostInSession(string baseUri, IRequest request, IXmlParser xmlParser, Handler handler, string sessionId);

        Response GetInSession(string baseUri, IRequest request, IXmlParser xmlParser, Handler handler, string sessionId);
        
        IConnection Connect(string baseUri, IRequest request, string sessionId);

        IConnection Connect(Uri uri, string sessionId);
    }
}