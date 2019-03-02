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
    internal sealed class CreateUserIndicatorCommand : CommandBase<CreateUserIndicatorRequest>
    {
        #region Constructors

        public CreateUserIndicatorCommand(ICore core, IPusher pusher) : base(core, pusher)
        {
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(CreateUserIndicatorRequest request)
        {
            var success = default(bool);
            var files = Core.GetIndicatorFiles(request.User, request.Name);

            success = PushStartCodeMessage(new StartIndicatorResponse
            {
                Login = request.User.Login,
                Selection = request.Selection,
                Name = request.Name,
                Parameters = request.Parameters,
                PriceType = request.PriceType,
                RequestID = request.RequestID,
                Dlls = files
            });

            if (!success)
            {
                PushResponse(new ErrorMessageResponse(new Exception("Scripting failed to start. Services are offline"))
                {
                    User = request.User
                });
            }
        }

        #endregion // CommandBase
    }
}
