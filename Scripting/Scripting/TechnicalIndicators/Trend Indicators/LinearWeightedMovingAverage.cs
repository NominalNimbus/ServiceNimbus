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
    public class LinearWeightedMovingAverage : IndicatorBase
    {
        private Selection _selection;
        private IDataProvider _dataProvider;
        public int Period = 10;
        public PriceConstants Type = PriceConstants.OPEN;

        public LinearWeightedMovingAverage()
        {
            Name = "Linear Weighted Moving Average";
            Series.Add(new Series("Main"));
        }

        protected override bool InternalInit(Selection selection, IDataProvider dataProvider)
        {
            _selection = selection;
            _dataProvider = dataProvider;
            IsOverlay = true;
            Series.ForEach(s => s.Values.Clear());

            Calculate(_selection.BarCount);

            return true;
        }

        protected override int InternalCalculate(IEnumerable<Bar> bars = null)
        {
            return Calculate(Period, bars);
        }

        private int Calculate(int count, IEnumerable<Bar> bars = null)
        {
            List<Bar> history = null;
            if (bars != null)
            {
                history = new List<Bar>(bars);
            }
            else
            {
                var sel = (Selection)_selection.Clone();
                sel.BarCount = count;
                history = _dataProvider.GetBars(sel);
            }
            if (history == null || history.Count == 0)
                return 0;

            decimal price, sum = 0, lsum = 0, weight = 0;
            for (var i = 0; i < Period; i++)
            {
                price = GetPrice(history[i], Type);
                sum += price * (i + 1);
                lsum += price;
                weight += i + 1;

                if(Series[0].Values.Count < Period)
                    Series[0].AppendOrUpdate(history[i].Date, EMPTY_VALUE);
            }
            
            Series[0].AppendOrUpdate(history[Period - 1].Date, (double)(sum / weight));
            
            for (var j = Period; j < history.Count; j++)
            {
                price = GetPrice(history[j], Type);
                sum = sum - lsum + price * Period;
                lsum -= GetPrice(history[j - Period], Type);
                lsum += price;
                Series[0].AppendOrUpdate(history[j].Date, (double)(sum / weight));
            }

            return history.Count;
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
                new IntParam("Period", "Indicator period", 1)
                {
                    Value = 10,
                    MinValue = 1,
                    MaxValue = 100
                },
                GetPriceTypeParam(2)
            };
        }

        protected override bool InternalSetParameters(List<ScriptingParameterBase> parameterBases)
        {
            Series[0].Color = ((SeriesParam)parameterBases[0]).Color;
            Series[0].Thickness = ((SeriesParam)parameterBases[0]).Thickness;

            Period = ((IntParam)parameterBases[1]).Value;
            Type = ParsePriceConstants((StringParam)parameterBases[2]);

            DisplayName = String.Format("{0}_{1}_{2}", Name, Period, Type);
            return true;
        }
    }
}