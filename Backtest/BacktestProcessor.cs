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

namespace Backtest
{
    public static class BacktestProcessor
    {

        #region Members

        private const int RandMax = 32767;
        private static readonly Random R = new Random((int)DateTime.Now.Ticks);

        #endregion

        #region Public

        public static Results Backtest(List<Trade> trades, decimal price = 0m, decimal slippage = 0m)
        {
            var results = new Results();

            slippage = Math.Max(0m, Math.Min(1m, slippage));
            if (slippage > 0m)
            {
                foreach (var trade in trades)
                {
                    var slip = (decimal)R.Next(RandMax) / RandMax * slippage;
                    var direction = (decimal)R.Next(RandMax) / RandMax * 1m;

                    if (direction > 0.5m)
                        trade.Price += trade.Price * slip;
                    else
                        trade.Price -= trade.Price * slip;
                }
            }

            var tempTrade = new List<Trade>(trades.Count);
            var lastTrade = SignalType.NoTrade;
            var tradeOpen = false;

            foreach (var trade in trades)
            {
                if (trade.Signal != lastTrade)
                {
                    if ((trade.Signal == SignalType.ExitShort || trade.Signal == SignalType.Exit) &&
                        lastTrade == SignalType.Sell)
                    {
                        tempTrade.Add(trade);
                        lastTrade = trade.Signal;
                    }
                    else if ((trade.Signal == SignalType.ExitLong || trade.Signal == SignalType.Exit) &&
                             lastTrade == SignalType.Buy)
                    {
                        tempTrade.Add(trade);
                        lastTrade = trade.Signal;
                    }

                    else if (trade.Signal == SignalType.Buy &&
                             (!tradeOpen || lastTrade == SignalType.Sell || lastTrade == SignalType.NoTrade))
                    {
                        tempTrade.Add(trade);
                        lastTrade = trade.Signal;
                    }
                    else if (trade.Signal == SignalType.Sell &&
                             (!tradeOpen || lastTrade == SignalType.Buy || lastTrade == SignalType.NoTrade))
                    {
                        tempTrade.Add(trade);
                        lastTrade = trade.Signal;
                    }

                    if (trade.Signal == SignalType.Buy || trade.Signal == SignalType.Sell) tradeOpen = true;
                    else tradeOpen = false;
                }
            }

            trades = new List<Trade>(tempTrade);
            tempTrade.Clear();

            var size = trades.Count;
            if (size == 0)
            {
                results.Error = "No trades";
                return results;
            }

            decimal value;
            var pl = new List<double>();
            var plJDates = new List<double>();

            for (var n = 1; n < size; ++n)
            {
                if (trades[n - 1].Signal == SignalType.Buy &&
                    (trades[n].Signal == SignalType.Sell || trades[n].Signal == SignalType.ExitLong ||
                     trades[n].Signal == SignalType.Exit))
                {
                    // Close out of a long position
                    value = (trades[n].Price - trades[n - 1].Price) * trades[n - 1].Quantity;
                    pl.Add((double)value);
                    plJDates.Add(trades[n].Date.Ticks);
                }
                else if (trades[n - 1].Signal == SignalType.Sell &&
                         (trades[n].Signal == SignalType.Buy || trades[n].Signal == SignalType.ExitShort ||
                          trades[n].Signal == SignalType.Exit))
                {
                    // Close out of a short position
                    value = (trades[n - 1].Price - trades[n].Price) * trades[n - 1].Quantity;
                    pl.Add((double)value);
                    plJDates.Add(trades[n].Date.Ticks);
                }
            }

            // Create the trade log (if not compounding, trades may have been removed)
            results.Trades = new List<Trade>(trades);

            // Get current PL for last position if open
            if (price > 0m)
            {
                if (trades[size - 1].Signal == SignalType.Buy)
                {
                    value = (price - trades[size - 1].Price) * trades[size - 1].Quantity;
                    pl.Add((double)value);
                    plJDates.Add(trades[size - 1].Date.Ticks);
                }
                else if (trades[size - 1].Signal == SignalType.Sell)
                {
                    value = (trades[size - 1].Price - price) * trades[size - 1].Quantity;
                    pl.Add((double)value);
                    plJDates.Add(trades[size - 1].Date.Ticks);
                }
            }

            // Calculate the statistics
            var stats = new Statistics();

            // Get sum and total of P&L
            double totalProfit = 0, totalLoss = 0;
            double maxProfit = 0, maxLoss = 0;
            int numProfits = 0, numLosses = 0;
            for (var n = 0; n < pl.Count; ++n)
            {
                if (pl[n] >= 0)
                {
                    totalProfit += pl[n];
                    if (pl[n] > maxProfit) maxProfit = pl[n];
                    numProfits++;
                }
                else if (pl[n] < 0)
                {
                    totalLoss += pl[n];
                    if (pl[n] < maxLoss) maxLoss = pl[n];
                    numLosses++;
                }
            }

            if (numProfits + numLosses == 0)
            {
                results.Error = "No trades";
                return results;
            }

            var monthlyPL = stats.ConvertToMonthly(pl, plJDates.Select(_ => new DateTime((long)_)).ToList());
            results.TotalNumberOfTrades = numProfits + numLosses;
            results.AverageTradesPerMonth = stats.AvgTradesPerMonth;
            results.NumberOfProfitableTrades = numProfits;
            results.NumberOfLosingTrades = numLosses;
            results.TotalProfit = totalProfit;
            results.TotalLoss = totalLoss;
            results.LargestProfit = maxProfit;
            results.LargestLoss = maxLoss;
            results.MaximumDrawDown = Statistics.MaximumDrawdown(pl);
            results.MaximumDrawDownMonteCarlo = Statistics.MaximumDrawdownMonteCarlo(pl);
            results.CompoundMonthlyROR = Statistics.CompoundAnnualizedROR(monthlyPL);
            results.StandardDeviation = Statistics.StandardDeviation(pl);
            results.StandardDeviationAnnualized = Statistics.AnnualizedStandardDeviation(monthlyPL);
            results.DownsideDeviationMar10 = Statistics.DownsideDeviation(monthlyPL, 0.1);
            results.SharpeRatio = Statistics.SharpeRatio(pl, 0.05);
            results.SortinoRatioMAR5 = Statistics.SortinoRatio(monthlyPL, 0.05);
            results.AnnualizedSortinoRatioMAR5 = Statistics.AnnualizedSortinoRatio(monthlyPL, 0.05);
            results.SterlingRatioMAR5 = Statistics.SterlingRatio(monthlyPL, 0.05);
            results.CalmarRatio = Statistics.CalmarRatio(monthlyPL);
            var vami = Statistics.ValueAddedMonthlyIndex(monthlyPL);

            if (totalProfit != 0.0)
                results.PercentProfit = 1 - Math.Abs(totalLoss) / Math.Abs(totalProfit);

            if (vami != 1)
                results.ValueAddedMonthlyIndex = vami / (vami - 1);

            if (numProfits > 0 && totalProfit != 0.0)
                results.RiskRewardRatio = (1 - numLosses / numProfits + (1 - Math.Abs(totalLoss) / totalProfit)) / 2;

            return results;
        }

        #endregion

    }
}