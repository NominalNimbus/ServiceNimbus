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
using ServerCommonObjects.Classes;

namespace ServerCommonObjects
{
    [DataContract]
    public class ScriptingResponse : ResponseMessage
    {
        [DataMember] 
        public Dictionary<string, List<ScriptingParameterBase>> Indicators { get; set; }

        [DataMember]
        public List<Signal> Signals { get; set; }

        [DataMember]
        public List<Signal> WorkingSignals { get; set; }

        [DataMember]
        public List<string> DefaultIndicators { get; set; }

        [DataMember]
        public string CommonObjectsDllVersion { get; set; }

        [DataMember]
        public string ScriptingDllVersion { get; set; }

        [DataMember]
        public string BacktesterDllVersion { get; set; }

        [DataMember]
        public byte[] CommonObjectsDll { get; set; }

        [DataMember]
        public byte[] ScriptingDll { get; set; }

        [DataMember]
        public byte[] BacktesterDll { get; set; }
    }
}