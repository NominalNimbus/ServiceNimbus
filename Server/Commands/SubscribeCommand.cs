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

namespace Server.Commands
{
    internal sealed class SubscribeCommand : CommandBase<SubscribeRequest>
    {
        #region Fields

        private readonly IRealTimeWorker _realTimeWorker;

        #endregion // Fields

        #region Constructors

        public SubscribeCommand(ICore core, IPusher pusher, IRealTimeWorker realTimeWorker) : base(core, pusher)
        {
            _realTimeWorker = realTimeWorker ?? throw new ArgumentNullException(nameof(realTimeWorker));
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(SubscribeRequest request)
            => _realTimeWorker.Level1Subscribe(request.Symbol, request.User.ID);

        #endregion // CommandBase
    }
}
