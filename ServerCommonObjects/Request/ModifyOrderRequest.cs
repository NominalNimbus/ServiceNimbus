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
    [DataContract]
    public class ModifyOrderRequest : RequestMessage
    {
        [DataMember]
        public string OrderID { get; set; }
        [DataMember]
        public string AccountId { get; set; }
        [DataMember]
        public decimal? SL { get; set; }
        [DataMember]
        public decimal? TP { get; set; }
        [DataMember]
        public bool IsServerSide { get; set; }
    }
}