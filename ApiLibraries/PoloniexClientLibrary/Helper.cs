/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PoloniexAPI
{
    public static class Helper
    {
        private const int DoubleRoundingPrecisionDigits = 8;
        internal const string ApiUrlHttpsRelativePublic = "public?command=";
        internal const string ApiUrlHttpsRelativeTrading = "tradingApi";
        internal const string ApiUrlWssBase = "wss://api.poloniex.com";
        internal const string Api2UrlWssBase = "wss://api2.poloniex.com";

        private static BigInteger CurrentHttpPostNonce { get; set; }
        internal static readonly string AssemblyVersionString = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
        internal static readonly DateTime DateTimeUnixEpochStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        internal static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

        internal static async Task<string> GetResponseString(this HttpWebRequest request)
        {
            using (var response = await request.GetResponseAsync())
            using (var stream = response.GetResponseStream())
            {
                if (stream == null)
                    throw new NullReferenceException("The HttpWebRequest's response stream cannot be empty.");

                using (var reader = new StreamReader(stream))
                    return await reader.ReadToEndAsync();
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        internal static T DeserializeObject<T>(this JsonSerializer serializer, string value)
        {
            using (var stringReader = new StringReader(value)) 
            using (var jsonTextReader = new JsonTextReader(stringReader))
                return (T)serializer.Deserialize(jsonTextReader, typeof(T));
        }

        internal static string ToHttpPostString(this Dictionary<string, object> dictionary)
        {
            if (dictionary == null || dictionary.Count == 0)
                return String.Empty;

            var output = String.Empty;
            foreach (var entry in dictionary)
            {
                var valueString = entry.Value as string;
                if (valueString == null)
                    output += "&" + entry.Key + "=" + entry.Value;
                else
                    output += "&" + entry.Key + "=" + valueString.Replace(' ', '+');
            }

            return output.Substring(1);
        }

        internal static OrderSide ToOrderSide(this string value)
        {
            switch (value.ToLowerInvariant())
            {
                case "buy":  return OrderSide.Buy;
                case "sell": return OrderSide.Sell;
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        internal static PositionSide ToPositionSide(this string value)
        {
            switch (value.ToLowerInvariant())
            {
                case "none": return PositionSide.None;
                case "long": return PositionSide.Long;
                case "short": return PositionSide.Short;
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        internal static decimal ToDecimal(this string value)
        {
            return Decimal.Parse(value, NumberStyles.Any, InvariantCulture);
        }

        public static double Normalize(this double value)
        {
            return Math.Round(value, DoubleRoundingPrecisionDigits, MidpointRounding.AwayFromZero);
        }

        public static decimal Normalize(this decimal value)
        {
            return Math.Round(value, DoubleRoundingPrecisionDigits, MidpointRounding.AwayFromZero);
        }

        public static string ToStringNormalized(this decimal value)
        {
            return value.ToString("0." + new string('#', DoubleRoundingPrecisionDigits), InvariantCulture);
        }

        public static string ToStringNormalized(this double value)
        {
            return value.ToString("0." + new string('#', DoubleRoundingPrecisionDigits), InvariantCulture);
        }

        public static string ToStringHex(this byte[] value)
        {
            var output = String.Empty;
            for (var i = 0; i < value.Length; i++)
                output += value[i].ToString("x2", InvariantCulture);
            return (output);
        }

        internal static DateTime UnixTimeStampToDateTime(ulong unixTimeStamp)
        {
            return DateTimeUnixEpochStart.AddSeconds(unixTimeStamp);
        }

        internal static ulong DateTimeToUnixTimeStamp(DateTime dateTime)
        {
            return (ulong)Math.Floor(dateTime.Subtract(DateTimeUnixEpochStart).TotalSeconds);
        }

        internal static DateTime ParseDateTime(string dateTime)
        {
            return DateTime.SpecifyKind(DateTime.ParseExact(dateTime, "yyyy-MM-dd HH:mm:ss", InvariantCulture), 
                DateTimeKind.Utc).ToLocalTime();
        }

        internal static string GetCurrentHttpPostNonce()
        {
            var span = DateTime.UtcNow.Subtract(DateTimeUnixEpochStart);
            var newHttpPostNonce = new BigInteger(Math.Round(span.TotalMilliseconds * 1000, MidpointRounding.AwayFromZero));
            if (newHttpPostNonce > CurrentHttpPostNonce)
                CurrentHttpPostNonce = newHttpPostNonce;
            else
                CurrentHttpPostNonce += 1;

            return CurrentHttpPostNonce.ToString(InvariantCulture);
        }
    }
}
