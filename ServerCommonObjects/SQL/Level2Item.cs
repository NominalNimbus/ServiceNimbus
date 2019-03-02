/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;

namespace ServerCommonObjects.SQL
{
    public class Level2Item
    {
        public DateTime DateTime { get; set; }

        public int DomLevel { get; set; }

        public decimal SecurityID { get; set; }

        public decimal BidPrice { get; set; }

        public double BidSize { get; set; }

        public decimal AskPrice { get; set; }

        public double AskSize { get; set; }
    }
}
