/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.Collections.Generic;
using CommonObjects;

namespace ServerCommonObjects
{
    /// <summary>
    /// list of subscribers for Level1 ticks
    /// </summary>
    public class Level1Subscribers
    {
        /// <summary>
        /// latest tick
        /// </summary>
        public Tick Tick { get; set; }
        /// <summary>
        /// list of subscribers identified by unique ID, generally session ID
        /// </summary>
        public List<string> Subscribers { get; set; }
    }
}