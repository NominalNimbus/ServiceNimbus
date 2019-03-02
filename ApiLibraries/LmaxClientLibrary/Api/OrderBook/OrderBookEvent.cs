/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Com.Lmax.Api.OrderBook
{
    /// <summary>
    /// The contents of the event raised whenever there is a change to the
    /// market data for a given order book.
    /// </summary>
    public class OrderBookEvent
    {
        private readonly long _instrumentId;
        private readonly bool _hasValuationBidPrice;
        private readonly bool _hasValuationAskPrice;
        private readonly decimal _valuationBidPrice;
        private readonly decimal _valuationAskPrice;
        private readonly List<PricePoint> _bidPrices;
        private readonly List<PricePoint> _askPrices;
        private readonly bool _hasMarketClosePrice;
        private readonly decimal _mktClosePrice;
        private readonly decimal _lastTradedPrice;
        private readonly bool _hasDailyHighestTradedPrice;
        private readonly decimal _dailyHighestTradedPrice;
        private readonly bool _hasDailyLowestTradedPrice;
        private readonly decimal _dailyLowestTradedPrice;
        private readonly long _mktClosePriceTimestamp;
        private readonly bool _hasLastTradedPrice;
        private readonly long _timestamp;

        public OrderBookEvent(long instrumentId, bool hasValuationBidPrice, bool hasValuationAskPrice, decimal valuationBidPrice, decimal valuationAskPrice,
            List<PricePoint> bidPrices, List<PricePoint> askPrices, bool hasMarketClosePrice, decimal mktClosePrice, long mktClosePriceTimestamp, 
            bool hasLastTradedPrice, decimal lastTradedPrice, bool hasDailyHighestTradedPrice, decimal dailyHighestTradedPrice, bool hasDailyLowestTradedPrice, 
            decimal dailyLowestTradedPrice, long timestamp)
        {
            _instrumentId = instrumentId;
            _hasValuationBidPrice = hasValuationBidPrice;
            _hasValuationAskPrice = hasValuationAskPrice;
            _valuationBidPrice = valuationBidPrice;
            _valuationAskPrice = valuationAskPrice;
            _bidPrices = bidPrices;
            _askPrices = askPrices;
            _hasMarketClosePrice = hasMarketClosePrice;
            _mktClosePrice = mktClosePrice;
            _lastTradedPrice = lastTradedPrice;
            _hasDailyHighestTradedPrice = hasDailyHighestTradedPrice;
            _dailyHighestTradedPrice = dailyHighestTradedPrice;
            _hasDailyLowestTradedPrice = hasDailyLowestTradedPrice;
            _dailyLowestTradedPrice = dailyLowestTradedPrice;
            _mktClosePriceTimestamp = mktClosePriceTimestamp;
            _hasLastTradedPrice = hasLastTradedPrice;
            _timestamp = timestamp;
        }

        /// <summary>
        /// Readonly property to determine if this OrderBook has a valuation bid price.
        /// This will only be set if the particalur instrument has been traded.
        /// </summary>
        public bool HasValuationBidPrice
        {
            get { return _hasValuationBidPrice; }
        }

        /// <summary>
        /// Readonly property to determine if this OrderBook has a valuation ask price.
        /// This will only be set if the particalur instrument has been traded.
        /// </summary>
        public bool HasValuationAskPrice
        {
            get { return _hasValuationAskPrice; }
        }
  
        /// <summary>
        /// Readonly value of the valuation bid price for the OrderBook 
        /// </summary>
        public decimal ValuationBidPrice
        {
            get { return _valuationBidPrice; }
        }

        /// <summary>
        /// Readonly value of the valuation ask price for the OrderBook 
        /// </summary>
        public decimal ValuationAskPrice
        {
            get { return _valuationAskPrice; }
        }
  
        /// <summary>
        /// The timestamp as number of milliseconds since unix time epoch (1st January 1970) when the market data was updated
        /// inside of the exchange.
        /// </summary>
        public long Timestamp
        {
            get { return _timestamp; }
        }

        /// <summary>
        /// List of all of the bid prices for an order book.  The best price
        /// will be at index 0.  If there is no liquidity in the market this
        /// list will be empty.
        /// </summary>
        public List<PricePoint> BidPrices
        {
            get { return _bidPrices; }
        }

        /// <summary>
        /// List of all of the bid prices for an order book.  The best price
        /// will be at index 0.  If there is no liquidity in the market this
        /// list will be empty.
        /// </summary>
        public List<PricePoint> AskPrices
        {
            get { return _askPrices; }
        }

        /// <summary>
        /// Instrument Id of the OrderBook, same as the value used in subscribing
        /// </summary>
        public long InstrumentId
        {
            get { return _instrumentId; }
        }

        /// <summary>
        /// Indicate if a Last Market Close Price is available
        /// </summary>
        public bool HasMarketClosePrice
        {
            get { return _hasMarketClosePrice; }
        }

        /// <summary>
        /// The Market Close Price for the previous trading session.
        /// </summary>
        public decimal MktClosePrice
        {
            get { return _mktClosePrice; }
        }

        /// <summary>
        /// Timestamp as number of milliseconds since unix time epoch (1st January 1970) of the market close of the previous trading session.
        /// </summary>
        public long MktClosePriceTimestamp
        {
            get { return _mktClosePriceTimestamp; }
        }

        /// <summary>
        /// Indicate if a last traded price is available.
        /// </summary>
        public bool HasLastTradedPrice
        {
            get { return _hasLastTradedPrice; }
        }

        /// <summary>
        /// The price of the last trade that occured on this OrderBook.
        /// </summary>
        public decimal LastTradedPrice
        {
            get { return _lastTradedPrice; }
        }

        /// <summary>
        /// Indicate if a Highest Daily Traded Price is available, could be false
        /// if there have be no trades in the current session.
        /// </summary>
        public bool HasDailyHighestTradedPrice
        {
            get { return _hasDailyHighestTradedPrice; }
        }

        /// <summary>
        /// The highest price at which a trade occured in this trading session.
        /// </summary>
        public decimal DailyHighestTradedPrice
        {
            get { return _dailyHighestTradedPrice; }
        }

        /// <summary>
        /// Indicate if a Lowest Daily Traded Price is available, could be false
        /// if there have be no trades in the current session.
        /// </summary>
        public bool HasDailyLowestTradedPrice
        {
            get { return _hasDailyLowestTradedPrice; }
        }

        /// <summary>
        /// The lowest price at which a trade occured in this trading session.
        /// </summary>
        public decimal DailyLowestTradedPrice
        {
            get { return _dailyLowestTradedPrice; }
        }        

        public bool Equals(OrderBookEvent other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other._instrumentId == _instrumentId && other._hasValuationBidPrice.Equals(_hasValuationBidPrice) && other._hasValuationAskPrice.Equals(_hasValuationAskPrice) && 
                other._valuationBidPrice == _valuationBidPrice && other._valuationAskPrice == _valuationAskPrice && other._mktClosePrice == _mktClosePrice && 
                other._lastTradedPrice == _lastTradedPrice && other._dailyHighestTradedPrice == _dailyHighestTradedPrice && other._dailyLowestTradedPrice == _dailyLowestTradedPrice && 
                Equals(other._mktClosePriceTimestamp, _mktClosePriceTimestamp) && other._timestamp == _timestamp && EqualPrices(other._bidPrices, _bidPrices) && EqualPrices(other._askPrices, _askPrices);
        }

        private static bool EqualPrices(List<PricePoint> other, List<PricePoint> me)
        {
            if (other.Count != me.Count)
            {
                return false;
            }
            int i = 0;
            foreach (PricePoint pricePoint in me)
            {
                if (!pricePoint.Equals(other[i]))
                {
                    return false;
                }
                i++;
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (OrderBookEvent)) return false;
            return Equals((OrderBookEvent) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = _instrumentId.GetHashCode();
                result = (result*397) ^ _hasValuationBidPrice.GetHashCode();
                result = (result*397) ^ _hasValuationAskPrice.GetHashCode();
                result = (result*397) ^ _valuationBidPrice.GetHashCode();
                result = (result*397) ^ _valuationAskPrice.GetHashCode();
                result = (result*397) ^ (_bidPrices != null ? _bidPrices.GetHashCode() : 0);
                result = (result*397) ^ (_askPrices != null ? _askPrices.GetHashCode() : 0);
                result = (result*397) ^ _mktClosePrice.GetHashCode();
                result = (result*397) ^ _lastTradedPrice.GetHashCode();
                result = (result*397) ^ _dailyHighestTradedPrice.GetHashCode();
                result = (result*397) ^ _dailyLowestTradedPrice.GetHashCode();
                result = (result*397) ^ _mktClosePriceTimestamp.GetHashCode();
                result = (result*397) ^ _timestamp.GetHashCode();
                return result;
            }
        }

        public override string ToString()
        {
            return string.Format("OrderBookEvent{{InstrumentId: {0}, ValuationBidPrice: {1}, ValuationAskPrice: {2}, BidPrices: {3}, AskPrices: {4}, " +
                                 "MarketClosePrice: {5}, MarketClosePriceTimestamp: {6}, LastTradedPrice: {7}, DailyHighestTradedPrice: {8}, " + 
                                 "DailyLowestTradedPrice: {9}, Timestamp: {10}}}",
                                 _instrumentId, _valuationBidPrice, _valuationAskPrice, FormatPricePoints(_bidPrices), FormatPricePoints(_askPrices), 
                                 _mktClosePrice, _mktClosePriceTimestamp, _lastTradedPrice, _dailyHighestTradedPrice, _dailyLowestTradedPrice, _timestamp);
        }

        private static string FormatPricePoints(List<PricePoint> pricePoints)
        {
            StringBuilder buf = new StringBuilder();
            foreach (PricePoint pricePoint in pricePoints)
            {
                buf.Append(pricePoint.Quantity).Append("@").Append(pricePoint.Price).Append(", ");
            }
            return buf.ToString();
        }

        private static class MillisToTicksConverter
        {
            private const long MillisToTicksOffset = 621355968000000000L;

            public static long MillisToTicks(long millis)
            {
                return (millis * TimeSpan.TicksPerMillisecond) + MillisToTicksOffset;
            }

            public static DateTime MillisToDateTime(long millis)
            {
                return new DateTime(MillisToTicks(millis));
            }
        }
    }
}
