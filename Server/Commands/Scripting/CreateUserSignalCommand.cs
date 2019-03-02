/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using System;
using CommonObjects;
using ServerCommonObjects;
using Server.Interfaces;

namespace Server.Commands.Scripting
{
    internal sealed class CreateUserSignalCommand : CommandBase<CreateUserSignalRequest>
    {
        #region Constructors
        public CreateUserSignalCommand(ICore core, IPusher pusher) : base(core, pusher)
        {
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(CreateUserSignalRequest request)
        {
            var success = default(bool);
            var signalInitParams = new SignalInitParams
            {
                FullName = request.SignalName,
                BacktestSettings = request.BacktestSettings,
                StrategyParameters = request.StrategyParameters,
                Parameters = request.Parameters,
                Selections = request.Selections,
                State = request.InitialState
            };

            Core.AddSignal(request.User, request.Files, new SignalInitParams
            {
                FullName = request.SignalName,
                BacktestSettings = request.BacktestSettings,
                StrategyParameters = request.StrategyParameters,
                Parameters = request.Parameters,
                Selections = request.Selections,
                State = request.InitialState
            });

            success = PushStartCodeMessage(new StartSignalExecutionResponse
            {
                Id = request.SignalName,
                UserName = request.User.Login,
                Files = request.Files,
                SignalInitParams = signalInitParams,
                Portfolios = Core.GetPortfolios(request.User),
                AccountInfos = request.AccountInfos
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
