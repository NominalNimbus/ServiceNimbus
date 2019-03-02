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
    public abstract class AbstractSimulatedBroker : AbstractBroker
    {
        #region Fields

        protected readonly DBSimulatedAccounts _accountsDB;
        protected readonly DBSimulatedPositions _positionsDB;
        protected string _userName;

        #endregion //Fields

        #region Abstract Members

        public abstract string AccountTableName { get; }
        public abstract string PositionTableName { get; }
        public abstract bool IsMarginBroker { get; } 

        protected abstract string ValidatePlaceOrder(Order order, decimal price);
        protected abstract string ValidateFillOrder(Order order, decimal price);
        protected abstract void UpdatePosition(Order order, decimal price);

        #endregion //Abstract Members
                
        #region Constructor

        public AbstractSimulatedBroker(IDataFeed datafeed, string userName) : base(datafeed)
        {
            _userName = userName;
            _accountsDB = new DBSimulatedAccounts(ConnectionString, AccountTableName, IsMarginBroker);
            _positionsDB = new DBSimulatedPositions(ConnectionString, PositionTableName);
        }

        #endregion //Constructor

        #region Static 

        protected static string ConnectionString => System.IO.File.ReadAllText("DataBaseConnection.set").Trim();

        public static List<string> GetAccounts(string tableName, string user) =>
            DBSimulatedAccounts.GetAccounts(ConnectionString, tableName, user);

        public static string CreateSimulatedAccount(string tableName, string userName, CreateSimulatedBrokerAccountInfo account, bool isMarginAccount) =>
            DBSimulatedAccounts.CreateSimulatedAccount(ConnectionString, tableName, userName, account, isMarginAccount);

        #endregion //Static 

        #region IBroker/AbstractBroker Implementation

        public override void Login(AccountInfo account)
        {
            account.UserName = _userName;//use loginned user account
            account.IsMarginAccount = IsMarginBroker;

            if (account?.BrokerName != Name)
                throw new ArgumentException($"{account?.BrokerName} account can't log into {Name} broker account");

            if (!_accountsDB.VerifyAccount(account.UserName, account.Account))
                throw new ArgumentException($"Login failed for {account.UserName} ({Name} broker). {account.Account} account doesn't exist");
            
            AccountInfo = account;
        }

        public override void Start()
        {
            base.Start();
            LoadAccountInfoData();
            LoadPositions();

            //HACK: trigger events with delay
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                System.Threading.Thread.Sleep(500);
                OnAccountStateChanged();
                OnPositionsChanged(Positions);
            });
        }

        public override void PlaceOrder(Order order)
        {
            //Validate order
            var error = ValidatePlaceOrder(order, GetPrice(order.Symbol));
            if (!string.IsNullOrEmpty(error))
            {
                OnOrderRejected(order, error);
                return;
            }

            if (!order.ServerSide && (order.OrderType != OrderType.Market
                || order.SLOffset.HasValue || order.TPOffset.HasValue))
            {
                order.ServerSide = true;
            }

            base.PlaceOrder(order);
        }

        public override void ModifyOrder(Order order, decimal? sl, decimal? tp, bool isServerSide)
        {
            Order existing;
            lock (Orders)
                existing = Orders.FirstOrDefault(i => i.BrokerID == order.BrokerID || i.UserID == order.UserID);

            if (existing != null)
            {
                existing.SLOffset = sl;
                existing.TPOffset = tp;

                OnOrdersUpdated(new List<Order> { existing });
            }
        }

        protected override void PlaceMarketOrder(Order order)
        {
            if (order.OrderType == OrderType.Market)  //just in case
                FillOrder(order, GetPrice(order.Symbol));
        }

        protected override void PlaceLimitStopOrder(Order order)
        {
            //already handled by AbstractBroker
        }

        #endregion

        #region Private Methods

        private void FillOrder(Order order, decimal price)
        {
            if (price <= 0m || !order.IsActive || order.QuantityToFill <= 0M)
                return;

            var error = ValidateFillOrder(order, price);
            if(!string.IsNullOrEmpty(error))
            {
                OnOrderRejected(order, error);
                return;
            }

            order.AvgFillPrice = price;
            order.FilledQuantity = order.QuantityToFill;// sharesThatCanFill;
            order.CancelledQuantity = order.Quantity - order.FilledQuantity;  //optional: cancel shares that can not be filled
            order.OpenQuantity = order.FilledQuantity;
            order.PlacedDate = order.FilledDate = DateTime.UtcNow;
            ProcessOrderExecution(order);

            UpdatePosition(order, price);
        }

        #endregion

        #region Save/Load Methods

        private void LoadPositions()
        {
            var positions = _positionsDB.GetPositions(AccountInfo.UserName, AccountInfo.ID, AccountInfo.BrokerName);
            lock (Positions)
            {
                Positions.Clear();
                if (positions != null && positions.Count > 0)
                    Positions.AddRange(positions);
            }
        }

        private void LoadAccountInfoData()
        {
            var accDetails = _accountsDB.GetAccountDetails(AccountInfo.UserName, AccountInfo.Account);
            if (accDetails != null)
            {
                AccountInfo.Currency = accDetails.Currency;
                AccountInfo.ID = accDetails.ID;
                AccountInfo.Balance = accDetails.Balance;
                AccountInfo.Margin = accDetails.Margin;
                AccountInfo.Profit = accDetails.Profit;
                AccountInfo.Equity = AccountInfo.Balance;  //+ AccountInfo.Profit; (for unrealized profit)
            }
        }

        protected void SaveAccountInfoDetails()
        {
            _accountsDB.SaveAccountDetails(AccountInfo);
            OnAccountStateChanged();
        }


        #endregion //Save/Load Methods

     
    }
}
