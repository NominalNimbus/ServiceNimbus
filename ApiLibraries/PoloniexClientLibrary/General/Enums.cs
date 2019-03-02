/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

namespace PoloniexAPI
{
    public enum OrderSide : byte { Buy = 1, Sell = 2 }

    public enum PositionSide : byte { None, Long, Short }

    public enum OrderBookItemSide : byte { Unknown, Buy, Sell, Bid, Ask }

    public enum OrderBookItemType : byte { Unknown, OrderBookModify, OrderBookRemove, NewTrade }

    public enum BarSize { M5 = 300, M15 = 900, M30 = 1800, H2 = 7200, H4 = 14400, Day = 86400 }
}
