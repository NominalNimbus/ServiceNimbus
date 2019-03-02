/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

namespace Com.Lmax.Api.Order
{
    ///<summary>
    /// Market order request.  Can be reused, but the instructionID needs to be reset every time it is submitted.
    ///</summary>
    public class MarketOrderSpecification : OrderSpecification
    {
        /// <summary>
        /// Construct a Market Order that contains a stop profit and/or stop loss offset.
        /// </summary>
        /// <param name="instructionId">The client specified instruction id, should be unique for an account.</param>
        /// <param name="instrumentId">The instrument id of the OrderBook to place the order on.</param>
        /// <param name="quantity">The size of the market order.  The side of the order is inferred
        /// from the sign of the quantity.  A positive value is a buy, negative is sell.</param>
        /// <param name="timeInForce">A <see cref="TimeInForce"/> that describes the behaviour of the order
        /// if not fully filled</param>
        /// <param name="stopLossPriceOffset">The stop loss offset, set this to null to not specify a stop loss.</param>
        /// <param name="stopProfitPriceOffset">The stop profit offset, set this to null to not specify a stop profit.</param>
        public MarketOrderSpecification(string instructionId, long instrumentId, decimal quantity, TimeInForce timeInForce, decimal? stopLossPriceOffset, decimal? stopProfitPriceOffset) :
            base(instructionId, instrumentId, timeInForce, quantity, stopLossPriceOffset, stopProfitPriceOffset)
        {   
        }

        /// <summary>
        /// Construct and Market Order Specification that does not contain a stop profit or stop loss offset.
        /// </summary>
        /// <param name="instructionId">The client specified instruction id, should be unique for an account.</param>
        /// <param name="instrumentId">The instrument id of the OrderBook to place the order on.</param>
        /// <param name="quantity">The size of the market order.  The side of the order is inferred
        /// from the sign of the quantity.  A positive value is a buy, negative is sell.</param>
        /// <param name="timeInForce">A <see cref="TimeInForce"/> that describes the behaviour of the order
        /// if not fully filled</param>
        public MarketOrderSpecification(string instructionId, long instrumentId, decimal quantity, TimeInForce timeInForce) :
            this(instructionId, instrumentId, quantity, timeInForce, null, null)
        {
        }
        
        protected override decimal? GetPrice()
        {
            return null;
        }
    }
}
