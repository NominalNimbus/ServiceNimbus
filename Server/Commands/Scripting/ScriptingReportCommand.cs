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
    internal sealed class ScriptingReportCommand : CommandBase<ScriptingReportRequest>
    {
        #region Constructors

        public ScriptingReportCommand(ICore core, IPusher pusher) : base(core, pusher)
        {
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(ScriptingReportRequest request)
        {
            var report = Core.GetCodeReport(request.User.Login, request.SignalName, request.FromTime, request.ToTime);
            var aResponse = new ScriptingReportResponse()
            {
                Id = request.Id,
                User = request.User,
                ReportFields = report
            };

            PushResponse(aResponse);
        }

        #endregion // CommandBase
    }
}
