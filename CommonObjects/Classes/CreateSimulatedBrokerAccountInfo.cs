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
    public class CreateSimulatedBrokerAccountInfo
    {
        [DataMember]
        public string BrokerName { get; set; }

        [DataMember]
        public string AccountName { get; set; }

        [DataMember]
        public string Currency { get; set; }

        [DataMember]
        public int Ballance { get; set; }
    }
}
