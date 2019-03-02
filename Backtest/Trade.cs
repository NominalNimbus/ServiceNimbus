/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;

namespace Backtest
{
    public class Trade
    {

        #region Properties

        public SignalType Signal { get; }
        public DateTime Date { get; }
        public decimal Quantity { get; }
        public decimal Price { get; set; }

        #endregion
        
        #region Constructors

        public Trade()
        {
        }

        public Trade(DateTime date, SignalType signal, decimal price, decimal quantity)
        {
            Date = date;
            Signal = signal;
            Price = Math.Abs(price);
            Quantity = quantity == 0M ? 1M : Math.Abs(quantity);
        }

        #endregion
        
        #region Operators

        public static bool operator <(Trade lsh, Trade rsh) =>
            lsh.Date < rsh.Date;

        public static bool operator >(Trade lsh, Trade rsh) =>
            lsh.Date > rsh.Date;

        public static bool operator ==(Trade lsh, Trade rsh) =>
            lsh?.Date == rsh?.Date;

        public static bool operator !=(Trade lsh, Trade rsh) =>
            lsh?.Date != rsh?.Date;

        #endregion

        #region Overrides

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != typeof(Trade))
                return false;

            return Equals((Trade)obj);
        }

        public override int GetHashCode() => 
            Date.GetHashCode();

        #endregion

        #region Helprers

        private bool Equals(Trade other)
        {
            if (other == null)
                return false;

            return ReferenceEquals(this, other) || other.Date.Equals(Date);
        }

        #endregion
        
    }
}