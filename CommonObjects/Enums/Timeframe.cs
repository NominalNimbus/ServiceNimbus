/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Runtime.Serialization;

namespace CommonObjects
{
    /// <summary>
    /// represents available periodicities in historical bars request
    /// </summary>
    [Serializable]
    [DataContract]
    public enum Timeframe
    {
        [EnumMember(Value = "Tick")]
        Tick = -1,
        [EnumMember(Value = "Minute")]
        Minute = 0,
        [EnumMember(Value = "Hour")]
        Hour,
        [EnumMember(Value = "Day")]
        Day,
        [EnumMember(Value = "Month")]
        Month
    }
}