/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using Com.Lmax.Api.OrderBook;

namespace Com.Lmax.Api.Internal.Protocol
{
    public class OrderBookStatusEventHandler : DefaultHandler
    {
        private const string OrderBookId = "id";
        private new const string Status = "status";        

        public OrderBookStatusEventHandler()
            : base("orderBookStatus")
        {  
            AddHandler(OrderBookId);
            AddHandler(Status);
        }

        public override void EndElement(string endElement)
        {
            if (OrderBookStatusChanged != null && ElementName == endElement)
            {
                long instrumentId;
                TryGetValue(OrderBookId, out instrumentId);
                string statusString = GetStringValue(Status);
                OrderBookStatus status = (OrderBookStatus)Enum.Parse(typeof(OrderBookStatus), statusString);
       

                OrderBookStatusChanged(new OrderBookStatusEvent(instrumentId, status));
            }
        }

        public event OnOrderBookStatusEvent OrderBookStatusChanged;        
    }
}

