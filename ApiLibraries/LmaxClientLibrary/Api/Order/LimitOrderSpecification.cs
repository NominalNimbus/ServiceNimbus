/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;

namespace Com.Lmax.Api.Order
{
    ///<summary>
    /// Limit order request.  Can be reused, but the instructionID needs to be reset every time it is submitted.
    ///</summary>
    public class LimitOrderSpecification : OrderSpecification
    {
        private decimal _price;

        /// <summary>
        /// Construct a Limit Order that contains a stop loss and/or stop profit price offset.
        /// </summary>
        /// <param name="instructionId">The client specified instruction id, should be unique for an account.</param>
        /// <param name="instrumentId">The instrument id of the OrderBook to place the order on.</param>
        /// <param name="price">The price for the order to be placed into the book if not aggressively filled.</param>
        /// <param name="quantity">The size of the limit order.  The side of the order is inferred
        /// from the sign of the quantity.  A positive value is a buy, negative is sell.</param>
        /// <param name="timeInForce">A <see cref="TimeInForce"/> that describes the behaviour of the order
        /// if not fully filled</param>
        /// <param name="stopLossPriceOffset">The stop loss offset, set this to null to not specify a stop loss.</param>
        /// <param name="stopProfitPriceOffset">The stop profit offset, set this to null to not specify a stop profit.</param>
        public LimitOrderSpecification(string instructionId, long instrumentId, decimal price, decimal quantity, TimeInForce timeInForce, decimal? stopLossPriceOffset, decimal? stopProfitPriceOffset) :
            base(instructionId, instrumentId, timeInForce, quantity, stopLossPriceOffset, stopProfitPriceOffset)
        {
            Price = price;
        }

        /// <summary>
        /// Construct a Limit Order that  does not contains a stop loss and/or stop profit price offset.
        /// </summary>
        /// <param name="instructionId">The client specified instruction id, should be unique for an account.</param>
        /// <param name="instrumentId">The instrument id of the OrderBook to place the order on.</param>
        /// <param name="price">The price for the order to be placed into the book if not aggressively filled.</param>
        /// <param name="quantity">The size of the limit order.  The side of the order is inferred
        /// from the sign of the quantity.  A positive value is a buy, negative is sell.</param>
        /// <param name="timeInForce">A <see cref="TimeInForce"/> that describes the behaviour of the order
        /// if not fully filled</param>
        public LimitOrderSpecification(string instructionId, long instrumentId, decimal price, decimal quantity, TimeInForce timeInForce) : this(instructionId, instrumentId, price, quantity, timeInForce, null, null)
        {
        }

        protected override decimal? GetPrice()
        {
            return _price;
        }

        /// <summary>
        /// Get/Set the price of the order, allows order instance to be reused.
        /// </summary>
        public decimal Price
        {
            get { return _price; }
            set { _price = value; }
        }
    }
}
