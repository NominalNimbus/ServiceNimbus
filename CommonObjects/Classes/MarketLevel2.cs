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
    /// <summary>
    /// Market DomLevel 2 data information
    /// </summary>
    [DataContract]
    [Serializable]
    public class MarketLevel2
    {
        [DataMember]
        public int DomLevel { get; set; }
        [DataMember]
        public decimal BidPrice { get; set; }
        [DataMember]
        public decimal BidSize { get; set; }
        [DataMember]
        public decimal AskPrice { get; set; }
        [DataMember]
        public decimal AskSize { get; set; }
        [DataMember]
        public decimal DailyLevel2AskSize { get; set; }
        [DataMember]
        public decimal DailyLevel2BidSize { get; set; }
    }
}