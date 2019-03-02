/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Runtime.Serialization;

namespace ServerCommonObjects
{
    [DataContract]
    public class ScriptingReportRequest : RequestMessage
    {
        [DataMember]
        public string Id { get; set; }
        [DataMember]
        public string SignalName { get; set; }
        [DataMember]
        public DateTime FromTime { get; set; }
        [DataMember]
        public DateTime ToTime { get; set; }

        public ScriptingReportRequest(string id, string signalName, DateTime fromTime, DateTime toTime)
        {
            Id = id;
            SignalName = signalName;
            FromTime = fromTime;
            ToTime = toTime;
        }
    }
}
