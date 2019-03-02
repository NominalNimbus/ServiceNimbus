/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using Com.Lmax.Api.Internal.Xml;

namespace Com.Lmax.Api.Order
{
    /// <summary>
    /// Request to amend stop loss/profit on an existing order.
    /// </summary>
    public sealed class AmendStopLossProfitRequest : IRequest
    {
        private readonly long _instrumentId;
        private readonly string _instructionId;
        private readonly string _originalInstructionId;
        private readonly decimal? _stopLossOffset;
        private readonly decimal? _stopProfitOffset;

        /// <summary>
        /// Construct an AmendStopLossProfitRequest using the instrument id and the instruction id
        /// of the original order.
        /// </summary>
        /// <param name="instrumentId">The instrument id that the original order was placed on.</param>
        /// <param name="instructionId">The instruction id used to correlate requests with responses.</param>
        /// <param name="originalInstructionId">The instruction id of the original order we want to amend.</param>
        /// <param name="stopLossOffset">The new stop loss offset, use null to
        /// indicate the value should be removed.</param>
        /// <param name="stopProfitOffset">The new stop profit offset, use null to
        /// indicate the value should be removed.</param>
        public AmendStopLossProfitRequest(long instrumentId, string instructionId, string originalInstructionId, decimal? stopLossOffset, decimal? stopProfitOffset)
        {
            _instrumentId = instrumentId;
            _instructionId = instructionId;
            _originalInstructionId = originalInstructionId;
            _stopLossOffset = stopLossOffset;
            _stopProfitOffset = stopProfitOffset;
        }

        public string Uri
        {
            get { return "/secure/trade/amendOrder"; }
        }

        /// <summary>
        /// Internal: Output this request.
        /// </summary>
        /// <param name="writer">The destination for the content of this request</param>
        public void WriteTo(IStructuredWriter writer)
        {
            writer
                .StartElement("req")
                    .StartElement("body")
                        .ValueOrEmpty("instrumentId", _instrumentId)
                        .ValueOrEmpty("originalInstructionId", _originalInstructionId)
                        .ValueOrEmpty("instructionId", _instructionId)
                        .ValueOrEmpty("stopLossOffset", _stopLossOffset)
                        .ValueOrEmpty("stopProfitOffset", _stopProfitOffset)
                    .EndElement("body")
                .EndElement("req");
        }
    }
}
