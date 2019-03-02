/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.Collections.Generic;

namespace ServerCommonObjects
{
    /// <summary>
    /// Interface for UI control to manage data feeder parameters
    /// </summary>
    public interface ILogonControl
    {
        /// <summary>
        /// get/set data feeder parameters
        /// </summary>
        Dictionary<string, object> Settings { get; set; }
        /// <summary>
        /// validate data feeder parameters
        /// </summary>
        void ValidateSettings();
    }
}
