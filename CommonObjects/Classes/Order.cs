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
    public class Order : ICloneable
    {
        [DataMember]
        public string BrokerID { get; set; }
        [DataMember]
        public string UserID { get; set; }
        [DataMember]
        public string SignalID{ get; set; }
        [DataMember]
        public string Symbol { get; set; }
        [DataMember]
        public string BrokerName { get; set; }
        [DataMember]
        public string DataFeedName { get; set; }
        [DataMember]
        public string AccountId { get; set; }
        [DataMember]
        public decimal Quantity { get; set; }
        [DataMember]
        public decimal FilledQuantity { get; set; }
        [DataMember]
        public decimal OpenQuantity { get; set; }  //number of shares to track via FIFO system
        [DataMember]
        public decimal CancelledQuantity { get; set; }
        [DataMember]
        public OrderType OrderType { get; set; }
        [DataMember]
        public Side OrderSide { get; set; }
        [DataMember]
        public decimal Commission { get; set; }
        [DataMember]
        public decimal AvgFillPrice { get; set; }
        [DataMember]
        public decimal Price { get; set; }
        [DataMember]
        public decimal? SLOffset { get; set; }
        [DataMember]
        public decimal? TPOffset { get; set; }
        [DataMember]
        public decimal CurrentPrice { get; set; }
        [DataMember]
        public DateTime OpenDate { get; set; }
        [DataMember]
        public DateTime PlacedDate { get; set; }
        [DataMember]
        public DateTime FilledDate { get; set; }
        [DataMember]
        public TimeInForce TimeInForce { get; set; }
        [DataMember]
        public decimal Profit { get; set; }
        [DataMember]
        public decimal PipProfit { get; set; }
        [DataMember]
        public decimal OpeningQty { get; set; }
        [DataMember]
        public decimal ClosingQty { get; set; }
        [DataMember]
        public string Origin { get; set; }
        [DataMember]
        public bool ServerSide { get; set; }

        public string UniqueUserId => UserID + Symbol + AccountId;

        public bool IsActive => OpenQuantity != 0 || Quantity != Math.Abs(FilledQuantity) + CancelledQuantity;

        public decimal QuantityToFill => Quantity - CancelledQuantity - FilledQuantity;

        public Order()
        {
            UserID = string.Empty;
            BrokerID = string.Empty;
            DataFeedName = string.Empty;
            Symbol = string.Empty;
            BrokerName = string.Empty;
            AccountId = string.Empty;
            OpenDate = DateTime.UtcNow;
        }

        public Order(string id, string symbol) : this()
        {
            UserID = id;
            Symbol = symbol;
        }

        public void SetAbsValuesForQuantities()
        {
            if (CancelledQuantity < 0)
                CancelledQuantity = -CancelledQuantity;
            if (FilledQuantity < 0)
                FilledQuantity = -FilledQuantity;
            if (OpenQuantity < 0)
                OpenQuantity = -OpenQuantity;
            if (Quantity < 0)
                Quantity = -Quantity;
        }

        #region ICloneable

        object ICloneable.Clone() => MemberwiseClone();

        public Order Clone() => new Order
        {
            Symbol = Symbol,
            PlacedDate = PlacedDate,
            FilledQuantity = FilledQuantity,
            OpenQuantity = OpenQuantity,
            Quantity = Quantity,
            FilledDate = FilledDate,
            AccountId = AccountId,
            AvgFillPrice = AvgFillPrice,
            BrokerID = BrokerID,
            BrokerName = BrokerName,
            CancelledQuantity = CancelledQuantity,
            ClosingQty = ClosingQty,
            Commission = Commission,
            CurrentPrice = CurrentPrice,
            DataFeedName = DataFeedName,
            ServerSide = ServerSide,
            OpenDate = OpenDate,
            OpeningQty = OpeningQty,
            OrderSide = OrderSide,
            OrderType = OrderType,
            Origin = Origin,
            PipProfit = PipProfit,
            Price = Price,
            Profit = Profit,
            SLOffset = SLOffset,
            SignalID = SignalID,
            TPOffset = TPOffset,
            TimeInForce = TimeInForce,
            UserID = UserID
        };

        #endregion
        
    }
}
