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
    public enum PriceType
    {
        [EnumMember(Value = "Unspecified")]
        Unspecified,
        [EnumMember(Value = "Bid")]
        Bid,
        [EnumMember(Value = "Ask")]
        Ask
    }
}