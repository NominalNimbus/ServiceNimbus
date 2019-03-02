/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using ServerCommonObjects;
using Server.Interfaces;

namespace Server.Commands.Scripting
{
    internal sealed class ScriptingCommand : CommandBase<ScriptingRequest>
    {
        #region Constructors
        public ScriptingCommand(ICore core, IPusher pusher) : base(core, pusher)
        {
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(ScriptingRequest request)
        {
            PushResponse(new ScriptingResponse
            {
                User = request.User,
                Indicators = Core.GetAllIndicators(request.User),
                DefaultIndicators = Core.GetDefaultIndicators(),
                Signals = Core.GetAllSignals(request.User),
                WorkingSignals = Core.GetWorkingSignalsAndUpdateUserInfo(request.User),
                CommonObjectsDllVersion = CommonHelper.GetFileVersion("CommonObjects.dll"),
                ScriptingDllVersion = CommonHelper.GetFileVersion("Scripting.dll"),
                BacktesterDllVersion = CommonHelper.GetFileVersion("Backtest.dll"),
                CommonObjectsDll = CommonHelper.ReadFromFileAndCompress("CommonObjects.dll"),
                ScriptingDll = CommonHelper.ReadFromFileAndCompress("Scripting.dll"),
                BacktesterDll = CommonHelper.ReadFromFileAndCompress("Backtest.dll")
            });
        }

        #endregion // CommandBase
    }
}
