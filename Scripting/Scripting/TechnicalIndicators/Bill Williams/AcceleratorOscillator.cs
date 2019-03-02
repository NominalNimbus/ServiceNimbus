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
    public class AcceleratorOscillator : IndicatorBase
    {
        private Selection _selection;
        private IDataProvider _dataProvider;
        private IndicatorBase MA1;
        private IndicatorBase MA2;

        public int Period1 = 10;
        public int Period2 = 11;
        public int Period3 = 12;

        public MovingAverageType Smoothing = MovingAverageType.SMA;
        public PriceConstants Type = PriceConstants.OPEN;

        public AcceleratorOscillator()
        {
            Name = "Accelerator Oscillator";
            IsOverlay = false;
            Series.Add(new Series("Main")
            {
                Style = DrawShapeStyle.DRAW_HISTOGRAM
            });
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

            InitialCalculate();

            return true;
        }

        protected override int InternalCalculate(IEnumerable<Bar> bars = null)
        {
            List<Bar> history = null;
            if (bars != null)
            {
                history = new List<Bar>(bars);
            }
            else if (Series[0].Values.Count > 0)
            {
                var sel = (Selection)_selection.Clone();
                sel.BarCount = 1;
                history = _dataProvider.GetBars(sel);
            }
            else
            {
                history = _dataProvider.GetBars(_selection);
            }

            if (history == null || history.Count == 0)
                return 0;

            MA1.Calculate(bars);
            MA2.Calculate(bars);

            double price1, price2, price3 = 0;
            double sum = 0;

            if (MA1.Series[0].Values.Count <= Period3)
                return 0;

            if (MA2.Series[0].Values.Count <= Period3)
                return 0;

            for (var i = 0; i < Period3; i++)
            {
                var c1 = MA1.Series[0].Values.Count - Period3 + i;
                var c2 = MA2.Series[0].Values.Count - Period3 + i;
                price1 = MA1.Series[0].Values[c1].Value;
                price2 = MA2.Series[0].Values[c2].Value;

                if (price1 == EMPTY_VALUE)
                    price1 = 0;
                if (price2 == EMPTY_VALUE)
                    price2 = 0;

                price3 = (price1 - price2);
                sum += price3;
            }

            Series[0].AppendOrUpdate(history.Last().Date, (price3 - sum / Period3) * 100);
            
            return 1;
        }

        private void InitialCalculate(IEnumerable<Bar> bars = null)
        {
            if (_selection.BarCount == 0 || _selection.BarCount <= Period3)
                return;

            MA1.Calculate(bars);
            MA2.Calculate(bars);

            double price1, price2, price3, price4;
            double sum = 0;

            for (var i = Math.Max(Period1, Period2); i < Period3; i++)
            {
                price1 = MA1.Series[0].Values[i].Value;
                price2 = MA2.Series[0].Values[i].Value;

                if (price1 == EMPTY_VALUE)
                    price1 = 0;
                if (price2 == EMPTY_VALUE)
                    price2 = 0;

                price3 = (price1 - price2);
                sum += price3;
            }

            for (var j = Period3; j < Math.Min(MA1.Series[0].Values.Count, MA2.Series[0].Values.Count); j++)
            {
                price1 = MA1.Series[0].Values[j].Value;
                price2 = MA2.Series[0].Values[j].Value;

                if (price1 == EMPTY_VALUE || price2 == EMPTY_VALUE)
                    continue;

                price3 = (price1 - price2);
                sum += price3;
                price4 = sum / Period3;

                Series[0].AppendOrUpdate(MA1.Series[0].Values[j].Date, (price3 - price4) * 100);

                price1 = MA1.Series[0].Values[j - Period3 + 1].Value;
                price2 = MA2.Series[0].Values[j - Period3 + 1].Value;

                if (price1 == EMPTY_VALUE || price2 == EMPTY_VALUE)
                   continue;

                price3 = (price1 - price2);
                sum -= price3;
            }
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
                    Value = 6,
                    MinValue = 1,
                    MaxValue = 100
                },
                 new IntParam("Period 2", "Slow MA Period", 2)
                {
                    Value = 14,
                    MinValue = 1,
                    MaxValue = 100
                },
                 new IntParam("Period 3", "Forming MA Period", 3)
                {
                    Value = 22,
                    MinValue = 1,
                    MaxValue = 100
                },
                GetSmoothingTypeParam(4),
                GetPriceTypeParam(5)
            };
        }

        protected override bool InternalSetParameters(List<ScriptingParameterBase> parameterBases)
        {
            Series[0].Color = ((SeriesParam) parameterBases[0]).Color;
            Series[0].Thickness = ((SeriesParam) parameterBases[0]).Thickness;

            Period1 = ((IntParam)parameterBases[1]).Value;
            Period2 = ((IntParam)parameterBases[2]).Value;
            Period3 = ((IntParam)parameterBases[3]).Value;
            Smoothing = ParseMovingAverageConstants((StringParam)parameterBases[4]);
            Type = ParsePriceConstants((StringParam)parameterBases[5]);

            DisplayName = String.Format("{0}_{1}_{2}_{3}_{4}_{5}", Name, Period1, Period2, Period3, Smoothing, Type);

            return true;
        }
    }
}