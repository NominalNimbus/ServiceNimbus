/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using System;
using ServerCommonObjects;
using Server.Interfaces;

namespace Server.Commands.Scripting
{
    internal sealed class SignalDataCommand : CommandBase<SignalDataRequest>
    {
        #region Fields

        private readonly IScriptingWorker _scriptingWorker;

        #endregion // Fields

        #region Constructors

        public SignalDataCommand(ICore core, IPusher pusher, IScriptingWorker scriptingWorker) : base(core, pusher)
        {
            _scriptingWorker = scriptingWorker ?? throw new ArgumentNullException(nameof(scriptingWorker));
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(SignalDataRequest request)
        {
            PushResponse(new SignalDataResponse
            {
                User = request.User,
                Data = _scriptingWorker.GetSignalFiles(request.User.Login)
            });
        }

        #endregion // CommandBase
    }
}
