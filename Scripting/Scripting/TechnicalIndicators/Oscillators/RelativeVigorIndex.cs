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
    public class RelativeVigorIndex : IndicatorBase
    {
        private Selection _selection;
        private IDataProvider _dataProvider;

        public int Period = 10;

        public RelativeVigorIndex()
        {
            Name = "Relative Vigor Index";
            Series.Add(new Series("Main"));
            Series.Add(new Series("Signal"));
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
                sel.BarCount = Period + 4;
                history = _dataProvider.GetBars(sel);
            }

            if (history == null || history.Count < Period)
                return 0;

            var count = Series[0].Length;
            if (count == 0)
            {
                for (int i = 0; i < Period + 3; i++)
                    Series[0].AppendOrUpdate(history[i].Date, EMPTY_VALUE);
            }

            for (var i = Period + 3; i < history.Count; i++)
            {
                var dNum = 0M;
                var dDeNum = 0M;
                for (var j = 1; j < Period; j++)
                {
                    var idx = i - Period + 1 + j;
                    var o = GetPrice(history[idx], PriceConstants.OPEN);
                    var h = GetPrice(history[idx], PriceConstants.HIGH);
                    var l = GetPrice(history[idx], PriceConstants.LOW);
                    var c = GetPrice(history[idx], PriceConstants.CLOSE);
                    var o1 = GetPrice(history[idx - 1], PriceConstants.OPEN);
                    var h1 = GetPrice(history[idx - 1], PriceConstants.HIGH);
                    var l1 = GetPrice(history[idx - 1], PriceConstants.LOW);
                    var c1 = GetPrice(history[idx - 1], PriceConstants.CLOSE);
                    var o2 = GetPrice(history[idx - 2], PriceConstants.OPEN);
                    var h2 = GetPrice(history[idx - 2], PriceConstants.HIGH);
                    var l2 = GetPrice(history[idx - 2], PriceConstants.LOW);
                    var c2 = GetPrice(history[idx - 2], PriceConstants.CLOSE);
                    var o3 = GetPrice(history[idx - 3], PriceConstants.OPEN);
                    var h3 = GetPrice(history[idx - 3], PriceConstants.HIGH);
                    var l3 = GetPrice(history[idx - 3], PriceConstants.LOW);
                    var c3 = GetPrice(history[idx - 3], PriceConstants.CLOSE);
                    dNum += ((c - o) + 2 * (c1 - o1) + 2 * (c2 - o2) + (c3 - o3)) / 6M;
                    dDeNum += ((h - l) + 2 * (h1 - l1) + 2 * (h2 - l2) + (h3 - l3)) / 6M;
                }

                if (!dDeNum.Equals(0.0))
                    Series[0].AppendOrUpdate(history[i].Date, (double)(dNum / dDeNum));
                else
                    Series[0].AppendOrUpdate(history[i].Date, (double)dNum);

                var last = Series[0].Values.GetRange(Series[0].Length - 4, 4).ToList();

                if (last.Any(p => p.Value == EMPTY_VALUE))
                {
                    Series[1].AppendOrUpdate(history[i].Date, EMPTY_VALUE);
                }
                else
                {
                    var signal = (last[3].Value + 2 * last[2].Value + 2 * last[1].Value + last[0].Value) / 6;
                    Series[1].AppendOrUpdate(history[i].Date, signal);
                }
            }

            return Series[0].Length - count > 0 ? Series[0].Length - count : 1;
        }

        protected override List<ScriptingParameterBase> InternalGetParameters()
        {
            return new List<ScriptingParameterBase>
            {
                new SeriesParam("Main", "Main series parameters", 0)
                {
                    Color = Colors.Red,
                    Thickness = 2
                },
                 new SeriesParam("Signal", "Signal series parameters", 1)
                {
                    Color = Colors.Blue,
                    Thickness = 2
                },
                new IntParam("Period", "Indicator period", 2)
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

            Series[1].Color = ((SeriesParam)parameterBases[1]).Color;
            Series[1].Thickness = ((SeriesParam)parameterBases[1]).Thickness;

            Period = ((IntParam)parameterBases[2]).Value;

            DisplayName = String.Format("{0}_{1}", Name, Period);
            return true;
        }
    }
}