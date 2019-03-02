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
    internal sealed class UpdateStrategyParamsCommand : CommandBase<UpdateStrategyParamsRequest>
    {
        #region Constructors
        public UpdateStrategyParamsCommand(ICore core, IPusher pusher) : base(core, pusher)
        {
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(UpdateStrategyParamsRequest request)
        {
            var serviceID = Core.GetScriptingServiceID(request.User.Login, request.SignalName, ScriptingType.Signal);
            var service = Core.GetProcessor(serviceID);
            if (service == null) return;

            PushToProcessor(new UpdateSignalStrategyParamsResponse
            {
                Login = request.User.Login,
                SignalName = request.SignalName,
                Parameters = request.Parameters
            }, service);
        }

        #endregion // CommandBase
    }
}
