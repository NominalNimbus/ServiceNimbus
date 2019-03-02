/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

namespace Com.Lmax.Api.MarketData
{
    ///<summary>
    /// Marker interface for the different types of requests for historic market data.
    ///</summary>
    public interface IHistoricMarketDataRequest : IRequest
    {
    }

    ///<summary>
    /// The format the historic market data is returned in.
    ///</summary>
    public enum Format
    {
        ///<summary>
        /// Comma Separated Values, not a fixed format, the columns will be different according to the different types of data returned.
        ///</summary>
        Csv
    }

    ///<summary>
    /// The time period the data will be aggregated over.
    ///</summary>
    public enum Resolution
    {
        Minute,
        Day
    }
}