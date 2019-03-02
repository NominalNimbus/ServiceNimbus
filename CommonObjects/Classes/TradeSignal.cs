/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace CommonObjects
{
    [Serializable]
    [DataContract]
    [DebuggerDisplay("${Side} {Instrument.Symbol, nq} for ${Price} at {Time.ToString(\"HH:mm:ss\"), nq}")]
    public class TradeSignal
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public Selection Instrument { get; set; }

        [DataMember]
        public DateTime Time { get; set; }

        [DataMember]
        public decimal Price { get; set; }

        [DataMember]
        public decimal Quantity { get; set; }

        [DataMember]
        public Side Side { get; set; }

        [DataMember]
        public OrderType TradeType { get; set; }

        [DataMember]
        public TimeInForce TimeInForce { get; set; }

        [DataMember]
        public decimal? SLOffset { get; set; }

        [DataMember]
        public decimal? TPOffset { get; set; }

        public TradeSignal()
        {
            Quantity = 1M;
        }
    }
}
