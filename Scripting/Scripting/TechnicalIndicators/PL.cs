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
    public class PL : IndicatorBase
    {
        private Selection _selection;
        private IDataProvider _dataProvider;
        private List<TradeSignal> _trades;
        private decimal _realizedPL;
        private decimal? _dummyClosingPrice;

        public PL()
        {
            Name = "PL";
            Series.Add(new Series("Main"));
        }

        protected override bool InternalInit(Selection selection, IDataProvider dataProvider)
        {
            _selection = selection;
            _dataProvider = dataProvider;
            IsOverlay = false;
            Series.ForEach(s => s.Values.Clear());

            return InitialCalculation();
        }

        protected override int InternalCalculate(IEnumerable<Bar> bars = null)
        {
            var history = bars == null ? _dataProvider.GetBars(_selection) : new List<Bar>(bars);
            if (history == null || history.Count < 3 || _trades == null || _trades.Count == 0)
                return 0;

            if (!Series[0].Values.Any())
            {
                InitialCalculation(bars);
                return _trades.Count;
            }

            if (_dummyClosingPrice == null)
            {
                //simply extend the PL series on new bar
                if (Series[0].Length < history.Count)
                {
                    var lastValue = Series[0].Values.Last().Value;
                    for (int i = Series[0].Length; i < history.Count; i++)
                        Series[0].AppendOrUpdate(history[i].Date, lastValue);
                }
            }
            else  //need to refresh last (dummy) closing price 
            {
                //extend the line on new bar(s)
                if (Series[0].Length < history.Count)
                {
                    var last = Series[0].Values.Count - 1;
                    Series[0].AppendOrUpdate(Series[0].Values[last].Date, Series[0].Values[last - 1].Value);
                    var lastValue = Series[0].Values[last].Value;
                    for (int i = Series[0].Length; i < history.Count; i++)
                        Series[0].AppendOrUpdate(history[i].Date, lastValue);
                }

                //recalculate latest value
                var lastBar = history[history.Count - 1];
                _dummyClosingPrice = GetPrice(lastBar, PriceConstants.CLOSE);
                var upl = GetUnrealizedPL(_trades.Last(), _dummyClosingPrice.Value, _realizedPL);
                Series[0].AppendOrUpdate(lastBar.Date, (double)upl);
            }

            return 1;
        }

        private bool InitialCalculation(IEnumerable<Bar> bars = null)
        {
            var history = bars == null ? _dataProvider.GetBars(_selection) : new List<Bar>(bars);
            if (history == null || history.Count < 3 || _trades == null || _trades.Count == 0)
                return false;

            _realizedPL = 0M;
            for (int i = 0, j = 0; i < history.Count; i++)
            {
                if (j < _trades.Count && history[i].Date >= _trades[j].Time)
                {
                    _realizedPL += _trades[j].Side == Side.Sell ? _trades[j].Price : -_trades[j].Price;
                    j++;
                }

                Series[0].AppendOrUpdate(history[i].Date, (double)_realizedPL);
            }

            //assign dummy closing price for last (open) position
            bool isLastPosOpen = _trades.Count % 2 != 0 || _trades[_trades.Count - 1].Side == _trades[_trades.Count - 2].Side;
            var lastBar = history[history.Count - 1];
            var lastTrade = _trades[_trades.Count - 1];
            if (isLastPosOpen && lastTrade.Time < lastBar.Date)
            {
                _dummyClosingPrice = GetPrice(lastBar, PriceConstants.CLOSE);
                var upl = GetUnrealizedPL(lastTrade, _dummyClosingPrice.Value, _realizedPL);
                Series[0].AppendOrUpdate(lastBar.Date, (double)upl);
            }

            return true;
        }

        private static decimal GetUnrealizedPL(TradeSignal lastOpeningTrade, decimal price, decimal currentPL)
        {
            return lastOpeningTrade.Side == Side.Buy ? currentPL + price : currentPL - price;
        }
        
        protected override List<ScriptingParameterBase> InternalGetParameters()
        {
            return new List<ScriptingParameterBase>
            {
                new SeriesParam("PL", "Series parameters", 0) { Color = Colors.Red, Thickness = 2 },
                new StringParam("Trades", "Serialized trades", 3)
            };
        }

        protected override bool InternalSetParameters(List<ScriptingParameterBase> parameterBases)
        {
            DisplayName = Name;
            Series[0].Color = ((SeriesParam)parameterBases[0]).Color;
            Series[0].Thickness = ((SeriesParam)parameterBases[0]).Thickness;

            var str = ((StringParam)parameterBases[1]).Value;
            if (String.IsNullOrWhiteSpace(str))
                return false;

            try
            {
                var bytes = Compression.Decompress(Convert.FromBase64String(str));
                str = System.Text.Encoding.UTF8.GetString(bytes);
            
                if (String.IsNullOrWhiteSpace(str) || !str.Contains('|'))
                    return false;

                //parse trades from supplied string
                string[] parts = null;
                string[] items = str.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                _trades = new List<TradeSignal>(items.Length);
                for (int i = 0; i < items.Length; i++)
                {
                    parts = items[i].Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        var price = Decimal.Parse(parts[1]);
                        if (price != 0m)
                        {
                            _trades.Add(new TradeSignal
                            {
                                Instrument = _selection,
                                Time = DateTime.ParseExact(parts[0], "yyyy-MM-dd HH:mm:ss.fff", 
                                    System.Globalization.CultureInfo.InvariantCulture),
                                Price = Math.Abs(price),
                                Side = price < 0M ? Side.Sell : Side.Buy,
                                Quantity = parts.Length > 2 ? Decimal.Parse(parts[2]) : 1m
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.TraceError("Failed to parse trade details for PL indicator: " + e.Message);
            }

            return true;
        }
    }
}
