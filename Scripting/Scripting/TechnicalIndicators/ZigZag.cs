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
    public class ZigZag : IndicatorBase
    {

        #region Fields

        private Selection _selection;
        private IDataProvider _dataProvider;

        private decimal[] _zigzagBuffer;      // main buffer
        private decimal[] _highMapBuffer;     // highs
        private decimal[] _lowMapBuffer;      // lows
        private decimal _deviation;           // deviation in points

        public int ExtDepth = 12;
        public int ExtDeviation = 5;
        public int ExtBackstep = 3;
        public PriceConstants Type = PriceConstants.CLOSE;

        private enum LoolingFor
        {
            Undefined = 0,
            Pike = 1,  // searching for next high
            Sill = -1  // searching for next low
        }

        #endregion

        #region Constructor

        public ZigZag()
        {
            Name = "ZigZag";
            Series.Add(new Series("Main"));
        }

        #endregion

        #region IndicatorBase

        protected override bool InternalInit(Selection selection, IDataProvider dataProvider)
        {
            _selection = selection;
            _dataProvider = dataProvider;
            IsOverlay = true;
            Series.ForEach(s => s.Values.Clear());
            return InitialCalculation();
        }

        protected override int InternalCalculate(IEnumerable<Bar> bars = null)
        {
            if (bars != null)
                return ZCalculate(bars) ? 1 : 0;

            var sel = (Selection)_selection.Clone();
            sel.BarCount = 20;
            bars = _dataProvider.GetBars(sel);

            return ZCalculate(bars) ? 1 : 0;
        }

        protected override List<ScriptingParameterBase> InternalGetParameters()
        {
            return new List<ScriptingParameterBase>
            {
                new SeriesParam("MainSeries", "Series parameters", 0)
                {
                    Color = Colors.Red,
                    Thickness = 2
                },
                new IntParam("ExtDepth", "Indicator period", 1)
                {
                    Value = 12,
                    MinValue = 1,
                    MaxValue = 100
                },
                new IntParam("ExtDeviation", "Indicator period", 1)
                {
                    Value = 5,
                    MinValue = 1,
                    MaxValue = 100
                },
                new IntParam("ExtBackstep", "Indicator period", 1)
                {
                    Value = 3,
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

            ExtDepth = ((IntParam)parameterBases[1]).Value;
            ExtDeviation = ((IntParam)parameterBases[2]).Value;
            ExtBackstep = ((IntParam)parameterBases[3]).Value;

            Type = ParsePriceConstants((StringParam)parameterBases[4]);

            DisplayName = $"{Name}_{ExtDepth}_{ExtDeviation}_{ExtBackstep}_{Type}";
            return true;
        }

        #endregion

        #region Private Methods

        private bool ZCalculate(IEnumerable<Bar> bars)
        {
            var history = bars == null ? _dataProvider.GetBars(_selection) : new List<Bar>(bars);
            if (history == null || history.Count < 100)
                return false;
            
            history.Reverse(); //MQL indicator is reversed

            var limit = ExtDepth;
            var whatlookfor = LoolingFor.Undefined;

            int shift;
            int lasthighpos = 0, lastlowpos = 0;
            decimal lasthigh = 0, lastlow = 0;

            _zigzagBuffer = Enumerable.Repeat(0m, history.Count).ToArray();
            _highMapBuffer = Enumerable.Repeat(0m, history.Count).ToArray();
            _lowMapBuffer = Enumerable.Repeat(0m, history.Count).ToArray();

            var low = history.Select(h => h.MeanLow).ToList();
            var high = history.Select(h => h.MeanHigh).ToList();

            //--- searching High and Low
            for (shift = limit; shift < history.Count; shift++)
            {
                decimal res;
                var val = low[Lowest(low, ExtDepth, shift)];

                if (val == lastlow)
                    val = 0m;
                else
                {
                    lastlow = val;
                    if (low[shift] - val > _deviation) val = 0m;
                    else
                    {
                        for (var back = 1; back <= ExtBackstep; back++)
                        {
                            res = _lowMapBuffer[shift - back];
                            if (res != 0 && res > val) _lowMapBuffer[shift - back] = 0m;
                        }
                    }
                }
                if (low[shift] == val) _lowMapBuffer[shift] = val;
                else _lowMapBuffer[shift] = 0m;

                val = high[Highest(high, ExtDepth, shift)];
                if (val == lasthigh) val = 0m;
                else
                {
                    lasthigh = val;
                    if (val - high[shift] > _deviation) val = 0m;
                    else
                    {
                        for (var back = 1; back <= ExtBackstep; back++)
                        {
                            res = _highMapBuffer[shift - back];
                            if (res != 0 && res < val) _highMapBuffer[shift - back] = 0m;
                        }
                    }
                }
                if (high[shift] == val) _highMapBuffer[shift] = val;
                else _highMapBuffer[shift] = 0m;
            }

            lastlow = 0;
            lasthigh = 0;

            //--- final rejection
            for (shift = limit; shift < history.Count; shift++)
            {
                switch (whatlookfor)
                {
                    case LoolingFor.Undefined: // search for peak or lawn
                        if (lastlow == 0 && lasthigh == 0)
                        {
                            if (_highMapBuffer[shift] != 0)
                            {
                                lasthigh = high[shift];
                                lasthighpos = shift;
                                whatlookfor = LoolingFor.Sill;
                                _zigzagBuffer[shift] = lasthigh;
                            }
                            if (_lowMapBuffer[shift] != 0)
                            {
                                lastlow = low[shift];
                                lastlowpos = shift;
                                whatlookfor = LoolingFor.Pike;
                                _zigzagBuffer[shift] = lastlow;
                            }
                        }
                        break;
                    case LoolingFor.Pike: // search for peak
                        if (_lowMapBuffer[shift] != 0m && _lowMapBuffer[shift] < lastlow && _highMapBuffer[shift] == 0m)
                        {
                            _zigzagBuffer[lastlowpos] = 0m;
                            lastlowpos = shift;
                            lastlow = _lowMapBuffer[shift];
                            _zigzagBuffer[shift] = lastlow;
                        }
                        if (_highMapBuffer[shift] != 0m && _lowMapBuffer[shift] == 0m)
                        {
                            lasthigh = _highMapBuffer[shift];
                            lasthighpos = shift;
                            _zigzagBuffer[shift] = lasthigh;
                            whatlookfor = LoolingFor.Sill;
                        }
                        break;
                    case LoolingFor.Sill: // search for lawn
                        if (_highMapBuffer[shift] != 0m && _highMapBuffer[shift] > lasthigh && _lowMapBuffer[shift] == 0m)
                        {
                            _zigzagBuffer[lasthighpos] = 0m;
                            lasthighpos = shift;
                            lasthigh = _highMapBuffer[shift];
                            _zigzagBuffer[shift] = lasthigh;
                        }
                        if (_lowMapBuffer[shift] != 0m && _highMapBuffer[shift] == 0m)
                        {
                            lastlow = _lowMapBuffer[shift];
                            lastlowpos = shift;
                            _zigzagBuffer[shift] = lastlow;
                            whatlookfor = LoolingFor.Pike;
                        }
                        break;
                }
            }

            for (var i = 0; i < history.Count; i++)
            {
                AddToSeries(_zigzagBuffer[i], history[i].Date);
            }

            return true;
        }

        private bool InitialCalculation(IEnumerable<Bar> bars = null)
        {
            var history = bars == null ? _dataProvider.GetBars(_selection) : new List<Bar>(bars);
            if (history == null || !history.Any())
                return false;

            _deviation = ExtDeviation * (decimal)0.01;
            return ZCalculate(history);
        }

        private static int Highest(IReadOnlyList<decimal> array, int depth, int startPos)
        {
            var index = startPos;
            if (startPos < 0) return 0;

            if (startPos - depth < 0) depth = startPos;
            var max = array[startPos];
            for (var i = startPos; i > startPos - depth; i--)
            {
                if (array[i] <= max) continue;

                index = i;
                max = array[i];
            }
            return index;
        }

        private static int Lowest(IReadOnlyList<decimal> array, int depth, int startPos)
        {
            var index = startPos;
            if (startPos < 0) return 0;

            if (startPos - depth < 0) depth = startPos;
            var min = array[startPos];
            for (var i = startPos; i > startPos - depth; i--)
            {
                if (array[i] >= min) continue;

                index = i;
                min = array[i];
            }
            return index;
        }

        private void AddToSeries(decimal value, DateTime dateTime) => Series[0].AppendOrUpdate(dateTime, (double)value);

        #endregion

    }
}