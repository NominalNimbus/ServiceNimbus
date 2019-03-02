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
using CommonObjects.Classes;

namespace ServerCommonObjects.Classes
{
    [DataContract]
    public abstract class ScriptingBase
    {
        [DataMember]
        public string ID { get; set; }

        [DataMember]
        public string Name { get; set; }
       
        [DataMember]
        public List<ScriptingParameterBase> Parameters { get; set; }
    }
}