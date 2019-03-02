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
    [DataContract]
    [Serializable]
    public class Tick
    {
        [DataMember]
        public Security Symbol { get; set; }

        [DataMember]
        public string DataFeed { get; set; }

        [DataMember]
        public DateTime Date { get; set; }

        [DataMember]
        public decimal Price { get; set; }

        [DataMember]
        public decimal Volume { get; set; }

        [DataMember]
        public decimal Bid { get; set; }

        [DataMember]
        public long BidSize { get; set; }

        [DataMember]
        public decimal Ask { get; set; }

        [DataMember]
        public long AskSize { get; set; }

        [DataMember]
        public List<MarketLevel2> Level2 { get; set; }
    }
}