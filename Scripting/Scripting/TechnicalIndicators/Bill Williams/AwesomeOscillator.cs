/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Windows.Media;
using CommonObjects;

namespace Scripting.TechnicalIndicators
{
    public class AwesomeOscillator : IndicatorBase
    {
        private Selection _selection;
        private IDataProvider _dataProvider;
        private IndicatorBase MA1;
        private IndicatorBase MA2;

        public int Period1 = 7;
        public int Period2 = 11;

        public MovingAverageType Smoothing = MovingAverageType.SMA;
        public PriceConstants Type = PriceConstants.OPEN;

        public AwesomeOscillator()
        {
            Name = "Awesome Oscillator";
            IsOverlay = false;
            Series.Add(new Series("Main"));
        }

        protected override bool InternalInit(Selection selection, IDataProvider dataProvider)
        {
            _selection = selection;
            _dataProvider = dataProvider;
            Series.ForEach(s => s.Values.Clear());

            if (Smoothing == MovingAverageType.EMA)
            {
                MA1 = new ExponentialMovingAverage
                {
                    Period = Period1,
                    Type = Type
                };
                MA2 = new ExponentialMovingAverage
                {
                    Period = Period2,
                    Type = Type
                };
            }
            else if (Smoothing == MovingAverageType.SMA)
            {
                MA1 = new SimpleMovingAverage
                {
                    Period = Period1,
                    Type = Type
                };
                MA2 = new SimpleMovingAverage
                {
                    Period = Period2,
                    Type = Type
                };
            }
            else if (Smoothing == MovingAverageType.SSMA)
            {
                MA1 = new SmoothedMovingAverage 
                {
                    Period = Period1,
                    Type = Type
                };
                MA2 = new SmoothedMovingAverage 
                {
                    Period = Period2,
                    Type = Type
                };
            }
            else if (Smoothing == MovingAverageType.LWMA)
            {
                MA1 = new LinearWeightedMovingAverage
                {
                    Period = Period1,
                    Type = Type
                };
                MA2 = new LinearWeightedMovingAverage
                {
                    Period = Period2,
                    Type = Type
                };
            }

            MA1.Init(selection, dataProvider);
            MA2.Init(selection, dataProvider);

            InternalCalculate();

            return true;
        }

        protected override int InternalCalculate(IEnumerable<Bar> bars = null)
        {
            MA1.Calculate(bars);
            MA2.Calculate(bars);

            if (MA1.Series[0].Length <= Period1)
                return 0;

            if (MA2.Series[0].Length <= Period2)
                return 0;

            if (MA1.Series[0].Length != MA2.Series[0].Length)
                return 0;

            var minCount = Series[0].Length;

            for (var i = Series[0].Length > 0 ? Series[0].Length - 1 : 0; i < MA1.Series[0].Length; i++)
            {
                var val = EMPTY_VALUE;

                if (!MA1.Series[0].Values[i].Value.Equals(EMPTY_VALUE) && !MA2.Series[0].Values[i].Value.Equals(EMPTY_VALUE))
                    val = MA1.Series[0].Values[i].Value - MA2.Series[0].Values[i].Value;

                Series[0].AppendOrUpdate(MA1.Series[0].Values[i].Date, val);
            }

            return Series[0].Length - minCount > 0 ? Series[0].Length - minCount : 1;
        }

        protected override List<ScriptingParameterBase> InternalGetParameters()
        {
            return new List<ScriptingParameterBase>
            {
                new SeriesParam("MainSeries", "Series parameters", 0)
                {
                    Color = Colors.Red,
                    Thickness = 2
                },
                new IntParam("Period 1", "Fast MA Period", 1)
                {
                    Value = 7,
                    MinValue = 1,
                    MaxValue = 100
                },
                new IntParam("Period 2", "Slow MA Period", 2)
                {
                    Value = 13,
                    MinValue = 1,
                    MaxValue = 100
                },
                GetSmoothingTypeParam(3),
                GetPriceTypeParam(4)
            };
        }

        protected override bool InternalSetParameters(List<ScriptingParameterBase> parameterBases)
        {
            Series[0].Color = ((SeriesParam)parameterBases[0]).Color;
            Series[0].Thickness = ((SeriesParam)parameterBases[0]).Thickness;

            Period1 = ((IntParam)parameterBases[1]).Value;
            Period2 = ((IntParam)parameterBases[2]).Value;
            Smoothing = ParseMovingAverageConstants((StringParam)parameterBases[3]);
            Type = ParsePriceConstants((StringParam)parameterBases[4]);

            DisplayName = String.Format("{0}_{1}_{2}_{3}_{4}", Name, Period1, Period2, Smoothing, Type);
            return true;
        }
    }
}