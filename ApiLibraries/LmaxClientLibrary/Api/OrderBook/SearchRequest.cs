/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using Com.Lmax.Api.Internal.Xml;

namespace Com.Lmax.Api.OrderBook
{
    /// <summary>
    /// A request to get the system to push out a group of security definitions.
    /// </summary>
    public class SearchRequest : IRequest
    {
        private readonly string _queryString;
        private readonly long _offsetInstrumentId;

        /// <summary>
        /// Construct a request for a security definition.  Uses a query string that
        /// provides flexible query mechanism.  There are 2 main forms of the query string
        /// to find a specific instrument the "id: (instrumentId)" form can be used.
        /// To do a general search, use a term such as "CURRENCY", which will find
        /// all of the currency instruments.  A search term like "UK" will find all
        /// of the instruments that have "UK" in the name.
        /// </summary>
        /// <param name="queryString"></param>
        public SearchRequest(string queryString)
            : this(queryString, 0)
        {
        }

        /// <summary>
        /// Construct a request for a security definition.  Uses a query string that
        /// provides flexible query mechanism.  There are 2 main forms of the query string
        /// to find a specific instrument the "id: (instrumentId)" form can be used.
        /// To do a general search, use a term such as "CURRENCY", which will find
        /// all of the currency instruments.  A search term like "UK" will find all
        /// of the instruments that have "UK" in the name.
        /// </summary>
        /// <param name="queryString"></param>
        /// <param name="offsetInstrumentId"></param>
        public SearchRequest(string queryString, long offsetInstrumentId)
        {
            _queryString = queryString;
            _offsetInstrumentId = offsetInstrumentId;
        }

        public string Uri
        {
            get { return "/secure/instrument/searchCurrentInstruments?q=" + System.Uri.EscapeDataString(_queryString) + "&offset=" + _offsetInstrumentId; }
        }

        /// <summary>
        /// Internal: Output this request.
        /// </summary>
        /// <param name="writer">The destination for the content of this request</param>
        public void WriteTo(IStructuredWriter writer)
        {
            throw new InvalidOperationException("This is a GET request and it does not generate a body.");
        }

        public static string All
        {
            get { return ""; }
        }
    }
}
