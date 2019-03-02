/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CommonObjects
{
    [Serializable]
    [DataContract]
    public class BacktestResults
    {
        /// <summary>
        /// Name of a signal that was backtested
        /// </summary>
        [DataMember]
        public string SignalName { get; set; }

        /// <summary>
        /// Number of test run in backtest collection (1-based)
        /// </summary>
        [DataMember]
        public int Index { get; set; }

        /// <summary>
        /// Timestamp showing backtest execution time stamp (UTC)
        /// </summary>
        [DataMember]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Slot number that was used for backtest
        /// </summary>
        [DataMember]
        public int Slot { get; set; }

        /// <summary>
        /// Start time of backtested data
        /// </summary>
        [DataMember]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End time of backtested data
        /// </summary>
        [DataMember]
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Parameters that have been used during this backtest
        /// </summary>
        [DataMember]
        public List<string> Parameters { get; set; }

        /// <summary>
        /// Total backtest progress (in %)
        /// </summary>
        [DataMember]
        public float TotalProgress { get; set; }

        /// <summary>
        /// Backtest summary figures (per symbol)
        /// </summary>
        [DataMember]
        public List<BacktestSummary> Summaries { get; set; }


        public BacktestResults()
        {
            Timestamp = DateTime.UtcNow;
            Parameters = new List<string>();
            Summaries = new List<BacktestSummary>();
        }

        public BacktestResults(string name, int slot) : this()
        {
            SignalName = name;
            Slot = slot;
        }
    }
}
