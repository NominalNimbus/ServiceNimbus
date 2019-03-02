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
    public class BullsPower : IndicatorBase
    {
        private Selection _selection;
        private IDataProvider _dataProvider;
        private IndicatorBase MA;
        private readonly Series _buff;

        public int Period = 10;

        public PriceConstants Type = PriceConstants.OPEN;
        public MovingAverageType MaType = MovingAverageType.EMA;

        public BullsPower()
        {
            Name = "Bulls Power";
            _buff = new Series();

            Series.Add(new Series("BullsPower")
            {
                Style = DrawShapeStyle.DRAW_HISTOGRAM
            });
        }

        protected override bool InternalInit(Selection selection, IDataProvider dataProvider)
        {
            _selection = selection;
            _dataProvider = dataProvider;
            Series.ForEach(s => s.Values.Clear());

            if (MaType == MovingAverageType.EMA)
            {
                MA = new ExponentialMovingAverage
                {
                    Period = Period,
                    Type = Type
                };
            }
            else if (MaType == MovingAverageType.SMA)
            {
                MA = new SimpleMovingAverage
                {
                    Period = Period,
                    Type = Type
                };
            }
            else if (MaType == MovingAverageType.SSMA)
            {
                MA = new SmoothedMovingAverage 
                {
                    Period = Period,
                    Type = Type
                };

            }
            else if (MaType == MovingAverageType.LWMA)
            {
                MA = new LinearWeightedMovingAverage
                {
                    Period = Period,
                    Type = Type
                };
            }

            MA.Init(selection, dataProvider);
            
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
            else if (_buff.Values.Count == 0)
            {
                history = _dataProvider.GetBars(_selection);
            }
            else
            {
                var sel = _selection.Clone() as Selection;
                sel.From = _buff.Values.Last().Date;
                sel.To = DateTime.MaxValue;
                history = _dataProvider.GetBars(sel);
            }

            if (history == null || history.Count == 0)
                return 0;

            MA.Calculate(bars);

            for (var i = _buff.Length > 0 ? _buff.Length - 1 : 0; i < MA.Series[0].Values.Count; i++)
                _buff.AppendOrUpdate(MA.Series[0].Values[i].Date, MA.Series[0].Values[i].Value);

            foreach (var bar in history)
            {
                var ma = _buff.Values.FirstOrDefault(p => p.Date.Equals(bar.Date));
                if(ma == null)
                    continue;

                if(ma.Value == EMPTY_VALUE)
                    Series[0].AppendOrUpdate(ma.Date, ma.Value);
                else
                    Series[0].AppendOrUpdate(ma.Date, (double)GetPrice(bar, PriceConstants.HIGH) - ma.Value);
            }

            return history.Count;
        }

        protected override List<ScriptingParameterBase> InternalGetParameters()
        {
            return new List<ScriptingParameterBase>
            {
                // Series
                new SeriesParam("BullsPower", "BullsPower series parameters", 0)
                {
                    Color = Colors.Lime,
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
                GetSmoothingTypeParam(2),
                GetPriceTypeParam(3)
            };
        }

        protected override bool InternalSetParameters(List<ScriptingParameterBase> parameterBases)
        {
            Series[0].Color = ((SeriesParam)parameterBases[0]).Color;
            Series[0].Thickness = ((SeriesParam)parameterBases[0]).Thickness;

            Period = ((IntParam)parameterBases[1]).Value;

            MaType = ParseMovingAverageConstants((StringParam) parameterBases[2]);
            Type = ParsePriceConstants((StringParam)parameterBases[3]);

            DisplayName = String.Format("{0}_{1}_{2}_{3}", Name, Period, MaType, Type);
            return true;
        }
    }
}