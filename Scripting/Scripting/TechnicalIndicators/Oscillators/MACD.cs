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
using System.Windows.Media;
using CommonObjects;

namespace Scripting.TechnicalIndicators
{
    public class MACD : IndicatorBase
    {
        private Selection _selection;
        private IDataProvider _dataProvider;
        private IndicatorBase FMA;
        private IndicatorBase SMA;

        public int SlowPeriod = 10;
        public int SignalPeriod = 11;
        public int FastPeriod = 12;

        public PriceConstants Type = PriceConstants.OPEN;

        public MACD()
        {
            Name = "MACD";

            Series.Add(new Series("MACD")
            {
                Style = DrawShapeStyle.DRAW_HISTOGRAM
            });
            Series.Add(new Series("Signal"));
        }

        protected override bool InternalInit(Selection selection, IDataProvider dataProvider)
        {
            _selection = selection;
            _dataProvider = dataProvider;
            Series.ForEach(s => s.Values.Clear());

            FMA = new ExponentialMovingAverage
            {
                Period = FastPeriod,
                Type = Type
            };
            SMA = new ExponentialMovingAverage
            {
                Period = SlowPeriod,
                Type = Type
            };
                
            FMA.Init(selection, dataProvider);
            SMA.Init(selection, dataProvider);

            InternalCalculate();

            return true;
        }

        protected override int InternalCalculate(IEnumerable<Bar> bars = null)
        {
            var minCount = Math.Min(Series[0].Values.Count, Series[1].Values.Count);

            FMA.Calculate(bars);
            SMA.Calculate(bars);

            for (var i = Series[0].Length > 0 ? Series[0].Length - 1 : 0; i < FMA.Series[0].Length; i++)
            {
                var fast = FMA.Series[0].Values[i].Value;
                var slow = SMA.Series[0].Values[i].Value;

                if (fast != EMPTY_VALUE && slow != EMPTY_VALUE)
                    Series[0].AppendOrUpdate(FMA.Series[0].Values[i].Date, fast - slow);
                else
                    Series[0].AppendOrUpdate(FMA.Series[0].Values[i].Date, EMPTY_VALUE);
            }

            for (var i = Series[1].Length > 0 ? Series[1].Length - 1 : 0; i < Series[0].Length; i++)
            {
                if (i < SignalPeriod)
                {
                    Series[1].AppendOrUpdate(Series[0].Values[i].Date, EMPTY_VALUE);
                    continue;
                }

                var sum = Series[0].Values.GetRange(i - SignalPeriod, SignalPeriod).Sum(p => p.Value == EMPTY_VALUE ? 0 : p.Value);
                Series[1].AppendOrUpdate(Series[0].Values[i].Date, sum/SignalPeriod);
            }

            var maxCount = Math.Max(Series[0].Values.Count, Series[1].Values.Count);
            return maxCount - minCount > 0 ? maxCount - minCount : 1;
        }

        protected override List<ScriptingParameterBase> InternalGetParameters()
        {
            return new List<ScriptingParameterBase>
            {
                // Series
                new SeriesParam("Main", "Main series parameters", 0)
                {
                    Color = Colors.LawnGreen,
                    Thickness = 3
                },
                new SeriesParam("Signal", "Signal series parameters", 1)
                {
                    Color = Colors.Red,
                    Thickness = 2
                },
                // Shifts and periods
                new IntParam("Fast Period", "Fast Period of the Indicator", 2)
                {
                    Value = 13,
                    MinValue = 1,
                    MaxValue = 100
                },
                new IntParam("Slow Period", "Slow Period of the Indicator", 3)
                {
                    Value = 8,
                    MinValue = 0,
                    MaxValue = 100
                },
                new IntParam("Signal Period", "Signal Period of the Indicator", 4)
                {
                    Value = 8,
                    MinValue = 1,
                    MaxValue = 100
                },
                // Types
                GetPriceTypeParam(5)
            };
        }

        protected override bool InternalSetParameters(List<ScriptingParameterBase> parameterBases)
        {
            Series[0].Color = ((SeriesParam)parameterBases[0]).Color;
            Series[0].Thickness = ((SeriesParam)parameterBases[0]).Thickness;

            Series[1].Color = ((SeriesParam)parameterBases[1]).Color;
            Series[1].Thickness = ((SeriesParam)parameterBases[1]).Thickness;
            
            FastPeriod = ((IntParam)parameterBases[2]).Value;
            SlowPeriod = ((IntParam)parameterBases[3]).Value;
            SignalPeriod = ((IntParam)parameterBases[4]).Value;

            Type = ParsePriceConstants((StringParam)parameterBases[5]);

            DisplayName = String.Format("{0}_{1}_{2}_{3}_{4}", Name, FastPeriod, SlowPeriod, SignalPeriod, Type);
            return true;
        }
    }
}