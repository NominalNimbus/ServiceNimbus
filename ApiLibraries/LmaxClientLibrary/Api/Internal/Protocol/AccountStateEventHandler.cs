/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.Collections.Generic;
using Com.Lmax.Api.Internal.Events;

namespace Com.Lmax.Api.Internal.Protocol
{
    public class AccountStateEventHandler : DefaultHandler
    {
        public event OnAccountStateEvent AccountStateUpdated;

        private const string RootNodeName = "accountState";
        private const string AccountIdNodeName = "accountId";
        private const string BalanceNodeName = "balance";
        private const string AvailableFundsNodeName = "availableFunds";
        private const string AvailableToWithdrawNodeName = "availableToWithdraw";
        private const string UnrealisedProfitAndLossNodeName = "unrealisedProfitAndLoss";
        private const string MarginNodeName = "margin";
        private const string ActiveNodeName = "active";

        private readonly WalletsHandler _walletsHandler = new WalletsHandler();

        public AccountStateEventHandler() : base(RootNodeName)
        {
            AddHandler(AccountIdNodeName);
            AddHandler(BalanceNodeName);
            AddHandler(AvailableFundsNodeName);
            AddHandler(AvailableToWithdrawNodeName);
            AddHandler(UnrealisedProfitAndLossNodeName);
            AddHandler(MarginNodeName);
            AddHandler(_walletsHandler);
            AddHandler(ActiveNodeName);
        }

        public override void EndElement(string endElement)
        {
            if (AccountStateUpdated != null && RootNodeName.Equals(endElement))
            {
                long accountId;
                decimal balance;
                decimal availableFunds;
                decimal availableToWithdraw;
                decimal unrealisedProfitAndLoss;
                decimal margin;

                TryGetValue(AccountIdNodeName, out accountId);
                TryGetValue(BalanceNodeName, out balance);
                TryGetValue(AvailableFundsNodeName, out availableFunds);
                TryGetValue(AvailableToWithdrawNodeName, out availableToWithdraw);
                TryGetValue(UnrealisedProfitAndLossNodeName, out unrealisedProfitAndLoss);
                TryGetValue(MarginNodeName, out margin);

                AccountStateBuilder accountStateBuilder = new AccountStateBuilder();
                accountStateBuilder.AccountId(accountId).Balance(balance).AvailableFunds(availableFunds).
                    AvailableToWithdraw(availableToWithdraw).UnrealisedProfitAndLoss(unrealisedProfitAndLoss).Margin(margin).Wallets(_walletsHandler.GetAndResetWallets());

                AccountStateUpdated(accountStateBuilder.NewInstance());
            }
        }
    }

    internal class WalletsHandler : DefaultHandler
    {
        private const string RootNodeName = "wallet";
        private const string CurrencyNodeName = "currency";
        private const string BalanceNodeName = "balance";
        private Dictionary<string, decimal> _wallets = new Dictionary<string, decimal>();

        public WalletsHandler()
            : base(RootNodeName)
        {
            AddHandler(CurrencyNodeName);
            AddHandler(BalanceNodeName);
            //<wallets><wallet><currency>GBP</currency><balance>15000</balance></wallet></wallets>
        }

        public override void EndElement (string endElement)
        {
            if (RootNodeName.Equals(endElement))
            {
                decimal balance;
                TryGetValue(BalanceNodeName, out balance);
                _wallets[GetStringValue(CurrencyNodeName)] = balance;
            }
        }

        public Dictionary<string, decimal> GetAndResetWallets()
        {
            Dictionary<string, decimal> copy = new Dictionary<string, decimal>(_wallets);
            _wallets.Clear();
            return copy;
        }
    }
}
