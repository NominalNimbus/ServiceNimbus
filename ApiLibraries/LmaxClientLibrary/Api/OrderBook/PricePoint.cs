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
    /// Representation of that quantity at a particular price.  Used in
    /// the <see cref="OrderBookEvent"/> to represent market data.
    /// </summary>
    public struct PricePoint
    {
        private readonly decimal _price;
        private readonly decimal _quantity;

        ///<summary>
        /// Creates a new PricePoint
        ///</summary>
        ///<param name="quantity">The quantity</param>
        ///<param name="price">The price</param>
        public PricePoint(decimal quantity, decimal price)
        {
            _price = price;
            _quantity = quantity;
        }
  
        /// <summary>
        /// Readonly price. 
        /// </summary>
        public decimal Price
        {
            get { return _price; }
        }

        /// <summary>
        /// Readonly quantity. 
        /// </summary>
        public decimal Quantity
        {
            get { return _quantity; }
        }

        ///<summary>
        /// Compares this PricePoint to another one
        ///</summary>
        ///<param name="other">The PricePoint to compare this to</param>
        ///<returns>True if this PricePoint is equal to the second</returns>
        public bool Equals(PricePoint other)
        {
            return other._price == _price && other._quantity == _quantity;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != typeof (PricePoint)) return false;
            return Equals((PricePoint) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_price.GetHashCode()*397) ^ _quantity.GetHashCode();
            }
        }

        public override string ToString()
        {
            return string.Format("Price: {0}, Quantity: {1}", _price, _quantity);
        }
    }
}