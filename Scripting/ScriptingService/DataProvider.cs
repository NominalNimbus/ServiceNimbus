/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonObjects;
using Scripting;

namespace ScriptingService
{
    public class DataProvider : MarshalByRefObject, IDataProvider
    {

        #region Fields

        private readonly TimeSpan _taskTimeOut;
        private readonly Connector _connector;
        private readonly Dictionary<Security, Tick> _lastTicks;

        #endregion // Fields

        #region Properties

        public List<string> AvailableDataFeeds => _connector.AvailableDataFeeds;

        #endregion // Properties

        #region Constructors

        public DataProvider(Connector connector)
        {
            _taskTimeOut = TimeSpan.FromSeconds(5);
            _connector = connector ?? throw new ArgumentNullException($"{nameof(connector)} is null.");
            _lastTicks = new Dictionary<Security, Tick>();
            SubscribeConnectorEvents();
        }

        #endregion // Constructors

        #region Private

        private void SubscribeConnectorEvents()
        {
            _connector.NewTick += ConnectorNewTick;
        }

        private void UnSubscribeConnectorEvents()
        {
            _connector.NewTick -= ConnectorNewTick;
        }

        #endregion // Private

        #region IDataProvider

        public List<string> AvailableSymbolsForDataFeed(string dataFeedName)
        {
            var symbolTask = _connector.GetAvailableSymbols(dataFeedName);
            symbolTask.Wait(_taskTimeOut);
            return symbolTask.Status == TaskStatus.RanToCompletion ? symbolTask.Result : new List<string>();
        }

        public List<Bar> GetBars(Selection parameters)
        {
            var barTask = _connector.GetBars(parameters);
            barTask.Wait(_taskTimeOut);
            //return barTask.Status == TaskStatus.RanToCompletion ? barTask.Result : new List<Bar>();
            var bars = barTask.Status == TaskStatus.RanToCompletion ? barTask.Result : new List<Bar>();
            return bars;
        }

        public Tick GetLastTick(string dataFeed, string symbol)
        {
            var security = new Security
            {
                Symbol = symbol,
                DataFeed = dataFeed
            };

            lock(_lastTicks)
            {
                _lastTicks.TryGetValue(security, out var tick);
                return tick;
            }
        }

        public Tick GetTick(string dataFeed, string symbol, DateTime timestamp)
        {
            var tickTask = _connector.GetTick(dataFeed, symbol, timestamp);
            tickTask.Wait(_taskTimeOut);
            return tickTask.Result;
        }

        public string GetLastError() => string.Empty;

        #endregion // IDataProvider

        #region Event Handlers

        private void ConnectorNewTick(object sender, Tick e)
        {
            if (e == null)
                return;

            lock(_lastTicks)
            {
                if (_lastTicks.ContainsKey(e.Symbol))
                    _lastTicks[e.Symbol] = e;
                else
                    _lastTicks.Add(e.Symbol, e);
            }
        }

        #endregion // Event Handlers

        #region Dispose

        public void Dispose()
        {
            UnSubscribeConnectorEvents();
        }

        #endregion // Dispose

    }
}