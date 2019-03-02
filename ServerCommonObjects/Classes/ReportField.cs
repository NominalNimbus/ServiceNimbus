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

namespace ServerCommonObjects
{
    public class ReportField
    {
        public string SignalName { get; set; }
        public string Symbol { get; set; }
        public Side Side { get; set; }
        public OrderType TradeType { get; set; }
        public decimal Quantity { get; set; }
        public TimeInForce TimeInForce { get; set; }
        public Status Status { get; set; }
        public DateTime SignalGeneratedDateTime { get; set; }
        public DateTime OrderGeneratedDate { get; set; }
        public DateTime OrderFilledDate { get; set; }
        public DateTime DBOrderEntryDate { get; set; }
        public DateTime DBSignalEntryDate { get; set; }
        public int SignalToOrderSpan { get; set; }
        public int OrderFillingDelay { get; set; }

        public void CalculateDiff()
        {
            SignalToOrderSpan = (OrderGeneratedDate - SignalGeneratedDateTime).Milliseconds;
            OrderFillingDelay = (OrderFilledDate - OrderGeneratedDate).Milliseconds;
        }
    }
}
