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
    public class BollingerBands : IndicatorBase
    {
        private Selection _selection;
        private IDataProvider _dataProvider;
        private IndicatorBase SMA;

        public int Period = 10;
        public int Deviation = 7;

        public PriceConstants Type = PriceConstants.OPEN;

        /// <summary>
        /// Class Constructor
        /// </summary>
        public BollingerBands()
        {
            Name = "Bands";
            IsOverlay = true;

            // Series count and name definitions
            Series.Add(new Series("Bands"));
            Series.Add(new Series("High"));
            Series.Add(new Series("Low"));
        }

        /// <summary>
        /// Initialize scripting inner parameters
        /// </summary>
        /// <param name="selection">Data description on which  code will be run</param>
        /// <param name="dataProvider">Object which provide access to historical and real time data</param>
        /// <returns>True if succeeded</returns>
        protected override bool InternalInit(Selection selection, IDataProvider dataProvider)
        {
            _selection = selection;
            _dataProvider = dataProvider;
            Series.ForEach(s => s.Values.Clear());

            // Inner using of other build - in indicator
            SMA = new SimpleMovingAverage
            {
                Period = Period,
                Type = Type
            };

            // SMA initialization (required in this place)
            SMA.Init(selection, dataProvider);
            
            InternalCalculate();

            return true;
        }

        /// <summary>
        /// Calculate function . Called after new tick arrived
        /// </summary>
        /// <returns>Working result. True in case of series changed</returns>
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

            // Calculation of SMA, build - in indicator
            SMA.Calculate(bars);

            if (Series[0].Length == 0)
            {
                for (var i = 0; i < Period - 1; i++)
                {
                    var date = SMA.Series[0].Values[i].Date;
                    Series[0].AppendOrUpdate(date, EMPTY_VALUE); // EMPTY_VALUE - will be not showed on chart, EMPTY_VALUE = 0x7FFFFFFF;
                    Series[1].AppendOrUpdate(date, EMPTY_VALUE);
                    Series[2].AppendOrUpdate(date, EMPTY_VALUE);
                }
            }

            // BBands calculation logic
            for (var i = Period - 1; i < history.Count; i++)
            {
                var sma = SMA.Series[0].Values.FirstOrDefault(p => p.Date.Equals(history[i].Date));
                if(sma == null || sma.Value == EMPTY_VALUE)
                    continue;

                double deviation, sum = 0, newres;

                for (var j = 0; j < Period; j++)
                {
                    newres = (double)GetPrice(history[i - j], Type) - sma.Value;
                    sum += newres * newres;
                }

                deviation = Deviation * Math.Sqrt(sum / Period);

                // AppendOrUpdate function in Series class allow you to update Value by specified Time in case if record with this Time is exist.
                // In case if record with this Time is not exist - new record with specified Time and Value will be added
                Series[0].AppendOrUpdate(sma.Date, sma.Value);
                Series[1].AppendOrUpdate(sma.Date, sma.Value + deviation);
                Series[2].AppendOrUpdate(sma.Date, sma.Value - deviation);
            }

            return history.Count - Period + 1;
        }

        /// <summary>
        /// Get list of parameters for configuration on client side
        /// </summary>
        protected override List<ScriptingParameterBase> InternalGetParameters()
        {
            return new List<ScriptingParameterBase>
            {
                // Series
                new SeriesParam("Bands", "Main series parameters", 0)
                {
                    Color = Colors.DodgerBlue,
                    Thickness = 2
                },
                new SeriesParam("Bands High", "High series parameters", 1)
                {
                    Color = Colors.Lime,
                    Thickness = 1
                },
                new SeriesParam("Bands Low", "Low series parameters", 2)
                {
                    Color = Colors.Red,
                    Thickness = 1
                },
                // Periods
                new IntParam("Period", "Period of the Indicator", 3)
                {
                    Value = 13,
                    MinValue = 1,
                    MaxValue = 100
                },
                new IntParam("Deviation", "Deviation of the Indicator", 4)
                {
                    Value = 2,
                    MinValue = 1,
                    MaxValue = 100
                },
                // Types
                GetPriceTypeParam(5)
            };
        }

        /// <summary>
        /// Apply parameters configured on client side
        /// </summary>
        /// <param name="parameterBases">List of configured parameters</param>
        /// <returns>True if case of succeeded configuration</returns>
        protected override bool InternalSetParameters(List<ScriptingParameterBase> parameterBases)
        {
            Series[0].Color = ((SeriesParam)parameterBases[0]).Color;
            Series[0].Thickness = ((SeriesParam)parameterBases[0]).Thickness;

            Series[1].Color = ((SeriesParam)parameterBases[1]).Color;
            Series[1].Thickness = ((SeriesParam)parameterBases[1]).Thickness;

            Series[2].Color = ((SeriesParam)parameterBases[2]).Color;
            Series[2].Thickness = ((SeriesParam)parameterBases[2]).Thickness;

            Period = ((IntParam)parameterBases[3]).Value;
            Deviation = ((IntParam)parameterBases[4]).Value;

            Type = ParsePriceConstants((StringParam)parameterBases[5]);

            DisplayName = String.Format("{0}_{1}_{2}_{3}", Name, Period, Deviation, Type);
            return true;
        }
    }
}