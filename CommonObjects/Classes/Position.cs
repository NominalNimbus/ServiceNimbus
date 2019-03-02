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
    [DataContract]
    [Serializable]
    public class Position : ICloneable
    {
        [DataMember]
        public string Symbol { get; set; }
        [DataMember]
        public string BrokerName { get; set; }
        [DataMember]
        public string DataFeedName { get; set; }
        [DataMember]
        public string UserName { get; set; }
        [DataMember]
        public string AccountId { get; set; }
        [DataMember]
        public decimal Quantity { get; set; }
        [DataMember]
        public Side PositionSide { get; set; }
        [DataMember]
        public decimal Price { get; set; }  //Avg. fill price. //currencyBase
        [DataMember]
        public decimal CurrentPrice { get; set; }
        [DataMember]
        public decimal Profit { get; set; }
        [DataMember]
        public decimal PipProfit { get; set; }
        [DataMember]
        public decimal Margin { get; set; }

        public decimal AbsQuantity => Math.Abs(Quantity);

        public Position()
        {
            Symbol = string.Empty;
            BrokerName = string.Empty;
            AccountId = string.Empty;
            UserName = string.Empty;
            DataFeedName = string.Empty;

        }

        public Position(string symbol)
        {
            Symbol = symbol;
            DataFeedName = string.Empty;
        }

        #region ICloneable

        object ICloneable.Clone() => MemberwiseClone();

        public Position Clone() => new Position
        {
            Symbol = Symbol,
            PipProfit = PipProfit,
            Profit = Profit,
            UserName = UserName,
            AccountId = AccountId,
            DataFeedName = DataFeedName,
            BrokerName = BrokerName,
            Quantity = Quantity,
            CurrentPrice = CurrentPrice,
            Price = Price,
            PositionSide = PositionSide
        };

        #endregion

    }
}