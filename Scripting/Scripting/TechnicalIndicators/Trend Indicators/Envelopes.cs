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
    public class Envelopes : IndicatorBase
    {
        private Selection _selection;
        private IDataProvider _dataProvider;
        private IndicatorBase MA;

        public int Period = 10;
        public double Deviation = 0.1;
        public PriceConstants Type = PriceConstants.OPEN;
        public MovingAverageType MaType = MovingAverageType.EMA;

        public Envelopes()
        {
            Name = "Envelopes";
            IsOverlay = true;
            Series.Add(new Series("Upper"));
            Series.Add(new Series("Lower"));
        }

        protected override bool InternalInit(Selection selection, IDataProvider dataProvider)
        {
            _selection = selection;
            _dataProvider = dataProvider;
            Series.ForEach(s => s.Values.Clear());

            if (MaType == MovingAverageType.EMA)
            {
                MA = new ExponentialMovingAverage
                {
                    Period = Period,
                    Type = Type
                };
            }
            else if (MaType == MovingAverageType.SMA)
            {
                MA = new SimpleMovingAverage
                {
                    Period = Period,
                    Type = Type
                };
            }
            else if (MaType == MovingAverageType.SSMA)
            {
                MA = new SmoothedMovingAverage 
                {
                    Period = Period,
                    Type = Type
                };

            }
            else if (MaType == MovingAverageType.LWMA)
            {
                MA = new LinearWeightedMovingAverage
                {
                    Period = Period,
                    Type = Type
                };
            }

            MA.Init(selection, dataProvider);
            InternalCalculate();
            return true;
        }

        protected override int InternalCalculate(IEnumerable<Bar> bars = null)
        {
            MA.Calculate(bars);
            double devPlus = (1.0 + Deviation / 100.0);
            double devMinus = (1.0 - Deviation / 100.0);

            var count = 0;

            for (var i = Series[0].Length > 0 ? MA.Series[0].Length - 1 : 0; i < MA.Series[0].Values.Count; i++)
            {
                var ma = MA.Series[0].Values[i];
                if (ma.Value != EMPTY_VALUE)
                {
                    Series[0].AppendOrUpdate(ma.Date, ma.Value*devPlus);
                    Series[1].AppendOrUpdate(ma.Date, ma.Value*devMinus);
                }
                else
                {
                    Series[0].AppendOrUpdate(ma.Date, EMPTY_VALUE);
                    Series[1].AppendOrUpdate(ma.Date, EMPTY_VALUE);
                }
                count++;
            }

            return count;
        }

        protected override List<ScriptingParameterBase> InternalGetParameters()
        {
            return new List<ScriptingParameterBase>
            {
                // Series
                new SeriesParam("Upper", "Upper series parameters", 0)
                {
                    Color = Colors.Lime,
                    Thickness = 2
                },
                 // Series
                new SeriesParam("Lower", "Lower series parameters", 1)
                {
                    Color = Colors.Red,
                    Thickness = 2
                },
                // Periods
                new IntParam("Period", "Period of the Indicator", 2)
                {
                    Value = 13,
                    MinValue = 1,
                    MaxValue = 100
                },
                 new DoubleParam("Deviation", "Deviation of the Indicator", 3)
                {
                    Value = 0.1,
                    MinValue = 0.001,
                    MaxValue = 10
                },
                // Types
                GetSmoothingTypeParam(4),
                GetPriceTypeParam(5)
            };
        }

        protected override bool InternalSetParameters(List<ScriptingParameterBase> parameterBases)
        {
            Series[0].Color = ((SeriesParam)parameterBases[0]).Color;
            Series[0].Thickness = ((SeriesParam)parameterBases[0]).Thickness;
            Series[1].Color = ((SeriesParam)parameterBases[1]).Color;
            Series[1].Thickness = ((SeriesParam)parameterBases[1]).Thickness;

            Period = ((IntParam)parameterBases[2]).Value;
            Deviation = ((DoubleParam)parameterBases[3]).Value;

            MaType = ParseMovingAverageConstants((StringParam)parameterBases[4]);
            Type = ParsePriceConstants((StringParam)parameterBases[5]);

            DisplayName = String.Format("{0}_{1}_{2}_{3}", Name, Period, MaType, Type);
            return true;
        }
    }
}