/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using Com.Lmax.Api.Internal.Xml;

namespace Com.Lmax.Api.Account
{
    ///<summary>
    /// Request a heartbeat event be returned from the server.
    ///</summary>
    public class HeartbeatRequest : IRequest
    {
        private readonly string _token;

        ///<summary>
        /// Creates a new Heartbeat request
        ///</summary>
        public HeartbeatRequest(string token)
        {
            _token = token;
        }

        public string Uri
        {
            get { return "/secure/read/heartbeat"; }
        }

        /// <summary>
        /// Internal: Output this request.
        /// </summary>
        /// <param name="writer">The destination for the content of this request</param>
        public void WriteTo(IStructuredWriter writer)
        {
            writer.
                StartElement("req").
                    StartElement("body").
                        ValueOrNone("token", _token).
                    EndElement("body").
                EndElement("req");

        }
    }
}