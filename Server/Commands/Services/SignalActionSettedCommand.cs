/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using CommonObjects;
using ServerCommonObjects;
using Server.Interfaces;

namespace Server.Commands.Services
{
    internal sealed class SignalActionSettedCommand : CommandBase<SignalActionSettedRequest>
    {
        #region Constructors

        public SignalActionSettedCommand(ICore core, IPusher pusher) : base(core, pusher)
        {
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(SignalActionSettedRequest request)
        {
            Core.SignalFlagSetted(request.Username, request.SignalName, request.Action, request.State);
            SignalActionSetted(request.Username, request.Action, request.SignalName, request.State);
        }

        #endregion // CommandBase

        #region Private

        private void SignalActionSetted(string username, SignalAction action, string signalName, SignalState state)
        {
            var user = Core.GetUser(username);
            PushResponse(new SignalActionResponse
            {
                User = user,
                SignalName = signalName,
                Action = action,
                State = state
            });
        }

        #endregion // Private
    }
}
