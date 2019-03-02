/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using System.Linq;
using ServerCommonObjects;
using Server.Interfaces;

namespace Server.Commands.Services
{
    internal sealed class GetBarsCommand : CommandBase<GetBarsRequest>
    {
        #region Constructors

        public GetBarsCommand(ICore core, IPusher pusher) : base(core, pusher)
        {
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(GetBarsRequest request)
        {
            Core.GetHistory(request.Selection, (barRequest, bars) =>
            {
                var barList = bars.OrderBy(i => i.Date);
                PushToProcessor(new GetBarsResponse
                {
                    Id = request.Id,
                    Bars = barList.ToList()
                }, request.Processor);
            });
        }

        #endregion // CommandBase
    }
}
