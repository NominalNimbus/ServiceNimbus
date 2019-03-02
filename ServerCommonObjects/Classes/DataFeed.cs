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

namespace ServerCommonObjects
{
    /// <summary>
    /// represents data feeder info
    /// </summary>
    [DataContract]
    public class DataFeed
    {
        /// <summary>
        /// feeder name
        /// </summary>
        [DataMember]
        public string Name { get; set; }
        /// <summary>
        /// list of symbols
        /// </summary>
        [DataMember]
        public List<Security> Symbols;

        [DataMember]
        public bool IsStarted { get; set; }
    }
}