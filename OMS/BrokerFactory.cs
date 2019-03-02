/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Brokers;
using CommonObjects;
using ServerCommonObjects;
using ServerCommonObjects.Interfaces;

namespace OMS
{
    public static class BrokerFactory
    {
        public static List<AvailableBrokerInfo> GetAvailableBrokers(string userName)
        {
            return new List<AvailableBrokerInfo>
            {
                PoloniexBroker.BrokerInfo(userName),
                LmaxLiveBroker.BrokerInfo(userName),
                LmaxDemoBroker.BrokerInfo(userName),
                SimulatedExchangeBroker.BrokerInfo(userName),
                SimulatedMarginBroker.BrokerInfo(userName)
            };
        }

        public static string CreateSimulatedAccount(string userName, CreateSimulatedBrokerAccountInfo account)
        {
            switch (account.BrokerName)
            {
                case PoloniexBroker.BrokerName:
                case LmaxLiveBroker.BrokerName:
                case LmaxDemoBroker.BrokerName:
                    return $"{account.BrokerName} broker doesn't support create account";
                case SimulatedMarginBroker.BrokerName:
                    return SimulatedMarginBroker.CreateSimulatedAccount(userName, account);
                case SimulatedExchangeBroker.BrokerName:
                    return SimulatedExchangeBroker.CreateSimulatedAccount(userName, account);
                default:
                    return $"No available broker {account.BrokerName}";
            }
        }

        public static IBroker CreateBrokerInstance(string brokerName, string dataFeedName, string userName, List<IDataFeed> dataFeeds)
        {
            switch (brokerName)
            {
                case PoloniexBroker.BrokerName:
                    dataFeedName = PoloniexBroker.DefaultDataFeedName;
                    break;
                case LmaxLiveBroker.BrokerName:
                case LmaxDemoBroker.BrokerName:
                    dataFeedName = LmaxBroker.DefaultDataFeedName;
                    break;
            }

            IDataFeed df = dataFeeds.FirstOrDefault(p => p.Name.Equals(dataFeedName, StringComparison.OrdinalIgnoreCase));
            if (df == null)
                throw new Exception($"No available data feeds {dataFeedName}");

            switch (brokerName)
            {
                case PoloniexBroker.BrokerName:
                    return new PoloniexBroker(df);
                case LmaxLiveBroker.BrokerName:
                    return new LmaxLiveBroker(df);
                case LmaxDemoBroker.BrokerName:
                    return new LmaxDemoBroker(df);
                case SimulatedMarginBroker.BrokerName:
                    return new SimulatedMarginBroker(df, userName);
                case SimulatedExchangeBroker.BrokerName:
                    return new SimulatedExchangeBroker(df, userName);
                default:
                    throw new Exception($"No available broker {brokerName}");
            }
        }
    }
}
