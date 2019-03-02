/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.Collections.Generic;
using CommonObjects;

namespace ServerCommonObjects.Interfaces
{
    public interface IDataFeed
    {
        event NewTickHandler NewTick;
        event NewSecurityHandler NewSecurity;

        bool IsStarted { get; }
        string Name { get; }
        int BalanceDecimals { get; }
        List<Security> Securities { get; set; }

        void Start();
        void Stop();
        void Subscribe(Security security);
        void UnSubscribe(Security security);
        void GetHistory(Selection parameters, HistoryAnswerHandler callback);
    }
}