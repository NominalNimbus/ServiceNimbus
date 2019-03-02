/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using Newtonsoft.Json.Linq;
using PoloniexAPI.MarketTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoloniexAPI.LiveTools
{
    internal class TickData
    {
        #region Properties

        public int CurrencyPairId { get; set; }
        public decimal LastTradePrice { get; set; }
        public decimal LowestAsk { get; set; }
        public decimal HighestBid { get; set; }
        public decimal PercentChange { get; set; }
        public decimal BaseVolume24Hours { get; set; }
        public decimal QuoteeVolume24Hours { get; set; }
        public byte IsFrozen { get; set; }
        public decimal HighestPrice24Hours { get; set; }
        public decimal LowstPrice24Hours { get; set; }

        #endregion //Properties

        #region Methods

        public Quote ToQuote()
        {
            return new Quote
            {
                Symbol = new CurrencyPair(string.Empty, string.Empty, CurrencyPairId),
                Id = CurrencyPairId,
                Last = LastTradePrice,
                PercentChange = PercentChange,
                BaseVolume = BaseVolume24Hours,
                Volume = QuoteeVolume24Hours,
                Bid = HighestBid,
                Ask = LowestAsk,
                IsFrozenValue = IsFrozen
            };
        }

        #endregion //Methods

        #region Static Members

        internal static TickData TickDataFromMessage(IEnumerable<JToken> values)
        {
            if (values.Count() != 10)
                return null;

            var valid = true;
            var tickData = new TickData()
            {
                CurrencyPairId = ConvertToInt(values.ElementAt(0), ref valid),
                LastTradePrice = ConvertStringToDecimal(values.ElementAt(1), ref valid),
                LowestAsk = ConvertStringToDecimal(values.ElementAt(2), ref valid),
                HighestBid = ConvertStringToDecimal(values.ElementAt(3), ref valid),
                PercentChange = ConvertStringToDecimal(values.ElementAt(4), ref valid),
                BaseVolume24Hours = ConvertStringToDecimal(values.ElementAt(5), ref valid),
                QuoteeVolume24Hours = ConvertStringToDecimal(values.ElementAt(6), ref valid),
                IsFrozen = (byte)ConvertToInt(values.ElementAt(7), ref valid),
                HighestPrice24Hours = ConvertStringToDecimal(values.ElementAt(8), ref valid),
                LowstPrice24Hours = ConvertStringToDecimal(values.ElementAt(9), ref valid)
            };

            return valid ? tickData : null;
        }

        private static int ConvertToInt(JToken jToken, ref bool valid) =>
            ConvertToType<int>(jToken, JTokenType.Integer, ref valid);

        private static string ConvertToString(JToken jToken, ref bool valid) =>
            ConvertToType<string>(jToken, JTokenType.String, ref valid);

        private static decimal ConvertStringToDecimal(JToken jToken, ref bool valid)
        {
            if (!decimal.TryParse(ConvertToString(jToken, ref valid), out var price))
                valid = false;

            return price;
        }

        private static T ConvertToType<T>(JToken jToken, JTokenType tokenType, ref bool valid)
        {
            if (jToken.Type != tokenType)
            {
                valid = false;
                return default(T);
            }

            return jToken.Value<T>();
        }

        #endregion //Static Members
    }
}
