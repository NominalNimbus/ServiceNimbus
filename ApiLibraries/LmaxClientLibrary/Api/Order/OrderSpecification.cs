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
    /// Base class for all order specifications.
    /// </summary>
    public abstract class OrderSpecification : IOrderSpecification
    {
        private string _instructionId;
        private long _instrumentId;
        private decimal _quantity;
        private decimal? _stopLossPriceOffset;
        private decimal? _stopProfitPriceOffset;
        private TimeInForce _timeInForce;
        
        protected OrderSpecification(string instructionId, long instrumentId, TimeInForce timeInForce, decimal quantity,
                                     decimal? stopLossPriceOffset, decimal? stopProfitPriceOffset)
        {
            _instructionId = instructionId;
            _instrumentId = instrumentId;
            _timeInForce = timeInForce;
            _quantity = quantity;
            _stopLossPriceOffset = stopLossPriceOffset;
            _stopProfitPriceOffset = stopProfitPriceOffset;
        }

        /// <summary>
        /// Get/Set the instruction id use for tracking the order.
        /// </summary>
        public string InstructionId
        {
            get { return _instructionId; }
            set { _instructionId = value; }
        }
        
        /// <summary>
        /// Get/Set the instrument id that the order should be placed on.
        /// </summary>
        public long InstrumentId
        {
            get { return _instrumentId; }
            set { _instrumentId = value; }
        }

        /// <summary>
        /// Get/Set the of the order, the sign infers the side of the order.
        /// A positive value is a buy, negative indicates sell.
        /// </summary>
        public decimal Quantity
        {
            get { return _quantity; }
            set { _quantity = value; }
        }

        /// <summary>
        /// Get/Set the stop loss offset for the order, null to indicates that
        /// the order does not have a stop less.
        /// </summary>
        public decimal? StopLossPriceOffset
        {
            get { return _stopLossPriceOffset; }
            set { _stopLossPriceOffset = value; }
        }

        /// <summary>
        /// Get/Set the stop profit offset for the order, null to indicates that
        /// the order does not have a stop profit.
        /// </summary>
        public decimal? StopProfitPriceOffset
        {
            get { return _stopProfitPriceOffset; }
            set { _stopProfitPriceOffset = value; }
        }

        /// <summary>
        /// Get/Set the <see cref="Com.Lmax.Api.TimeInForce"/> for the order.
        /// </summary>
        public TimeInForce TimeInForce
        {
            get { return _timeInForce; }
            set { _timeInForce = value; }
        }

        public string Uri
        {
            get { return "/secure/trade/placeOrder"; }
        }

        /// <summary>
        /// Internal: Output this request.
        /// </summary>
        /// <param name="writer">The destination for the content of this request</param>
        public virtual void WriteTo(IStructuredWriter writer)
        {
            writer.
                StartElement("req").
                    StartElement("body").
                        StartElement("order").
                            ValueOrNone("instrumentId", _instrumentId).
                            ValueOrNone("instructionId", _instructionId).
                            ValueOrNone("price", GetPrice()).
                            ValueOrNone("quantity", _quantity).
                            ValueOrNone("timeInForce", Enum.GetName(TimeInForce.GetType(), _timeInForce)).
                            ValueOrNone("stopLossOffset", _stopLossPriceOffset).
                            ValueOrNone("stopProfitOffset", _stopProfitPriceOffset).
                        EndElement("order").
                    EndElement("body").
                EndElement("req");
        }

        protected abstract decimal? GetPrice();
    }
}