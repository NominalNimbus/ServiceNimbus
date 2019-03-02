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
    public class AverageDirectionalMovement  : IndicatorBase
    {
        private Selection _selection;
        private IDataProvider _dataProvider;
        
        public PriceConstants Type = PriceConstants.OPEN;
        public int Period = 10;

        public AverageDirectionalMovement ()
        {
            Name = "Average Directional Movement";
            IsOverlay = false;
            Series.Add(new Series("ADX"));
            Series.Add(new Series("Plus_Di"));
            Series.Add(new Series("Minus_Di"));
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

            decimal pdm;
            decimal mdm;
            decimal tr;
            decimal price_high;
            decimal price_low;
            decimal plusSdi;
            decimal minusSdi;
            decimal temp;
            decimal exp = 2M / (Period + 1);

            for (var pos = 1; pos < history.Count; pos++)
            {
                price_low =  GetPrice(history[pos], PriceConstants.LOW);
                price_high = GetPrice(history[pos], PriceConstants.HIGH);

                pdm = price_high - GetPrice(history[pos - 1], PriceConstants.HIGH);
                mdm = GetPrice(history[pos - 1], PriceConstants.LOW) - price_low;

                if (pdm < 0)
                    pdm = 0; // +DM
                if (mdm < 0)
                    mdm = 0; // -DM
                if (pdm.Equals(mdm))
                {
                    pdm = 0;
                    mdm = 0;
                }
                else if (pdm < mdm)
                    pdm = 0;
                else if (mdm < pdm)
                    mdm = 0;

                var num1 = Math.Abs(price_high - price_low);
                var num2 = Math.Abs(price_high - GetPrice(history[pos - 1], Type));
                var num3 = Math.Abs(price_low - GetPrice(history[pos - 1], Type));

                tr = Math.Max(num1, num2);
                tr = Math.Max(tr, num3);

                if (tr.Equals(0))
                {
                    plusSdi = 0;
                    minusSdi = 0;
                }
                else
                {
                    plusSdi = 100M * pdm / tr;
                    minusSdi = 100M * mdm / tr;
                }

                var last_p = 0M;
                var last_m = 0M;

                if (Series[1].Values.Count > 0)
                {
                    var item = Series[1].Values.LastOrDefault(p => p.Date < history[pos].Date);
                    if (item != null)
                        last_p = (decimal)item.Value;
                }
                if (Series[2].Values.Count > 0)
                {
                    var item = Series[2].Values.LastOrDefault(p => p.Date < history[pos].Date);
                    if (item != null)
                        last_m = (decimal)item.Value;
                }

                var pd = plusSdi * exp + last_p * (1 - exp);
                var md = minusSdi * exp + last_m * (1 - exp);

                Series[1].AppendOrUpdate(history[pos].Date, (double)pd);
                Series[2].AppendOrUpdate(history[pos].Date, (double)md);

                var div = Math.Abs(pd + md);
                if (div.Equals(0.00))
                    temp = 0;
                else
                    temp = 100 * (Math.Abs(pd - md) / div);

                var last = 0M;
                if (Series[0].Values.Count > 0)
                {
                    var item = Series[0].Values.LastOrDefault(p => p.Date < history[pos].Date);
                    if (item != null)
                        last = (decimal)item.Value;
                }

                Series[0].AppendOrUpdate(history[pos].Date, (double)(temp * exp + last * (1 - exp)));
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
                 new SeriesParam("Plus_Di", "Series parameters", 1)
                {
                    Color = Colors.Green,
                    Thickness = 1
                },
                 new SeriesParam("Minus_Di", "Series parameters", 2)
                {
                    Color = Colors.Red,
                    Thickness = 1
                },
                  new IntParam("Period", "Indicator period", 3)
                {
                    Value = 10,
                    MinValue = 1,
                    MaxValue = 100
                },
                GetPriceTypeParam(4)
            };
        }

        protected override bool InternalSetParameters(List<ScriptingParameterBase> parameterBases)
        {
            Series[0].Color = ((SeriesParam)parameterBases[0]).Color;
            Series[0].Thickness = ((SeriesParam)parameterBases[0]).Thickness;

            Series[1].Color = ((SeriesParam)parameterBases[1]).Color;
            Series[1].Thickness = ((SeriesParam)parameterBases[1]).Thickness;

            Series[2].Color = ((SeriesParam)parameterBases[2]).Color;
            Series[2].Thickness = ((SeriesParam)parameterBases[2]).Thickness;

            Period = ((IntParam)parameterBases[3]).Value;
            Type = ParsePriceConstants((StringParam)parameterBases[4]);

            DisplayName = String.Format("{0}_{1}_{2}", Name, Type, Period);
            return true;
        }
    }
}