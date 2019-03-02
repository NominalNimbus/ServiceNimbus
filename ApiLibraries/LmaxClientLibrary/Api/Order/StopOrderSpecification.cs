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
    ///<summary>
    /// Stop order request.  Can be reused, but the instructionID needs to be reset every time it is submitted.
    ///</summary>
    public class StopOrderSpecification : OrderSpecification
    {
        private decimal _stopPrice;

        /// <summary>
        /// Construct a Stop order that contains a stop loss and/or stop profit price offset.
        /// </summary>
        /// <param name="instructionId">The client specified instruction id, should be unique for an account.</param>
        /// <param name="instrumentId">The instrument id of the OrderBook to place the order on.</param>
        /// <param name="stopPrice">The trigger price which will cause the market order to be placed.</param>
        /// <param name="quantity">The size of the Market order.  The side of the order is inferred
        /// from the sign of the quantity.  A positive value is a buy, negative is sell.</param>
        /// <param name="timeInForce">A <see cref="TimeInForce"/> that describes the time the stop will remain
        /// valid options, GoodForDay and GoodTilCancelled </param>
        /// <param name="stopLossPriceOffset">The stop loss offset, set this to null to not specify a stop loss.</param>
        /// <param name="stopProfitPriceOffset">The stop profit offset, set this to null to not specify a stop profit.</param>
        public StopOrderSpecification(string instructionId, long instrumentId, decimal stopPrice, decimal quantity, TimeInForce timeInForce, decimal? stopLossPriceOffset, decimal? stopProfitPriceOffset) : 
            base(instructionId, instrumentId, timeInForce, quantity, stopLossPriceOffset, stopProfitPriceOffset)
        {
            _stopPrice = stopPrice;
        }

        /// <summary>
        /// Construct a Stop order that contains a stop loss and/or stop profit price offset.
        /// </summary>
        /// <param name="instructionId">The client specified instruction id, should be unique for an account.</param>
        /// <param name="instrumentId">The instrument id of the OrderBook to place the order on.</param>
        /// <param name="stopPrice">The trigger price which will cause the market order to be placed.</param>
        /// <param name="quantity">The size of the Market order.  The side of the order is inferred
        /// from the sign of the quantity.  A positive value is a buy, negative is sell.</param>
        /// <param name="timeInForce">A <see cref="TimeInForce"/> that describes the time the stop will remain
        /// valid options, GoodForDay and GoodTilCancelled </param>
        public StopOrderSpecification(string instructionId, long instrumentId, decimal stopPrice, decimal quantity, TimeInForce timeInForce)
            : this(instructionId, instrumentId, stopPrice, quantity, timeInForce,  null, null)
        {
        }

        /// <summary>
        /// Internal: Output this request.
        /// </summary>
        /// <param name="writer">The destination for the content of this request</param>
        public override void WriteTo(IStructuredWriter writer)
        {
            writer.
                StartElement("req").
                    StartElement("body").
                        StartElement("order").
                            ValueOrNone("instrumentId", InstrumentId).
                            ValueOrNone("instructionId", InstructionId).
                            ValueOrNone("stopPrice", StopPrice).
                            ValueOrNone("quantity", Quantity).
                            ValueOrNone("timeInForce", Enum.GetName(TimeInForce.GetType(), TimeInForce)).
                            ValueOrNone("stopLossOffset", StopLossPriceOffset).
                            ValueOrNone("stopProfitOffset", StopProfitPriceOffset).
                        EndElement("order").
                    EndElement("body").
                EndElement("req");
        }


        protected override decimal? GetPrice()
        {
            return null;
            
        }

        public decimal StopPrice
        {
            get { return _stopPrice; }
            set { _stopPrice = value; }
        }
    }
}
