/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using Com.Lmax.Api.Account;

namespace Com.Lmax.Api.Internal.Protocol
{
    public class LoginResponseHandler : DefaultHandler
    {
        private const string CURRENCY = "currency";
        private const string ACCOUNT_ID = "accountId";
        private const string USERNAME = "username";
        private const string REGISTRATION_LEGAL_ENTITY = "registrationLegalEntity";
        private const string FAILURE_TYPE = "failureType";
        private const string DISPLAY_LOCALE = "displayLocale";
        private const string FUNDING_DISALLOWED = "fundingDisallowed";

        private AccountDetails _accountDetails;

        public LoginResponseHandler()
        {
            AddHandler(ACCOUNT_ID);
            AddHandler(USERNAME);
            AddHandler(REGISTRATION_LEGAL_ENTITY);
            AddHandler(FAILURE_TYPE);
            AddHandler(CURRENCY);
            AddHandler(DISPLAY_LOCALE);
            AddHandler(FUNDING_DISALLOWED);
        }

        public override void EndElement(string local)
        {
            if (BODY == local)
            {
                if (IsOk)
                {
                    long accountId;
                    TryGetValue(ACCOUNT_ID, out accountId);

                    string username = GetStringValue(USERNAME);
                    string legalEntity = GetStringValue(REGISTRATION_LEGAL_ENTITY);
                    bool fundingEnabled = false.ToString().Equals(GetStringValue(FUNDING_DISALLOWED), StringComparison.OrdinalIgnoreCase);
                    string currency = GetStringValue(CURRENCY);
                    string displayLocale = GetStringValue(DISPLAY_LOCALE);

                    _accountDetails = new AccountDetails(accountId, username, currency, legalEntity, displayLocale, fundingEnabled);
                }
            }
        }

        public AccountDetails AccountDetails
        {
            get { return _accountDetails; }
        }

        public string FailureType
        {
            get { return GetStringValue(FAILURE_TYPE); }
        }
    }
}

