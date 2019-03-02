/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.Collections.Generic;
using System.Runtime.Serialization;
using CommonObjects;

namespace ServerCommonObjects
{
    [DataContract]
    public class ScriptingDataSavedResponse : ResponseMessage
    {
        [DataMember]
        public List<ScriptingParameterBase> Parameters { get; set; }

        [DataMember]
        public string RequestID { get; set; }

        [DataMember]
        public string Path { get; set; }  //'portfolio/strategy/signal' for signals or 'indicator' for indicators

        [DataMember]
        public string Error { get; set; }

        [DataMember]
        public ScriptingType ScriptingType { get; set; }
    }
}