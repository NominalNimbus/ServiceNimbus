/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using CommonObjects;

namespace Scripting.TechnicalIndicators
{
    public class MarketFacilitationIndex : IndicatorBase
    {
        private Selection _selection;
        private IDataProvider _dataProvider;

        public MarketFacilitationIndex()
        {
            Name = "Market Facilitation Index";
            IsOverlay = false;

            Series.Add(new Series("BWMFI"));
            Series.Add(new Series("Up_Up"));
            Series.Add(new Series("Down_Down"));
            Series.Add(new Series("Up_Down"));
            Series.Add(new Series("Down_Up"));

            foreach (var series in Series)
                series.Style = DrawShapeStyle.DRAW_HISTOGRAM;

            Series[0].Style = DrawShapeStyle.DRAW_NONE;
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
                sel.BarCount = 2;
                history = _dataProvider.GetBars(sel);
            }

            if (history == null || history.Count < 2)
                return 0;

            for (var i = 0; i < history.Count; i++)
            {
                var bar = history[i];
                var diff = GetPrice(bar, PriceConstants.HIGH) - GetPrice(bar, PriceConstants.LOW);

                if (bar.MeanVolume.Equals(0))
                    Series[0].AppendOrUpdate(bar.Date, (double)(100000 * diff / history[i - 1].MeanVolume));
                else
                    Series[0].AppendOrUpdate(bar.Date, (double)(100000 * diff / bar.MeanVolume));
            }

            bool bMfiUp = true, bVolUp = true;

            if (Series[1].Length > 1)
            {
                if ((Series[1].Values[Series[1].Length - 2].Value.Equals(0)))
                {
                    bMfiUp = true;
                    bVolUp = true;
                }
                if ((Series[2].Values[Series[1].Length - 2].Value.Equals(0)))
                {
                    bMfiUp = false;
                    bVolUp = false;
                }
                if ((Series[3].Values[Series[1].Length - 2].Value.Equals(0)))
                {
                    bMfiUp = true;
                    bVolUp = false;
                }
                if ((Series[4].Values[Series[1].Length - 2].Value.Equals(0)))
                {
                    bMfiUp = false;
                    bVolUp = true;
                }
            }

            for (var i = 1; i < history.Count; i++)
            {
                var bar = history[i];
                var prevLast = Series[0].Values.LastOrDefault(p => p.Date < bar.Date);
                var last = Series[0].Values.FirstOrDefault(p => p.Date == bar.Date);

                if(prevLast == null)
                    continue;

                if (Series[0].Values.Last().Value > prevLast.Value)
                    bMfiUp = true;
                if (Series[0].Values.Last().Value < prevLast.Value)
                    bMfiUp = false;
                if (bar.MeanVolume > history[i -1].MeanVolume)
                    bVolUp = true;
                if (bar.MeanVolume < history[i - 1].MeanVolume)
                    bVolUp = false;

                if (bMfiUp && bVolUp)
                {
                    Series[1].AppendOrUpdate(last.Date, last.Value);
                    Series[2].AppendOrUpdate(last.Date, 0);
                    Series[3].AppendOrUpdate(last.Date, 0);
                    Series[4].AppendOrUpdate(last.Date, 0);
                }
                else if (!bMfiUp && !bVolUp)
                {
                    Series[2].AppendOrUpdate(last.Date, last.Value);
                    Series[1].AppendOrUpdate(last.Date, 0);
                    Series[3].AppendOrUpdate(last.Date, 0);
                    Series[4].AppendOrUpdate(last.Date, 0);
                }
                else if (bMfiUp && !bVolUp)
                {
                    Series[3].AppendOrUpdate(last.Date, last.Value);
                    Series[2].AppendOrUpdate(last.Date, 0);
                    Series[1].AppendOrUpdate(last.Date, 0);
                    Series[4].AppendOrUpdate(last.Date, 0);
                }
                else if (!bMfiUp && bVolUp)
                {
                    Series[4].AppendOrUpdate(last.Date, last.Value);
                    Series[2].AppendOrUpdate(last.Date, 0);
                    Series[3].AppendOrUpdate(last.Date, 0);
                    Series[1].AppendOrUpdate(last.Date, 0);
                }
            }

            return history.Count - 1;
        }

        protected override List<ScriptingParameterBase> InternalGetParameters()
        {
            return new List<ScriptingParameterBase>
            {
                // Series
                new SeriesParam("BWMFI", "BWMFI series parameters", 0)
                {
                    Color = Colors.Black,
                    Thickness = 1
                },
                new SeriesParam("Up_Up", "Up_Up series parameters", 1)
                {
                    Color = Colors.Lime,
                    Thickness = 1
                },
                new SeriesParam("Down_Down", "Down_Down series parameters", 2)
                {
                    Color = Colors.SaddleBrown,
                    Thickness = 1
                },
                new SeriesParam("Up_Down", "Up_Down series parameters", 3)
                {
                    Color = Colors.Blue,
                    Thickness = 1
                },
                new SeriesParam("Down_Up", "Down_Up series parameters", 4)
                {
                    Color = Colors.Pink,
                    Thickness = 1
                }
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

            Series[3].Color = ((SeriesParam)parameterBases[3]).Color;
            Series[3].Thickness = ((SeriesParam)parameterBases[3]).Thickness;

            Series[4].Color = ((SeriesParam)parameterBases[4]).Color;
            Series[4].Thickness = ((SeriesParam)parameterBases[4]).Thickness;

            DisplayName = Name;
            return true;
        }
    }
}