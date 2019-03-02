/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PoloniexAPI.MarketTools
{
    public class Markets
    {
        private readonly ApiWebClient _apiWebClient;

        internal Markets(ApiWebClient apiWebClient)
        {
            _apiWebClient = apiWebClient;
        }

        public async Task<List<Quote>> GetSummary()
        {
            var data = await GetData<Dictionary<string, Quote>>("returnTicker");
            if (data == null)
                return new List<Quote>(0);

            var result = new List<Quote>(data.Count);
            foreach (var item in data)
            {
                item.Value.Symbol = CurrencyPair.Parse(item.Key, item.Value.Id);
                result.Add(item.Value);
            }
            return result;
        }

        public async Task<OrderBook> GetOrderBook(CurrencyPair currencyPair, byte depth = 5)
        {
            return await GetData<OrderBook>("returnOrderBook", "currencyPair=" + currencyPair, "depth=" + depth);
        }

        public async Task<Dictionary<CurrencyPair, OrderBook>> GetOrderBooks(byte depth = 5)
        {
            var books = await GetData<Dictionary<string, OrderBook>>("returnOrderBook", 
                "currencyPair=all", "depth=" + depth);

            var result = new Dictionary<CurrencyPair, OrderBook>(books.Count);
            foreach (var book in books)
                result.Add(CurrencyPair.Parse(book.Key), book.Value);
            return result;
        }

        public async Task<List<Trade>> GetTrades(CurrencyPair currencyPair)
        {
            return await GetData<List<Trade>>("returnTradeHistory", "currencyPair=" + currencyPair);
        }

        public async Task<List<Trade>> GetTrades(CurrencyPair currencyPair, DateTime startTime, DateTime endTime)
        {
            return await GetData<List<Trade>>("returnTradeHistory", "currencyPair=" + currencyPair,
                "start=" + Helper.DateTimeToUnixTimeStamp(startTime),
                "end=" + Helper.DateTimeToUnixTimeStamp(endTime));
        }

        public async Task<List<ChartData>> GetChartData(CurrencyPair currencyPair, 
            BarSize period, DateTime startTime, DateTime endTime)
        {
            return await GetData<List<ChartData>>("returnChartData", "currencyPair=" + currencyPair,
                "start=" + Helper.DateTimeToUnixTimeStamp(startTime),
                "end=" + Helper.DateTimeToUnixTimeStamp(endTime),
                "period=" + (int)period);
        }

        public async Task<List<ChartData>> GetChartData(CurrencyPair currencyPair, BarSize period)
        {
            return await GetChartData(currencyPair, period, Helper.DateTimeUnixEpochStart, DateTime.UtcNow);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task<T> GetData<T>(string command, params object[] parameters)
        {
            return await _apiWebClient.GetData<T>(Helper.ApiUrlHttpsRelativePublic + command, parameters);
        }
    }
}
