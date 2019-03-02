/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CommonObjects;
using ServerCommonObjects;
using Server.Interfaces;

namespace Server.Commands.Scripting
{
    internal sealed class BacktestResultsCommand : CommandBase<BacktestResultsRequest>
    {
        #region Constructors
        public BacktestResultsCommand(ICore core, IPusher pusher) : base(core, pusher)
        {
        }

        #endregion // Constructors

        #region CommandBase

        protected override void ExecuteCommand(BacktestResultsRequest request)
        {
            var results = default(List<BacktestResults>);
            var file = Path.Combine("CustomSignals", request.User.Login, request.SignalName, "Backtest Results.xml");
            if (File.Exists(file))
            {
                try
                {
                    using (var textReader = new StreamReader(file, Encoding.UTF8))
                    {
                        var xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(List<BacktestResults>));
                        results = (List<BacktestResults>)xmlSerializer.Deserialize(textReader);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to deserialize backtest results: " + e.Message);
                }
            }

            if (results != null && results.Count > 0)
            {
                Task.Run(() =>
                {
                    PushResponse(new BacktestReportMessage
                    {
                        User = request.User,
                        BacktestProgress = 100F,
                        BacktestResults = results,
                        IsFromEarlierSession = true
                    });
                });
            }
        }

        #endregion // CommandBase
    }
}
