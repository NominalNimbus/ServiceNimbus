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
    public class Gator : IndicatorBase
    {
        private Selection _selection;
        private IDataProvider _dataProvider;
        private IndicatorBase Alligator;

        public int Period1 = 10;
        public int Period2 = 11;
        public int Period3 = 12;

        public int Shift1 = 5;
        public int Shift2 = 5;
        public int Shift3 = 3;

        public MovingAverageType Smoothing = MovingAverageType.SMA;
        public PriceConstants Type = PriceConstants.OPEN;

        public Gator()
        {
            Name = "Gator";
            Series.Add(new Series("Up")
            {
                Style = DrawShapeStyle.DRAW_HISTOGRAM
            });
            Series.Add(new Series("Down")
            {
                Style = DrawShapeStyle.DRAW_HISTOGRAM
            });
        }

        protected override bool InternalInit(Selection selection, IDataProvider dataProvider)
        {
            _selection = selection;
            _dataProvider = dataProvider;
            Series.ForEach(s => s.Values.Clear());

            Alligator = new Alligator
            {
                Period1 = Period1,
                Period2 = Period2,
                Period3 = Period3,
                Shift1 = Shift1,
                Shift2 = Shift2,
                Shift3 = Shift3,
                Smoothing = Smoothing,
                Type = Type
            };
            Alligator.Init(selection, dataProvider);

            InternalCalculate();

            return true;
        }

        protected override int InternalCalculate(IEnumerable<Bar> bars = null)
        {
            var minCount = Math.Min(Series[0].Values.Count, Series[1].Values.Count);

            Alligator.Calculate(bars);

            double jaw, teeth, lips;

            for (var i = Series[0].Length > 0 ? Series[0].Length - 1 : 0; i < Alligator.Series[0].Length; i++)
            {
                jaw = Alligator.Series[0].Values[i].Value;
                teeth = Alligator.Series[1].Values[i].Value;
                lips = Alligator.Series[2].Values[i].Value;

                if (teeth != EMPTY_VALUE && jaw != EMPTY_VALUE)
                    Series[0].AppendOrUpdate(Alligator.Series[0].Values[i].Date, Math.Abs(teeth - jaw));
                else
                    Series[0].AppendOrUpdate(Alligator.Series[0].Values[i].Date, EMPTY_VALUE);

                if (teeth != EMPTY_VALUE && lips != EMPTY_VALUE)
                    Series[1].AppendOrUpdate(Alligator.Series[0].Values[i].Date, Math.Abs(teeth - lips) * -1);
                else
                    Series[1].AppendOrUpdate(Alligator.Series[0].Values[i].Date, EMPTY_VALUE);
            }
            
            var maxCount = Math.Max(Series[0].Values.Count, Series[1].Values.Count);
            return maxCount - minCount > 0 ? maxCount - minCount : 1;
        }
        
        protected override List<ScriptingParameterBase> InternalGetParameters()
        {
            return new List<ScriptingParameterBase>
            {
                // Series
                new SeriesParam("Up", "Up series parameters", 0)
                {
                    Color = Colors.Lime,
                    Thickness = 2
                },
                new SeriesParam("Down", "Down series parameters", 1)
                {
                    Color = Colors.Red,
                    Thickness = 2
                },
                // Shifts and periods
                new IntParam("Jaw Period", "Jaw Period of the Alligator Indicator", 2)
                {
                    Value = 13,
                    MinValue = 1,
                    MaxValue = 100
                },
                new IntParam("Jaw Shift", "Jaw Shift of the Alligator Indicator", 3)
                {
                    Value = 8,
                    MinValue = 0,
                    MaxValue = 100
                },
                new IntParam("Teeth Period", "Teeth Period of the Alligator Indicator", 4)
                {
                    Value = 8,
                    MinValue = 1,
                    MaxValue = 100
                },
                new IntParam("Teeth Shift", "Teeth Shift of the Alligator Indicator", 5)
                {
                    Value = 5,
                    MinValue = 0,
                    MaxValue = 100
                },
                new IntParam("Lips Period", "Lips Period of the Alligator Indicator", 6)
                {
                    Value = 5,
                    MinValue = 1,
                    MaxValue = 100
                },
                new IntParam("Lips Shift", "Lips Shift of the Alligator Indicator", 7)
                {
                    Value = 3,
                    MinValue = 0,
                    MaxValue = 100
                },
                // Types
                GetSmoothingTypeParam(8),
                GetPriceTypeParam(9)
            };
        }

        protected override bool InternalSetParameters(List<ScriptingParameterBase> parameterBases)
        {
            Series[0].Color = ((SeriesParam)parameterBases[0]).Color;
            Series[0].Thickness = ((SeriesParam)parameterBases[0]).Thickness;

            Series[1].Color = ((SeriesParam)parameterBases[1]).Color;
            Series[1].Thickness = ((SeriesParam)parameterBases[1]).Thickness;
            
            Period1 = ((IntParam)parameterBases[2]).Value;
            Shift1 = ((IntParam)parameterBases[3]).Value;
            Period2 = ((IntParam)parameterBases[4]).Value;
            Shift2 = ((IntParam)parameterBases[5]).Value;
            Period3 = ((IntParam)parameterBases[6]).Value;
            Shift3 = ((IntParam)parameterBases[7]).Value;

            Smoothing = ParseMovingAverageConstants((StringParam)parameterBases[8]);
            Type = ParsePriceConstants((StringParam)parameterBases[9]);

            DisplayName = String.Format("{0}_{1}_{2}_{3}_{4}_{5}", Name, Period1, Period2, Period3, Smoothing, Type);
            return true;
        }
    }
}