/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.IO;

namespace ServerCommonObjects
{
    public sealed class FileManager : IFileManager
    {
        #region IFileManager

        public void DeleteFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return;

            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                Logger.Error("FileManager.DeleteFile -> ", ex);
            }
        }

        public string LoadContent(string path)
        {
            var content = string.Empty;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return content;

            try
            {
                content = File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                Logger.Error("FileManager.LoadContent -> ", ex);
            }

            return content;
        }

        public void SaveContent(string path, string content)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(content))
                return;

            var directoryPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(directoryPath))
                return;

            try
            {
                if (File.Exists(path))
                    return;

                File.WriteAllText(path, content);
            }
            catch (Exception ex)
            {
                Logger.Error("FileManager.SaveContent -> ", ex);
            }
        }

        #endregion // IFileManager
    }
}
