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
    public enum DrawShapeStyle
    {
        [EnumMember(Value = "DRAW_LINE")]
        DRAW_LINE = 0, //Drawing line.
        [EnumMember(Value = "DRAW_SECTION")]
        DRAW_SECTION = 1, //Drawing sections.
        [EnumMember(Value = "DRAW_HISTOGRAM")]
        DRAW_HISTOGRAM = 2, //Drawing histogram.
        [EnumMember(Value = "DRAW_ARROW")]
        DRAW_ARROW = 3, //Drawing arrows (symbols).
        [EnumMember(Value = "DRAW_ZIGZAG")]
        DRAW_ZIGZAG = 4, //Drawing sections between even and odd indicator buffers.
        [EnumMember(Value = "DRAW_NONE")]
        DRAW_NONE = 12
        //No drawing.                                                                                                         
    }
}