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
    public class WPercentRange : IndicatorBase
    {
        private Selection _selection;
        private IDataProvider _dataProvider;
        public int Period = 10;

        public WPercentRange()
        {
            Name = "W percent range";
            Series.Add(new Series("Main"));
        }

        protected override bool InternalInit(Selection selection, IDataProvider dataProvider)
        {
            _selection = selection;
            _dataProvider = dataProvider;
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
                var sel = (Selection)_selection.Clone();
                sel.BarCount = Period;
                history = _dataProvider.GetBars(sel);
            }

            if (history == null || history.Count == 0)
                return 0;

            var minCount = Series[0].Length;

            for (int i = 0; i < history.Count; i++)
            {
                var bar = history[i];
                if (Series[0].Length < Period)
                {
                    Series[0].AppendOrUpdate(bar.Date, EMPTY_VALUE);
                    continue;
                }

                if (i >= Period - 1)
                {
                    var min = GetMinValue(history.GetRange(i - Period + 1, Period));
                    var max = GetMaxValue(history.GetRange(i - Period + 1, Period));
                    if ((max - min).Equals(0))
                        Series[0].AppendOrUpdate(bar.Date, 0.0);
                    else
                        Series[0].AppendOrUpdate(bar.Date, (double)(-100M * (max - GetPrice(bar, PriceConstants.CLOSE)) / (max - min)));
                }
            }

            return Series[0].Length - minCount > 0 ? Series[0].Length - minCount : 1;
        }

        protected override List<ScriptingParameterBase> InternalGetParameters()
        {
            return new List<ScriptingParameterBase>
            {
                new SeriesParam("Main", "Series parameters", 0)
                {
                    Color = Colors.Red,
                    Thickness = 2
                },
                new IntParam("Period", "Indicator period", 1)
                {
                    Value = 10,
                    MinValue = 1,
                    MaxValue = 100
                }
            };
        }

        protected override bool InternalSetParameters(List<ScriptingParameterBase> parameterBases)
        {
            Series[0].Color = ((SeriesParam)parameterBases[0]).Color;
            Series[0].Thickness = ((SeriesParam)parameterBases[0]).Thickness;

            Period = ((IntParam)parameterBases[1]).Value;

            DisplayName = String.Format("{0}_{1}", Name, Period);
            return true;
        }
    }
}