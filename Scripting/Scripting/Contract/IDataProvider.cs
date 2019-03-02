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


namespace Scripting
{
    public interface IDataProvider : IDisposable
    {
        List<string> AvailableDataFeeds { get; }

        List<string> AvailableSymbolsForDataFeed(string dataFeedName);

        List<Bar> GetBars(Selection parameters);

        Tick GetLastTick(string dataFeed, string symbol);

        Tick GetTick(string dataFeed, string symbol, DateTime timestamp);

        string GetLastError();
    }
}
