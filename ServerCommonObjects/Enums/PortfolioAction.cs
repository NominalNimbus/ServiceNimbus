/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.Runtime.Serialization;

namespace ServerCommonObjects
{
    [DataContract]
    public enum PortfolioAction
    {
        [EnumMember(Value = "Add")]
        Add,
        [EnumMember(Value = "Remove")]
        Remove,
        [EnumMember(Value = "Edit")]
        Edit,
    }
}