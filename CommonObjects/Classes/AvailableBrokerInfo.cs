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
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CommonObjects
{
    [DataContract]
    [Serializable]
    public class AvailableBrokerInfo
    {
        [DataMember]
        public string BrokerName { get; set; }
        [DataMember]
        public string DataFeedName { get; set; }
        [DataMember]
        public List<string> Accounts { get; set; }
        [DataMember]
        public string Url { get; set; }
        [DataMember]
        public BrokerType BrokerType { get; set; }
        

        public AvailableBrokerInfo()
        {

        }

        public AvailableBrokerInfo(string broker, string dataFeed, List<string> accounts, string url, BrokerType brokerType)
        {
            BrokerName = broker;
            DataFeedName = dataFeed;
            Accounts = accounts;
            Url = url;
            BrokerType = brokerType;
        }

        public static AvailableBrokerInfo CreateLiveBroker(string brokerName, string dataFeed, string url)
            => new AvailableBrokerInfo(brokerName, dataFeed, new List<string>(), url, BrokerType.Live);

        public static AvailableBrokerInfo CreateSimulatedBroker(string brokerName, List<string> accounts)
            => new AvailableBrokerInfo(brokerName, string.Empty, accounts, string.Empty, BrokerType.Simulated);

    }
}
