/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.Collections.Generic;
using System.Runtime.Serialization;
using ServerCommonObjects.Classes;

namespace ServerCommonObjects
{
    [DataContract]
    public class StartedSignalExecutionRequest : RequestMessage
    {
        [DataMember]
        public string UserName { get; set; }
        [DataMember]
        public Signal Signal { get; set; }
        [DataMember]
        public string SignalName { get; set; }
        [DataMember]
        public List<string> Alerts { get; set; }
    }
}
