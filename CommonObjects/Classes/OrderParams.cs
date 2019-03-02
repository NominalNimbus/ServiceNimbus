/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;

namespace CommonObjects
{
    [Serializable]
    public class OrderParams : ICloneable
    {
        public string UserID { get; set; }
        public string Symbol { get; set; }
        public string SignalId { get; set; }
        public decimal Quantity { get; set; }
        public OrderType OrderType { get; set; }
        public Side OrderSide { get; set; }
        public bool ServerSide { get; set; }
        public decimal Price { get; set; }
        public decimal? SLOffset { get; set; }
        public decimal? TPOffset { get; set; }
        public TimeInForce TimeInForce { get; set; }

        public OrderParams()
        {
            UserID = String.Empty;
            Symbol = String.Empty;
            OrderSide = Side.Buy;
            OrderType = OrderType.Market;
            ServerSide = false;
            TimeInForce = TimeInForce.GoodTilCancelled;
        }

        public OrderParams(string id, string symbol) : this()
        {
            UserID = id;
            Symbol = symbol;
        }

        public OrderParams(Order order)
        {
            UserID = order.UserID;
            Symbol = order.Symbol;
            Quantity = order.Quantity;
            OrderSide = order.OrderSide;
            OrderType = order.OrderType;
            ServerSide = order.ServerSide;
            Price = order.Price;
            TimeInForce = order.TimeInForce;
            SLOffset = order.SLOffset;
            TPOffset = order.TPOffset;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public override string ToString()
        {
            return $"#{UserID}, {OrderSide} {Quantity} {Symbol} @ {Price} ({OrderType}), {TimeInForce}, TP = {TPOffset}, SL = {SLOffset}, Hidden = {ServerSide}";
        }
    }
}