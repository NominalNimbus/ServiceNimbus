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
    internal sealed class GetPositionsCommand : CommandBase<GetPositionsRequest>
    {
        #region Constructors

        public GetPositionsCommand(ICore core, IPusher pusher) : base(core, pusher)
        {
        }

        #endregion // Constructors

        #region CommanBase

        protected override void ExecuteCommand(GetPositionsRequest request)
        {
            var positions = string.IsNullOrEmpty(request.Symbol)
                ? Core.OMS.GetPositions(request.Account)
                : Core.OMS.GetPositions(request.Account, request.Symbol);

            PushToProcessor(new GetPositionsResponse
            {
                Id = request.Id,
                Positions = positions
            }, request.Processor);
        }

        #endregion // CommanBase
    }
}
