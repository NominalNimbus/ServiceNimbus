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
    internal sealed class ScriptingAlertCommand : CommandBase<ScriptingAlertRequest>
    {
        #region Constructors

        public ScriptingAlertCommand(ICore core, IPusher pusher) : base(core, pusher)
        {
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(ScriptingAlertRequest request)
        {
            var user = Core.GetUser(request.Login);
            if (user == null) return;

            PushResponse(new ScriptingMessageResponse
            {
                User = user,
                ScriptingType = request.Type,
                Message = request.Alerts,
                Id = request.CodeId
            });
        }

        #endregion // CommandBase
    }
}
