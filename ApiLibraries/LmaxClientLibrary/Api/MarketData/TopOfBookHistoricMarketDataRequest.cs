/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using Com.Lmax.Api.Internal;
using Com.Lmax.Api.Internal.Xml;

namespace Com.Lmax.Api.MarketData
{
    ///<summary>
    /// Request historic order book prices and quantities
    ///</summary>
    public class TopOfBookHistoricMarketDataRequest : IHistoricMarketDataRequest
    {
        private readonly long _instructionId;
        private readonly long _instrumentId;
        private readonly DateTime _from;
        private readonly DateTime _to;
        private readonly Format _format;
        private const int Depth = 1;

        ///<summary>
        /// Request historic prices and quantities for the given order book
        ///</summary>
        ///<param name="instructionId">Unique ID for this request</param>
        ///<param name="instrumentId">The ID of the instrument to return the data for</param>
        ///<param name="from">The date and time of the start of the range</param>
        ///<param name="to">The date and time for the end of the range</param>
        ///<param name="format">Protocol - e.g CSV, ITCH</param>
        public TopOfBookHistoricMarketDataRequest(long instructionId, long instrumentId, DateTime from, DateTime to,
                                                  Format format)
        {
            _instructionId = instructionId;
            _instrumentId = instrumentId;
            _from = from;
            _to = to;
            _format = format;
        }

        public string Uri
        {
            get { return "/secure/read/marketData/requestHistoricMarketData"; }
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
                ValueOrNone("instructionId", _instructionId).
                ValueOrNone("orderBookId", _instrumentId).
                ValueOrNone("from", Convert.ToString(DateTimeUtil.DateTimeToMillis(_from))).
                ValueOrNone("to", Convert.ToString(DateTimeUtil.DateTimeToMillis(_to))).
                StartElement("orderBook").
                    StartElement("options").
                        ValueOrNone("option", "BID").
                        ValueOrNone("option", "ASK").
                    EndElement("options").
                ValueOrNone("depth", Depth).
                ValueOrNone("format", Convert.ToString(_format).ToUpper()).
                EndElement("orderBook").
                EndElement("body").
                EndElement("req");
        }
    }
}