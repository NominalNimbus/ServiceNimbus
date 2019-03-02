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
    public sealed class SimulatedMarginBroker : AbstractSimulatedBroker
    {
        #region Members

        private bool _proccessClosePositions = false;

        #endregion //Members

        #region Properties

        public const string BrokerName = "Simulated Margin";
        public const string AccountTable = "SimulatedMarginAccounts";

        public override string Name => BrokerName;
        public override string AccountTableName => AccountTable;
        public override string PositionTableName => "SimulatedMarginPositions";
        public override bool IsMarginBroker => true;

        #endregion //Properties

        #region Constructor

        public SimulatedMarginBroker(IDataFeed datafeed, string userName) : base(datafeed, userName)
        {
         
        }

        #endregion //Constructor

        #region Static

        public static AvailableBrokerInfo BrokerInfo(string user) =>
            AvailableBrokerInfo.CreateSimulatedBroker(BrokerName, GetAccounts(AccountTable, user));

        public static string CreateSimulatedAccount(string userName, CreateSimulatedBrokerAccountInfo account) =>
           CreateSimulatedAccount(AccountTable, userName, account, true);

        #endregion //Static

        #region overrides

        protected override void UpdatePosition(Order order, decimal price)
        {
            var security = GetSecurity(order.Symbol);
            var contractSize = security?.ContractSize ?? 1m;
            var currencyBase = GetCurrencyRate(security);
            var sideSign = order.OrderSide == Side.Sell ? -1 : 1;
            Position pos = GetOrCreatePosition(order);
            var openQuantity = order.FilledQuantity;

            if (pos.Quantity != 0 && pos.PositionSide != order.OrderSide)
            {
                //proccess close or partial close position
                decimal closeQuantity = Math.Min(order.FilledQuantity, pos.AbsQuantity);
                openQuantity -= closeQuantity;
                var pl = currencyBase * closeQuantity * contractSize * (pos.Price - price) * sideSign;

                AccountInfo.Profit += pl;
                AccountInfo.Balance += pl;
                pos.Quantity += sideSign * closeQuantity;
            }

            if (openQuantity != 0M)
            {
                //open or substract position
                var avgPrice = (pos.Price * pos.AbsQuantity + price * openQuantity) / Math.Abs(pos.AbsQuantity + openQuantity);
                pos.Quantity += sideSign * openQuantity;
                pos.Price = avgPrice;
                pos.PositionSide = pos.Quantity < 0 ? Side.Sell : Side.Buy;
            }
            //calculate comission
            var fillCash = currencyBase * order.FilledQuantity * contractSize * price;
            AccountInfo.Balance -= security?.CaclulateCommision(order, fillCash, order.FilledQuantity) ?? 0;
            //

            pos.CurrentPrice = price;

            ProcessPositionUpdate(pos);
            _positionsDB.SavePosition(pos, true);

            SaveAccountInfoDetails();
        }

        protected override string ValidatePlaceOrder(Order order, decimal price)
        {
            var security = GetSecurity(order.Symbol);
            var contractSize = security?.ContractSize ?? 1M;
            var margin = security?.MarginRate ?? 1M;
            var currencyBase = GetCurrencyRate(security);

            var pos = GetPosition(order.Symbol);
            var quantityToFill = (pos == null || pos.Quantity == 0M || pos.PositionSide == order.OrderSide) ?
            order.QuantityToFill :
            Math.Max(order.QuantityToFill - pos.AbsQuantity, 0);

            if (quantityToFill == 0)
                return string.Empty;

            var freeMargin = AccountInfo.FreeMargin - (pos != null && pos.PositionSide != order.OrderSide ? pos.Margin : 0);
            var orderMargin = CalculateMargin(quantityToFill, price, contractSize, currencyBase, margin);
            return freeMargin > orderMargin ? string.Empty : "Account balance is too low";
        }

        protected override string ValidateFillOrder(Order order, decimal price) => string.Empty;

        protected override void UpdateAccount()
        {
            base.UpdateAccount();
            if(!_proccessClosePositions && AccountInfo.Equity <= 0 && Positions.Count > 0)
            {
                _proccessClosePositions = true;
                CloseAllPositions();
                _proccessClosePositions = false;
            }
        }

        #endregion overrides

    }
}
