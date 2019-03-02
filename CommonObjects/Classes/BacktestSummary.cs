/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Runtime.Serialization;

namespace CommonObjects
{
    [Serializable]
    [DataContract]
    public class BacktestSummary
    {
        [DataMember]
        public Selection Selection { get; set; }

        [DataMember]
        public int NumberOfTradeSignals { get; set; }

        [DataMember]
        public int NumberOfTrades { get; set; }

        [DataMember]
        public int NumberOfProfitableTrades { get; set; }

        [DataMember]
        public int NumberOfLosingTrades { get; set; }

        [DataMember]
        public double TotalProfit { get; set; }

        [DataMember]
        public double TotalLoss { get; set; }

        [DataMember]
        public double PercentProfit { get; set; }

        [DataMember]
        public double LargestProfit { get; set; }

        [DataMember]
        public double LargestLoss { get; set; }

        [DataMember]
        public double MaximumDrawDown { get; set; }

        [DataMember]
        public double MaximumDrawDownMonteCarlo { get; set; }

        [DataMember]
        public double CompoundMonthlyROR { get; set; }

        [DataMember]
        public double StandardDeviation { get; set; }

        [DataMember]
        public double StandardDeviationAnnualized { get; set; }

        [DataMember]
        public double DownsideDeviationMar10 { get; set; }

        [DataMember]
        public double ValueAddedMonthlyIndex { get; set; }

        [DataMember]
        public double SharpeRatio { get; set; }

        [DataMember]
        public double SortinoRatioMAR5 { get; set; }

        [DataMember]
        public double AnnualizedSortinoRatioMAR5 { get; set; }

        [DataMember]
        public double SterlingRatioMAR5 { get; set; }

        [DataMember]
        public double CalmarRatio { get; set; }

        [DataMember]
        public double RiskRewardRatio { get; set; }

        [DataMember]
        public byte[] TradesCompressed { get; set; }

        public BacktestSummary()
        {
        }

        public BacktestSummary(Selection instrument)
        {
            Selection = (Selection)instrument.Clone();
        }
    }
}
