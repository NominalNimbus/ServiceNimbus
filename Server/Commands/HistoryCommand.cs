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
using System.Threading.Tasks;
using CommonObjects;
using ServerCommonObjects;
using Server.Interfaces;

namespace Server.Commands
{
    internal sealed class HistoryCommand : CommandBase<HistoryDataRequest>
    {
        #region Fields

        private readonly IDataFeedWorker _dataFeedWorker;
        private readonly IHistoryWorker _historyWorker;

        #endregion // Fields

        #region Constructors

        public HistoryCommand(ICore core, IPusher pusher, IDataFeedWorker dataFeedWorker, IHistoryWorker historyWorker) : base(core, pusher)
        {
            _dataFeedWorker = dataFeedWorker ?? throw new ArgumentNullException(nameof(dataFeedWorker));
            _historyWorker = historyWorker ?? throw new ArgumentNullException(nameof(historyWorker));
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(HistoryDataRequest request)
        {
            var aDataFeed = _dataFeedWorker.GetDataFeedByName(request.Selection.DataFeed);
            if (aDataFeed != null)
            {
                lock (_historyWorker.HistoryRequest)
                {
                    var copy = _historyWorker.HistoryRequest.FirstOrDefault(p => p.Selection.Id.Equals(request.Selection.Id));

                    if (copy != null)
                        _historyWorker.HistoryRequest.Remove(copy);

                    _historyWorker.HistoryRequest.Add(request);
                }

                Task.Run(() => Core.GetHistory(request.Selection, HistoryReceivedCallback));
            }
        }

        #endregion // CommandBase

        #region Private

        private void HistoryReceivedCallback(Selection historyRequest, List<Bar> bars)
        {
            var request = default(HistoryDataRequest);
            lock (_historyWorker.HistoryRequest)
            {
                request = _historyWorker.HistoryRequest.FirstOrDefault(p => p.Selection.Id.Equals(historyRequest.Id));
                if (request == null)
                    return;

                _historyWorker.HistoryRequest.Remove(request);
            }

            try
            {
                var aResponse = new HistoryDataResponse
                {
                    Bars = bars.OrderByDescending(o => o.Date).ToList(),
                    ID = historyRequest.Id,
                    User = request.User
                };

                PushResponse(aResponse);
            }
            catch (Exception e)
            {
                Logger.Error("HistoryReceivedCallback", e);
                try
                {
                    request.User.SendError(new ApplicationException($"Historical request (ID='{historyRequest.Id}')"));
                }
                catch
                {
                }
            }
        }

        #endregion // Private
    }
}
