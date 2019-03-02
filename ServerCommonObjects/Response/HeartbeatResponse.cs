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
    /// <summary>
    /// heartbeat, generally used to detect sessions's state
    /// </summary>
    [DataContract]
    public class HeartbeatResponse : ResponseMessage
    {
        [DataMember]
        public string Text { get; set; }

        public HeartbeatResponse(string aText)
        {
            this.Text = aText;
        }
    }
}