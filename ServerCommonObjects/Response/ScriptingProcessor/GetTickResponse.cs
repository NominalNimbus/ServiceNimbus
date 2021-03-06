﻿/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using CommonObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ServerCommonObjects
{
    [DataContract]
    public class GetTickResponse : ResponseMessage
    {
        [DataMember]
        public long Id { get; set; }
        [DataMember]
        public Tick Tick { get; set; }
    }
}
