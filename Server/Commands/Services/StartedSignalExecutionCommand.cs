/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using ServerCommonObjects;
using Server.Interfaces;

namespace Server.Commands.Services
{
    internal sealed class StartedSignalExecutionCommand : CommandBase<StartedSignalExecutionRequest>
    {
        #region Constructors

        public StartedSignalExecutionCommand(ICore core, IPusher pusher) : base(core, pusher)
        {
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(StartedSignalExecutionRequest request)
        {
            var user = Core.GetUser(request.UserName);
            Core.SignalStarted(user, request.Signal, request.Processor);
            if (request.Alerts.Count > 0)
            {
                PushResponse(new ScriptingMessageResponse
                {
                    User = user,
                    ScriptingType = ScriptingType.Signal,
                    Message = request.Alerts,
                    Id = request.Signal.ID
                });
            }

            PushResponse(new ScriptingInstanceCreatedResponse
            {
                User = user,
                RequestID = request.SignalName,
                Script = request.Signal,
                ScriptingType = ScriptingType.Signal
            });
        }

        #endregion // CommandBase
    }
}
