/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using Com.Lmax.Api.Internal.Xml;

namespace Com.Lmax.Api
{
    /// <summary>
    /// Base subscription request.
    /// </summary>
    public abstract class SubscriptionRequest : ISubscriptionRequest
    {
        /// <summary>
        /// Readonly property containing the URI for the request. 
        /// </summary>
        public string Uri
        {
            get { return "/secure/subscribe"; }
        }

        /// <summary>
        /// Internal: Output this request.
        /// </summary>
        /// <param name="writer">The destination for the request content</param>
        public void WriteTo(IStructuredWriter writer)
        {
            writer.StartElement("req").StartElement("body").StartElement("subscription");
            WriteSubscriptionBodyTo(writer);
            writer.EndElement("subscription").EndElement("body").EndElement("req");
        }

        protected abstract void WriteSubscriptionBodyTo(IStructuredWriter writer);
    }
}