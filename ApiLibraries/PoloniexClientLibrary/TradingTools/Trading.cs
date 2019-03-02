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
using Newtonsoft.Json.Linq;

namespace PoloniexAPI.TradingTools
{
    public class Trading
    {
        private readonly ApiWebClient _apiWebClient;

        internal Trading(ApiWebClient apiWebClient)
        {
            _apiWebClient = apiWebClient;
        }

        #region Orders and Order Trades

        public async Task<List<Order>> GetOpenOrders(CurrencyPair currencyPair)
        {
            var postData = new Dictionary<string, object> { ["currencyPair"] = currencyPair };
            var orders = await PostData<List<Order>>("returnOpenOrders", postData);
            if (orders != null && orders.Count > 0)
                orders.ForEach(o => o.CurrencyPair = currencyPair);
            return orders;
        }

        public async Task<List<Order>> GetOpenOrders()
        {
            var postData = new Dictionary<string, object> { ["currencyPair"] = "all" };
            var ordersPerSymbol = await PostData<Dictionary<string, List<Order>>>("returnOpenOrders", postData);

            var orders = new List<Order>();
            if (ordersPerSymbol != null && ordersPerSymbol.Count > 0)
            {
                foreach (var symbol in ordersPerSymbol)
                {
                    if (symbol.Value != null && symbol.Value.Count > 0)
                    {
                        var currencyPair = CurrencyPair.Parse(symbol.Key);
                        symbol.Value.ForEach(o => o.CurrencyPair = currencyPair);
                        orders.AddRange(symbol.Value);
                    }
                }
            }
            return orders;
        }

        public async Task<List<OrderTrade>> GetOrderTrades(ulong orderId)
        {
            var postData = new Dictionary<string, object> { ["orderNumber"] = orderId };
            return await PostData<List<OrderTrade>>("returnOrderTrades", postData);
        }

        public async Task<List<Trade>> GetTrades(CurrencyPair currencyPair, 
            DateTime startTime, DateTime endTime, int count = 500)
        {
            var postData = new Dictionary<string, object>
            {
                ["currencyPair"] = currencyPair,
                ["start"] = Helper.DateTimeToUnixTimeStamp(startTime),
                ["end"] = Helper.DateTimeToUnixTimeStamp(endTime),
                ["limit"] = count  //default: 500, maximum: 10000
            };

            var trades = await PostData<List<Trade>>("returnTradeHistory", postData);
            if (trades != null && trades.Count > 0)
                trades.ForEach(t => t.CurrencyPair = currencyPair);
            return trades;
        }

        public async Task<List<Trade>> GetTrades(DateTime startTime, DateTime endTime)
        {
            var postData = new Dictionary<string, object>
            {
                ["currencyPair"] = "all",
                ["start"] = Helper.DateTimeToUnixTimeStamp(startTime),
                ["end"] = Helper.DateTimeToUnixTimeStamp(endTime)
            };

            var tradesPerSymbol = await PostData<Dictionary<string, List<Trade>>>("returnTradeHistory", postData);
            var trades = new List<Trade>();
            if (tradesPerSymbol != null && tradesPerSymbol.Count > 0)
            {
                foreach (var symbol in tradesPerSymbol)
                {
                    if (symbol.Value != null && symbol.Value.Count > 0)
                    {
                        var currencyPair = CurrencyPair.Parse(symbol.Key);
                        symbol.Value.ForEach(o => o.CurrencyPair = currencyPair);
                        trades.AddRange(symbol.Value);
                    }
                }
            }
            return trades;
        }

        #endregion

        #region Margin Positions

        public async Task<List<Position>> GetMarginPositions(CurrencyPair currencyPair)
        {
            var postData = new Dictionary<string, object> { ["currencyPair"] = currencyPair };
            var positions = await PostData<List<Position>>("getMarginPosition", postData);
            if (positions != null && positions.Count > 0)
                positions.ForEach(p => p.CurrencyPair = currencyPair);
            return positions;
        }

        public async Task<List<Position>> GetMarginPositions()
        {
            var postData = new Dictionary<string, object> { ["currencyPair"] = "all" };
            var grouped = await PostData<Dictionary<string, List<Position>>>("getMarginPosition", postData);
            var positions = new List<Position>();
            if (grouped != null && grouped.Count > 0)
            {
                foreach (var symbol in grouped)
                {
                    if (symbol.Value != null && symbol.Value.Count > 0)
                    {
                        var currencyPair = CurrencyPair.Parse(symbol.Key);
                        symbol.Value.ForEach(o => o.CurrencyPair = currencyPair);
                        positions.AddRange(symbol.Value);
                    }
                }
            }
            return positions;
        }

        public async Task<bool> CloseMarginPosition(CurrencyPair currencyPair)
        {
            var postData = new Dictionary<string, object> { ["currencyPair"] = currencyPair };
            var result = await PostData<JObject>("closeMarginPosition", postData);
            return result.Value<byte>("success") == 1;
        }

        #endregion

        #region Trading

        public async Task<ulong> PlaceOrder(CurrencyPair currencyPair, OrderSide side, 
            decimal pricePerCoin, decimal quantity, bool isMarginOrder)
        {
            var postData = new Dictionary<string, object>
            {
                ["currencyPair"] = currencyPair,
                ["rate"] = pricePerCoin.ToStringNormalized(),
                ["amount"] = quantity.ToStringNormalized()
            };

            var cmd = isMarginOrder ? ("margin" + side.ToString()) : side.ToString().ToLowerInvariant();
            var data = await PostData<JObject>(cmd, postData);
            if (data.TryGetValue("error", out var error))
                throw new Exception(error.ToString());

            return data.Value<ulong>("orderNumber");
        }

        public async Task<bool> CancelOrder(CurrencyPair currencyPair, ulong orderId)
        {
            var postData = new Dictionary<string, object>
            {
                ["currencyPair"] = currencyPair,
                ["orderNumber"] = orderId
            };

            var data = await PostData<JObject>("cancelOrder", postData);
            return data.Value<byte>("success") == 1;
        }

        public async Task<ulong> ModifyOrder(ulong orderId, OrderSide side, decimal pricePerCoin, decimal quantity = 0m)
        {
            var postData = new Dictionary<string, object>
            {
                ["orderNumber"] = orderId,
                ["rate"] = pricePerCoin.ToStringNormalized()
            };
            if (quantity > 0m)
                postData["amount"] = quantity;

            var data = await PostData<JObject>("moveOrder", postData);
            if (data.TryGetValue("error", out var error))
                throw new Exception(error.ToString());

            return data.Value<ulong>("orderNumber");
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task<T> PostData<T>(string command, Dictionary<string, object> postData)
        {
            return await _apiWebClient.PostData<T>(command, postData);
        }
    }
}
