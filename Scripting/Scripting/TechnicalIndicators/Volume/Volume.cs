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
    public class Volume : IndicatorBase
    {
        private Selection _selection;
        private IDataProvider _dataProvider;

        public Volume()
        {
            Name = "Volume";
            Series.Add(new Series("Volume")
            {
                Style = DrawShapeStyle.DRAW_HISTOGRAM
            });
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
                var sel = _selection.Clone() as Selection;
                sel.From = Series[0].Values.Last().Date;
                sel.To = DateTime.MaxValue;
                history = _dataProvider.GetBars(sel);
            }

            if (history == null || history.Count == 0)
                return 0;

            foreach (var bar in history)
                Series[0].AppendOrUpdate(bar.Date, (double)bar.MeanVolume);

            return history.Count;
        }

        protected override List<ScriptingParameterBase> InternalGetParameters()
        {
            return new List<ScriptingParameterBase>
            {
                new SeriesParam("Volume", "Volume parameters", 0)
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