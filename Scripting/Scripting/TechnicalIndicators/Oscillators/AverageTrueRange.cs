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
    public class AverageTrueRange : IndicatorBase
    {
        private Selection _selection;
        private IDataProvider _dataProvider;
        private readonly Series _tr; 

        public int Period = 10;

        public AverageTrueRange()
        {
            Name = "Average True Range";
            IsOverlay = false;
            Series.Add(new Series("Main"));
            _tr = new Series();
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
                var sel = _selection.Clone() as Selection;
                sel.From = Series[0].Values[Series[0].Values.Count - 2].Date;
                sel.To = DateTime.MaxValue;
                history = _dataProvider.GetBars(sel);
            }

            if (history == null || history.Count < 2)
                return 0;

            // True range calculation
            for (var i = 1; i < history.Count; i++)
            {
                var hi = GetPrice(history[i], PriceConstants.HIGH);
                var lo = GetPrice(history[i], PriceConstants.LOW);
                var prevClose = GetPrice(history[i - 1], PriceConstants.CLOSE);
                if (_tr.Length == 0)
                {
                    _tr.AppendOrUpdate(history[i].Date, (double)(hi - lo));
                }
                else
                {
                    var value = Math.Max((hi - lo), Math.Abs((hi - prevClose)));
                    value = Math.Max(value, Math.Abs((lo - prevClose)));
                    _tr.AppendOrUpdate(history[i].Date, (double)value);
                }
            }

            //Avg. true range calculation
            for (var i = 0; i < history.Count; i++)
            {
                if (Series[0].Length < Period - 1)
                {
                    Series[0].AppendOrUpdate(history[i].Date, EMPTY_VALUE);
                }
                else if (Series[0].Length == Period - 1)
                {
                    var sum = _tr.Values.GetRange(0, Period).Sum(p => p.Value)*(1/Period);
                    Series[0].AppendOrUpdate(history[i].Date, sum);
                }
                else
                {
                    var prevATR = Series[0].Values.LastOrDefault(p => p.Date < history[i].Date);
                    if(prevATR == null)
                        continue;

                    var tr = _tr.Values.FirstOrDefault(p => p.Date.Equals(history[i].Date));
                    if(tr != null)
                        Series[0].AppendOrUpdate(history[i].Date, ((prevATR.Value * (Period - 1)) + tr.Value) / Period);
                }
            }

            return history.Count - 1;
        }

        protected override List<ScriptingParameterBase> InternalGetParameters()
        {
            return new List<ScriptingParameterBase>
            {
                new SeriesParam("Main", "Series parameters", 0)
                {
                    Color = Colors.Blue,
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