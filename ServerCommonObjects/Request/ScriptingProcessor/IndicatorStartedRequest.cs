/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.Runtime.Serialization;
using ServerCommonObjects.Classes;

namespace ServerCommonObjects
{
    public class IndicatorStartedRequest : RequestMessage
    {
        [DataMember] public string RequestID { get; set; }
        [DataMember] public string Login { get; set; }
        [DataMember] public string IndicatorName { get; set; }
        [DataMember] public ScriptingBase Indicator { get; set; }
    }
}