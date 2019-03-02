/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using Com.Lmax.Api.Order;
using CommonObjects;
using TimeInForce = Com.Lmax.Api.TimeInForce;

namespace LmaxDataFeed
{
    public static class Converter
    {
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
    }
}