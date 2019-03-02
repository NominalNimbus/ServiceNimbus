/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using System;
using CommonObjects.Classes;
using ServerCommonObjects;
using Server.Interfaces;

namespace Server.Commands.Scripting
{
    internal sealed class RemoveScriptingInstanceCommand : CommandBase<RemoveScriptingInstanceRequest>
    {
        #region Fields

        private readonly IScriptingWorker _scriptingWorker;

        #endregion // Fields

        #region Constructors

        public RemoveScriptingInstanceCommand(ICore core, IPusher pusher, IScriptingWorker scriptingWorker) : base(core, pusher)
        {
            _scriptingWorker = scriptingWorker ?? throw new ArgumentNullException(nameof(scriptingWorker));
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(RemoveScriptingInstanceRequest request)
        {
            Helpers.ProceedThroughQueue(_scriptingWorker.RemoveScriptingInstanceRequests, request, RemoveCodeInstance);
        }

        #endregion // CommandBase

        #region Private

        private void RemoveCodeInstance(RemoveScriptingInstanceRequest request)
        {
            if (request.RemoveProject)
            {
                var error = request.ScriptingType == ScriptingType.Indicator
                    ? Core.RemoveCustomIndicatorData(request.User, request.Name)
                    : Core.RemoveSignalData(request.User, request.Name);

                PushResponse(new ScriptingDataRemoveResponse
                {
                    User = request.User,
                    ScriptingType = request.ScriptingType,
                    Error = error,
                    Name = request.Name
                });
            }
            else
            {
                if (request.ScriptingType == ScriptingType.Indicator)
                    Core.RemoveUserIndicator(request.Name, request.User);
                else
                    Core.RemoveSignal(request.Name, request.User);
            }
        }

        #endregion // Private
    }
}
