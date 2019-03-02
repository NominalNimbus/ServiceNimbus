/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using CommonObjects;
using ServerCommonObjects.Enums;

namespace ServerCommonObjects.SQL
{
    public static class DBConverters
    {
        public static OrderType ParseOrderType(string type)
        {
            if (type.Equals("MARKET", StringComparison.InvariantCultureIgnoreCase))
                return OrderType.Market;

            return type.Equals("LIMIT", StringComparison.InvariantCultureIgnoreCase) ? OrderType.Limit : OrderType.Stop;
        }

        public static string OrderStatusToString(Status status)
        {
            return status != Status.Cancelled ? "FILLED" : "CANCELED";
        }

        public static Status ParseOrderStatus(string status)
        {
            return status.Equals("FILLED", StringComparison.InvariantCultureIgnoreCase) ? Status.Filled : Status.Cancelled;
        }

        public static string OrderTypeToString(OrderType type)
        {
            return type.ToString("G").ToUpper();
        }

        public static TimeInForce ParseTif(string tif)
        {
            if (tif.Equals("FOK", StringComparison.InvariantCultureIgnoreCase))
                return TimeInForce.FillOrKill;
            if (tif.Equals("GFD", StringComparison.InvariantCultureIgnoreCase))
                return TimeInForce.GoodForDay;
            if (tif.Equals("GTC", StringComparison.InvariantCultureIgnoreCase))
                return TimeInForce.GoodTilCancelled;

            return TimeInForce.ImmediateOrCancel;
        }

        public static string TifToString(TimeInForce tif)
        {
            switch (tif)
            {
                case TimeInForce.FillOrKill: return "FOK";
                case TimeInForce.GoodForDay: return "GFD";
                case TimeInForce.ImmediateOrCancel: return "IOC";
                default: return "GTC";
            }
        }
    }
}
