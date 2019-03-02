/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;
using CommonObjects;
using ServerCommonObjects;
using ServerCommonObjects.Interfaces;
using ServerCommonObjects.SQL;
using Timer = System.Timers.Timer;

namespace SimulatedDataFeed
{
    public class SimulatedDataFeed : IDataFeed
    {
        #region Fields

        private readonly Timer _timer;
        private int _tickUpdateInterval = 500;//0.5 sec

        #endregion

        #region Properties/Events

        public string Name => "Simulated";

        public int BalanceDecimals => 2;

        public List<Security> Securities { get; set; }
        public bool IsStarted { get; private set; }

        private Dictionary<string, SymbolTickGenerator> Generators { get; set; }

        public event NewTickHandler NewTick;
        public event NewSecurityHandler NewSecurity;

        #endregion

        #region Constructor

        public SimulatedDataFeed()
        {
            Securities = new List<Security>();
            Generators = new Dictionary<string, SymbolTickGenerator>();
            foreach (var symbol in GetSymbols())
            {
                var securuty = CreateSecurity(symbol, Name);
                Securities.Add(securuty);
                Generators.Add(symbol.Symbol, new SymbolTickGenerator(securuty, symbol.StartPrice, Name));
            }

            _timer = new Timer(_tickUpdateInterval);
            _timer.Elapsed += NewTickTimer;
        }

        #endregion

        #region IDataFeed

        public void Start()
        {
            IsStarted = true;
            _timer.Start();
        }

        public void Stop()
        {
            IsStarted = false;
            _timer.Stop();
        }

        public void Subscribe(Security security)
        {
        }

        public void UnSubscribe(Security security)
        {
        }

        public void GetHistory(Selection parameters, HistoryAnswerHandler callback)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                if (!Generators.TryGetValue(parameters.Symbol, out var generator))
                {
                    callback(parameters, new List<Bar>());
                    return;
                }

                if ((parameters.From == DateTime.MinValue || parameters.From == DateTime.MaxValue) &&
                    (parameters.To == DateTime.MinValue || parameters.To == DateTime.MaxValue))
                {
                    if (parameters.BarCount <= 3)
                    {
                        callback(parameters, new List<Bar>());
                        return;
                    }

                    parameters.To = DateTime.UtcNow;

                    if (parameters.Timeframe == Timeframe.Minute)
                        parameters.From = parameters.To.AddMinutes(-1 * 3 * parameters.BarCount * parameters.TimeFactor);
                    else if (parameters.Timeframe == Timeframe.Hour)
                        parameters.From = parameters.To.AddHours(-1 * 3 * parameters.BarCount * parameters.TimeFactor);
                    else if (parameters.Timeframe == Timeframe.Day)
                        parameters.From = parameters.To.AddDays(-1 * 2 * parameters.BarCount * parameters.TimeFactor);
                    else
                        parameters.From = parameters.To.AddDays(-1 * parameters.BarCount * parameters.TimeFactor * 31);
                }

                var bars = generator.GenerateHistory(parameters);
                callback(parameters, bars);
            });
        }

        #endregion

        #region Private

        private void NewTickTimer(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();
            foreach (var security in Generators)
            {
                NewTick?.Invoke(security.Value.GenerateNewTick());
            }
            _timer.Start();
        }

        private static Security CreateSecurity(SimulatedSymbol dbSymbol, string dataFeedName)
        {
            return new Security
            {
                Digit = 2,
                Name = dbSymbol.Symbol,
                AssetClass = "STOCK",
                DataFeed = dataFeedName,
                BaseCurrency = dbSymbol.Currency,
                ContractSize = dbSymbol.ContractSize,
                MarginRate = dbSymbol.Margin,
                MarketOpen = new TimeSpan(8, 0, 0),
                MarketClose = new TimeSpan(17, 0, 0),
                MaxPosition = 500000,
                PriceIncrement = 0.01m,
                QtyIncrement = 1m,
                SecurityId = dbSymbol.Id,
                Symbol = dbSymbol.Symbol,
                UnitOfMeasure = dbSymbol.Currency,
                UnitPrice = 1000,
                CommisionCalculator = CreateCommissionCalculator(dbSymbol)
            };
        }

        private static ICommisionCalculator CreateCommissionCalculator(SimulatedSymbol dbSymbol)
        {
            switch (dbSymbol.CommissionType)
            {
                case CommonObjects.Enums.ComisionType.PerContract:
                    return new CommissionPerContractCalculator(dbSymbol.CommissionValue);
                case CommonObjects.Enums.ComisionType.Percent:
                    return new CommissionPercentCalculator(dbSymbol.CommissionValue);
                default:
                    return null;
            }
        }

        private IEnumerable<SimulatedSymbol> GetSymbols()
        {
            var connection = File.ReadAllText("DataBaseConnection.set");
            var db = new DBSimulatedSymbols(connection);
            return db.GetSymbols();
        }

        #endregion

    }
}
