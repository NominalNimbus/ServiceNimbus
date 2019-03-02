/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Runtime.Serialization;

namespace CommonObjects
{
    /// <summary>
    /// implements functionality to store and compare items
    /// </summary>
    [DataContract]
    [Serializable]
    public class Security
    {
        [DataMember]
        public string DataFeed;

        [DataMember] 
        public string Symbol;
        [DataMember]
        public string Name;
        [DataMember]
        public int SecurityId;
        [DataMember]
        public string BaseCurrency;
        [DataMember]
        public string UnitOfMeasure;
        [DataMember]
        public string AssetClass;
        [DataMember]
        public int Digit;
        [DataMember]
        public decimal PriceIncrement;
        [DataMember]
        public decimal QtyIncrement;
        [DataMember]
        public decimal MarginRate;
        [DataMember]
        public decimal MaxPosition;
        [DataMember]
        public decimal UnitPrice;
        [DataMember]
        public decimal ContractSize;
        [DataMember]
        public TimeSpan MarketOpen;
        [DataMember]
        public TimeSpan MarketClose;

        public ICommisionCalculator CommisionCalculator { get; set; }

        public decimal CaclulateCommision(Order order, decimal fillCash, decimal fillQuantity) =>
            CommisionCalculator?.CalculateCommission(order, fillCash, fillQuantity) ?? 0;

        /// <summary>
        /// equalizes objects
        /// </summary>
        /// <param name="obj">object to compare</param>
        /// <returns>true, if two objects are equal</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            Security o = obj as Security;

            if ((object)o == null)
                return false;
            
            return DataFeed == o.DataFeed && Symbol == o.Symbol;
        }

        /// <summary>
        /// compares two objects
        /// </summary>
        /// <param name="a">left object</param>
        /// <param name="b">right object</param>
        /// <returns>true, if equal</returns>
        public static bool operator ==(Security a, Security b)
        {
            if (ReferenceEquals(a, null))
                return ReferenceEquals(b, null);

            return a.Equals(b);
        }

        public string GetKey()
            => DataFeed + Symbol.ToLower();

        /// <summary>
        /// compares two objects
        /// </summary>
        /// <param name="a">left object</param>
        /// <param name="b">right object</param>
        /// <returns>true, if non equal</returns>
        public static bool operator != (Security a, Security b)
        {
            return !(a == b);
        }

        /// <summary>
        /// calculates hash code
        /// </summary>
        /// <returns>hash code</returns>
        public override int GetHashCode()
        {
            return String.Format("{0}:{1}"
                , (DataFeed == null) ? String.Empty : DataFeed
                , (Symbol == null) ? String.Empty : Symbol).GetHashCode();
        }

    }
}