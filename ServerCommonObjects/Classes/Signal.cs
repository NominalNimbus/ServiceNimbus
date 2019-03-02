/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.Runtime.Serialization;
using CommonObjects;

namespace ServerCommonObjects.Classes
{
    [DataContract]
    public class Signal : ScriptingBase
    {
        [DataMember]
        public SignalState State { get; set; }

        [DataMember]
        public System.Collections.Generic.List<Selection> Selections { get; set; }
    }
}