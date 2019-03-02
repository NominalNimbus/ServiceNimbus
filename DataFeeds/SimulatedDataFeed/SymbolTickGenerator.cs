/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using CommonObjects;
using ServerCommonObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulatedDataFeed
{
    internal class SymbolTickGenerator
    {
        #region constants

        private const int RandomVolumeMinValue = 1000;
        private const int RandomVolumeMaxValue = 10000;
        private const int RandomPriceMaxValue = 100;

        #endregion //constants

        #region Members
        
        private decimal _priceOffset;
        private decimal _askOffset;
        private decimal _bidOffset;
        private decimal _maxPriceLimit;
        private decimal _minPriceLimit;

        private Security _security;
        private decimal _startPrice;
        private Random _random;
        private string _dataFeedName;

        private TrendType _trend = TrendType.NoTrend;
        private int _trendCount = 0;
        private decimal _lastPrice;

        #endregion //Members

        #region Init

        public SymbolTickGenerator(Security security, decimal startPrice, string dataFeed)
        {
            _security = security;
            _lastPrice = _startPrice = startPrice;
            _dataFeedName = dataFeed;
            _random = new Random(Guid.NewGuid().GetHashCode());
            InitOffsets();
        }

        private void InitOffsets()
        {
            _priceOffset = _security.PriceIncrement * 10;
            _askOffset = _security.PriceIncrement * 3;
            _bidOffset = _security.PriceIncrement * 3;
            _maxPriceLimit = _startPrice + _security.PriceIncrement * 500;
            _minPriceLimit = Math.Max(2 * (_priceOffset + _bidOffset), _startPrice - _security.PriceIncrement * 500);
        }
        
        #endregion //Init

        #region Public methods

        public Tick GenerateNewTick()
        {
            GenerateLast();
            var askSize = GenerateSize();
            var bidSize = GenerateSize();
            var level2 = new List<MarketLevel2>();
            for (int i = 1; i < 11; i++)
            {
                level2.Add(new MarketLevel2
                {
                    DomLevel = i + 1,
                    AskPrice = GenerateAsk(),
                    AskSize = GenerateSize(),
                    BidPrice = GenerateBid(),
                    BidSize = GenerateSize()
                });
            }

            return new Tick
            {
                DataFeed = _dataFeedName,
                Symbol = _security,
                Date = DateTime.UtcNow,
                Bid = GenerateBid(),
                Ask = GenerateAsk(),
                AskSize = askSize,
                BidSize = bidSize,
                Price = _lastPrice,
                Volume = askSize + bidSize,
                Level2 = level2
            };
        }

        public List<Bar> GenerateHistory(Selection parameters)
        {
            var bars = new List<Bar>();
            var currentDate = CommonHelper.GetIdealBarTime(parameters.From, parameters.Timeframe, parameters.TimeFactor);
            var lastPrice = _startPrice;

            while (currentDate <= parameters.To && currentDate <= DateTime.UtcNow)
            {
                var price = lastPrice + GenerateHistoryPriceChange();
                var price1 = lastPrice + GenerateHistoryPriceChange();
                var price2 = lastPrice + GenerateHistoryPriceChange();
                var high = Math.Max(Math.Max(price, lastPrice), Math.Max(price1, price2));
                var low = Math.Min(Math.Min(price, lastPrice), Math.Min(price1, price2));
                var volume = GenerateSize();
                bars.Add(new Bar
                {
                    Date = currentDate,
                    OpenBid = lastPrice,
                    OpenAsk = lastPrice,
                    CloseBid = price,
                    CloseAsk = price,
                    HighBid = high,
                    HighAsk = high,
                    LowBid = low,
                    LowAsk = low,
                    VolumeBid = volume,
                    VolumeAsk = volume
                });
                lastPrice = price;
                if (parameters.Timeframe == Timeframe.Minute)
                    currentDate = currentDate.AddMinutes(parameters.TimeFactor);
                else
                    currentDate = currentDate.AddDays(parameters.TimeFactor);
            }

            return bars;
        }

        #endregion //Public methods

        #region Helper methods

        private decimal RandomValue(decimal range) => 
            (decimal)_random.NextDouble() * range;

        private decimal GenerateHistoryPriceChange() =>
            (decimal)_random.Next(RandomPriceMaxValue) / RandomPriceMaxValue;
        
        private long GenerateSize() =>
            _random.Next(RandomVolumeMinValue, RandomVolumeMaxValue);

        private decimal GenerateLast()
        {
            if (_trendCount == 0 || _lastPrice < _minPriceLimit)
            {
                _trendCount = _random.Next(3, 13);
                if (_lastPrice > _maxPriceLimit)
                    _trend = TrendType.Down;
                else if (_lastPrice < _minPriceLimit)
                    _trend = TrendType.Up;
                else
                    _trend = _trend == TrendType.Down ? TrendType.Up : TrendType.Down;
            }

            var multiplier = _trend == TrendType.Down ? -1 : 1;
            _lastPrice += multiplier * RandomValue(_priceOffset);
            _trendCount--;
            return _lastPrice;
        }

        private decimal GenerateAsk() =>
            RoundToDigits(_lastPrice + RandomValue(_askOffset));

        private decimal GenerateBid() =>
            RoundToDigits(_lastPrice - RandomValue(_bidOffset));

        public decimal RoundToDigits(decimal price) =>
            Math.Round(price, _security.Digit);
        
        #endregion //Helper methods

    }

 
}
