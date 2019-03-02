/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using Com.Lmax.Api.Position;

namespace Com.Lmax.Api.Internal.Events
{
    class PositionBuilder
    {
        private long _accountId;
        private long _instrumentId;
        private decimal _shortUnfilledCost;
        private decimal _longUnfilledCost;
        private decimal _openQuantity;
        private decimal _openCost;
        private decimal _cumlativeCost;

        public PositionBuilder AccountId(long accountId)
        {
            _accountId = accountId;
            return this;
        }

        public PositionBuilder InstrumentId(long instrumentId)
        {
            _instrumentId = instrumentId;
            return this;
        }

        public PositionBuilder ShortUnfilledCost(decimal shortUnfilledCost)
        {
            _shortUnfilledCost = shortUnfilledCost;
            return this;
        }

        public PositionBuilder LongUnfilledCost(decimal longUnfilledCost)
        {
            _longUnfilledCost = longUnfilledCost;
            return this;
        }

        public PositionBuilder OpenQuantity(decimal openQuantity)
        {
            _openQuantity = openQuantity;
            return this;
        }

        public PositionBuilder OpenCost(decimal openCost)
        {
            _openCost = openCost;
            return this;
        }

        public PositionBuilder CumulativeCost(decimal cumulativeCost)
        {
            _cumlativeCost = cumulativeCost;
            return this;
        }

        public PositionEvent NewInstance()
        {
            return new PositionEvent(_accountId, _instrumentId, _shortUnfilledCost, _longUnfilledCost, _openQuantity, _cumlativeCost, _openCost);
        }
    }
}
