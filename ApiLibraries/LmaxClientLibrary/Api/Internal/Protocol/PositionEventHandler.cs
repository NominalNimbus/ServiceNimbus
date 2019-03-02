/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using Com.Lmax.Api.Internal.Events;

namespace Com.Lmax.Api.Internal.Protocol
{
    public class PositionEventHandler : DefaultHandler
    {
        private const string RootNode = "position";
        private const string AccountId = "accountId";
        private const string InstrumentId = "instrumentId";
        private const string ShortUnfilledCost = "shortUnfilledCost";
        private const string LongUnfilledCost = "longUnfilledCost";
        private const string OpenQuantity = "openQuantity";
        private const string CumulativeCost = "cumulativeCost";
        private const string OpenCost = "openCost";

        public event OnPositionEvent PositionEventListener;

        public PositionEventHandler()
            : base(RootNode)
        {
            AddHandler(AccountId);
            AddHandler(InstrumentId);
            AddHandler(ShortUnfilledCost);
            AddHandler(LongUnfilledCost);
            AddHandler(OpenQuantity);
            AddHandler(OpenCost);
            AddHandler(CumulativeCost);
        }

        public override void EndElement(string endElement)
        {
            if (PositionEventListener != null && RootNode.Equals(endElement))
            {
                long accountId;
                long instrumentId;
                decimal shortUnfilledCost;
                decimal longUnfilledCost;
                decimal openQuantity;
                decimal openCost;
                decimal cumulativeCost;

                TryGetValue(AccountId, out accountId);
                TryGetValue(InstrumentId, out instrumentId);
                TryGetValue(OpenQuantity, out openQuantity);
                TryGetValue(OpenCost, out openCost);
                TryGetValue(CumulativeCost, out cumulativeCost);

                PositionBuilder positionBuilder = new PositionBuilder();
                positionBuilder.AccountId(accountId).InstrumentId(instrumentId).OpenQuantity(openQuantity).OpenCost(openCost).CumulativeCost(cumulativeCost);

                if(TryGetValue(ShortUnfilledCost, out shortUnfilledCost))
                {
                    positionBuilder.ShortUnfilledCost(shortUnfilledCost);
                }
                else
                {
                    positionBuilder.ShortUnfilledCost(0);
                }
                
                if(TryGetValue(LongUnfilledCost, out longUnfilledCost))
                {
                    positionBuilder.LongUnfilledCost(longUnfilledCost);
                }
                else
                {
                    positionBuilder.LongUnfilledCost(0);
                }

                PositionEventListener(positionBuilder.NewInstance());
            }
        }
    }
}