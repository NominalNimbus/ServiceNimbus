/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using Com.Lmax.Api.Internal.Xml;

namespace Com.Lmax.Api.Order
{
    /// <summary>
    /// Request to cancel an order.
    /// </summary>
    public class CancelOrderRequest :IRequest
    {
        private readonly string _instructionId;
        private readonly long _instrumentId;
        private readonly string _originalInstructionId;

        /// <summary>
        /// Construction and request to cancel an order.
        /// </summary>
        /// <param name="instructionId">The instruction id used to correlate requests with responses</param>
        /// <param name="instrumentId">The instrument id of the OrderBook that received the original order.</param>
        /// <param name="originalInstructionId">The instruction id of the original order.</param>
        public CancelOrderRequest(string instructionId, long instrumentId, string originalInstructionId)
        {
            _instructionId = instructionId;
            _instrumentId = instrumentId;
            _originalInstructionId = originalInstructionId;
        }

        public string Uri
        {
            get { return "/secure/trade/cancel"; }
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
                        ValueOrEmpty("instructionId", _instructionId).
                        ValueOrEmpty("instrumentId", _instrumentId).                        
                        ValueOrEmpty("originalInstructionId", _originalInstructionId).
                    EndElement("body").
                EndElement("req");            

        }
    }
}