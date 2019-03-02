/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

namespace PoloniexAPI
{
    public class CurrencyPair
    {
        private const char SeparatorCharacter = '_';

        public int Id { get; private set; }
        public string BaseCurrency { get; private set; }
        public string QuoteCurrency { get; private set; }
        

        public CurrencyPair(string baseCurrency, string quoteCurrency, int id = 0)
        {
            BaseCurrency = baseCurrency;
            QuoteCurrency = quoteCurrency;
            Id = id;
        }

        public static CurrencyPair Parse(string currencyPair, int id = 0)
        {
            var valueSplit = currencyPair.Split(SeparatorCharacter);
            return new CurrencyPair(valueSplit[0], valueSplit[1], id);
        }

        public override string ToString()
        {
            return BaseCurrency + SeparatorCharacter + QuoteCurrency;
        }

        public static bool operator ==(CurrencyPair a, CurrencyPair b)
        {
            if (ReferenceEquals(a, b)) return true;
            if ((object)a == null ^ (object)b == null) return false;

            return a.BaseCurrency == b.BaseCurrency && a.QuoteCurrency == b.QuoteCurrency;
        }

        public static bool operator !=(CurrencyPair a, CurrencyPair b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            var b = obj as CurrencyPair;
            return (object)b != null && Equals(b);
        }

        public bool Equals(CurrencyPair b)
        {
            return b.BaseCurrency == BaseCurrency && b.QuoteCurrency == QuoteCurrency;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}
