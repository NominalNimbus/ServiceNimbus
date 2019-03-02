/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CommonObjects
{
    [DataContract]
    [Serializable]
    public class Selection : ICloneable
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public int TimeFactor { get; set; }

        [DataMember]
        public Timeframe Timeframe { get; set; }

        [DataMember]
        public string DataFeed { get; set; }

        [DataMember]
        public string Symbol { get; set; }

        [DataMember]
        public int BarCount { get; set; }

        [DataMember]
        public DateTime From { get; set; }

        [DataMember]
        public DateTime To { get; set; }

        [DataMember]
        public byte Level { get; set; }

        [DataMember]
        public PriceType BidAsk { get; set; }

        [DataMember]
        public bool? IncludeWeekendData { get; set; }

        [DataMember]
        public int MarketDataSlot { get; set; }  //for signals and backtest

        [DataMember]
        public int Leverage { get; set; }  //for backtest

        [DataMember]
        public decimal Slippage { get; set; }  //for backtest

        public Selection()
        {
            Id = Guid.NewGuid().ToString("N");
            TimeFactor = 1;
            Timeframe = Timeframe.Minute;
            DataFeed = String.Empty;
            Symbol = String.Empty;
        }

        public string GetKey()
        {
            var keyBuilder = new StringBuilder();
            keyBuilder.Append(Symbol.ToLower());
            keyBuilder.Append(Timeframe);
            keyBuilder.Append(TimeFactor);
            keyBuilder.Append(From);
            keyBuilder.Append(To);
            keyBuilder.Append(BarCount);
            keyBuilder.Append(IncludeWeekendData);
            keyBuilder.Append(Level);

            return keyBuilder.ToString();
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public override string ToString()
        {
            return String.Format("{0} {1}{2}", Symbol, GetTimeframeAbbrev(Timeframe, TimeFactor),
                Level > 0 ? (" L" + Level) : String.Empty);
        }

        public static string GetTimeframeAbbrev(Timeframe tf, int interval)
        {
            switch (tf)
            {
                case Timeframe.Tick: return "Tick";
                case Timeframe.Minute: return "M" + interval;
                case Timeframe.Hour: return "H" + interval;
                case Timeframe.Day: return "D" + interval;
                case Timeframe.Month: return "MN";
                default: return "Unknown";
            }
        }

        public bool IsEnoughData(List<Bar> bars)
        {
            if (From == DateTime.MinValue) //use bar count (and end date if specified)
            {
                return bars.Count >= BarCount;
            }
            else
            {
                return bars.Count > 0 && bars.First().Date <= From;
            }
        }

        public List<Bar> TrimBars(List<Bar> bars)
        {
            if (From == DateTime.MinValue) //use bar count (and end date if specified)
            {
                if (bars.Count > BarCount)
                    return bars.Skip(bars.Count - BarCount).ToList();
                else
                    return bars;
            }
            else
            {
                return bars.Where(b => b.Date >= From && b.Date <= To).ToList();
            }
        }

    }
}
