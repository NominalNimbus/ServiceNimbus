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
    public class StandardDeviation : IndicatorBase
    {
        private Selection _selection;
        private IDataProvider _dataProvider;
        private IndicatorBase MA;

        public int Period = 7;

        public MovingAverageType Smoothing = MovingAverageType.SMA;
        public PriceConstants Type = PriceConstants.OPEN;

        public StandardDeviation()
        {
            Name = "Standard Deviation";
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
                MA = new ExponentialMovingAverage
                {
                    Period = Period,
                    Type = Type
                };
            }
            else if (Smoothing == MovingAverageType.SMA)
            {
                MA = new SimpleMovingAverage
                {
                    Period = Period,
                    Type = Type
                };
            }
            else if (Smoothing == MovingAverageType.SSMA)
            {
                MA = new SmoothedMovingAverage 
                {
                    Period = Period,
                    Type = Type
                };
            }
            else if (Smoothing == MovingAverageType.LWMA)
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

            if (MA.Series[0].Length <= Period)
                return 0;

            List<Bar> history;
            if (bars != null)
            {
                history = new List<Bar>(bars);
            }
            else if (Series[0].Values.Count < Period)
            {
                history = _dataProvider.GetBars(_selection);
            }
            else
            {
                var sel = (Selection)_selection.Clone();
                sel.BarCount = Period + 1;
                history = _dataProvider.GetBars(sel);
            }

            if (history == null || history.Count == 0)
                return 0;
           
            var minCount = Series[0].Length;
            for (var i = Series[0].Length > 0 ? Series[0].Length - 1 : 0; i < MA.Series[0].Length; i++)
            {
                if (MA.Series[0].Values[i].Value == EMPTY_VALUE)
                {
                    Series[0].AppendOrUpdate(MA.Series[0].Values[i].Date, EMPTY_VALUE);
                    continue;
                }

                var bar = history.LastOrDefault(p => p.Date <= MA.Series[0].Values[i].Date);
                if(bar == null)
                    continue;

                var index = history.IndexOf(bar);
                if(index < Period - 1)
                    continue;

                var dAmount = 0.0;
                for (var j = index - Period + 1; j <= index; j++)
                {
                    var dAPrice = (double)GetPrice(history[j], Type);
                    dAmount += (dAPrice - MA.Series[0].Values[i].Value) * (dAPrice - MA.Series[0].Values[i].Value);
                }

                Series[0].AppendOrUpdate(MA.Series[0].Values[i].Date, Math.Sqrt(dAmount / Period));
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
                new IntParam("Period", "MA Period", 1)
                {
                    Value = 7,
                    MinValue = 1,
                    MaxValue = 100
                },
                GetSmoothingTypeParam(2),
                GetPriceTypeParam(3)
            };
        }

        protected override bool InternalSetParameters(List<ScriptingParameterBase> parameterBases)
        {
            Series[0].Color = ((SeriesParam)parameterBases[0]).Color;
            Series[0].Thickness = ((SeriesParam)parameterBases[0]).Thickness;

            Period = ((IntParam)parameterBases[1]).Value;
            Smoothing = ParseMovingAverageConstants((StringParam)parameterBases[2]);
            Type = ParsePriceConstants((StringParam)parameterBases[3]);

            DisplayName = String.Format("{0}_{1}_{2}_{3}", Name, Period, Smoothing, Type);
            return true;
        }
    }
}