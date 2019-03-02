/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using ServerCommonObjects;
using System;

namespace WebSocketServiceHost
{
    internal abstract class CommandBase<T> : IHostCommand where T : class
    {
        #region Properties

        protected ICoreServiceHost Core { get; }

        #endregion // Properties

        #region Abstract

        public abstract void ExecuteComamnd(string sessionId, T request);

        #endregion // Abstract

        #region Constructors

        public CommandBase(ICoreServiceHost core)
        {
            Core = core ?? throw new ArgumentNullException(nameof(core));
        }

        #endregion // Constructors

        #region ICommand

        public void Execute(string sessionId, RequestMessage request)
        {
            if (request == null || string.IsNullOrEmpty(sessionId))
                return;

            try
            {
                request.User = Core.MessageManager.GetUserInfo(sessionId);
                var commandRequest = request as T;
                if (commandRequest == null)
                    return;

                ExecuteComamnd(sessionId, commandRequest);
            }
            catch (Exception ex)
            {
                Logger.Error("CommandBase.Execute -> ", ex);
            }
        }

        #endregion // ICommand
    }
}
