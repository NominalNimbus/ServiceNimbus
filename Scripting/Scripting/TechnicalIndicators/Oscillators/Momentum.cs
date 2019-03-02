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
    public class Momentum : IndicatorBase
    {
        private Selection _selection;
        private IDataProvider _dataProvider;

        public int Period = 10;
        public new PriceConstants PriceType = PriceConstants.OPEN;

        public Momentum()
        {
            Name = "Momentum";
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
            List<Bar> history;
            if (bars != null)
            {
                history = new List<Bar>(bars);
            }
            else if (Series[0].Values.Count < 2)
            {
                history = _dataProvider.GetBars(_selection);
            }
            else
            {
                var sel = (Selection)_selection.Clone();
                sel.BarCount = Period;
                history = _dataProvider.GetBars(sel);
            }

            if (history == null || history.Count < Period)
                return 0;

            var count = Series[0].Length;

            if (count == 0)
                for (int i = 0; i < Period - 1; i++)
                    Series[0].AppendOrUpdate(history[i].Date, EMPTY_VALUE);

            // True range calculation
            for (var i = Period - 1; i < history.Count; i++)
            {
                Series[0].AppendOrUpdate(history[i].Date,
                    (double)GetPrice(history[i], PriceType) * 100 
                    / (double)GetPrice(history[i - Period + 1], PriceType));
            }

            return Series[0].Length - count > 0 ? Series[0].Length - count : 1;
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
                },
                GetPriceTypeParam(2)
            };
        }

        protected override bool InternalSetParameters(List<ScriptingParameterBase> parameterBases)
        {
            Series[0].Color = ((SeriesParam)parameterBases[0]).Color;
            Series[0].Thickness = ((SeriesParam)parameterBases[0]).Thickness;

            Period = ((IntParam)parameterBases[1]).Value;
            PriceType = ParsePriceConstants((StringParam) parameterBases[2]);

            DisplayName = String.Format("{0}_{1}_{2}", Name, Period, PriceType);
            return true;
        }
    }
}