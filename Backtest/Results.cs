/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.Collections.Generic;

namespace Backtest
{
    public class Results
    {
        public string Error { get; internal set; } = "";
        
        public List<Trade> Trades { get; internal set; } = new List<Trade>();
        
        public int TotalNumberOfTrades { get; internal set; }
        
        public int AverageTradesPerMonth { get; internal set; }
        
        public int NumberOfProfitableTrades { get; internal set; }
        
        public int NumberOfLosingTrades { get; internal set; }
        
        public double TotalProfit { get; internal set; }
        
        public double TotalLoss { get; internal set; }
        
        public double PercentProfit { get; internal set; }
        
        public double LargestProfit { get; internal set; }
        
        public double LargestLoss { get; internal set; }
        
        public double MaximumDrawDown { get; internal set; }
        
        public double MaximumDrawDownMonteCarlo { get; internal set; }
        
        public double CompoundMonthlyROR { get; internal set; }
        
        public double StandardDeviation { get; internal set; }
        
        public double StandardDeviationAnnualized { get; internal set; }
        
        public double DownsideDeviationMar10 { get; internal set; }
        
        public double ValueAddedMonthlyIndex { get; internal set; }
        
        public double SharpeRatio { get; internal set; }
        
        public double SortinoRatioMAR5 { get; internal set; }
        
        public double AnnualizedSortinoRatioMAR5 { get; internal set; }
        
        public double SterlingRatioMAR5 { get; internal set; }
        
        public double CalmarRatio { get; internal set; }
        
        public double RiskRewardRatio { get; internal set; }
    }
}
