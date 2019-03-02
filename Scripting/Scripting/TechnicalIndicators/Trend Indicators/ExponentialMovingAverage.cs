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
    public class ExponentialMovingAverage : IndicatorBase
    {
        private Selection _selection;
        private IDataProvider _dataProvider;
        public int Period = 10;
        public PriceConstants Type = PriceConstants.OPEN;

        public ExponentialMovingAverage()
        {
            Name = "Exponential Moving Average";
            Series.Add(new Series("Main"));
        }

        protected override bool InternalInit(Selection selection, IDataProvider dataProvider)
        {
            _selection = selection;
            _dataProvider = dataProvider;
            IsOverlay = true;
            Series.ForEach(s => s.Values.Clear());
            InternalCalculate();
            return true;
        }

        protected override int InternalCalculate(IEnumerable<Bar> bars = null)
        {
            List<Bar> history = null;
            if (bars != null)
            {
                history = new List<Bar>(bars);
            }
            else if (Series[0].Values.Count == 0)
            {
                history = _dataProvider.GetBars(_selection);
            }
            else
            {
                var sel = _selection.Clone() as Selection;
                sel.From = Series[0].Values.Last().Date;
                sel.To = DateTime.MaxValue;
                history = _dataProvider.GetBars(sel);
            }

            if (history == null || history.Count == 0)
                return 0;

            var exp = 2 / (double)(Period + 1);

            foreach (var bar in history)
            {
                var i = Series[0].Values.Count;
                if (i == 0)
                    Series[0].AppendOrUpdate(bar.Date, (double)GetPrice(bar, Type));
                else
                {
                    if (bar.Date == Series[0].Values.Last().Date)
                        Series[0].AppendOrUpdate(bar.Date, (double)GetPrice(bar, Type) * exp + Series[0].Values[Series[0].Values.Count - 2].Value * (1 - exp));
                    else
                        Series[0].AppendOrUpdate(bar.Date, (double)GetPrice(bar, Type) * exp + Series[0].Values.Last().Value * (1 - exp));
                }
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