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

namespace ServerCommonObjects.Classes
{
    [DataContract]
    public class Indicator : ScriptingBase
    {
        [DataMember]
        public bool IsOverlay { get; set; }

        [DataMember]
        public string DisplayName { get; set; }

        [DataMember]
        public List<Series> Series { get; set; }
    }
}
