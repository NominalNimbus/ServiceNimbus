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
    public class SimpleMovingAverage : IndicatorBase
    {
        private Selection _selection;
        private IDataProvider _dataProvider;
        public int Period = 10;
        public PriceConstants Type = PriceConstants.OPEN;

        public SimpleMovingAverage()
        {
            Name = "Simple Moving Average";
            Series.Add(new Series("Main"));
        }

        protected override bool InternalInit(Selection selection, IDataProvider dataProvider)
        {
            _selection = selection;
            _dataProvider = dataProvider;
            IsOverlay = true;
            Series.ForEach(s => s.Values.Clear());
            return InitialCalculation();
        }

        protected override int InternalCalculate(IEnumerable<Bar> bars = null)
        {
            List<Bar> history = null;
            if (bars == null)
            {
                var sel = (Selection)_selection.Clone();
                sel.BarCount = Period + 1;
                history = _dataProvider.GetBars(sel);
            }
            else
            {
                history = new List<Bar>(bars);
            }

            if (!history.Any()) return 0;

            if (history.Count > Period + 1)
                history = history.Skip(history.Count - Period - 1).ToList();

            if (history.Count != Period + 1)
                return 0;

            if (!Series[0].Values.Any())
                InitialCalculation(bars);

            if (Series[0].Values.Count == 0)
                return 0;

            if (history.Last().Date == Series[0].Values.Last().Date)
            {
                var value = Series[0].Values[Series[0].Values.Count - 2].Value 
                    + (double)(GetPrice(history.Last(), Type) - GetPrice(history.First(), Type)) / Period;
                Series[0].AppendOrUpdate(history.Last().Date, value);
            }
            else
            {
                var value = Series[0].Values.Last().Value 
                    + (double)(GetPrice(history.Last(), Type) - GetPrice(history.First(), Type)) / Period;
                Series[0].AppendOrUpdate(history.Last().Date, value);
            }
            
            return 1;
        }

        private bool InitialCalculation(IEnumerable<Bar> bars = null)
        {
            var history = bars == null ? _dataProvider.GetBars(_selection) : new List<Bar>(bars);
            if (history == null || history.Count < Period)
                return false;

            foreach (var bar in history)
            {
                var i = Series[0].Values.Count;
                if (i < Period - 1)
                {
                    Series[0].AppendOrUpdate(bar.Date, EMPTY_VALUE);
                }
                else if (i == Period - 1)
                {
                    Series[0].AppendOrUpdate(bar.Date, 
                        (double)history.Take(Period).Sum(p => GetPrice(p, Type)) / Period);
                }
                else
                {
                    var value = Series[0].Values.Last().Value
                        + (double)(GetPrice(bar, Type) - GetPrice(history[i - Period], Type)) / Period;
                    Series[0].AppendOrUpdate(history[i].Date, value);
                }
            }

            return Series[0].Values.Count > Period;
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

            Period = ((IntParam) parameterBases[1]).Value;
            Type = ParsePriceConstants((StringParam)parameterBases[2]);

            DisplayName = String.Format("{0}_{1}_{2}", Name, Period, Type);
            return true;
        }
    }
}
