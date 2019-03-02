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
    public class StochasticOscillator : IndicatorBase
    {
        private Selection _selection;
        private IDataProvider _dataProvider;
        
        public int KPeriod;
        public int DPeriod;
        public int Slowing;

        public StochasticOscillator()
        {
            Name = "Stochastic Oscillator";
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
            else if (Series[0].Values.Count == 0)
            {
                history = _dataProvider.GetBars(_selection);
            }
            else
            {
                var sel = (Selection)_selection.Clone();
                sel.BarCount = KPeriod + 1;
                history = _dataProvider.GetBars(sel);
            }

            if (history == null || history.Count <= KPeriod)
                return 0;

            var minCount = Series[0].Length;

            if (Series[0].Values.Count == 0)
            {
                for (int i = 0; i < KPeriod; i++)
                {
                    Series[0].AppendOrUpdate(history[i].Date, EMPTY_VALUE);
                    Series[1].AppendOrUpdate(history[i].Date, EMPTY_VALUE);
                }
            }

            for (var i = KPeriod; i < history.Count; i++)
            {
                var highest = GetMaxValue(history.GetRange(i - KPeriod + 1, KPeriod));
                var lowest = GetMinValue(history.GetRange(i - KPeriod + 1, KPeriod));

                var k = (GetPrice(history[i], PriceConstants.CLOSE) - lowest) / (highest - lowest) * 100;
                Series[0].AppendOrUpdate(history[i].Date, (double)k);

                if (Series[0].Length < KPeriod + DPeriod)
                {
                    Series[1].AppendOrUpdate(history[i].Date, EMPTY_VALUE);
                }
                else if (Series[0].Length == KPeriod + DPeriod)
                {
                    var reversed = Series[0].Values.ToList();
                    reversed.Reverse();
                    Series[1].AppendOrUpdate(history[i].Date, reversed.Take(DPeriod).Sum(p => p.Value) / DPeriod);
                }
                else
                {
                    var last = Series[1].Values.LastOrDefault(p => p.Date < history[i].Date);
                    if (last != null)
                    {
                        Series[1].AppendOrUpdate(history[i].Date, last.Value
                            + (Series[0].Values.Last().Value - Series[0].Values[Series[0].Length - DPeriod].Value) / DPeriod);
                    }
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
                 new SeriesParam("Signal", "Series parameters", 1)
                {
                    Color = Colors.Green,
                    Thickness = 2
                },
                new IntParam("KPeriod", "Indicator period", 2)
                {
                    Value = 10,
                    MinValue = 1,
                    MaxValue = 100
                },
                new IntParam("DPeriod", "SMA period period", 3)
                {
                    Value = 3,
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

            KPeriod = ((IntParam)parameterBases[2]).Value;
            DPeriod = ((IntParam)parameterBases[3]).Value;

            DisplayName = String.Format("{0}_{1}_{2}", Name, KPeriod, DPeriod);
            return true;
        }
    }
}