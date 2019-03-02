/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Runtime.Serialization;

namespace CommonObjects
{
    [DataContract]
    [Serializable]
    public class AccountInfo
    {
        #region Properties

        [DataMember]
        public string ID { get; set; }
        [DataMember]
        public string Currency { get; set; }
        [DataMember]
        public string UserName { get; set; }
        [DataMember]
        public string Password { get; set; }
        [DataMember]
        public string Account { get; set; }
        [DataMember]
        public string Uri { get; set; }
        [DataMember]
        public string BrokerName { get; set; }
        [DataMember]
        public string DataFeedName { get; set; }
        [DataMember]
        public decimal Balance { get; set; }
        [DataMember]
        public decimal Margin { get; set; }
        [DataMember]
        public decimal Equity { get; set; }
        [DataMember]
        public decimal Profit { get; set; }
        [DataMember]
        public int BalanceDecimals { get; set; }
        [DataMember]
        public bool IsMarginAccount { get; set; }

        public decimal FreeMargin => Balance - Margin;

        #endregion //Properties

        #region Methods

        public AccountInfo()
        {
            ID = string.Empty;
            Currency = string.Empty;
            UserName = string.Empty;
            DataFeedName = string.Empty;
            Password = string.Empty;
            Account = string.Empty;
            Uri = string.Empty;
            BrokerName = string.Empty;
            BalanceDecimals = 2;
        }

        protected bool Equals(AccountInfo other) =>
            string.Equals(ID, other.ID)
            && string.Equals(Currency, other.Currency)
            && string.Equals(UserName, other.UserName)
            && string.Equals(Password, other.Password)
            && string.Equals(Account, other.Account)
            && string.Equals(Uri, other.Uri)
            && string.Equals(BrokerName, other.BrokerName)
            && string.Equals(DataFeedName, other.DataFeedName)
            && Balance == other.Balance
            && Margin == other.Margin
            && Equity == other.Equity
            && Profit == other.Profit
            && BalanceDecimals == other.BalanceDecimals;

        #endregion //Methods
    }
}