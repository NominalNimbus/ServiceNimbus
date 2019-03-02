/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using Com.Lmax.Api.Order;
using CommonObjects;
using TimeInForce = Com.Lmax.Api.TimeInForce;

namespace Brokers
{
    public static class Converter
    {
        public static CommonObjects.Order ToCommonOrder(Execution execution, string symbol, string brokerName)
        {
            return new CommonObjects.Order(execution.Order.InstructionId, symbol)
            {
                AccountId = execution.Order.AccountId.ToString(),
                AvgFillPrice = execution.Price,
                BrokerID = execution.Order.OrderId,
                BrokerName = brokerName,
                CancelledQuantity = execution.CancelledQuantity,
                Commission = execution.Order.Commission,
                CurrentPrice = execution.Price,
                FilledQuantity = execution.Order.FilledQuantity,
                OpenQuantity = execution.Quantity,
                FilledDate = DateTime.UtcNow,
                OrderSide = execution.Order.Quantity > 0 ? Side.Buy : Side.Sell,
                OrderType = ToCommonOrderType(execution.Order.OrderType),
                Price = execution.Price,
                Quantity = execution.Quantity,
                SLOffset = execution.Order.StopLossOffset,
                TimeInForce = ToCommonTIF(execution.Order.TimeInForce),
                TPOffset = execution.Order.StopProfitOffset,
                PlacedDate = DateTime.UtcNow
            };
        }

        public static CommonObjects.Order ToCommonOrder(Com.Lmax.Api.Order.Order order, string symbol, string brokerName)
        {
            var orderPrice = order.StopReferencePrice.HasValue ? order.StopReferencePrice.Value : 0m;
            if (order.OrderType == Com.Lmax.Api.Order.OrderType.LIMIT && order.LimitPrice.HasValue)
                orderPrice = order.LimitPrice.Value;
            else if (order.OrderType == Com.Lmax.Api.Order.OrderType.STOP_ORDER && order.StopPrice.HasValue)
                orderPrice = order.StopPrice.Value;

            return new CommonObjects.Order(order.InstructionId, symbol)
            {
                AccountId = order.AccountId.ToString(),
                AvgFillPrice = order.FilledQuantity != 0 ? orderPrice : 0,
                BrokerName = brokerName,
                BrokerID = order.OrderId,
                CancelledQuantity = order.CancelledQuantity,
                Commission = order.Commission,
                CurrentPrice = orderPrice,
                FilledQuantity = order.FilledQuantity,
                OpenDate = GetDateFromTicksString(order.InstructionId),
                OpenQuantity = order.FilledQuantity,
                OrderSide = order.Quantity > 0 ? Side.Buy : Side.Sell,
                OrderType = ToCommonOrderType(order.OrderType),
                Price = order.OrderType != Com.Lmax.Api.Order.OrderType.MARKET ? orderPrice : 0,
                Quantity = order.Quantity,
                SLOffset = order.StopLossOffset,
                TimeInForce = ToCommonTIF(order.TimeInForce),
                TPOffset = order.StopProfitOffset,
                PlacedDate = DateTime.UtcNow
            };
        }

        public static CommonObjects.OrderType ToCommonOrderType(Com.Lmax.Api.Order.OrderType type)
        {
            switch (type)
            {
                case Com.Lmax.Api.Order.OrderType.LIMIT: return CommonObjects.OrderType.Limit;
                case Com.Lmax.Api.Order.OrderType.STOP_ORDER: return CommonObjects.OrderType.Stop;
                default: return CommonObjects.OrderType.Market;
            }
        }

        public static CommonObjects.TimeInForce ToCommonTIF(TimeInForce tif)
        {
            switch (tif)
            {
                case TimeInForce.FillOrKill: return CommonObjects.TimeInForce.FillOrKill;
                case TimeInForce.GoodForDay: return CommonObjects.TimeInForce.GoodForDay;
                case TimeInForce.GoodTilCancelled: return CommonObjects.TimeInForce.GoodTilCancelled;
                case TimeInForce.ImmediateOrCancel: return CommonObjects.TimeInForce.ImmediateOrCancel;
                default: return CommonObjects.TimeInForce.FillOrKill; ;
            }
        }

        public static TimeInForce ToTIF(CommonObjects.TimeInForce tf)
        {
            switch (tf)
            {
                case CommonObjects.TimeInForce.FillOrKill: return TimeInForce.FillOrKill;
                case CommonObjects.TimeInForce.GoodForDay: return TimeInForce.GoodForDay;
                case CommonObjects.TimeInForce.GoodTilCancelled: return TimeInForce.GoodTilCancelled;
                case CommonObjects.TimeInForce.ImmediateOrCancel: return TimeInForce.ImmediateOrCancel;
                default: return TimeInForce.Unknown;
            }
        }

        public static DateTime GetDateFromTicksString(string ticksString, bool defaultToUtcNow = true)
        {
            Int64.TryParse(ticksString, out long ticks);
            var min = DateTime.UtcNow.AddYears(-1).Ticks;
            var max = DateTime.UtcNow.AddYears(1).Ticks;
            return (min < ticks && ticks < max)
                ? new DateTime(ticks, DateTimeKind.Utc)
                : (defaultToUtcNow ? DateTime.UtcNow : DateTime.MinValue);
        }
    }
}