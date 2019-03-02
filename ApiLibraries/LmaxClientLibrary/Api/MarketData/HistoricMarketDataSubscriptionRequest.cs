/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using Com.Lmax.Api.Internal.Xml;

namespace Com.Lmax.Api.MarketData
{
    ///<summary>
    /// Used when subscribing to the events containing historic market data responses.
    ///</summary>
    public class HistoricMarketDataSubscriptionRequest : ISubscriptionRequest
    {
            /// <summary>
            /// Readonly property containing the URI for the request. 
            /// </summary>
            public string Uri
            {
                get { return "/secure/subscribe"; }
            }

            /// <summary>
            /// Internal: Output this request.  Can't use the Abstract Subscription Request because it needs to append two separate subscriptions.
            /// </summary>
            /// <param name="writer">The destination for the request content</param>
            public void WriteTo(IStructuredWriter writer)
            {
                writer.StartElement("req").StartElement("body");
                writer.StartElement("subscription");
                writer.ValueOrEmpty("type", "historicMarketData");
                writer.EndElement("subscription").EndElement("body").EndElement("req");
            }

    }
}