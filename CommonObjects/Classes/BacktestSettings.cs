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
    public class BacktestSettings
    {
        /// <summary>
        /// Start date/time for data to run backtest on
        /// </summary>
        [DataMember]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date/time for data to run backtest on
        /// </summary>
        [DataMember]
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Number of bars for data to run backtest on
        /// </summary>
        [DataMember]
        public int BarsBack { get; set; }

        /// <summary>
        /// Historical data (bars) to use during backtest (optional)
        /// </summary>
        [DataMember]
        public Dictionary<Selection, List<Bar>> BarData { get; set; }

        //strategy settings
        [DataMember]
        public decimal InitialBalance { get; set; }

        [DataMember]
        public decimal Risk { get; set; }

        [DataMember]
        public decimal TransactionCosts { get; set; }
    }
}
