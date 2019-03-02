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
    public class MovingAverageOfOscillator : IndicatorBase
    {
        private Selection _selection;
        private IDataProvider _dataProvider;
        private IndicatorBase MACD;

        public int SlowPeriod = 10;
        public int SignalPeriod = 11;
        public int FastPeriod = 12;

        public MovingAverageOfOscillator()
        {
            Name = "Moving Average of Oscillator";

            Series.Add(new Series("OsMA")
            {
                Style = DrawShapeStyle.DRAW_HISTOGRAM
            });
            Series.Add(new Series("MACD"));
            Series.Add(new Series("Signal"));
        }

        protected override bool InternalInit(Selection selection, IDataProvider dataProvider)
        {
            _selection = selection;
            _dataProvider = dataProvider;
            Series.ForEach(s => s.Values.Clear());

            MACD = new MACD
            {
                FastPeriod = FastPeriod,
                SlowPeriod = SlowPeriod,
                SignalPeriod = SignalPeriod,
                Type = PriceConstants.CLOSE
            };

            MACD.Init(selection, dataProvider);

            InternalCalculate();

            return true;
        }

        protected override int InternalCalculate(IEnumerable<Bar> bars = null)
        {
            var minCount = Math.Min(Series[0].Values.Count, Series[1].Values.Count);
            minCount = Math.Min(minCount, Series[2].Length);

            MACD.Calculate(bars);

            if(Series[1].Length == 0)
                Series[1].Values.AddRange(MACD.Series[0].Values);
            else if(MACD.Series[0].Length > 0)
                Series[1].AppendOrUpdate(MACD.Series[0].Values.Last().Date, MACD.Series[0].Values.Last().Value);


            if (Series[2].Length == 0)
            {
                Series[2].Values.AddRange(MACD.Series[1].Values);

                for (var i = 0; i < Math.Min(Series[2].Length, Series[1].Length); i++)
                {
                    if (Series[1].Values[i].Value != EMPTY_VALUE && Series[2].Values[i].Value != EMPTY_VALUE)
                         Series[0].AppendOrUpdate(Series[1].Values[i].Date, Series[1].Values[i].Value - Series[2].Values[i].Value);
                    else
                        Series[0].AppendOrUpdate(Series[1].Values[i].Date, EMPTY_VALUE);
                }
            }
            else if (MACD.Series[1].Length > 0)
            {
                Series[2].AppendOrUpdate(MACD.Series[1].Values.Last().Date, MACD.Series[1].Values.Last().Value);
                Series[0].AppendOrUpdate(Series[1].Values.Last().Date, Series[1].Values.Last().Value - Series[2].Values.Last().Value);
            }

            var maxCount = Math.Max(Series[0].Values.Count, Series[1].Values.Count);
            maxCount = Math.Max(maxCount, Series[2].Length);

            return maxCount - minCount > 0 ? maxCount - minCount : 1;
        }

        protected override List<ScriptingParameterBase> InternalGetParameters()
        {
            return new List<ScriptingParameterBase>
            {
                // Series
                new SeriesParam("OsMA", "Main series parameters", 0)
                {
                    Color = Colors.LawnGreen,
                    Thickness = 3
                },
                new SeriesParam("MACD", "MACD series parameters", 1)
                {
                    Color = Colors.Red,
                    Thickness = 1
                },
                new SeriesParam("Signal", "Signal series parameters", 2)
                {
                    Color = Colors.Blue,
                    Thickness = 1
                },
                // Shifts and periods
                new IntParam("Fast Period", "Fast Period of the Indicator", 3)
                {
                    Value = 13,
                    MinValue = 1,
                    MaxValue = 100
                },
                new IntParam("Slow Period", "Slow Period of the Indicator", 4)
                {
                    Value = 8,
                    MinValue = 0,
                    MaxValue = 100
                },
                new IntParam("Signal Period", "Signal Period of the Indicator", 5)
                {
                    Value = 8,
                    MinValue = 1,
                    MaxValue = 100
                }
            };
        }

        protected override bool InternalSetParameters(List<ScriptingParameterBase> parameterBases)
        {
            Series[0].Color = ((SeriesParam)parameterBases[0]).Color;
            Series[0].Thickness = ((SeriesParam)parameterBases[0]).Thickness;

            Series[1].Color = ((SeriesParam)parameterBases[1]).Color;
            Series[1].Thickness = ((SeriesParam)parameterBases[1]).Thickness;

            Series[2].Color = ((SeriesParam)parameterBases[2]).Color;
            Series[2].Thickness = ((SeriesParam)parameterBases[2]).Thickness;

            FastPeriod = ((IntParam)parameterBases[3]).Value;
            SlowPeriod = ((IntParam)parameterBases[4]).Value;
            SignalPeriod = ((IntParam)parameterBases[5]).Value;

            DisplayName = String.Format("{0}_{1}_{2}_{3}", Name, FastPeriod, SlowPeriod, SignalPeriod);
            return true;
        }
    }
}