/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using CommonObjects;
using ServerCommonObjects;
using ServerCommonObjects.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    internal interface IHistoryDataMultiTimeframeCache
    {
        bool CacheTickData { get; set; }
        void GetHistory(IDataFeed dataFeed, Selection p, HistoryAnswerHandler callback);
        void Add2Cache(Tick tick);
    }
}
