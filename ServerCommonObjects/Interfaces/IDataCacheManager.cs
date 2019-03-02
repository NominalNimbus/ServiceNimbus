/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using CommonObjects;
using ServerCommonObjects;
using ServerCommonObjects.Interfaces;

namespace ServerCommonObjects.Interfaces
{
    public interface IDataCacheManager
    {
        event EventHandler<EventArgs<Security>> NewMinBar;
        event EventHandler<EventArgs<string, double>> CachedDataProgress;

        void Start(IEnumerable<IDataFeed> dataFeeds, string connectionString);
        void Stop();
        //void AddMinBarsToCache(string symbol, string dataFeed, List<Bar> bars);
        //void AddDayBarsToCache(string symbol, string dataFeed, List<Bar> bars);
        
        //bool IsMinDataCached(string symbol, string dataFeed);
        //bool IsDayDataCached(string symbol, string dataFeed);
        decimal GetTotalDailyAskVolume(string symbol, string dataFeed, int level);
        decimal GetTotalDailyBidVolume(string symbol, string dataFeed, int level);

        void GetHistory(Selection p, IDataFeed dataFeed, HistoryAnswerHandler callback);
        Tick GetTick(string symbol, string dataFeed, DateTime timestamp);
        List<Security> GetDatafeedSecurities(string name);
        /*List<Bar> GetTickHistory(Selection parameters);
        List<Bar> GetMinHistory(Selection parameters);
        List<Bar> GetDayHistory(Selection parameters);
        List<Bar> GetMinHistoryL2(Selection parameters);
        List<Bar> GetDayHistoryL2(Selection parameters);
        void RequestHistoryFromFeed(Selection parameters, IDataFeed dataFeed, HistoryAnswerHandler callback);*/
    }
}