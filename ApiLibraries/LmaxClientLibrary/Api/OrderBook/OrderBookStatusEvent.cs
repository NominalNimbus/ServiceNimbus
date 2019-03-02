/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

namespace Com.Lmax.Api.OrderBook
{
    /// <summary>
    /// The contents of the event raised whenever there is a change to the
    /// status of a given order book.
    /// </summary>
    public sealed class OrderBookStatusEvent
    {
        private readonly long _instrumentId;
        private readonly OrderBookStatus _status;

        public OrderBookStatusEvent(long instrumentId, OrderBookStatus status)
        {
            _instrumentId = instrumentId;
            _status = status;
        }
        
        /// <summary>
        /// Instrument id of the OrderBook. Same as the value used in subscribing.
        /// </summary>
        public long InstrumentId
        {
            get { return _instrumentId; }
        }

        /// <summary>
        /// Current status of the order book.
        /// </summary>
        public OrderBookStatus Status
        {
            get { return _status; }
        }

        public bool Equals(OrderBookStatusEvent other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other._instrumentId == _instrumentId && other._status == _status;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(OrderBookStatusEvent)) return false;
            return Equals((OrderBookStatusEvent)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = _instrumentId.GetHashCode();
                result = (result * 397) ^ _status.GetHashCode();
                return result;
            }
        }

        public override string ToString()
        {
            return string.Format("InstrumentId: {0}, Status: {1}", _instrumentId, _status);
        }
    }

    /// <summary>
    /// The product type used to connect to the LMAX Trader platform. 
    /// </summary>
    public enum OrderBookStatus
    {
        ///<summary>
        /// The order book has been newly created.
        ///</summary>
        New,
        ///<summary>
        /// The order book is accepting orders. 
        ///</summary>
        Opened,
        ///<summary>
        /// The order book is temporarily not accepting orders. 
        ///</summary>
        Suspended,
        ///<summary>
        /// The order book is closed and waiting to be opened. 
        ///</summary>
        Closed,
        ///<summary>
        /// The trades on the order book have been settled. 
        ///</summary>
        Settled,
        ///<summary>
        /// The order book is no longer trading. 
        ///</summary>
        Withdrawn
    }
}
