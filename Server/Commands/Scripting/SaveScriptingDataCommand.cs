/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using CommonObjects;
using CommonObjects.Classes;
using ServerCommonObjects;
using Server.Interfaces;

namespace Server.Commands.Scripting
{
    internal sealed class SaveScriptingDataCommand : CommandBase<SaveScriptingDataRequest>
    {
        #region Fields

        private readonly IScriptingWorker _scrptingWorker;

        #endregion // Fields

        #region Constructors

        public SaveScriptingDataCommand(ICore core, IPusher pusher, IScriptingWorker scriptingWorker) : base(core, pusher)
        {
            _scrptingWorker = scriptingWorker ?? throw new ArgumentNullException(nameof(scriptingWorker));
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(SaveScriptingDataRequest request)
        {
            Helpers.ProceedThroughQueue(_scrptingWorker.SaveScriptingDataRequestMessages, request, SaveUCData);
        }

        #endregion // CommandBase

        #region Private

        private void SaveUCData(SaveScriptingDataRequest request)
        {
            var res = request.ScriptingType == ScriptingType.Indicator
                ? Core.ValidateAndSaveCustomIndicator(request.User, request.Path, request.Files, out var error)
                : Core.ValidateAndSaveSignal(request.User, request.Path, request.Files, out error)?.Parameters;

            PushResponse(new ScriptingDataSavedResponse
            {
                User = request.User,
                Path = request.Path,
                RequestID = request.RequestID,
                Error = error,
                ScriptingType = request.ScriptingType,
                Parameters = res ?? new List<ScriptingParameterBase>(0)
            });
        }

        #endregion // Private
    }
}
