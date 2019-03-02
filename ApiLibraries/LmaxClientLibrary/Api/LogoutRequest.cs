/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using Com.Lmax.Api.Internal.Xml;

namespace Com.Lmax.Api
{
    /// <summary>
    /// Contains a of the necessary credential information and product type required
    /// connect to the LMAX Trader platform.
    /// </summary>
    public class LogoutRequest : IRequest
    {
        private const string LogoutUri = "/public/security/logout";

        /// <summary>
        /// The URI for the login request. 
        /// </summary>
        public string Uri { get { return LogoutUri; } }

        /// <summary>
        /// Internal: Output this request.
        /// </summary>
        /// <param name="writer">The destination for the content of this request</param>
        public void WriteTo(IStructuredWriter writer)
        {
            writer.
                StartElement("req").
                    WriteEmptyTag("body").
                EndElement("req");
        }
    }
}
