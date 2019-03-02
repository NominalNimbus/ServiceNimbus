/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using Com.Lmax.Api.OrderBook;

//        <instrument>
//	        <id>850</id>
//	        <name>#728 Test Instrument One</name>
//	        <startTime>2008-12-16T08:00:00</startTime>
//	        <tradingHours>
//		        <openingOffset>0</openingOffset>
//		        <closingOffset>1440</closingOffset>
//		        <timezone>Europe/London</timezone>
//	        </tradingHours>
//	        <margin>10</margin>
//	        <currency>USD</currency>
//	        <unitPrice>1</unitPrice>
//	        <minimumOrderQuantity>0.1</minimumOrderQuantity>
//	        <orderQuantityIncrement>0.1</orderQuantityIncrement>
//	        <minimumPrice>0</minimumPrice>
//	        <maximumPrice>5000</maximumPrice>
//	        <priceIncrement>0.01</priceIncrement>
//	        <stopBuffer>10</stopBuffer>
//	        <assetClass>EQUITY</assetClass>
//	        <underlyingIsin>isin2</underlyingIsin>
//	        <symbol>728A</symbol>
//	        <maximumPositionThreshold>1000000000000000</maximumPositionThreshold>
//	        <aggressiveCommissionRate>0.00</aggressiveCommissionRate>
//	        <passiveCommissionRate>0.00</passiveCommissionRate>
//	        <minimumCommission>0</minimumCommission>
//	        <fundingPremiumPercentage>0.00</fundingPremiumPercentage>
//	        <fundingReductionPercentage>0.00</fundingReductionPercentage>
//	        <fundingBaseRate>Wholesale</fundingBaseRate>
//	        <dailyInterestRateBasis>360</dailyInterestRateBasis>
//	        <contractUnitOfMeasure>POINT</contractUnitOfMeasure>
//	        <contractSize>1.00</contractSize>
//	        <tradingDays>
//		        <tradingDay>MONDAY</tradingDay>
//		        <tradingDay>TUESDAY</tradingDay>
//		        <tradingDay>WEDNESDAY</tradingDay>
//		        <tradingDay>THURSDAY</tradingDay>
//		        <tradingDay>FRIDAY</tradingDay>
//	        </tradingDays>
//	        <retailVolatilityBandPercentage>7</retailVolatilityBandPercentage>
//        </instrument>

namespace Com.Lmax.Api.Internal.Protocol
{
    public class SearchResponseHandler : DefaultHandler
    {
        private const string Id = "id";
        private const string Name = "name";
        private const string StartTime = "startTime";
        private const string EndTime = "endTime";
        private const string OpeningOffset = "openingOffset";
        private const string ClosingOffset = "closingOffset";
        private const string TradingDay = "tradingDay";
        private const string Timezone = "timezone";
        private const string Margin = "margin";
        private const string Currency = "currency";
        private const string UnitPrice = "unitPrice";
        private const string MinimumOrderQuantity = "minimumOrderQuantity";
        private const string OrderQuantityIncrement = "orderQuantityIncrement";
        private const string PriceIncrement = "priceIncrement";
        private const string AssetClass = "assetClass";
        private const string UnderlyingIsin = "underlyingIsin";
        private const string Symbol = "symbol";
        private const string MaximumPositionThreshold = "maximumPositionThreshold";
        private const string AggressiveCommisionRate = "aggressiveCommissionRate";
        private const string PassiveCommissionRate = "passiveCommissionRate";
        private const string MinimumCommission = "minimumCommission";
        private const string AggressiveCommissionPerContract = "aggressiveCommissionPerContract";
        private const string PassiveCommissionPerContract = "passiveCommissionPerContract";
        private const string FundingPremiumPercentage = "fundingPremiumPercentage";
        private const string FundingReductionPercentage = "fundingReductionPercentage";
        private const string LongSwapPoints = "longSwapPoints";
        private const string ShortSwapPoints = "shortSwapPoints";
        private const string FundingBaseRate = "fundingBaseRate";
        private const string DailyInteresetRateBasis = "dailyInterestRateBasis";
        private const string ContractUnitMeasure = "contractUnitOfMeasure";
        private const string ContractSize = "contractSize";
        private const string RetailVolatilityBandPercentage = "retailVolatilityBandPercentage";
        private const string HasMoreResultsTag = "hasMoreResults";

        private readonly List<Instrument> _instruments = new List<Instrument>();
        private readonly ListHandler _tradingDaysHandler = new ListHandler(TradingDay);

        public SearchResponseHandler()
        {
            AddHandler(Id);
            AddHandler(Name);
            AddHandler(StartTime);
            AddHandler(EndTime);
            AddHandler(OpeningOffset);
            AddHandler(ClosingOffset);
            AddHandler(Timezone);
            AddHandler(Margin);
            AddHandler(Currency);
            AddHandler(UnitPrice);
            AddHandler(MinimumOrderQuantity);
            AddHandler(OrderQuantityIncrement);
            AddHandler(PriceIncrement);
            AddHandler(AssetClass);
            AddHandler(UnderlyingIsin);
            AddHandler(Symbol);
            AddHandler(MaximumPositionThreshold);
            AddHandler(AggressiveCommisionRate);
            AddHandler(PassiveCommissionRate);
            AddHandler(MinimumCommission);
            AddHandler(AggressiveCommissionPerContract);
            AddHandler(PassiveCommissionPerContract);
            AddHandler(FundingPremiumPercentage);
            AddHandler(FundingReductionPercentage);
            AddHandler(LongSwapPoints);
            AddHandler(ShortSwapPoints);
            AddHandler(FundingBaseRate);
            AddHandler(DailyInteresetRateBasis);
            AddHandler(ContractUnitMeasure);
            AddHandler(ContractSize);
            AddHandler(RetailVolatilityBandPercentage);
            AddHandler(_tradingDaysHandler);
            AddHandler(HasMoreResultsTag);
        }

