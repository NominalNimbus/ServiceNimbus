/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.Collections.Generic;
using ServerCommonObjects.Interfaces;

namespace ServerCommonObjects.Managers
{
    public class JSONSettingsManager : ISettingsManager
    {
        public IFileManager FileManager { get; }

        public JSONSettingsManager(IFileManager fileManager) =>
            FileManager = fileManager;

        public Dictionary<string, object> Load(string path)
        {
            var content = FileManager.LoadContent(path);
            return content.FromJson<Dictionary<string, object>>();
        }

        public void Save(Dictionary<string, object> settings, string path)
        {
            var content = settings.ToJson();
            FileManager.SaveContent(path, content);
        }
    }
}
