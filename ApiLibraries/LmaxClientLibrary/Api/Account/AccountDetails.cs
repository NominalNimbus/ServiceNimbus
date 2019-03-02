/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

namespace Com.Lmax.Api.Account
{
    /// <summary>
    /// Contains all of the information about a users account that is received in
    /// in response to a login request.
    /// </summary>
    public sealed class AccountDetails
    {
        private readonly long _accountId;
        private readonly string _username;
        private readonly string _currency;
        private readonly string _legalEntity;
        private readonly string _displayLocale;
        private readonly bool _fundingAllowed;

        ///<summary>
        /// Create a new object representing the user's account
        ///</summary>
        ///<param name="accountId">The unique ID of the account</param>
        ///<param name="username">The username</param>
        ///<param name="currency">The base currency of the account</param>
        ///<param name="legalEntity">The legal entity the account is based in, e.g. UK, Germany etc</param>
        ///<param name="displayLocale">The Locale used for displaying information (i.e. language)</param>
        ///<param name="fundingAllowed">True if the account is enabled for funding</param>
        public AccountDetails(long accountId, string username, string currency, string legalEntity, string displayLocale, bool fundingAllowed)
        {
            _accountId = accountId;
            _username = username;
            _currency = currency;
            _legalEntity = legalEntity;
            _displayLocale = displayLocale;
            _fundingAllowed = fundingAllowed;
        }
  
        /// <summary>
        /// Readonly, the numeric system accountId. 
        /// </summary>
        public long AccountId
        {
            get { return _accountId; }
        }
  
        /// <summary>
        /// The username used to login to the LMAX Trader platform. 
        /// </summary>
        public string Username
        {
            get { return _username; }
        }
  
        /// <summary>
        /// The user's base currency. 
        /// </summary>
        public string Currency
        {
            get { return _currency; }
        }
  
        /// <summary>
        /// The legal entity where the user is located, e.g. UK. 
        /// </summary>
        public string LegalEntity
        {
            get { return _legalEntity; }
        }
  
        /// <summary>
        /// The locale used to display text, mainly used by the UI to handle
        /// internationalisation.
        /// </summary>
        public string DisplayLocale
        {
            get { return _displayLocale; }
        }
  
        /// <summary>
        /// If funding is enabled for the account. 
        /// </summary>
        public bool FundingAllowed
        {
            get { return _fundingAllowed; }
        }

        public bool Equals(AccountDetails other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other._accountId == _accountId && 
                Equals(other._username, _username) && 
                Equals(other._currency, _currency) && 
                Equals(other._legalEntity, _legalEntity) && 
                Equals(other._displayLocale, _displayLocale) && 
                other._fundingAllowed.Equals(_fundingAllowed);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (AccountDetails)) return false;
            return Equals((AccountDetails) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = _accountId.GetHashCode();
                result = (result*397) ^ (_username != null ? _username.GetHashCode() : 0);
                result = (result*397) ^ (_currency != null ? _currency.GetHashCode() : 0);
                result = (result*397) ^ (_legalEntity != null ? _legalEntity.GetHashCode() : 0);
                result = (result*397) ^ (_displayLocale != null ? _displayLocale.GetHashCode() : 0);
                result = (result*397) ^ _fundingAllowed.GetHashCode();
                return result;
            }
        }
    }
}
