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
    /// <summary>
    /// The order as represented on the exchange
    /// </summary>
    public class Order : IEquatable<Order>
    {
        private readonly string _instructionId;
        private readonly string _orderId;
        private readonly long _instrumentId;
        private readonly long _accountId;
        private readonly decimal? _price;
        private readonly decimal? _stopLossOffset;
        private readonly decimal? _stopProfitOffset;
        private readonly decimal? _stopReferencePrice;
        private readonly decimal _quantity;
        private readonly decimal _filledQuantity;
        private readonly decimal _cancelledQuantity;
        private readonly OrderType _orderType;
        private readonly TimeInForce _timeInForce;
        private readonly decimal _commission;

        public Order(string instructionId, string orderId, long instrumentId, long accountId, decimal? price, decimal quantity, decimal filledQuantity,
                 decimal cancelledQuantity, OrderType orderType, TimeInForce timeInForce, decimal? stopLossOffset, decimal? stopProfitOffset, decimal? stopReferencePrice, decimal commission)
        {
            _instructionId = instructionId;
            _orderId = orderId;
            _instrumentId = instrumentId;
            _accountId = accountId;
            _price = price;
            _quantity = quantity;
            _filledQuantity = filledQuantity;
            _cancelledQuantity = cancelledQuantity;
            _orderType = orderType;
            _timeInForce = timeInForce;
            _stopReferencePrice = stopReferencePrice;
            _stopProfitOffset = stopProfitOffset;
            _stopLossOffset = stopLossOffset;
            _commission = commission;
        }



        /// <summary>
        /// Get the id of the instruction that placed the order.  Can be used to
        /// correlate with the original request.
        /// </summary>
        public string InstructionId
        {
            get { return _instructionId; }
        }
        
        /// <summary>
        /// Get the id of the order.
        /// </summary>
        public string OrderId
        {
            get { return _orderId; }
        }
        
        /// <summary>
        /// Get the id of the instrument on which the order was placed.
        /// </summary>
        public long InstrumentId
        {
            get { return _instrumentId; }
        }
        
        /// <summary>
        /// Get the id of the account that placed the order.
        /// </summary>
        public long AccountId
        {
            get { return _accountId; }
        }
        
        /// <summary>
        /// Get the type of the order.
        /// </summary>
        public OrderType OrderType
        {
            get { return _orderType; }
        }
        
        /// <summary>
        /// Get the quantity of the order.
        /// </summary>
        public decimal Quantity
        {
            get { return _quantity; }
        }
        
        /// <summary>
        /// Get the quantity of this order that has filled.
        /// </summary>
        public decimal FilledQuantity
        {
            get { return _filledQuantity; }
        }
        
        /// <summary>
        /// Get the limit price for this order, will be null if this is not
        /// a limit order.
        /// </summary>
        public decimal? LimitPrice
        {
            get { return _orderType == OrderType.LIMIT || _orderType == OrderType.STOP_PROFIT_LIMIT_ORDER ? _price : null; }
        }

        /// <summary>
        /// Get the stop price for this order, will be null if this is not
        /// a stop order.
        /// </summary>
        public decimal? StopPrice
        {
            get { return _orderType == OrderType.STOP_ORDER || _orderType == OrderType.STOP_LOSS_MARKET_ORDER ? _price : null; }
        }

        /// <summary>
        /// Get the quantity of this order that has been cancelled.
        /// </summary>
        public decimal CancelledQuantity
        {
            get { return _cancelledQuantity; }
        }
        
        /// <summary>
        /// Get the distance from the <code>StopReferencePrice</code> at which the stop loss order trigger will be placed.
        /// Will be null if no stop loss requested for this order.
        /// </summary>
        public decimal? StopLossOffset
        {
            get { return _stopLossOffset; }
        }

        /// <summary>
        /// Get the distance from the <code>StopReferencePrice</code> at which the stop profit order will be placed.
        /// Will be null if no stop profit requested for this order.
        /// </summary>
        public decimal? StopProfitOffset
        {
            get { return _stopProfitOffset; }
        }

        /// <summary>
        /// The price relative to which stop loss and stop profit prices will be calculated.
        /// Will be null for unmatched market orders.
        /// </summary>
        public decimal? StopReferencePrice
        {
            get { return _stopReferencePrice; }
        }

        /// <summary>
        /// The commulative commssion that you have aleady been charged for this order.
        /// </summary>
        public decimal Commission
        {
            get { return _commission;  }
        }

        /// <summary>
        /// The time in force for this order
        /// </summary>
        public TimeInForce TimeInForce
        {
            get { return _timeInForce; }
        }

        public bool Equals(Order other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other._instructionId == _instructionId && Equals(other._orderId, _orderId) && other._instrumentId == _instrumentId && 
                   other._accountId == _accountId && other._price.Equals(_price) && other._stopLossOffset.Equals(_stopLossOffset) && 
                   other._stopProfitOffset.Equals(_stopProfitOffset) && other._stopReferencePrice.Equals(_stopReferencePrice) && 
                   other._quantity == _quantity && other._filledQuantity == _filledQuantity && other._cancelledQuantity == _cancelledQuantity && other._commission == _commission && 
                   other._timeInForce == _timeInForce &&
                   Equals(other._orderType, _orderType);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (Order)) return false;
            return Equals((Order) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = _instructionId.GetHashCode();
                result = (result*397) ^ (_orderId != null ? _orderId.GetHashCode() : 0);
                result = (result*397) ^ _instrumentId.GetHashCode();
                return result;
            }
        }

        public static bool operator ==(Order left, Order right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Order left, Order right)
        {
            return !Equals(left, right);
        }


        public override string ToString()
        {
            return string.Format("Order{{InstructionId: {0}, OrderId: {1}, InstrumentId: {2}, AccountId: {3}, Price: {4}, StopLossOffset: {5}, StopProfitOffset: {6}, " +
                                 "StopReferencePrice: {7}, Quantity: {8}, FilledQuantity: {9}, CancelledQuantity: {10}, OrderType: {11}, Commission: {12}}}", 
                                 _instructionId, _orderId, _instrumentId, _accountId, _price, _stopLossOffset, _stopProfitOffset, _stopReferencePrice, _quantity, 
                                 _filledQuantity, _cancelledQuantity, _orderType, _commission);
        }
    }
    
    public enum OrderType
    {
        MARKET,
        LIMIT,
        STOP_ORDER,
        CLOSE_OUT_ORDER_POSITION,
        CLOSE_OUT_POSITION,
        STOP_LOSS_MARKET_ORDER,
        STOP_PROFIT_LIMIT_ORDER,
        SETTLEMENT_ORDER,
        OFF_ORDERBOOK,
        REVERSAL,
        UNKNOWN
    }
}
