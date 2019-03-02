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
    public class OnBalanceVolume : IndicatorBase
    {
        private Selection _selection;
        private IDataProvider _dataProvider;

        public OnBalanceVolume()
        {
            Name = "On Balance Volume";
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
            else if (Series[0].Length < 2)
            {
                history = _dataProvider.GetBars(_selection);
            }
            else
            {
                var sel = _selection.Clone() as Selection;
                sel.From = Series[0].Values[Series[0].Length - 2].Date;
                sel.To = DateTime.MaxValue;
                history = _dataProvider.GetBars(sel);
            }

            if (history == null || history.Count < 2)
                return 0;

            var count = Series[0].Length;

            if (count == 0)
                Series[0].AppendOrUpdate(history[0].Date, (double)history[0].MeanVolume);

            // True range calculation
            for (var i = 1; i < history.Count; i++)
            {
                var prevPrice = GetPrice(history[i - 1], PriceConstants.CLOSE);
                var curPrice = GetPrice(history[i], PriceConstants.CLOSE);
                var last = Series[0].Values.LastOrDefault(p => p.Date < history[i].Date);

                if (last == null)
                    return 0;

                if (curPrice.Equals(prevPrice))
                    Series[0].AppendOrUpdate(history[i].Date, last.Value);
                else
                {
                    if (curPrice < prevPrice)
                        Series[0].AppendOrUpdate(history[i].Date, last.Value - (double)history[i].MeanVolume);
                    else
                        Series[0].AppendOrUpdate(history[i].Date, last.Value + (double)history[i].MeanVolume);
                }
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
                }
            };
        }

        protected override bool InternalSetParameters(List<ScriptingParameterBase> parameterBases)
        {
            Series[0].Color = ((SeriesParam)parameterBases[0]).Color;
            Series[0].Thickness = ((SeriesParam)parameterBases[0]).Thickness;

            DisplayName = Name;
            return true;
        }
    }
}