        public override void EndElement(string endElement)
        {
            if ("instrument" == endElement)
            {
                long id = GetLongValue(Id, 0L);
                String name = GetStringValue(Name);

                string symbol = GetStringValue(Symbol);
                string isin = GetStringValue(UnderlyingIsin);
                string assetClass = GetStringValue(AssetClass);

                UnderlyingInfo underlying = new UnderlyingInfo(symbol, isin, assetClass);

                DateTime startTime = GetDateTime(StartTime, DateTime.MinValue);
                DateTime? expiryTime = GetDateTime(EndTime);
                TimeSpan openOffset = GetTimeSpan(OpeningOffset, TimeSpan.MinValue);
                TimeSpan closeOffset = GetTimeSpan(ClosingOffset, TimeSpan.MinValue);
                string timeZone = GetStringValue(Timezone);
                List<DayOfWeek> daysOfWeek = GetDaysOfWeek();

                CalendarInfo calendarInfo = new CalendarInfo(startTime, expiryTime, openOffset, closeOffset, timeZone, daysOfWeek);

                decimal marginRate = GetDecimalValue(Margin, 0);
                decimal maximumPosition = GetDecimalValue(MaximumPositionThreshold, 0);

                RiskInfo riskInfo = new RiskInfo(marginRate, maximumPosition);

                decimal priceIncrement = GetDecimalValue(PriceIncrement, 0);
                decimal quantityIncrement = GetDecimalValue(OrderQuantityIncrement, 0);
                decimal volatilityBandPercentage = GetDecimalValue(RetailVolatilityBandPercentage, 0);

                OrderBookInfo orderBookInfo = new OrderBookInfo(priceIncrement, quantityIncrement, volatilityBandPercentage);

                string currency = GetStringValue(Currency);
                decimal unitPrice = GetDecimalValue(UnitPrice, 0);
                string unitOfMeasure = GetStringValue(ContractUnitMeasure);
                decimal contractSize = GetDecimalValue(ContractSize, 0);

                ContractInfo contractInfo = new ContractInfo(currency, unitPrice, unitOfMeasure, contractSize);

                decimal minimumCommission = GetDecimalValue(MinimumCommission, 0);
                decimal? aggressiveCommissionRate = GetDecimalValue(AggressiveCommisionRate);
                decimal? passiveCommissionRate = GetDecimalValue(PassiveCommissionRate);
                decimal? aggressiveCommissionPerContract = GetDecimalValue(AggressiveCommissionPerContract);
                decimal? passiveCommissionPerContract = GetDecimalValue(PassiveCommissionPerContract);
                string fundingBaseRate = GetStringValue(FundingBaseRate);
                int dailyInterestRateBasis = GetIntValue(DailyInteresetRateBasis, 0);
                decimal? fundingPremiumPercentage = GetDecimalValue(FundingPremiumPercentage);
                decimal? fundingReductionPercentage = GetDecimalValue(FundingReductionPercentage);
                decimal? longSwapPoints = GetDecimalValue(LongSwapPoints);
                decimal? shortSwapPoints = GetDecimalValue(ShortSwapPoints);

                CommercialInfo commercialInfo = new CommercialInfo(minimumCommission, aggressiveCommissionRate, passiveCommissionRate,
                                                                   aggressiveCommissionPerContract, passiveCommissionPerContract,
                                                                   fundingBaseRate, dailyInterestRateBasis, 
																   fundingPremiumPercentage, fundingReductionPercentage, longSwapPoints, shortSwapPoints);

                _instruments.Add(new Instrument(id, name, underlying, calendarInfo, riskInfo, orderBookInfo, contractInfo, commercialInfo));
            }
        }

        private List<DayOfWeek> GetDaysOfWeek()
        {
            List<DayOfWeek> daysOfWeek = _tradingDaysHandler.GetContentList<DayOfWeek>(ConvertToDayOfWeek);
            _tradingDaysHandler.Clear();
            return daysOfWeek;
        }

        private TimeSpan GetTimeSpan(string tag, TimeSpan defaultValue)
        {
            long timeSpanInMinutes;
            if (TryGetValue(tag, out timeSpanInMinutes))
            {
                if (timeSpanInMinutes < 0)
                {
                    timeSpanInMinutes = (24*60) + timeSpanInMinutes;
                }

                return TimeSpan.FromMinutes(timeSpanInMinutes);
            }

            return defaultValue;
        }

        private DateTime GetDateTime(string tag, DateTime defaultValue)
        {
            string dateTimeString = GetStringValue(tag);
            if (dateTimeString != null)
            {
                DateTime dateTime;
                if (DateTime.TryParse(dateTimeString, out dateTime))
                {
                    return dateTime;
                }
            }

            return defaultValue;
        }

        private DateTime? GetDateTime(string tag)
        {
            string dateTimeString = GetStringValue(tag);
            if (dateTimeString != null)
            {
                DateTime dateTime;
                if (DateTime.TryParse(dateTimeString, out dateTime))
                {
                    return dateTime;
                }
            }

            return null;
        }

        public List<Instrument> Instruments
        {
            get { return _instruments; }
        }

        public bool HasMoreResults
        {
            get { return "true".Equals(GetStringValue(HasMoreResultsTag), StringComparison.OrdinalIgnoreCase); }
        }

        private static DayOfWeek ConvertToDayOfWeek(string dayOfWeekAsString)
        {
            return (DayOfWeek) Enum.Parse(typeof (DayOfWeek), dayOfWeekAsString, true);
        }
    }
}
