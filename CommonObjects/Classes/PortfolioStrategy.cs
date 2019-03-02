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
    [DataContract]
    [Serializable]
    public class PortfolioStrategy
    {
        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string[] Signals { get; set; }

        [DataMember]
        public string[] DataFeeds { get; set; }

        [DataMember]
        public decimal ExposedBalance { get; set; }

        public PortfolioStrategy()
        {
        }

        public PortfolioStrategy(int id, string name)
        {
            ID = id;
            Name = name;
        }
    }
}
