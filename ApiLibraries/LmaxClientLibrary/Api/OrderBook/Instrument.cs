/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;

namespace Com.Lmax.Api.OrderBook
{
    /// <summary>
    /// Contains the meta-data for an instrument (Security Definition)
    /// </summary>
    public class Instrument
    {
        private readonly long _id;
        private readonly string _name;
        private readonly UnderlyingInfo _underlying;
        private readonly CalendarInfo _calendar;
        private readonly RiskInfo _risk;
        private readonly OrderBookInfo _orderBook;
        private readonly ContractInfo _contract;
        private readonly CommercialInfo _commercial;

        ///<summary>
        /// Create a new instrument (e.g. security definition)
        ///</summary>
        ///<param name="id">The unique identifier of the instrument</param>
        ///<param name="name">The readable name of the instrument</param>
        ///<param name="underlying">The information about the underlying instrument</param>
        ///<param name="calendar">Contains information about trading dates and times</param>
        ///<param name="risk">Details on how to calculate risk for this instrument</param>
        ///<param name="orderBook">Information relating to the order book</param>
        ///<param name="contract">The contract information about this instrument</param>
        ///<param name="commercial">Data to calculate the commercials for this instrument</param>
        public Instrument(long id, string name, UnderlyingInfo underlying, CalendarInfo calendar, 
                          RiskInfo risk, OrderBookInfo orderBook, ContractInfo contract,
                          CommercialInfo commercial)
        {
            _id = id;
            _name = name;
            _underlying = underlying;
            _calendar = calendar;
            _risk = risk;
            _orderBook = orderBook;
            _contract = contract;
            _commercial = commercial;
        }

        /// <summary>
        /// Get the id of the instrument, also used a key for the order book on
        /// order requests, etc.
        /// </summary>
        public long Id
        {
            get { return _id; }
        }
        
        /// <summary>
        /// Get the name of the instrument, this is same value that is displayed
        /// on the Lmax Trader UI.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }
        
        /// <summary>
        /// Get information about the underlying instrument.
        /// </summary>
        public UnderlyingInfo Underlying
        {
            get { return _underlying; }
        }
        
        /// <summary>
        /// Get all date and time related information for the instrument.
        /// </summary>
        public CalendarInfo Calendar
        {
            get { return _calendar; }
        }
        
        /// <summary>
        /// Get all of the information relating to risk for this instrument.
        /// </summary>
        public RiskInfo Risk
        {
            get { return _risk; }
        }
        
        /// <summary>
        /// Get information relating the behaviour of the order book.
        /// </summary>
        public OrderBookInfo OrderBook
        {
            get { return _orderBook; }
        }
        
        /// <summary>
        /// Get information relating to the contract for this instrument.
        /// </summary>
        public ContractInfo Contract
        {
            get { return _contract; }
        }

        /// <summary>
        /// Get information relating to the commerical aggrements for this instrument.
        /// </summary>
        public CommercialInfo Commercial
        {
            get { return _commercial; }
        }

        ///<summary>
        /// Determines if this instrument is the same as another one.
        ///</summary>
        ///<param name="other">An instrument to compare this instance to</param>
        ///<returns>true if the given instrument is the same as this instrument</returns>
        public bool Equals(Instrument other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other._id == _id && Equals(other._name, _name) && Equals(other._underlying, _underlying) && Equals(other._calendar, _calendar) && Equals(other._risk, _risk) && Equals(other._orderBook, _orderBook) && Equals(other._contract, _contract) && Equals(other._commercial, _commercial);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (Instrument)) return false;
            return Equals((Instrument) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = _id.GetHashCode();
                result = (result*397) ^ (_name != null ? _name.GetHashCode() : 0);
                result = (result*397) ^ (_underlying != null ? _underlying.GetHashCode() : 0);
                result = (result*397) ^ (_calendar != null ? _calendar.GetHashCode() : 0);
                result = (result*397) ^ (_risk != null ? _risk.GetHashCode() : 0);
                result = (result*397) ^ (_orderBook != null ? _orderBook.GetHashCode() : 0);
                result = (result*397) ^ (_contract != null ? _contract.GetHashCode() : 0);
                result = (result*397) ^ (_commercial != null ? _commercial.GetHashCode() : 0);
                return result;
            }
        }

