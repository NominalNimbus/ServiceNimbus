/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using Com.Lmax.Api.Internal.Xml;
using Com.Lmax.Api.Internal;

namespace Com.Lmax.Api.MarketData
{
    ///<summary>
    /// A request for market data.
    ///</summary>
    public class AggregateHistoricMarketDataRequest : IHistoricMarketDataRequest
    {
        private readonly long _instructionId;
        private readonly long _instrumentId;
        private readonly DateTime _from;
        private readonly DateTime _to;
        private readonly Option[] _options;
        private readonly Resolution _resolution;
        private readonly Format _format;
        private readonly int _depth;

        ///<summary>
        /// Request market data for a given instrument, for a given date range and in a given format.  The data returned will be for at least the requested date range. 
        /// For example, if the user asks for 10 days of data they might get a response containing a whole month, but those 10 days will be contained in the month.
        ///</summary>
        ///<param name="instructionId">A unique ID to identify this request</param>
        ///<param name="instrumentId">The ID of the instrument to return the market data for</param>
        ///<param name="from">The date and time of the start of the range</param>
        ///<param name="to">The date and time for the end of the range</param>        
        ///<param name="resolution">Granularity - e.g. tick/minute/day</param>
        ///<param name="format">Protocol - e.g CSV, ITCH</param>
        ///<param name="options">The type of prices to be returned</param>
        public AggregateHistoricMarketDataRequest(long instructionId, long instrumentId, DateTime from, DateTime to,
                                                  Resolution resolution, Format format, params Option[] options)
        {
            _instructionId = instructionId;
            _instrumentId = instrumentId;
            _from = from;
            _to = to;
            _options = options;
            _resolution = resolution;
            _format = format;
            _depth = 1;
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
                StartElement("aggregate");
                            WriteOptions(writer).
                            ValueOrNone("resolution", Convert.ToString(_resolution).ToUpper()).
                            ValueOrNone("depth", _depth).
                            ValueOrNone("format", Convert.ToString(_format).ToUpper()).
                            EndElement("aggregate").
                    EndElement("body").
                EndElement("req");
        }

        private IStructuredWriter WriteOptions(IStructuredWriter writer)
        {
            if (_options.Length <= 0) return writer;
            writer.StartElement("options");
            foreach (Option option in _options)
            {
                writer.ValueOrNone("option", Convert.ToString(option).ToUpper());
            }
            writer.EndElement("options");
            return writer;
        }
    }

    /// <summary>
    /// Defines the different types of data that can be returned.
    /// </summary>
    public enum Option
    {
        ///<summary>
        /// Bid Prices
        ///</summary>
        Bid,
        ///<summary>
        /// Ask Prices
        ///</summary>
        Ask
    }
}