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
    /// Results of a action on the exchange the affects an order.
    /// </summary>
    public class Execution
    {
        private readonly long _exceutionId;
        private readonly decimal _price;
        private readonly decimal _quantity;
        private readonly Order _order;
        private readonly decimal _cancelledQuantity;

        /// <summary>
        /// Exposed for testing
        /// </summary>
        public Execution(long exceutionId, decimal price, decimal quantity, Order order, decimal cancelledQuantity)
        {
            _exceutionId = exceutionId;
            _price = price;
            _quantity = quantity;
            _order = order;
            _cancelledQuantity = cancelledQuantity;
        }

        /// <summary>
        /// Get the execution id, which is the key for this execution.  It is 
        /// unique within an order book.
        /// </summary>
        public long ExecutionId
        {
            get { return _exceutionId; }
        }
        
        /// <summary>
        /// Get the price at which the execution took place.
        /// </summary>
        public decimal Price
        {
            get { return _price; }
        }
        
        /// <summary>
        /// Get the size of the execution.
        /// </summary>
        public decimal Quantity
        {
            get { return _quantity; }
        }
        
        /// <summary>
        /// Get the order that this execution affected.
        /// </summary>
        public Order Order
        {
            get { return _order; }
        }
        
        /// <summary>
        /// Get quantity of the order that was cancelled in this execution.
        /// </summary>
        public decimal CancelledQuantity
        {
            get { return _cancelledQuantity; }
        }

        public bool Equals(Execution other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other._exceutionId == _exceutionId && other._price == _price && other._quantity == _quantity && Equals(other._order, _order) && other._cancelledQuantity == _cancelledQuantity;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (Execution)) return false;
            return Equals((Execution) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = _exceutionId.GetHashCode();
                result = (result*397) ^ _price.GetHashCode();
                result = (result*397) ^ _quantity.GetHashCode();
                result = (result*397) ^ (_order != null ? _order.GetHashCode() : 0);
                result = (result*397) ^ _cancelledQuantity.GetHashCode();
                return result;
            }
        }

        public override string ToString()
        {
            return string.Format("Execution{{ExceutionId: {0}, Price: {1}, Quantity: {2}, Order: {3}, CancelledQuantity: {4}}}", _exceutionId, _price, _quantity, _order, _cancelledQuantity);
        }
    }
}
