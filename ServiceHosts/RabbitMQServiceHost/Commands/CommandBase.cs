/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using ServerCommonObjects;
using RabbitMQServiceHost.Core;

namespace RabbitMQServiceHost.Commands
{
    internal abstract class CommandBase<T> : IHostCommand where T : class
    {

        #region Properties

        protected HostCore Core { get; }

        #endregion // Properties

        #region Abstract

        protected abstract void ExecuteCommand(string sessionId, T request);

        #endregion // Abstract

        #region Constructors

        protected CommandBase(HostCore core)
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

                ExecuteCommand(sessionId, commandRequest);
            }
            catch (Exception ex)
            {
                Logger.Error("CommandBase.Execute -> ", ex);
            }
        }

        #endregion // ICommand

    }
}