        public override string ToString()
        {
            return string.Format("Id: {0}, Name: {1}, Underlying: {2}, Calendar: {3}, Risk: {4}, OrderBook: {5}, Contract: {6}, Commercial: {7}", 
                _id, _name, _underlying, _calendar, _risk, _orderBook, _contract, _commercial);
        }
    }

    /// <summary>
    /// The underlying asset of the traded instrument, contain information
    /// such as symbol, asset class etc.
    /// </summary>
    public class UnderlyingInfo
    {
        private readonly string _symbol;
        private readonly string _isin;
        private readonly string _assetClass;

        public UnderlyingInfo(string symbol, string isin, string assetClass)
        {
            _symbol = symbol;
            _isin = isin;
            _assetClass = assetClass;
        }

        /// <summary>
        /// Get the text symbol used for the instrument.
        /// </summary>
        public string Symbol
        {
            get { return _symbol; }
        }

        /// <summary>
        /// Get the ISIN of the underlying instrument.
        /// </summary>
        public string Isin
        {
            get { return _isin; }
        }

        /// <summary>
        /// Get the asset class of the underlying.
        /// </summary>
        public String AssetClass
        {
            get { return _assetClass; }
        }

        public bool Equals(UnderlyingInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other._symbol, _symbol) && Equals(other._isin, _isin) && Equals(other._assetClass, _assetClass);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (UnderlyingInfo)) return false;
            return Equals((UnderlyingInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = (_symbol != null ? _symbol.GetHashCode() : 0);
                result = (result*397) ^ (_isin != null ? _isin.GetHashCode() : 0);
                result = (result*397) ^ (_assetClass != null ? _assetClass.GetHashCode() : 0);
                return result;
            }
        }

        public override string ToString()
        {
            return string.Format("Symbol: {0}, Isin: {1}, AssetClass: {2}", _symbol, _isin, _assetClass);
        }
    }

    /// <summary>
    /// Contains all of the information relating to dates and times during
    /// which the instrument is tradeable.
    /// </summary>
    public class CalendarInfo
    {
        private readonly DateTime _startTime;
        private readonly DateTime? _expiryTime;
        private readonly TimeSpan _open;
        private readonly TimeSpan _close;
        private readonly string _timeZone;
        private readonly List<DayOfWeek> _tradingDays;

        public CalendarInfo(DateTime startTime, DateTime? expiryTime, TimeSpan open, TimeSpan close, string timeZone, List<DayOfWeek> tradingDays)
        {
            _startTime = startTime;
            _expiryTime = expiryTime;
            _open = open;
            _close = close;
            _timeZone = timeZone;
            _tradingDays = tradingDays;
        }

        /// <summary>
        /// Get the start time of the instrument, this is the time that the instrument
        /// will first be available to trade.
        /// </summary>
        public DateTime StartTime
        {
            get { return _startTime; }
        }

        /// <summary>
        /// Get the DateTime the instrument will expire, mostly relevant for instruments
        /// like futures that only exist for a limited time.
        /// </summary>
        public DateTime? ExpiryTime
        {
            get { return _expiryTime; }
        }

        /// <summary>
        /// Get the time of day that the market will open (in time zone specified)
        /// by this object.  
        /// </summary> 
        public TimeSpan Open
        {
            get { return _open; }
        }

        /// <summary>
        /// Get the time of day that the market will close (in time zone specified)
        /// by this object.
        /// </summary>
        public TimeSpan Close
        {
            get { return _close; }
        }

        /// <summary>
        /// Get the timezone in which this calendar operates.
        /// </summary>
        public string TimeZone
        {
            get { return _timeZone; }
        }

        /// <summary>
        /// Get the days of week that the market is open.
        /// </summary>
        public List<DayOfWeek> TradingDays
        {
            get { return _tradingDays; }
        }

        public bool Equals(CalendarInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other._startTime.Equals(_startTime) && other._expiryTime.Equals(_expiryTime) && other._open.Equals(_open) && other._close.Equals(_close) && Equals(other._timeZone, _timeZone) && CompareDaysOfWeek(other._tradingDays, _tradingDays);
        }

        private static bool CompareDaysOfWeek(List<DayOfWeek> thisObj, List<DayOfWeek> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(thisObj, other)) return true;

            int thisBitMap = 0;
            int otherBitMap = 0;

            foreach (DayOfWeek dayOfWeek in thisObj)
            {
                thisBitMap |= (1 << (int)dayOfWeek);
            }

            foreach (DayOfWeek dayOfWeek in other)
            {
                otherBitMap |= (1 << (int)dayOfWeek);
            }

            return thisBitMap == otherBitMap;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (CalendarInfo)) return false;
            return Equals((CalendarInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = _startTime.GetHashCode();
                result = (result*397) ^ (_expiryTime.HasValue ? _expiryTime.Value.GetHashCode() : 0);
                result = (result*397) ^ _open.GetHashCode();
                result = (result*397) ^ _close.GetHashCode();
                result = (result*397) ^ (_timeZone != null ? _timeZone.GetHashCode() : 0);
                result = (result*397) ^ (_tradingDays != null ? _tradingDays.GetHashCode() : 0);
                return result;
            }
        }

        public override string ToString()
        {
            return string.Format("StartTime: {0}, ExpiryTime: {1}, Open: {2}, Close: {3}, TimeZone: {4}, TradingDays: [{5}]",
                _startTime, _expiryTime, _open, _close, _timeZone,
                string.Join(",", _tradingDays.ConvertAll<string>(DayToWeekToString).ToArray()));
        }

        private static string DayToWeekToString(DayOfWeek dayOfWeek)
        {
            return dayOfWeek.ToString();
        }

    }

    /// <summary>
    /// Holds the Risk elements for the instrument.
    /// </summary>
    public class RiskInfo
    {
        private readonly decimal _marginRate;
        private readonly decimal _maximumPosition;

        public RiskInfo(decimal marginRate, decimal maximumPosition)
        {
            _marginRate = marginRate;
            _maximumPosition = maximumPosition;
        }

        /// <summary>
        /// Get the margin rate as a percentage for this instrument.
        /// </summary>
        public decimal MarginRate
        {
            get { return _marginRate; }
        }

        /// <summary>
        /// Get the maxium position that can be held by a retail user on
        /// this instrument.
        /// </summary>
        public decimal MaximumPosition
        {
            get { return _maximumPosition; }
        }

        public bool Equals(RiskInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other._marginRate == _marginRate && other._maximumPosition == _maximumPosition;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (RiskInfo)) return false;
            return Equals((RiskInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_marginRate.GetHashCode()*397) ^ _maximumPosition.GetHashCode();
            }
        }

        public override string ToString()
        {
            return string.Format("MarginRate: {0}, MaximumPosition: {1}", _marginRate, _maximumPosition);
        }
    }

    /// <summary>
    /// Holds information related to the behaviour of the order book.
    /// </summary>
    public class OrderBookInfo
    {
        private readonly decimal _priceIncrement;
        private readonly decimal _quantityIncrement;
        private readonly decimal _volatilityBandPercentage;

        public OrderBookInfo(decimal priceIncrement, decimal quantityIncrement, decimal volatilityBandPercentage)
        {
            _priceIncrement = priceIncrement;
            _quantityIncrement = quantityIncrement;
            _volatilityBandPercentage = volatilityBandPercentage;
        }

        /// <summary>
        /// Get the price increment in which orders can be placed, i.e. the
        /// tick size.
        /// </summary>
        public decimal PriceIncrement
        {
            get { return _priceIncrement; }
        }

        /// <summary>
        /// Get the quantity increment in which orders can be placed.
        /// </summary>
        public decimal QuantityIncrement
        {
            get { return _quantityIncrement; }
        }

        /// <summary>
        /// Get the retail volatility band for the order book, this limits how
        /// far from the spread an order can be placed.
        /// </summary>
        public decimal VolatilityBandPercentage
        {
            get { return _volatilityBandPercentage; }
        }

        public bool Equals(OrderBookInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other._priceIncrement == _priceIncrement && other._quantityIncrement == _quantityIncrement && other._volatilityBandPercentage == _volatilityBandPercentage;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (OrderBookInfo)) return false;
            return Equals((OrderBookInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = _priceIncrement.GetHashCode();
                result = (result*397) ^ _quantityIncrement.GetHashCode();
                result = (result*397) ^ _volatilityBandPercentage.GetHashCode();
                return result;
            }
        }

        public override string ToString()
        {
            return string.Format("PriceIncrement: {0}, QuantityIncrement: {1}, VolatilityBandPercentage: {2}", _priceIncrement, _quantityIncrement, _volatilityBandPercentage);
        }
    }

    /// <summary>
    /// Holds the information about the contract for this instrument.
    /// </summary>
    public class ContractInfo
    {
        private readonly string _currency;
        private readonly decimal _unitPrice;
        private readonly string _unitOfMeasure;
        private readonly decimal _contractSize;

        public ContractInfo(string currency, decimal unitPrice, string unitOfMeasure, decimal contractSize)
        {
            _currency = currency;
            _unitPrice = unitPrice;
            _unitOfMeasure = unitOfMeasure;
            _contractSize = contractSize;
        }

        /// <summary>
        /// Get the currency this instrument is traded in.
        /// </summary>
        public string Currency
        {
            get { return _currency; }
        }

        /// <summary>
        /// Get the price for a single contract unit.
        /// </summary>
        public decimal UnitPrice
        {
            get { return _unitPrice; }
        }

        /// <summary>
        /// Get the name of the units being traded, e.g. barrels of oil, US dollars
        /// </summary>
        public string UnitOfMeasure
        {
            get { return _unitOfMeasure; }
        }

        /// <summary>
        /// Get the contract size.
        /// </summary>
        public decimal ContractSize
        {
            get { return _contractSize; }
        }

        public bool Equals(ContractInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other._currency, _currency) && other._unitPrice == _unitPrice && Equals(other._unitOfMeasure, _unitOfMeasure) && other._contractSize == _contractSize;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (ContractInfo)) return false;
            return Equals((ContractInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = (_currency != null ? _currency.GetHashCode() : 0);
                result = (result*397) ^ _unitPrice.GetHashCode();
                result = (result*397) ^ (_unitOfMeasure != null ? _unitOfMeasure.GetHashCode() : 0);
                result = (result*397) ^ _contractSize.GetHashCode();
                return result;
            }
        }

        public override string ToString()
        {
            return string.Format("Currency: {0}, UnitPrice: {1}, UnitOfMeasure: {2}, ContractSize: {3}", _currency, _unitPrice, _unitOfMeasure, _contractSize);
        }
    }

    /// <summary>
    /// Hold information pertaining to the commerical detail of this instrument.
    /// </summary>
    public class CommercialInfo
    {
        private readonly decimal _minimumCommission;
        private readonly decimal? _aggressiveCommissionRate;
        private readonly decimal? _passiveCommissionRate;
        private readonly decimal? _aggressiveCommissionPerContract;
        private readonly decimal? _passiveCommissionPerContract;
        private readonly string _fundingBaseRate;
        private readonly int _dailyInterestRateBasis;
		private readonly decimal? _fundingPremiumPercentage;
		private readonly decimal? _fundingReductionPercentage;
		private readonly decimal? _longSwapPoints;
		private readonly decimal? _shortSwapPoints;

        public CommercialInfo(decimal minimumCommission, decimal? aggressiveCommissionRate, decimal? passiveCommissionRate,
                              decimal? aggressiveCommissionPerContract, decimal? passiveCommissionPerContract,
                              string fundingBaseRate, int dailyInterestRateBasis, decimal? fundingPremiumPercentage, decimal? fundingReductionPercentage,
							  decimal? longSwapPoints, decimal? shortSwapPoints)
        {
            _minimumCommission = minimumCommission;
            _aggressiveCommissionRate = aggressiveCommissionRate;
            _passiveCommissionRate = passiveCommissionRate;
            _aggressiveCommissionPerContract = aggressiveCommissionPerContract;
            _passiveCommissionPerContract = passiveCommissionPerContract;
            _fundingBaseRate = fundingBaseRate;
            _dailyInterestRateBasis = dailyInterestRateBasis;
			_fundingPremiumPercentage = fundingPremiumPercentage;
			_fundingReductionPercentage = fundingReductionPercentage;
			_longSwapPoints = longSwapPoints;
			_shortSwapPoints = shortSwapPoints;
        }

        /// <summary>
        /// Get the minimum commision applied for a trade on this instrument.
        /// </summary>
        public decimal MinimumCommission
        {
            get { return _minimumCommission; }
        }

        /// <summary>
        /// Get the aggressive commission rate, may be null if commission is
        /// charged per contract.  The commission charge when the order is
        /// on the aggressive side of the trade.
        /// </summary>
        public decimal? AggressiveCommissionRate
        {
            get { return _aggressiveCommissionRate; }
        }

        /// <summary>
        /// Get the passive commission rate, may be null if commission is
        /// charged per contract.  The commission charge when the order is
        /// on the passive side of the trade.
        /// </summary>
        public decimal? PassiveCommissionRate
        {
            get { return _passiveCommissionRate; }
        }

        /// <summary>
        /// Get the aggressive commission per contract, may be null if commission is
        /// charged using a rate.  The commission charge when the order is
        /// on the aggressive side of the trade.
        /// </summary>
        public decimal? AggressiveCommissionPerContract
        {
            get { return _aggressiveCommissionPerContract; }
        }

        /// <summary>
        /// Get the passive commission per contract, may be null if commission is
        /// charged using a rate.  The commission charge when the order is
        /// on the passive side of the trade.
        /// </summary>
        public decimal? PassiveCommissionPerContract
        {
            get { return _passiveCommissionPerContract; }
        }

        /// <summary>
        /// Get the base rate used for funding this instrument.
        /// </summary>
        public string FundingBaseRate
        {
            get { return _fundingBaseRate; }
        }

        /// <summary>
        /// Get the number of days per year used to calculate the daily
        /// interest charged for funding.
        /// </summary>
        public int DailyInterestRateBasis
        {
            get { return _dailyInterestRateBasis; }
        }

        /// <summary>
        /// Get the rate used for overnight funding.
        /// </summary>
		[Obsolete("Use FundingPremiumPercentage, FundingReductionPercentage, LongSwapPoints and ShortSwapPoints instead")]
        public decimal FundingRate
        {
            get { return 0; }
        }

        /// <summary>
		/// Get the percentage premium added to the funding base rate for overnight funding of long positions on non-FX instruments.
		/// </summary>
		public decimal? FundingPremiumPercentage
		{
		    get { return _fundingPremiumPercentage; }
		}

        /// <summary>
		/// Get the percentage reduction applied to the funding base rate for overnight funding of short positions on non-FX instruments.
		/// </summary>
		public decimal? FundingReductionPercentage
		{
		    get { return _fundingReductionPercentage; }
		}

        /// <summary>
		/// Get the swap points used to calculate overnight interest swap charges for long positions on FX instruments.
		/// </summary>
		public decimal? LongSwapPoints
		{
		    get { return _longSwapPoints; }
		}

        /// <summary>
		/// Get the swap points used to calculate overnight interest swap charges for short positions on FX instruments.
		/// </summary>
		public decimal? ShortSwapPoints
		{
		    get { return _shortSwapPoints; }
		}

        public bool Equals(CommercialInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other._minimumCommission == _minimumCommission && other._aggressiveCommissionRate.Equals(_aggressiveCommissionRate) && other._passiveCommissionRate.Equals(_passiveCommissionRate) && other._aggressiveCommissionPerContract.Equals(_aggressiveCommissionPerContract) && other._passiveCommissionPerContract.Equals(_passiveCommissionPerContract) && Equals(other._fundingBaseRate, _fundingBaseRate) && other._dailyInterestRateBasis == _dailyInterestRateBasis && other._fundingPremiumPercentage == _fundingPremiumPercentage && other._fundingReductionPercentage == _fundingReductionPercentage && other._longSwapPoints == _longSwapPoints && other._shortSwapPoints == _shortSwapPoints;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (CommercialInfo)) return false;
            return Equals((CommercialInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = _minimumCommission.GetHashCode();
                result = (result*397) ^ (_aggressiveCommissionRate.HasValue ? _aggressiveCommissionRate.Value.GetHashCode() : 0);
                result = (result*397) ^ (_passiveCommissionRate.HasValue ? _passiveCommissionRate.Value.GetHashCode() : 0);
                result = (result*397) ^ (_aggressiveCommissionPerContract.HasValue ? _aggressiveCommissionPerContract.Value.GetHashCode() : 0);
                result = (result*397) ^ (_passiveCommissionPerContract.HasValue ? _passiveCommissionPerContract.Value.GetHashCode() : 0);
                result = (result*397) ^ (_fundingBaseRate != null ? _fundingBaseRate.GetHashCode() : 0);
                result = (result*397) ^ _dailyInterestRateBasis;
                result = (result*397) ^ _fundingPremiumPercentage.GetHashCode();
                result = (result*397) ^ _fundingReductionPercentage.GetHashCode();
                result = (result*397) ^ _longSwapPoints.GetHashCode();
                result = (result*397) ^ _shortSwapPoints.GetHashCode();
                return result;
            }
        }

        public override string ToString()
        {
            return string.Format("MinimumCommission: {0}, AggressiveCommissionRate: {1}, PassiveCommissionRate: {2}, AggressiveCommissionPerContract: {3}, PassiveCommissionPerContract: {4}, FundingBaseRate: {5}, DailyInterestRateBasis: {6}, FundingPremiumPercentage: {7}, FundingReductionPercentage: {8}, LongSwapPoints: {9}, ShortSwapPoints{10}", 
                _minimumCommission, _aggressiveCommissionRate, _passiveCommissionRate, _aggressiveCommissionPerContract, _passiveCommissionPerContract, _fundingBaseRate, _dailyInterestRateBasis, _fundingPremiumPercentage, _fundingReductionPercentage, _longSwapPoints, _shortSwapPoints);
        }
    }
}
