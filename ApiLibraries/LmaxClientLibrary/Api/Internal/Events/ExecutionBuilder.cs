/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using Com.Lmax.Api.Order;

namespace Com.Lmax.Api.Internal.Events
{
    public class ExecutionBuilder
    {
        private long _executionId;
        private decimal _price;
        private decimal _quantity;
        private decimal _cancelledQuantity;
        private Order.Order _order;

        public ExecutionBuilder ExecutionId(long executionId)
        {
            _executionId = executionId;
            return this;
        }

        public ExecutionBuilder Price(decimal price)
        {
            _price = price;
            return this;
        }

        public ExecutionBuilder Quantity(decimal quantity)
        {
            _quantity = quantity;
            return this;
        }

        public ExecutionBuilder CancelledQuantity(decimal cancelledQuantity)
        {
            _cancelledQuantity = cancelledQuantity;
            return this;
        }

        public ExecutionBuilder Order(Order.Order order)
        {
            _order = order;
            return this;
        }

        public Execution NewInstance()
        {
            return new Execution(_executionId, _price, _quantity, _order, _cancelledQuantity);
        }
    }
}
