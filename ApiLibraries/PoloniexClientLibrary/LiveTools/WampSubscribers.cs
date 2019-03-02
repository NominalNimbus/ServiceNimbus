/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PoloniexAPI.MarketTools;
using WampSharp.Core.Serialization;
using WampSharp.V2.Core.Contracts;
using WampSharp.V2.PubSub;

namespace PoloniexAPI.LiveTools
{
    internal class TickSubscriber
    {
        public event EventHandler<Quote> OnTickerUpdate;

        [WampTopic("ticker")]
        public void OnMessage(string currencyPair, decimal last, decimal lowestAsk, decimal highestBid, decimal percentChange,
            decimal baseVolume, decimal quoteVolume, byte isFrozen, decimal dayHigh, decimal dayLow)
        {
            var quote = new Quote
            {
                Symbol = CurrencyPair.Parse(currencyPair),
                Last = last,
                Ask = lowestAsk,
                Bid = highestBid,
                PercentChange = percentChange,
                BaseVolume = baseVolume,
                Volume = quoteVolume,
                IsFrozenValue = isFrozen
            };
            OnTickerUpdate?.Invoke(this, quote);
        }
    }

    internal class OrderBookSubscriber : IWampRawTopicClientSubscriber
    {
        public event EventHandler<OrderBookItem> OnOrderBookUpdate;

        public void Event<TMessage>(IWampFormatter<TMessage> formatter, long publicationId, EventDetails details)
        {
            throw new NotImplementedException();
        }

        public void Event<TMessage>(IWampFormatter<TMessage> formatter, long publicationId, EventDetails details, TMessage[] arguments)
        {
            throw new NotImplementedException();
        }

        public void Event<TMessage>(IWampFormatter<TMessage> formatter, long publicationId, EventDetails details, TMessage[] arguments,
                                    IDictionary<string, TMessage> argumentsKeywords)
        {
            if (arguments == null)
                return;

            foreach (var arg in arguments)
            {
                var token = arg as JToken;
                var type = (OrderBookItemType)Enum.Parse(typeof(OrderBookItemType), token.Value<string>("type"), true);
                var item = type == OrderBookItemType.NewTrade 
                    ? token.SelectToken("data").ToObject<OrderBookTrade>() 
                    : token.SelectToken("data").ToObject<OrderBookItem>();
                item.Symbol = CurrencyPair.Parse(details.Topic);
                item.Type = type;
                OnOrderBookUpdate?.Invoke(this, item);
            }
        }
    }
}
