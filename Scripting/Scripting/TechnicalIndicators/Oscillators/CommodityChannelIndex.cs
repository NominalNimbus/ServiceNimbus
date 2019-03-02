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
    public class CommodityChannelIndex : IndicatorBase
    {
        private Selection _selection;
        private IDataProvider _dataProvider;
        private IndicatorBase SMA;

        public int Period = 10;
        public PriceConstants Type = PriceConstants.OPEN;

        public CommodityChannelIndex()
        {
            Name = "Commodity Channel Index";
            Series.Add(new Series("Main"));
        }

        protected override bool InternalInit(Selection selection, IDataProvider dataProvider)
        {
            _selection = selection;
            _dataProvider = dataProvider;
            Series.ForEach(s => s.Values.Clear());

            SMA = new SimpleMovingAverage
            {
                Period = Period,
                Type = Type
            };

            SMA.Init(selection, dataProvider);

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
                sel.BarCount = Period;
                history = _dataProvider.GetBars(sel);
            }

            if (history == null || history.Count == 0)
                return 0;

            SMA.Calculate(bars);

            double d;
            double r;
            var mul = 0.015 / Period;

            if (Series[0].Length == 0)
            {
                for (var i = 0; i < Period - 1; i++)
                {
                    var date = SMA.Series[0].Values[i].Date;
                    Series[0].AppendOrUpdate(date, EMPTY_VALUE);
                }
            }

            for (var i = Period - 1; i < history.Count; i++)
            {
                var sma = SMA.Series[0].Values.FirstOrDefault(p => p.Date.Equals(history[i].Date));
                if (sma == null)
                    continue;

                double sum = 0;
                for (var j = 0; j < Period; j++)
                    sum += Math.Abs((double)GetPrice(history[i - j], Type) - sma.Value);

                d = sum * mul;
                r = (double)GetPrice(history[i], Type) - sma.Value;
                if (d.Equals(0))
                    Series[0].AppendOrUpdate(sma.Date, EMPTY_VALUE);
                else
                    Series[0].AppendOrUpdate(sma.Date, r/d);
            }

            return history.Count - Period + 1;
        }

        protected override List<ScriptingParameterBase> InternalGetParameters()
        {
            return new List<ScriptingParameterBase>
            {
                // Series
                new SeriesParam("CCI", "Series parameters", 0)
                {
                    Color = Colors.Red,
                    Thickness = 2
                },
                // Periods
                new IntParam("Period", "Period of the Indicator", 1)
                {
                    Value = 13,
                    MinValue = 1,
                    MaxValue = 100
                },
                // Types
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