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
    [DataContract]
    [Serializable]
    public enum DrawStyle
    {
        [EnumMember(Value = "STYLE_SOLID")]
        STYLE_SOLID = 0, //The pen is solid.
        [EnumMember(Value = "STYLE_DASH")]
        STYLE_DASH = 1, //The pen is dashed.
        [EnumMember(Value = "STYLE_DOT")]
        STYLE_DOT = 2, //The pen is dotted.
        [EnumMember(Value = "STYLE_DASHDOT")]
        STYLE_DASHDOT = 3, //The pen has alternating dashes and dots.
        [EnumMember(Value = "STYLE_DASHDOTDOT")]
        STYLE_DASHDOTDOT = 4 //The pen has alternating dashes and double dots.
    }
}