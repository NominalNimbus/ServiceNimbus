/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using CommonObjects;
using ServerCommonObjects;
using ServerCommonObjects.Interfaces;
using ServerCommonObjects.SQL;

namespace Brokers
{
    public sealed class SimulatedExchangeBroker : AbstractSimulatedBroker
    {
        #region Properties

        public const string BrokerName = "Simulated Exchange";
        public const string AccountTable = "SimulatedExchangeAccounts";

        public override string Name => BrokerName;
        public override string AccountTableName => AccountTable;
        public override string PositionTableName => "SimulatedExchangePositions";
        public override bool IsMarginBroker => false;

        #endregion //Properties

        public SimulatedExchangeBroker(IDataFeed datafeed, string userName) : base(datafeed, userName)
        {
        }

        #region Static

        public static AvailableBrokerInfo BrokerInfo(string user) =>
            AvailableBrokerInfo.CreateSimulatedBroker(BrokerName, GetAccounts(AccountTable, user));

        public static string CreateSimulatedAccount(string userName, CreateSimulatedBrokerAccountInfo account) =>
            CreateSimulatedAccount(AccountTable, userName, account, false);

        #endregion //Static

        #region overrides

        protected override void UpdatePosition(Order order, decimal price)
        {
            var security = GetSecurity(order.Symbol);
            var contractSize = security?.ContractSize ?? 1m;
            var currencyBase = GetCurrencyRate(security);
            Position pos = GetOrCreatePosition(order);

            var cashe = currencyBase * order.FilledQuantity * contractSize * price;

            if (order.OrderSide == Side.Buy)
            {
                AccountInfo.Balance -= cashe;

                var avgPrice = (pos.Price * pos.AbsQuantity + price * order.FilledQuantity) / Math.Abs(pos.AbsQuantity + order.FilledQuantity);
                pos.Quantity += order.FilledQuantity;
                pos.Price = avgPrice;
            }
            else
            {
                AccountInfo.Balance += cashe;
                AccountInfo.Profit += (price - pos.Price) * order.FilledQuantity * contractSize;
                pos.Quantity -= order.FilledQuantity;
            }

            //pos.CurrentPrice = price;

            ProcessPositionUpdate(pos);
            _positionsDB.SavePosition(pos, true);

            SaveAccountInfoDetails();
        }

        protected override string ValidatePlaceOrder(Order order, decimal price)
        {
            var security = GetSecurity(order.Symbol);
            var contractSize = security?.ContractSize ?? 1M;
            var currencyBase = GetCurrencyRate(security);
            var pos = GetPosition(order.Symbol);

            if (order.OrderSide == Side.Buy)
            {
                var orderCash = currencyBase * order.Quantity * contractSize * price;
                return orderCash > AccountInfo.Balance ? "Account balance is too low" : string.Empty;
            }
            else
            {
                return pos == null || order.Quantity > pos.Quantity ? "Please buy before sell" : string.Empty;
            }
        }

        protected override string ValidateFillOrder(Order order, decimal price) 
            => ValidatePlaceOrder(order, price);


        protected override void UpdateAccount()
        {
            lock (Positions)
            {
                AccountInfo.Profit = Positions.Sum(p => p.Profit);
            }

            OnAccountStateChanged();
        }

        #endregion //overrides
    }
}

