/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.Collections.Generic;
using Com.Lmax.Api.Internal.Events;
using Com.Lmax.Api.Order;

namespace Com.Lmax.Api.Internal.Protocol
{
    public class OrderStateEventHandler : DefaultHandler
    {
        private const string InstructionIdNodeName = "instructionId";
        private const string OrderIdNodeName = "orderId";
        private const string InstrumentIdNodeName = "instrumentId";
        private const string AccountIdNodeName = "accountId";
        private const string QuantityNodeName = "quantity";
        private const string MatchedQuantityNodeName = "matchedQuantity";
        private const string CancelledQuantityNodeName = "cancelledQuantity";
        private const string OrderTypeNodeName = "orderType";
        private const string StopProfitOffsetNodeName = "stopProfitOffset";
        private const string StopLossOffsetNodeName = "stopLossOffset";
        private const string StopReferencePriceNodeName = "stopReferencePrice";
        private const string PriceNodeName = "price";
        private const string RootNode = "order";
        private const string CommissionNodeName = "commission";
        private const string TimeInForceNodeName = "timeInForce";
        private readonly ExecutionEventHandler _executionEventHandler;

        public OrderStateEventHandler() : base(RootNode)
        {
            _executionEventHandler = new ExecutionEventHandler();
            AddHandler(InstructionIdNodeName);
            AddHandler(OrderIdNodeName);
            AddHandler(InstrumentIdNodeName);
            AddHandler(AccountIdNodeName);
            AddHandler(QuantityNodeName);
            AddHandler(MatchedQuantityNodeName);
            AddHandler(CancelledQuantityNodeName);
            AddHandler(PriceNodeName);
            AddHandler(OrderTypeNodeName);
            AddHandler(StopProfitOffsetNodeName);
            AddHandler(StopLossOffsetNodeName);
            AddHandler(StopReferencePriceNodeName);
            AddHandler(CommissionNodeName);
            AddHandler(_executionEventHandler);
            AddHandler(TimeInForceNodeName);
        }

        public override void EndElement(string endElement)
        {
            if (RootNode.Equals(endElement))
            {
                OrderBuilder orderEventBuilder = new OrderBuilder();
                string instructionId;
                long instrumentId;
                long accountId;
                decimal quantity;
                decimal matchQuantity;
                decimal cancelledQuantity;
                decimal price;
                decimal stopReferencePrice;
                decimal stopLossOffset;
                decimal stopProfitOffset;
                decimal commission;

                TryGetValue(InstructionIdNodeName, out instructionId);
                TryGetValue(InstrumentIdNodeName, out instrumentId);
                TryGetValue(AccountIdNodeName, out accountId);
                TryGetValue(QuantityNodeName, out quantity);
                TryGetValue(MatchedQuantityNodeName, out matchQuantity);
                TryGetValue(CancelledQuantityNodeName, out cancelledQuantity);
                TryGetValue(CommissionNodeName, out commission);

                orderEventBuilder.InstructionId(instructionId).OrderId(GetStringValue(OrderIdNodeName)).InstrumentId(instrumentId).AccountId(accountId).
                    Quantity(quantity).FilledQuantity(matchQuantity).CancelledQuantity(cancelledQuantity).OrderType(GetStringValue(OrderTypeNodeName)).
                    Commission(commission).TimeInForce(GetStringValue(TimeInForceNodeName));

                if (TryGetValue(PriceNodeName, out price))
                {
                    orderEventBuilder.Price(price);
                }
                if (TryGetValue(StopReferencePriceNodeName, out stopReferencePrice))
                {
                    orderEventBuilder.StopReferencePrice(stopReferencePrice);
                }
                if (TryGetValue(StopProfitOffsetNodeName, out stopProfitOffset))
                {
                    orderEventBuilder.StopProfitOffset(stopProfitOffset);
                }
                if (TryGetValue(StopLossOffsetNodeName, out stopLossOffset))
                {
                    orderEventBuilder.StopLossOffset(stopLossOffset);
                }

                Order.Order order = orderEventBuilder.NewInstance();
                var executed = NotifyExecutions(order);
                if (ShouldEmitOrder(order) && OrderEvent != null && !executed)
                {
                    OrderEvent(order);
                }
                ResetAll();
            }
        }

        private bool NotifyExecutions(Order.Order order)
        {
            IList<ExecutionBuilder> executionBuilders = _executionEventHandler.GetExecutionBuilders();
            var res = false;
            foreach (ExecutionBuilder executionBuilder in executionBuilders)
            {
                executionBuilder.Order(order);
                Execution execution = executionBuilder.NewInstance();
                if (IsExecutionForOrder(order, execution) && ExecutionEvent != null)
                {
                    ExecutionEvent(execution);
                    res = true;
                }
            }
            _executionEventHandler.Clear();

            return res;
        }

        private bool ShouldEmitOrder(Order.Order order)
        {
            return _executionEventHandler.GetExecutionBuilders().Count != 0
                       ? IsExecutionForOrder(order, _executionEventHandler.GetExecutionBuilders()[0].NewInstance())
                       : true;
        }

        private static bool IsExecutionForOrder(Order.Order order, Execution execution)
        {
            return order.Quantity.CompareTo(0) == execution.Quantity.CompareTo(0) ||
                   order.Quantity.CompareTo(0) == execution.CancelledQuantity.CompareTo(0);
        }

        public event OnExecutionEvent ExecutionEvent;

        public event OnOrderEvent OrderEvent;
    }
}