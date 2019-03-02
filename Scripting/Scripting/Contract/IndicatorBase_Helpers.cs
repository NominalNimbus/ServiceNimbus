/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.Collections.Generic;
using System.Linq;
using CommonObjects;

namespace Scripting
{
    public abstract partial class IndicatorBase
    {
        protected decimal GetPrice(Bar bar, PriceConstants type)
        {
            if (PriceType == PriceType.Bid)
            {
                switch (type)
                {
                    case PriceConstants.OPEN: return bar.OpenBid;
                    case PriceConstants.HIGH: return bar.HighBid;
                    case PriceConstants.LOW: return bar.LowBid;
                    case PriceConstants.CLOSE: return bar.CloseBid;
                    case PriceConstants.MEDIAN: return (bar.HighBid + bar.LowBid) / 2M;
                    case PriceConstants.TYPICAL: return (bar.HighBid + bar.LowBid + bar.CloseBid) / 3M;
                    case PriceConstants.WEIGHTED: return (bar.HighBid + bar.LowBid + bar.CloseBid + bar.OpenBid) / 4M;
                }
            }
            else if (PriceType == PriceType.Ask)
            {
                switch (type)
                {
                    case PriceConstants.OPEN: return bar.OpenAsk;
                    case PriceConstants.HIGH: return bar.HighAsk;
                    case PriceConstants.LOW: return bar.LowAsk;
                    case PriceConstants.CLOSE: return bar.CloseAsk;
                    case PriceConstants.MEDIAN: return (bar.HighAsk + bar.LowAsk) / 2M;
                    case PriceConstants.TYPICAL: return (bar.HighAsk + bar.LowAsk + bar.CloseAsk) / 3M;
                    case PriceConstants.WEIGHTED: return (bar.HighAsk + bar.LowAsk + bar.CloseAsk + bar.OpenAsk) / 4M;
                }
            }
            else
            {
                switch (type)
                {
                    case PriceConstants.OPEN: return bar.MeanOpen;
                    case PriceConstants.HIGH: return bar.MeanHigh;
                    case PriceConstants.LOW: return bar.MeanLow;
                    case PriceConstants.CLOSE: return bar.MeanClose;
                    case PriceConstants.MEDIAN: return (bar.MeanHigh + bar.MeanLow) / 2M;
                    case PriceConstants.TYPICAL: return (bar.MeanHigh + bar.MeanLow + bar.MeanClose) / 3M;
                    case PriceConstants.WEIGHTED: return (bar.MeanHigh + bar.MeanLow + bar.MeanClose + bar.MeanOpen) / 4M;
                }
            }

            System.Diagnostics.Trace.TraceError("Unsupported price type: {0} ({1})", type, PriceType);
            return 0M;
        }

        protected decimal GetMaxValue(IEnumerable<Bar> bars)
        {
            if (bars == null || !bars.Any())
                return 0M;

            switch (PriceType)
            {
                case PriceType.Bid: return bars.Max(i => i.HighBid);
                case PriceType.Ask: return bars.Max(i => i.HighAsk);
                default: return bars.Max(i => i.MeanHigh);
            }
        }

        protected decimal GetMinValue(IEnumerable<Bar> bars)
        {
            if (bars == null || !bars.Any())
                return 0M;

            switch (PriceType)
            {
                case PriceType.Bid: return bars.Min(i => i.LowBid);
                case PriceType.Ask: return bars.Min(i => i.LowAsk);
                default: return bars.Min(i => i.MeanLow);
            }
        }

        protected StringParam GetPriceTypeParam(int id)
        {
            return new StringParam("Price Type", "Indicator price type", id)
            {
                Value = "Close",
                AllowedValues = new List<string>
                {
                    "Open", "High", "Low", "Close", "Median", "Typical", "Weighed"
                }
            };
        }

        protected StringParam GetSmoothingTypeParam(int id)
        {
            return new StringParam("Method", "Smoothing method", id)
            {
                Value = "SMA",
                AllowedValues = new List<string> { "SMA", "EMA", "SSMA", "LWMA" }
            };
        }

        protected PriceConstants ParsePriceConstants(StringParam param)
        {
            switch (param.Value)
            {
                case "Open": return PriceConstants.OPEN;
                case "High": return PriceConstants.HIGH;
                case "Low": return PriceConstants.LOW;
                case "Close": return PriceConstants.CLOSE;
                case "Median": return PriceConstants.MEDIAN;
                case "Typical": return PriceConstants.TYPICAL;
                case "Weighed": return PriceConstants.WEIGHTED;
                default: throw new System.ArgumentException("Failed to parse '" + param.Value + "' price type");
            }
        }

        protected MovingAverageType ParseMovingAverageConstants(StringParam param)
        {
            switch (param.Value)
            {
                case "SMA": return MovingAverageType.SMA;
                case "EMA": return MovingAverageType.EMA;
                case "SSMA": return MovingAverageType.SSMA;
                case "LWMA": return MovingAverageType.LWMA;
                default: throw new System.ArgumentException("Failed to parse '" + param.Value + "' MA type");
            }
        }
    }
}
