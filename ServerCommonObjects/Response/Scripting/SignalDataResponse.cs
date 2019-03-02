/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServerCommonObjects
{
    [DataContract]
    public class SignalDataResponse : ResponseMessage
    {
        [DataMember] 
        public Dictionary<string, byte[]> Data { get; set; }  //file path -> data
    }
}