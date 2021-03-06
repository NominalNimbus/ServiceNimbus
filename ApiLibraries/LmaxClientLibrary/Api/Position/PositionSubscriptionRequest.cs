/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using Com.Lmax.Api.Internal.Xml;

namespace Com.Lmax.Api.Position
{
    /// <summary>
    /// Request to subscribe to position events.
    /// </summary>
    public class PositionSubscriptionRequest : SubscriptionRequest
    {
        protected override void WriteSubscriptionBodyTo(IStructuredWriter writer)
        {
            writer.ValueOrEmpty("type", "position");
        }
    }
}
