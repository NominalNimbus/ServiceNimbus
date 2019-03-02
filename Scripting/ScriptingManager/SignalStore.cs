/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using ServerCommonObjects;
using ServerCommonObjects.Classes;
using System;

namespace ScriptingManager
{
    internal sealed class SignalStore : IEntityStore<Signal>
    {
        #region Fields

        private readonly IFileManager _fileManager;

        #endregion // Fields

        #region Constructors

        public SignalStore(IFileManager fileManager)
        {
            _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
        }

        #endregion // Constructors

        #region IEntetyStore

        public void AddEntity(string path, Signal entity)
        {
            var content = entity.ToJson();
            if (string.IsNullOrEmpty(content))
            {
                Logger.Info("SignalStore.AddEntety -> content is empty.");
                return;
            }

            _fileManager.SaveContent(path, content);
        }

        public Signal GetEntity(string path)
        {
            var entity = default(Signal);
            var content = _fileManager.LoadContent(path);
            if (string.IsNullOrEmpty(content))
            {
                Logger.Info("SignalStore.GetEntity -> content is empty.");
                return entity;
            }

            entity = content.FromJson<Signal>();
            return entity;
        }

        public void RemoveEntity(string path)
            => _fileManager.DeleteFile(path);

        public void UpdateEntity(string path, Signal entity)
        {
            RemoveEntity(path);
            AddEntity(path, entity);
        }

        #endregion // IEntetyStore
    }
}
