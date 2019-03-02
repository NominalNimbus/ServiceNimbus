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
    public class Alligator : IndicatorBase
    {
        private Selection _selection;
        private IDataProvider _dataProvider;
        private IndicatorBase MA1;
        private IndicatorBase MA2;
        private IndicatorBase MA3;

        public int Period1 = 10;
        public int Period2 = 11;
        public int Period3 = 12;

        public int Shift1 = 5;
        public int Shift2 = 5;
        public int Shift3 = 3;

        public MovingAverageType Smoothing = MovingAverageType.SMA;
        public PriceConstants Type = PriceConstants.OPEN;

        public Alligator()
        {
            Name = "Alligator";
            IsOverlay = true;

            Series.Add(new Series("Jaw"));
            Series.Add(new Series("Teeth"));
            Series.Add(new Series("Lips"));
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
                MA3 = new ExponentialMovingAverage
                {
                    Period = Period3,
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
                MA3 = new SimpleMovingAverage
                {
                    Period = Period3,
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
                MA3 = new SmoothedMovingAverage 
                {
                    Period = Period3,
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
                MA3 = new LinearWeightedMovingAverage
                {
                    Period = Period3,
                    Type = Type
                };
            }

            MA1.Init(selection, dataProvider);
            MA2.Init(selection, dataProvider);
            MA3.Init(selection, dataProvider);

            InternalCalculate();

            return true;
        }

        protected override int InternalCalculate(IEnumerable<Bar> bars = null)
        {
            var minCount = Math.Min(Series[0].Values.Count, Series[1].Values.Count);
            minCount = Math.Min(Series[2].Values.Count, minCount);

            MA1.Calculate(bars);
            MA2.Calculate(bars);
            MA3.Calculate(bars);

            for (var i = Series[0].Length > 0 ?  Series[0].Length - 1 : 0; i < MA1.Series[0].Values.Count; i++)
                Series[0].AppendOrUpdate(MA1.Series[0].Values[i].Date,
                    i < Shift1 ? EMPTY_VALUE : MA1.Series[0].Values[i - Shift1].Value);
            

            for (var i = Series[1].Length > 0 ? Series[1].Length - 1 : 0; i < MA2.Series[0].Values.Count; i++)
                Series[1].AppendOrUpdate(MA2.Series[0].Values[i].Date,
                    i < Shift2 ? EMPTY_VALUE : MA2.Series[0].Values[i - Shift2].Value);
            

            for (var i = Series[2].Length > 0 ? Series[2].Length - 1 : 0; i < MA3.Series[0].Values.Count; i++)
                Series[2].AppendOrUpdate(MA3.Series[0].Values[i].Date,
                    i < Shift3 ? EMPTY_VALUE : MA3.Series[0].Values[i - Shift3].Value);
            

            var maxCount = Math.Max(Series[0].Values.Count, Series[1].Values.Count);
            maxCount = Math.Max(Series[2].Values.Count, maxCount);

            return maxCount - minCount > 0 ? maxCount - minCount : 1;
        }
        
        protected override List<ScriptingParameterBase> InternalGetParameters()
        {
            return new List<ScriptingParameterBase>
            {
                // Series
                new SeriesParam("Jaw", "Jaw series parameters", 0)
                {
                    Color = Colors.Blue,
                    Thickness = 2
                },
                new SeriesParam("Teeth", "Teeth series parameters", 1)
                {
                    Color = Colors.Red,
                    Thickness = 1
                },
                new SeriesParam("Lips", "Lips series parameters", 2)
                {
                    Color = Colors.Lime,
                    Thickness = 1
                },
                // Shifts and periods
                new IntParam("Jaw Period", "Jaw Period of the Alligator Indicator", 3)
                {
                    Value = 13,
                    MinValue = 1,
                    MaxValue = 100
                },
                new IntParam("Jaw Shift", "Jaw Shift of the Alligator Indicator", 4)
                {
                    Value = 8,
                    MinValue = 0,
                    MaxValue = 100
                },
                new IntParam("Teeth Period", "Teeth Period of the Alligator Indicator", 5)
                {
                    Value = 8,
                    MinValue = 1,
                    MaxValue = 100
                },
                new IntParam("Teeth Shift", "Teeth Shift of the Alligator Indicator", 6)
                {
                    Value = 5,
                    MinValue = 0,
                    MaxValue = 100
                },
                new IntParam("Lips Period", "Lips Period of the Alligator Indicator", 7)
                {
                    Value = 5,
                    MinValue = 1,
                    MaxValue = 100
                },
                new IntParam("Lips Shift", "Lips Shift of the Alligator Indicator", 8)
                {
                    Value = 3,
                    MinValue = 0,
                    MaxValue = 100
                },
                // Types
                GetSmoothingTypeParam(9),
                GetPriceTypeParam(10)
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

            Period1 = ((IntParam)parameterBases[3]).Value;
            Shift1 = ((IntParam)parameterBases[4]).Value;
            Period2 = ((IntParam)parameterBases[5]).Value;
            Shift2 = ((IntParam)parameterBases[6]).Value;
            Period3 = ((IntParam)parameterBases[7]).Value;
            Shift3 = ((IntParam)parameterBases[8]).Value;

            Smoothing = ParseMovingAverageConstants((StringParam)parameterBases[9]);
            Type = ParsePriceConstants((StringParam)parameterBases[10]);

            DisplayName = String.Format("{0}_{1}_{2}_{3}_{4}_{5}", Name, Period1, Period2, Period3, Smoothing, Type);
            return true;
        }
    }
}