/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.Collections.Generic;
using CommonObjects;
using CommonObjects.Classes;

namespace Scripting
{
    public interface IScripting
    {
        /// <summary>
        /// ID
        /// </summary>
        string ID { get; }

        /// <summary>
        /// name/title
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Some ID (eg. login) of owner
        /// </summary>
        string Owner { get; set; }

        /// <summary>
        /// Original user defined parameters
        /// </summary>
        List<ScriptingParameterBase> OrigParameters { get; }

        /// <summary>
        /// Show alert with message
        /// </summary>
        void Alert(string message);

        /// <summary>
        /// Return list of non showed alerts. Calling of this function clear list of alerts.
        /// </summary>
        /// <returns></returns>
        List<string> GetActualAlerts();

        /// <summary>
        /// Get list of parameters for configuration on client side
        /// </summary>
        List<ScriptingParameterBase> GetParameters();

        /// <summary>
        /// Apply parameters configured on client side
        /// </summary>
        /// <param name="parameterBases">List of configured parameters</param>
        /// <returns>True if case of succeeded configuration</returns>
        bool SetParameters(List<ScriptingParameterBase> parameterBases);
    }
}
