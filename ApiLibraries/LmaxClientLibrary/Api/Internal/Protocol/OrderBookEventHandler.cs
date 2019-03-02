/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.Collections.Generic;
using System.Globalization;
using Com.Lmax.Api.OrderBook;

namespace Com.Lmax.Api.Internal.Protocol
{
    public class OrderBookEventHandler : Handler
    {
        private const int InstrumentId = 0;
        private const int Timestamp = 1;
        private const int Bids = 2;
        private const int Asks = 3;
        private const int MarketClose = 4;
        private const int DailyHigh = 5;
        private const int DailyLow = 6;
        private const int ValuationBid = 7;
        private const int ValuationAsk = 8;
        private const int LastTraded = 9;

        private static readonly char[] PipeDelimiter = new char[] {'|'};
        private static readonly char[] SemicolonDelimiter = new char[] {';'};
        private static readonly char[] AtDelimiter = new char[] {'@'};
        
        public OrderBookEventHandler()
            : base("ob2")
        {            
        }

        public override void EndElement(string endElement)
        {
            if (ElementName == endElement)
            {
                string[] payload = Content.Split(PipeDelimiter);

                long instrumentId = ParseLong(payload[InstrumentId]);
                long timestamp = ParseTimestap(payload[Timestamp]);
                decimal valuationBidPrice;
                bool hasValuationBidPrice = TryParseDecimal(payload[ValuationBid], out valuationBidPrice);
                decimal valuationAskPrice;
                bool hasValuationAskPrice = TryParseDecimal(payload[ValuationAsk], out valuationAskPrice);
                decimal lastTradedPrice;
                bool hasLastTradedPrice = TryParseDecimal(payload[LastTraded], out lastTradedPrice);
                decimal dailyHighestTradedPrice;
                bool hasDailyHighestTradedPrice = TryParseDecimal(payload[DailyHigh], out dailyHighestTradedPrice);
                decimal dailyLowestTradedPrice;
                bool hasDailyLowestTradedPrice = TryParseDecimal(payload[DailyLow], out dailyLowestTradedPrice);
                List<PricePoint> bidPrices = ParsePrices(payload[Bids]);
                List<PricePoint> askPrices = ParsePrices(payload[Asks]);
                decimal marketClosePrice;
                long marketClosePriceTimestamp;
                bool hasMarketClosePrice = TryParseMarketClose(payload[MarketClose], out marketClosePrice, out marketClosePriceTimestamp);

                if (MarketDataChanged != null)
                    MarketDataChanged(new OrderBookEvent(instrumentId, hasValuationBidPrice, hasValuationAskPrice, valuationBidPrice, valuationAskPrice, bidPrices, askPrices, 
                                      hasMarketClosePrice, marketClosePrice, marketClosePriceTimestamp, hasLastTradedPrice, lastTradedPrice,  hasDailyHighestTradedPrice, 
                                      dailyHighestTradedPrice, hasDailyLowestTradedPrice, dailyLowestTradedPrice, timestamp));
            }
        }

        public event OnOrderBookEvent MarketDataChanged;

        private static bool TryParseMarketClose(string payload, out decimal marketClosePrice, out long marketClosePriceTimestamp)
        {            
            if (payload.Length == 0)
            {
                marketClosePrice = 0;
                marketClosePriceTimestamp = 0;
                return false;
            }

            string[] marketCloseEncodedData = payload.Split(SemicolonDelimiter);            
            if (marketCloseEncodedData[0].Length == 0)
            {
                marketClosePrice = 0;
                marketClosePriceTimestamp = 0;
                return false;
            }

            marketClosePrice = decimal.Parse(marketCloseEncodedData[0], DefaultHandler.NumberFormat);
            marketClosePriceTimestamp = ParseTimestap(marketCloseEncodedData[1]);
            return true;
        }

        private static List<PricePoint> ParsePrices(string payload)
        {
            if (payload.Length == 0)
            {
                return new List<PricePoint>(0);
            }

            string[] encodedPricePoints = payload.Split(SemicolonDelimiter);
            List<PricePoint> pricePointList = new List<PricePoint>(encodedPricePoints.Length);
            foreach (string encodedPricePoint in encodedPricePoints)
            {               
                string[] pricePoint = encodedPricePoint.Split(AtDelimiter, 2);
                pricePointList.Add(new PricePoint(decimal.Parse(pricePoint[0], DefaultHandler.NumberFormat), decimal.Parse(pricePoint[1], DefaultHandler.NumberFormat)));
            }
            return pricePointList;
        }

        private static bool TryParseDecimal(string value, out decimal output)
        {
            if (value.Length == 0)
            {
                output = 0;
                return false;
            }
            output = decimal.Parse(value, DefaultHandler.NumberFormat);
            return true;
        }

        private static long ParseTimestap(string value)
        {
            return long.Parse(value, NumberStyles.HexNumber);
        }

        private static long ParseLong(string value)
        {
            return long.Parse(value, DefaultHandler.NumberFormat);
        }        
    }
}

