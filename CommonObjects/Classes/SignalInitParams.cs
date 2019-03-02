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
    public class SignalInitParams
    {
        [DataMember]
        public string FullName { get; set; }

        [DataMember]
        public StrategyParams StrategyParameters { get; set; }

        [DataMember]
        public List<ScriptingParameterBase> Parameters { get; set; }

        [DataMember]
        public List<Selection> Selections { get; set; }

        [DataMember]
        public BacktestSettings BacktestSettings { get; set; }

        [DataMember]
        public SignalState State { get; set; }

        public SignalInitParams()
        {
            Parameters = new List<ScriptingParameterBase>();
            Selections = new List<Selection>();
        }
    }
}
