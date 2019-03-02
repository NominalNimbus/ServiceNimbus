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
    public class ParabolicSAR : IndicatorBase
    {
        private Selection _selection;
        private IDataProvider _dataProvider;
        private decimal af_Up = 0.02M;
        private decimal af_down = 0.02M; 
        private decimal ep_Up;
        private decimal ep_Down;
        private bool up_direction = true;

        public int Period = 10;
        public decimal Step = 0.02M;
        public decimal Maximum = 0.2M;

        public ParabolicSAR()
        {
            Name = "Parabolic SAR";
            IsOverlay = true;
            Series.Add(new Series("Up")
            {
                Type = DrawStyle.STYLE_DOT
            });
            Series.Add(new Series("Down")
            {
                Type = DrawStyle.STYLE_DOT
            });
        }

        protected override bool InternalInit(Selection selection, IDataProvider dataProvider)
        {
            _selection = selection;
            _dataProvider = dataProvider;
            Series.ForEach(s => s.Values.Clear());
            InitialCalculate();
            return true;
        }

        protected override int InternalCalculate(IEnumerable<Bar> bars = null)
        {
            List<Bar> history = null;
            if (bars != null)
            {
                history = new List<Bar>(bars);
            }
            else
            {
                var sel = (Selection)_selection.Clone();
                sel.BarCount = Period + 1;
                history = _dataProvider.GetBars(sel);
            }

            if (history == null || history.Count < Period + 1)
                return 0;

            if (!Series[0].Values.Any())
                InitialCalculate(bars);

            for (var i = Period; i < history.Count; i++)
            {
                if (up_direction)
                {
                    var prevSar = Series[0].Values.LastOrDefault(p => p.Date < history[i].Date);
                    if (prevSar == null)
                        return 0;

                    if (prevSar.Value == EMPTY_VALUE)
                    {
                        Series[0].AppendOrUpdate(history[i].Date, (double)GetMinValue(history.GetRange(i - Period, Period)));
                        continue;
                    }

                    var prevValue = (decimal)prevSar.Value;
                    var sar = prevValue + af_Up*(ep_Up - prevValue);

                    Series[0].AppendOrUpdate(history[i].Date, (double)sar);
                    Series[1].AppendOrUpdate(history[i].Date, EMPTY_VALUE);

                    if (sar > GetPrice(history[i], PriceConstants.LOW))
                    {
                        up_direction = false;
                        af_Up = 0.02M;
                        af_down = 0.02M;
                        ep_Up = Decimal.MinValue;
                        ep_Down = GetMinValue(history.GetRange(i - Period, Period));
                        Series[1].AppendOrUpdate(history[i].Date,(double)GetMaxValue(history.GetRange(i - Period, Period)));
                    }
                    else
                    {
                        var hi = GetPrice(history[i], PriceConstants.HIGH);
                        if (ep_Up < hi)
                        {
                            ep_Up = hi;
                            if (af_Up < Maximum)
                                af_Up += Step;
                        }
                    }
                }
                else
                {
                    var prevSar = Series[1].Values.LastOrDefault(p => p.Date < history[i].Date);
                    if (prevSar == null)
                        return 0;

                    if (prevSar.Value == EMPTY_VALUE)
                    {
                        Series[1].AppendOrUpdate(history[i].Date, (double)GetMaxValue(history.GetRange(i - Period, Period)));
                        continue;
                    }

                    var prevValue = (decimal)prevSar.Value;
                    var sar = prevValue - af_down * (prevValue - ep_Down);

                    Series[1].AppendOrUpdate(history[i].Date, (double)sar);
                    Series[0].AppendOrUpdate(history[i].Date, EMPTY_VALUE);

                    if (sar < GetPrice(history[i], PriceConstants.HIGH))
                    {
                        up_direction = true;
                        af_Up = 0.02M;
                        af_down = 0.02M;
                        ep_Up = GetMaxValue(history.GetRange(i - Period, Period));
                        ep_Down = Decimal.MaxValue;
                        Series[0].AppendOrUpdate(history[i].Date, (double)GetMinValue(history.GetRange(i - Period, Period)));
                    }
                    else
                    {
                        var lo = GetPrice(history[i], PriceConstants.LOW);
                        if (ep_Down > lo)
                        {
                            ep_Down = lo;
                            if (af_down < Maximum)
                                af_down += Step;
                        }
                    }
                }
            }

            return 1;
        }

        private void InitialCalculate(IEnumerable<Bar> bars = null)
        {
            var history = bars == null ? _dataProvider.GetBars(_selection) : new List<Bar>(bars);
            if(history.Count <= Period + 2)
                return;

            for (var i = 0; i < Period; i++)
            {
                Series[0].AppendOrUpdate(history[i].Date, EMPTY_VALUE);
                Series[1].AppendOrUpdate(history[i].Date, EMPTY_VALUE);
            }

            for (int i = Period; i < Period + 2; i++)
            {
                if (up_direction)
                {
                    Series[0].AppendOrUpdate(history[i].Date, (double)GetPrice(history[Period - 1], PriceConstants.LOW));
                    Series[1].AppendOrUpdate(history[i].Date, EMPTY_VALUE);
                }
                else
                {
                    Series[0].AppendOrUpdate(history[i].Date, (double)GetPrice(history[Period - 1], PriceConstants.HIGH));
                    Series[1].AppendOrUpdate(history[i].Date, EMPTY_VALUE);
                }
            }

            if (up_direction)
            {
                ep_Up = GetMaxValue(history.GetRange(0, Period + 2));
                ep_Down = Decimal.MaxValue;
            }
            else
            {
                ep_Down = GetMinValue(history.GetRange(0, Period + 2));
                ep_Up = Decimal.MinValue;
            }

            for (var i = Period + 2; i < history.Count; i++)
            {
                if (up_direction)
                {
                    var prevSar = Series[0].Values.LastOrDefault(p => p.Date < history[i].Date);
                    if(prevSar == null)
                        return;

                    if (prevSar.Value == EMPTY_VALUE)
                    {
                        Series[0].AppendOrUpdate(history[i].Date, (double)GetMinValue(history.GetRange(i - Period, Period)));
                        continue;
                    }

                    var prevValue = (decimal)prevSar.Value;
                    var sar = prevValue + af_Up * (ep_Up - prevValue);

                    Series[0].AppendOrUpdate(history[i].Date, (double)sar);
                    Series[1].AppendOrUpdate(history[i].Date, EMPTY_VALUE);

                    if (sar > GetPrice(history[i], PriceConstants.LOW))
                    {
                        up_direction = false;
                        af_Up = 0.02M;
                        af_down = 0.02M;
                        ep_Up = Decimal.MinValue;
                        ep_Down = GetMinValue(history.GetRange(i - Period, Period));
                        Series[1].AppendOrUpdate(history[i].Date, (double)GetMaxValue(history.GetRange(i - Period, Period)));
                    }
                    else
                    {
                        var hi = GetPrice(history[i], PriceConstants.HIGH);
                        if (ep_Up < hi)
                        {
                            ep_Up = hi;
                            if (af_Up < Maximum)
                                af_Up += Step;
                        }
                    }
                }
                else
                {
                    var prevSar = Series[1].Values.LastOrDefault(p => p.Date < history[i].Date);
                    if (prevSar == null)
                        return;

                    if (prevSar.Value == EMPTY_VALUE)
                    {
                        Series[1].AppendOrUpdate(history[i].Date, (double)GetMaxValue(history.GetRange(i - Period, Period)));
                        continue;
                    }

                    var prevValue = (decimal)prevSar.Value;
                    var sar = prevValue - af_down * (prevValue - ep_Down);

                    Series[1].AppendOrUpdate(history[i].Date, (double)sar);
                    Series[0].AppendOrUpdate(history[i].Date, EMPTY_VALUE);

                    if (sar < GetPrice(history[i], PriceConstants.HIGH))
                    {
                        up_direction = true;
                        af_Up = 0.02M;
                        af_down = 0.02M;
                        ep_Up = GetMaxValue(history.GetRange(i - Period, Period));
                        ep_Down = Decimal.MaxValue;
                        Series[0].AppendOrUpdate(history[i].Date, (double)GetMinValue(history.GetRange(i - Period, Period)));
                    }
                    else
                    {
                        var lo = GetPrice(history[i], PriceConstants.LOW);
                        if (ep_Down > lo)
                        {
                            ep_Down = lo;
                            if (af_down < Maximum)
                                af_down += Step;
                        }
                    }
                }
            }
        }

        protected override List<ScriptingParameterBase> InternalGetParameters()
        {
            return new List<ScriptingParameterBase>
            {
                new SeriesParam("Up", "Up series parameters", 0)
                {
                    Color = Colors.Lime,
                    Thickness = 2
                },
                new SeriesParam("Down", "Down series parameters", 1)
                {
                    Color = Colors.Red,
                    Thickness = 2
                },
                new IntParam("Period", "Indicator period", 2)
                {
                    Value = 10,
                    MinValue = 1,
                    MaxValue = 100
                },
                new DoubleParam("Step", "The Acceleration Factor (AF), which is also referred to as the Step, dictates SAR sensitivity", 3)
                {
                    Value = 0.02,
                    MinValue = 0.01,
                    MaxValue = 0.1
                },
                new DoubleParam("Maximum", "The sensitivity of the indicator can also be adjusted using the Maximum Step", 4)
                {
                    Value = 0.2,
                    MinValue = 0.1,
                    MaxValue = 1
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
            Step = (decimal)((DoubleParam)parameterBases[3]).Value;
            Maximum = (decimal)((DoubleParam)parameterBases[4]).Value;

            DisplayName = String.Format("{0}_{1}", Name, Period);
            return true;
        }
    }
}