/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using CommonObjects;
using ServerCommonObjects.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Brokers
{
    public class LmaxLiveBroker : LmaxBroker
    {
        public const string BrokerName = "LMAX Live";
        private const string Url = "https://web-order.london-demo.lmax.com/";

        public override string Name => BrokerName;
        public override string Uri => Url;
        
        public LmaxLiveBroker(IDataFeed datafeed) : base(datafeed)
        {

        }

        public static AvailableBrokerInfo BrokerInfo(string user) =>
            AvailableBrokerInfo.CreateLiveBroker(BrokerName, DefaultDataFeedName, Url);
    }
}
