/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;

namespace Com.Lmax.Api.Position
{
    /// <summary>
    /// Represents a position on a instrument, held by the account. A number of
    /// fields refer to the term 'cost'.  Cost is price x quantity.  A position is
    /// an aggregate view of all of the open and unfilled orders 
    /// </summary>
    public class PositionEvent : IEquatable<PositionEvent>
    {
        private readonly long _accountId;
        private readonly long _instrumentId;
        private readonly decimal _shortUnfilledCost;
        private readonly decimal _longUnfilledCost;
        private readonly decimal _openQuantity;
        private readonly decimal _cumulativeCost;
        private readonly decimal _openCost;

        ///<summary>
        /// Create an object that contains the details of the changes that occurred to a position.
        ///</summary>
        ///<param name="accountId">The ID of the account that holds the position</param>
        ///<param name="instrumentId">The instrument the position is on</param>
        ///<param name="shortUnfilledCost">The cost of the unfilled sell positions against this instrument</param>
        ///<param name="longUnfilledCost">The cost of the unfilled buy positions against this instrument</param>
        ///<param name="openQuantity">The filled quantity of this position, signed, e.g. sell is a negative value</param>
        ///<param name="cumulativeCost">The cost accumulated so far for holiding this position</param>
        ///<param name="openCost">The cost of opening this position</param>
        public PositionEvent(long accountId, long instrumentId, decimal shortUnfilledCost, decimal longUnfilledCost, decimal openQuantity, decimal cumulativeCost, decimal openCost)
        {
            _accountId = accountId;
            _openCost = openCost;
            _cumulativeCost = cumulativeCost;
            _openQuantity = openQuantity;
            _longUnfilledCost = longUnfilledCost;
            _shortUnfilledCost = shortUnfilledCost;
            _instrumentId = instrumentId;
        }

        /// <summary>
        /// Get the id for the account that the position belongs to.
        /// </summary>
        public long AccountId
        {
            get { return _accountId; }
        }

        /// <summary>
        /// Get the id for the instrument that the position pertains to.
        /// </summary>
        public long InstrumentId
        {
            get { return _instrumentId; }
        }
        
        /// <summary>
        /// Get the short unfilled cost for this position.  I.e. the cost of sell
        /// orders that have not filled.
        /// </summary>
        public decimal ShortUnfilledCost
        {
            get { return _shortUnfilledCost; }
        }

        /// <summary>
        /// Get the long unfilled cost for this position.  I.e. the cost of buy
        /// orders that have not filled.
        /// </summary>
        public decimal LongUnfilledCost
        {
            get { return _longUnfilledCost; }
        }

        /// <summary>
        /// Get the net open quantity for this position.
        /// </summary>        
        public decimal OpenQuantity
        {
            get { return _openQuantity; }
        }

        /// <summary>
        /// The total cost incurred over the lifetime of the position.
        /// </summary>
        public decimal CumulativeCost
        {
            get { return _cumulativeCost; }
        }

        /// <summary>
        /// The cost to establish the current open position.
        /// </summary>
        public decimal OpenCost
        {
            get { return _openCost; }
        }

        public override string ToString()
        {
            return string.Format("AccountId: {0}, InstrumentId: {1}, ShortUnfilledCost: {2}, LongUnfilledCost: {3}, OpenQuantity: {4}, CumulativeCost: {5}, OpenCost: {6}", 
                                 _accountId, _instrumentId, _shortUnfilledCost, _longUnfilledCost, _openQuantity, _cumulativeCost, _openCost);
        }

        public bool Equals(PositionEvent other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other._accountId == _accountId && other._instrumentId == _instrumentId && other._shortUnfilledCost == _shortUnfilledCost && other._longUnfilledCost == _longUnfilledCost && other._openQuantity == _openQuantity && other._cumulativeCost == _cumulativeCost && other._openCost == _openCost;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (PositionEvent)) return false;
            return Equals((PositionEvent) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = _accountId.GetHashCode();
                result = (result*397) ^ _instrumentId.GetHashCode();
                result = (result*397) ^ _shortUnfilledCost.GetHashCode();
                result = (result*397) ^ _longUnfilledCost.GetHashCode();
                result = (result*397) ^ _openQuantity.GetHashCode();
                result = (result*397) ^ _cumulativeCost.GetHashCode();
                result = (result*397) ^ _openCost.GetHashCode();
                return result;
            }
        }

        public static bool operator ==(PositionEvent left, PositionEvent right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PositionEvent left, PositionEvent right)
        {
            return !Equals(left, right);
        }
    }
}
