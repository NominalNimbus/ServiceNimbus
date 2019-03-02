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
using CommonObjects;
using ServerCommonObjects;
using Server.Interfaces;

namespace Server.Commands
{
    internal sealed class DataFeedListCommand : CommandBase<GetDataFeedListRequest>
    {
        #region Fields

        private readonly IDataFeedWorker _dataFeedWorker;

        #endregion // Fields

        #region Constructors

        public DataFeedListCommand(ICore core, IPusher pusher, IDataFeedWorker dataFeedWorker) : base(core, pusher)
        {
            _dataFeedWorker = dataFeedWorker ?? throw new ArgumentNullException(nameof(dataFeedWorker));
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(GetDataFeedListRequest request)
        {
            var response = new GetDataFeedListResponse { DataFeeds = new List<DataFeed>() };

            foreach (var itemPair in _dataFeedWorker.DataFeeds)
            {
                var res = Core.GetDatafeedSecurities(itemPair.Value.Name);

                response.DataFeeds.Add(new DataFeed
                {
                    Name = itemPair.Value.Name,
                    Symbols = new List<Security>(res.Select(q => new Security
                    {
                        DataFeed = itemPair.Value.Name,
                        Symbol = q.Symbol,
                        SecurityId = q.SecurityId,
                        AssetClass = q.AssetClass,
                        Digit = q.Digit,
                        PriceIncrement = q.PriceIncrement,
                        QtyIncrement = q.QtyIncrement
                    })),
                    IsStarted = itemPair.Value.IsStarted
                });
            }

            response.User = request.User;
            PushResponse(response);
        }

        #endregion // CommandBase
    }
}
