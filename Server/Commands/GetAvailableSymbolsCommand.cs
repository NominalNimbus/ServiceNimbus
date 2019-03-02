/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using ServerCommonObjects;
using Server.Interfaces;

namespace Server.Commands
{
    internal sealed class GetAvailableSymbolsCommand : CommandBase<GetAvailableSymbolsRequest>
    {
        #region Fields

        private readonly IDataFeedWorker _dataFeedWorker;

        #endregion // Fields

        #region Constructors

        public GetAvailableSymbolsCommand(ICore core, IPusher pusher, IDataFeedWorker dataFeedWorker) : base(core, pusher)
        {
            _dataFeedWorker = dataFeedWorker ?? throw new ArgumentNullException(nameof(dataFeedWorker));
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(GetAvailableSymbolsRequest request)
        {
            var symbols = default(List<string>);
            if (_dataFeedWorker.DataFeeds.TryGetValue(request.DataFeed, out var dataFeed))
                symbols = dataFeed.Securities.Select(x => x.Symbol).ToList();

            PushToProcessor(new GetAvailableSymbolsResponse
            {
                Id = request.Id,
                Symbols = symbols
            }, request.Processor);
        }

        #endregion // CommandBase
    }
}
