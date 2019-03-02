/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCommonObjects.Classes
{
    public class LocalConfigHelper
    {
        private Configuration Config { get; } 

        public LocalConfigHelper(string location, string libraryName)
        {
            Config = ConfigurationManager.OpenExeConfiguration(location);
            if (!Config.HasFile)
                throw new FileNotFoundException($"{libraryName}. Config file doesnt exist");
        }

        public string GetString(string propertName) =>
            GetString(propertName, propertName);

        public string GetString(string propertName, string key) =>
            SetValue.String(propertName, Config.AppSettings.Settings[key]?.Value);

        public int GetInt(string propertName) =>
            GetInt(propertName, propertName);

        public int GetInt(string propertName, string key) =>
            SetValue.Int(propertName, Config.AppSettings.Settings[key]?.Value);
    }
}
