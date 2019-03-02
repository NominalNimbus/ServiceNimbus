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
using Backtest;
using CommonObjects;
using ScriptingManager;
using BacktestResults = CommonObjects.BacktestResults;

namespace Scripting
{
    public abstract class SignalBase : MarshalByRefObject, IScripting
    {
        private readonly object _locker = new object();
        private readonly List<string> _alerts = new List<string>();
        private readonly List<Output> _outputs = new List<Output>();
        private readonly List<ScriptingParameterBase> _origParameters = new List<ScriptingParameterBase>();
        private readonly Dictionary<AccountInfo, List<Selection>> _isPositionClosing = new Dictionary<AccountInfo, List<Selection>>();
        private readonly List<TradeSignal> _generatedSignals = new List<TradeSignal>();
        private byte _backtestNestingLevel;
        private bool _isBusy;

        #region Properties

        public string ID { get; private set; }

        public string Name { get; set; } //portfolio/strategy/signal

        public string ShortName //signal
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name))
                    return string.Empty;

                var idx = Name.LastIndexOf(System.IO.Path.DirectorySeparatorChar);
                return (idx > 0 && idx < Name.Length - 1) ? Name.Substring(idx + 1) : Name;
            }
        }

        public StrategyParams StrategyParameters { get; private set; }

        public string Owner { get; set; }

        private IBroker Broker { get; set; }

        public ISimulationBroker SimulationBroker { get; set; }

        protected IDataProvider DataProvider { get; private set; }

        /// <summary>
        /// Determinate Start function call (on new tick/ new bar/ once)
        /// </summary>
        public StartMethod StartMethod { get; set; }

        /// <summary>
        /// Signal state (running, backtesting, etc.)
        /// </summary>
        public SignalState State { get; private set; }

        /// <summary>
        /// Shows if signal is set/ready to run the start method
        /// </summary>
        public bool IsReadyToRun => State == SignalState.Running || State == SignalState.RunningSimulated;

        /// <summary>
        /// Shows backtest progress (in %)
        /// </summary>
        public float BacktestProgress { get; private set; }

        /// <summary>
        /// Execution period (in milliseconds) for Signals with StartMethod.Periodic
        /// </summary>
        public int ExecutionPeriod { get; set; }

        public List<ScriptingParameterBase> OrigParameters => _origParameters.Select(p => p.Clone() as ScriptingParameterBase).ToList();

        /// <summary>
        /// Collection of data selections to scan
        /// </summary>
        public List<Selection> Selections { get; private set; }

        /// <summary>
        /// Backtest settings
        /// </summary>
        public BacktestSettings BacktestSettings { get; set; }

        /// <summary>
        /// Latest backtest results
        /// </summary>
        public List<BacktestResults> BacktestResults { get; private set; }

        public DateTime PreviousRunTime { get; set; }

        #endregion

        protected SignalBase()
        {
            ID = Guid.NewGuid().ToString("N");
            Name = string.Empty;
            State = SignalState.Stopped;
            Selections = new List<Selection>();
            BacktestResults = new List<BacktestResults>();
        }

        /// <summary>
        /// Initializes scripting instance
        /// </summary>
        /// <param name="selections">List of data descriptions on which code will be run</param>
        /// <returns>True if succeeded</returns>
        protected abstract bool InternalInit(IEnumerable<Selection> selections);

        /// <summary>
        /// Runs on new tick, new bar or by timer (see <see cref="StartMethod"/> property)
        /// </summary>
        /// <param name="instrument">Instrument that triggered execution (optional)</param>
        /// <param name="ticks">Accumulated ticks (optional)</param>
        protected abstract void InternalStart(Selection instrument = null, IEnumerable<Tick> ticks = null);

        /// <summary>
        /// Runs backtest for single instrument and a set of parameter values
        /// <param name="instruments">Instruments to be backtested</param>
        /// <param name="values">Set of parameter values to use for backtest</param>
        /// </summary>
        protected abstract List<TradeSignal> BacktestSlotItem(IEnumerable<Selection> instruments,
            IEnumerable<object> values);

        /// <summary>
        /// Analyzes order before submitting it to IBroker
        /// </summary>
        /// <param name="order">Order to analyze</param>
        /// <returns>Processed order details</returns>
        protected abstract OrderParams AnalyzePreTrade(OrderParams order);

        /// <summary>
        /// Analyzes order that has been filled by broker
        /// </summary>
        /// <param name="order">Order details from broker API</param>
        protected abstract void AnalyzePostTrade(Order order);

        /// <summary>
        /// Processes order failure or rejection
        /// </summary>
        /// <param name="order">Order details</param>
        /// <param name="error">Error message</param>
        protected abstract void ProcessTradeFailure(Order order, string error);

        /// <summary>
        /// Gets list of parameters for configuration on client side
        /// </summary>
        protected abstract List<ScriptingParameterBase> InternalGetParameters();

        /// <summary>
        /// Applies parameters configured on client side
        /// </summary>
        /// <param name="parameterBases">List of configured parameters</param>
        /// <returns>True if case of succeeded configuration</returns>
        protected abstract bool InternalSetParameters(List<ScriptingParameterBase> parameterBases);

        /// <summary>
        /// Calls scripting inner parameters Initialization using cross-thread lock's 
        /// </summary>
        /// <param name="broker">Data broker</param>
        /// <param name="selections">Data descriptions on which  code will be run</param>
        /// <param name="dataProvider">Object which provide access to historical and real time data</param>
        /// <returns>True if case of succeeded initialization</returns>
        public bool Init(IBroker broker, IDataProvider dataProvider, IEnumerable<Selection> selections,
            SignalState state, StrategyParams strategyParameters, ISimulationBroker simulationBroker = null)
        {
            Broker = broker;
            SimulationBroker = simulationBroker ?? new SimulationBroker(broker.AvailableAccounts, broker.Portfolios);
            DataProvider = dataProvider;
            State = state;
            SetStrategyParameters(strategyParameters);

            lock (_locker)
            {
                try
                {
                    return InternalInit(selections);
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool IsBusy() => _isBusy;

        /// <summary>
        /// Starts function which is called on new tick, new bar or by timer (see <see cref="StartMethod"/> property)
        /// </summary>
        /// <param name="instrument">Instrument that triggered execution (optional)</param>
        /// <param name="ticks">Accumulated ticks (optional)</param>
        /// <returns>True if succeeded, false otherwise (eg. busy or failure)</returns>
        public bool Start(Selection instrument = null, IEnumerable<Tick> ticks = null)
        {
            if (_isBusy)
                return false;

            lock (_locker)
            {
                try
                {
                    if (!_isBusy && IsReadyToRun)
                    {
                        _isBusy = true;
                        InternalStart(instrument, ticks);
                        PreviousRunTime = DateTime.UtcNow;
                        return true;
                    }
                }
                catch
                {
                    // ignored
                }
                finally
                {
                    _isBusy = false;
                }

                return false;
            }
        }

        /// <summary>
        /// Runs backtest (in separate thread) for all Selections and numeric Parameters
        /// </summary>
        /// <param name="settings">Settings to use for backtest</param>
        public void StartBacktest(BacktestSettings settings)
        {
            if (!_isBusy && State == SignalState.Backtesting)
            {
                BacktestSettings = settings ?? new BacktestSettings();
                var thread = new System.Threading.Thread(() => Backtest())
                {
                    Name = "Signal Backtest",
                    IsBackground = true
                };
                thread.Start();
            }
            else
            {
                if (State != SignalState.Backtesting)
                    Alert("Can't start backtest: signal is not in a backtest mode");
                else if (_isBusy)
                    Alert("Can't start backtest: signal is busy");
            }
        }

        /// <summary>
        /// Runs backtest for all Selections and numeric Parameters
        /// </summary>
        /// <param name="pushResultsToSignal">Update signal's backtest results during execution</param>
        /// <returns>Collection of backtest results per each selection and parameters combination</returns>
        public List<BacktestResults> Backtest(bool pushResultsToSignal = true)
        {
            if (_backtestNestingLevel > 2)
            {
                Alert("Won't run backtest: possible infinite recursion loop");
                return null;
            }

            var resetBusyFlag = !_isBusy;
            _isBusy = true;

            var results = new List<BacktestResults>();

            if (pushResultsToSignal)
            {
                BacktestProgress = 0F;
                BacktestResults.Clear();
            }

            lock (_locker)
            {
                try
                {
                    _backtestNestingLevel++;

                    //run backtest for each instrument using each possible parameters combinations
                    var paramSpaces = GetParamCombinations(_origParameters);
                    var paramNames = GetParamNames(_origParameters);
                    var paramCombinations = CartesianProduct(paramSpaces);
                    var total = paramCombinations.Count()
                                * Selections.Select(i => i.MarketDataSlot).Distinct().Count();
                    results.Capacity = total;
                    var current = 0;
                    foreach (var slot in Selections.GroupBy(i => i.MarketDataSlot))
                    {
                        foreach (var paramsCombo in paramCombinations)
                        {
                            while (State == SignalState.BacktestingPaused)
                                System.Threading.Thread.Sleep(50);

                            if (State == SignalState.Stopped)
                                return results;

                            try
                            {
                                var allTrades = BacktestSlotItem(slot, paramsCombo);
                                var progress = (++current / (float)total) * 100F;
                                var result = new BacktestResults
                                {
                                    SignalName = this.Name,
                                    Slot = slot.Key,
                                    Index = current,
                                    StartDate = BacktestSettings.StartDate,
                                    EndDate = BacktestSettings.EndDate,
                                    TotalProgress = progress
                                };

                                for (var i = 0; i < paramNames.Length; i++)
                                    result.Parameters.Add(paramNames[i] + "|" + paramsCombo.ElementAt(i).ToString());

                                if (allTrades != null && allTrades.Count > 0 && State != SignalState.Stopped)
                                {
                                    foreach (var symTrades in allTrades.GroupBy(i => new
                                    {
                                        i.Instrument.Symbol,
                                        i.Instrument.Timeframe,
                                        i.Instrument.TimeFactor
                                    }))
                                    {
                                        var key = symTrades.First().Instrument;
                                        var summary = GetBacktestSummary(key, symTrades);
                                        summary.TradesCompressed = CompressTrades(symTrades);
                                        result.Summaries.Add(summary);
                                    }
                                }

                                results.Add(result);
                                if (pushResultsToSignal)
                                {
                                    BacktestResults.Add(result);
                                    BacktestProgress = progress;
                                }
                            }
                            catch (Exception e)
                            {
                                Alert($"Failed to run backtest for {Name} on slot #{slot.Key}: {e.Message}");
                            }
                        } //^ parameters loop
                    }     //^ slots loop
                }
                catch (Exception e)
                {
                    System.Diagnostics.Trace.TraceError($"Failed to run backtest for {Name}: {e.Message}");
                }
                finally
                {
                    if (pushResultsToSignal)
                        BacktestProgress = 100F;
                    if (resetBusyFlag)
                        _isBusy = false;
                    _backtestNestingLevel--;
                }
            } //^ lock

            return results;
        }

        /// <summary>
        /// Reports order execution
        /// </summary>
        /// <param name="order">Details of executed order</param>
        public void ReportOrderExecution(Order order)
        {
            if (order.Origin == StrategyParameters.StrategyID + "/" + ShortName)
                AnalyzePostTrade(order);
        }

        /// <summary>
        /// Reports order failure or rejection
        /// </summary>
        /// <param name="order">Details of executed order</param>
        /// <param name="error">Error message</param>
        /// <remarks>
        /// Order parameter might not contain all fields of the original order,
        /// only client order ID and instrument symbol are guaranteed to be present
        /// </remarks>
        public void ReportOrderFailure(Order order, string error)
        {
            if (order.Origin == StrategyParameters.StrategyID + "/" + ShortName)
                ProcessTradeFailure(order, error);
        }

        /// <summary>
        /// Shows alert with message
        /// </summary>
        /// <param name="message">Message to show</param>
        public void Alert(string message)
        {
            lock (_alerts)
                _alerts.Add(message);
        }

        /// <summary>
        /// Returns list of non showed alerts. Calling of this function clear list of alerts.
        /// </summary>
        public List<string> GetActualAlerts()
        {
            lock (_alerts)
            {
                var res = _alerts.ToList();
                _alerts.Clear();
                return res;
            }
        }

        /// <summary>
        /// Gets code parameters for settings on client side using cross-thread lock's 
        /// </summary>
        /// <returns>List of parameters</returns>
        public List<ScriptingParameterBase> GetParameters()
        {
            lock (_locker)
            {
                try
                {
                    return _origParameters?.Count > 0 ? _origParameters : InternalGetParameters();
                }
                catch (Exception)
                {
                    return new List<ScriptingParameterBase>();
                }
            }
        }

        /// <summary>
        /// Sets parameters that configured on client side using cross-thread lock's 
        /// </summary>
        /// <param name="parameterBases">List of parameters</param>
        /// <returns>False in case of invalid parameters</returns>
        public bool SetParameters(List<ScriptingParameterBase> parameterBases)
        {
            lock (_locker)
            {
                try
                {
                    _origParameters.Clear();
                    _origParameters.AddRange(parameterBases.Select(p => p.Clone() as ScriptingParameterBase));
                    return InternalSetParameters(parameterBases);
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Sets strategy parameters
        /// </summary>
        /// <param name="parameters">Parameters to use for this signal</param>
        public void SetStrategyParameters(StrategyParams parameters)
        {
            if (parameters == null)
                return;

            if (StrategyParameters == null)
                StrategyParameters = new StrategyParams(parameters.StrategyID);

            StrategyParameters.StrategyID = parameters.StrategyID;
            StrategyParameters.ExposedBalance = parameters.ExposedBalance;
        }

        /// <summary>
        /// Update signal's state
        /// </summary>
        /// <param name="state">New state to set</param>
        /// <returns>True if succeeded</returns>
        public bool SetSignalState(SignalState state)
        {
            if (state == State)
                return true;

            if (state == SignalState.Stopped)
            {
                Exit(null); //will set state to Stopped
                return true;
            }

            var oldState = State;
            switch (state)
            {
                case SignalState.Running:
                    if (State == SignalState.RunningSimulated)
                        State = SignalState.Running;
                    break;
                case SignalState.RunningSimulated:
                    if (State == SignalState.Running)
                        State = SignalState.RunningSimulated;
                    break;
                case SignalState.Backtesting:
                    if (State == SignalState.BacktestingPaused)
                        State = SignalState.Backtesting;
                    break;
                case SignalState.BacktestingPaused:
                    if (State == SignalState.Backtesting)
                        State = SignalState.BacktestingPaused;
                    break;
            }

            return State != oldState;
        }

        /// <summary>
        /// Sets current signal to exit
        /// </summary>
        /// <param name="message">Message to trigger as alert</param>
        public void Exit(string message)
        {
            _isBusy = false;
            State = SignalState.Stopped;
            Broker?.Dispose();
            Broker = null;

            if (!string.IsNullOrEmpty(message))
                Alert(message);
        }

        public override object InitializeLifetimeService()
        {
            var lease = (System.Runtime.Remoting.Lifetime.ILease)base.InitializeLifetimeService();
            if (lease != null && lease.CurrentState == System.Runtime.Remoting.Lifetime.LeaseState.Initial)
                lease.InitialLeaseTime = TimeSpan.FromDays(256);
            return lease;
        }

        #region ScriptingLogHelpers

        //public void Output(string output) => _outputs.Add(new Output(DateTime.Now, ShortName, output));
        public void Output(string output) => _outputs.Add(new Output(DateTime.Now, Name, output));

        public List<Output> GetOutputs()
        {
            var outputs = new List<Output>(_outputs);
            _outputs.Clear();
            return outputs;
        }

        public void TradeSignal(TradeSignal tradeSignal) => _generatedSignals.Add(tradeSignal);
        public void TradeSignal(IEnumerable<TradeSignal> tradeSignals) => _generatedSignals.AddRange(tradeSignals);

        public IEnumerable<TradeSignal> GetTradeSignals()
        {
            var signals = new List<TradeSignal>(_generatedSignals);
            _generatedSignals.Clear();
            return signals;
        }

        #endregion

        #region Close Position Helpers

        public bool IsClosingPosition(Selection selection, AccountInfo accountInfo)
        {
            if (_isPositionClosing.TryGetValue(accountInfo, out var selections))
            {
                return selections != null && selections.Contains(selection);
            }

            return false;
        }

        public void AddClosePositionMarker(Selection selection, AccountInfo accountInfo)
        {
            if (_isPositionClosing.ContainsKey(accountInfo))
                _isPositionClosing[accountInfo].Add(selection);
            else
                _isPositionClosing.Add(accountInfo, new List<Selection> { selection });
        }

        public void RemoveClosePositionMarker(Selection selection, AccountInfo accountInfo)
        {
            if (_isPositionClosing.TryGetValue(accountInfo, out var selections))
                selections.Remove(selection);
        }

        public List<TradeSignal> GenerateClosePositionSignals(AccountInfo account, Selection selection, DateTime time)
        {
            var positions = GetPositions(account, selection.Symbol);
            var signals = new List<TradeSignal>();

            foreach (var position in positions)
            {
                if (position.Quantity <= 0) continue;

                signals.Add(new TradeSignal
                {
                    Instrument = selection,
                    Side = position.PositionSide == Side.Buy ? Side.Sell : Side.Buy,
                    Quantity = position.Quantity,
                    TimeInForce = TimeInForce.FillOrKill,
                    TradeType = OrderType.Market,
                    Time = time
                });
            }

            return signals;
        }

        public List<TradeSignal> GenerateCloseAllPositionsSignals(AccountInfo account, DateTime time)
        {
            var positions = GetPositions(account);
            var signals = new List<TradeSignal>();

            foreach (var position in positions)
            {
                if (position.Quantity <= 0) continue;

                signals.Add(new TradeSignal
                {
                    Instrument = new Selection { Symbol = position.Symbol },
                    Side = position.PositionSide == Side.Buy ? Side.Sell : Side.Buy,
                    Quantity = position.Quantity,
                    TimeInForce = TimeInForce.FillOrKill,
                    TradeType = OrderType.Market,
                    Time = time
                });
            }

            return signals;
        }

        #endregion

        #region Broker Wrappers

        public List<AccountInfo> BrokerAccounts => State == SignalState.Backtesting
            ? SimulationBroker.AvailableAccounts.ToList()
            : Broker?.AvailableAccounts;

        public List<Portfolio> Portfolios =>
            State == SignalState.Backtesting ? SimulationBroker.Portfolios : Broker?.Portfolios;

        public List<Position> GetPositions(AccountInfo account) => State == SignalState.Backtesting
            ? SimulationBroker.GetPositions(account).ToList()
            : Broker?.GetPositions(account);

        public List<Position> GetPositions(AccountInfo account, string symbol) => State == SignalState.Backtesting
            ? SimulationBroker.GetPositions(account, symbol).ToList()
            : Broker?.GetPositions(account, symbol);

        public List<Security> GetAvailableSecurities(AccountInfo account) => State == SignalState.Backtesting
            ? new List<Security>(0)
            : Broker?.GetAvailableSecurities(account);

        public void PlaceOrder(OrderParams order, AccountInfo account, bool skipAnalyzer = false)
        {
            var processed = skipAnalyzer ? order : AnalyzePreTrade(order);
            if (processed == null || processed.Quantity == 0M) return;

            if (State == SignalState.Backtesting)
                SimulationBroker.PlaceOrder(order, account);
            else
                Broker?.PlaceOrder(order, account);
        }

        public void ModifyOrder(string orderId, decimal? sl, decimal? tp, bool isServerSide, AccountInfo account)
        {
            //(optional) analyze/process provided order details before passing them to broker
            var original = Broker?.GetOrder(orderId, account);
            if (original == null) return;

            var processed = new OrderParams(original)
            {
                SLOffset = sl,
                TPOffset = tp
            };
            processed = AnalyzePreTrade(processed);

            if (processed == null) return;
            if (State != SignalState.Backtesting)
                Broker?.Modify(orderId, processed.SLOffset, processed.TPOffset, isServerSide, account);
        }

        public void CancelOrder(string orderId, AccountInfo account)
        {
            if (State == SignalState.Backtesting)
                SimulationBroker.CancelOrder(orderId, account);
            else
                Broker?.CancelOrder(orderId, account);
        }

        public List<Order> GetOrders(AccountInfo account) => State == SignalState.Backtesting
            ? SimulationBroker.GetOrders(account).ToList()
            : Broker?.GetOrders(account);

        #endregion

        #region DataProviderWrappers

        public Selection CreateSelection(Selection basedOn, string symbol) =>
            CreateSelection(basedOn, symbol, basedOn.BarCount);

        public Selection CreateSelection(Selection basedOn, string symbol, int barCount) => new Selection
        {
            Symbol = symbol,
            DataFeed = basedOn.DataFeed,
            BarCount = barCount,
            BidAsk = basedOn.BidAsk,
            IncludeWeekendData = basedOn.IncludeWeekendData,
            Level = basedOn.Level,
            Leverage = basedOn.Leverage,
            MarketDataSlot = basedOn.MarketDataSlot,
            Slippage = basedOn.Slippage,
            TimeFactor = basedOn.TimeFactor,
            Timeframe = basedOn.Timeframe,
            From = basedOn.From,
            To = basedOn.To
        };

        public List<Bar> GetHistoricalBars(Selection selection) => DataProvider.GetBars(selection);
        public Tick GetLastTick(Selection selection) => DataProvider.GetLastTick(selection.DataFeed, selection.Symbol);

        public Tick GetHistoricalTick(Selection selection, DateTime dateTime) =>
            DataProvider.GetTick(selection.DataFeed, selection.Symbol, dateTime);

        #endregion

        #region Static Helpers

        public static IEnumerable<OrderParams> GenerateOrderParams(IEnumerable<TradeSignal> signals)
        {
            if (signals == null)
                return new List<OrderParams>(0);

            return signals.Select(tradeSignal => new OrderParams
            {
                UserID = DateTime.Now.Ticks.ToString(),
                SignalId = tradeSignal.Id,
                Symbol = tradeSignal.Instrument.Symbol,
                TimeInForce = tradeSignal.TimeInForce,
                Quantity = tradeSignal.Quantity,
                Price = tradeSignal.Price,
                OrderType = tradeSignal.TradeType,
                OrderSide = tradeSignal.Side,
                SLOffset = tradeSignal.SLOffset,
                TPOffset = tradeSignal.TPOffset,
                ServerSide = tradeSignal.SLOffset.HasValue || tradeSignal.TPOffset.HasValue
            });
        }

        private static List<List<object>> GetParamCombinations(List<ScriptingParameterBase> parameters)
        {
            var result = new List<List<object>>();
            for (var i = 0; i < parameters.Count; i++)
            {
                result.Add(new List<object>());
                if (parameters[i] is IntParam)
                {
                    var p = parameters[i] as IntParam;
                    for (var j = p.StartValue; j <= p.StopValue; j += p.Step)
                    {
                        result[i].Add(j);
                        if (p.Step <= 0)
                            break;
                    }
                }
                else if (parameters[i] is DoubleParam)
                {
                    var p = parameters[i] as DoubleParam;
                    var val = p.StartValue;
                    do
                    {
                        result[i].Add(val);
                        val += p.Step;
                        if (p.Step <= 0.0)
                            break;
                    } while (val <= p.StopValue);
                }
                else if (parameters[i] is BoolParam boolParam)
                {
                    result[i].Add(boolParam.Value);
                }
                else
                {
                    result[i].Add(parameters[i].ValueAsString);
                }
            }

            return result;
        }

        private static BacktestSummary GetBacktestSummary(Selection instrument, IEnumerable<TradeSignal> trades,
            decimal lastPrice = 0m)
        {
            if (!trades.Any())
                return new BacktestSummary(instrument ?? new Selection());

            var qty = 0;
            var backtesterTrades = new List<Trade>(trades.Count());
            foreach (var item in trades)
            {
                var type = SignalType.NoTrade;
                if (qty <= 0 && item.Side == Side.Sell)
                    type = SignalType.Sell;
                else if (qty >= 0 && item.Side == Side.Buy)
                    type = SignalType.Buy;
                else if (qty < 0 && item.Side == Side.Buy)
                    type = SignalType.ExitShort;
                else if (qty > 0 && item.Side == Side.Sell)
                    type = SignalType.ExitLong;

                qty += (item.Side == Side.Buy ? 1 : -1);
                backtesterTrades.Add(new Trade(item.Time, type, item.Price, item.Quantity));
            }

            if (instrument == null)
                instrument = new Selection();

            var results = BacktestProcessor.Backtest(backtesterTrades, lastPrice, instrument.Slippage);

            return results == null
                ? new BacktestSummary(instrument)
                : new BacktestSummary
                {
                    Selection = (Selection)instrument.Clone(),
                    AnnualizedSortinoRatioMAR5 = results.AnnualizedSortinoRatioMAR5,
                    CalmarRatio = results.CalmarRatio,
                    CompoundMonthlyROR = results.CompoundMonthlyROR,
                    DownsideDeviationMar10 = results.DownsideDeviationMar10,
                    LargestLoss = results.LargestLoss,
                    LargestProfit = results.LargestProfit,
                    MaximumDrawDown = results.MaximumDrawDown,
                    MaximumDrawDownMonteCarlo = results.MaximumDrawDownMonteCarlo,
                    NumberOfTradeSignals = backtesterTrades.Count,
                    NumberOfTrades = results.TotalNumberOfTrades,
                    NumberOfLosingTrades = results.NumberOfLosingTrades,
                    NumberOfProfitableTrades = results.NumberOfProfitableTrades,
                    PercentProfit = results.PercentProfit,
                    RiskRewardRatio = results.RiskRewardRatio,
                    SharpeRatio = results.SharpeRatio,
                    SortinoRatioMAR5 = results.SortinoRatioMAR5,
                    StandardDeviation = results.StandardDeviation,
                    StandardDeviationAnnualized = results.StandardDeviationAnnualized,
                    SterlingRatioMAR5 = results.SterlingRatioMAR5,
                    TotalLoss = results.TotalLoss,
                    TotalProfit = results.TotalProfit,
                    ValueAddedMonthlyIndex = results.ValueAddedMonthlyIndex
                };
        }

        private static string[] GetParamNames(IEnumerable<ScriptingParameterBase> parameters, bool numericOnly = false)
        {
            return numericOnly
                ? parameters.Where(i => i is IntParam || i is DoubleParam).Select(i => i.Name).ToArray()
                : parameters.Select(i => i.Name).ToArray();
        }

        private static byte[] CompressTrades(IEnumerable<TradeSignal> trades)
        {
            if (trades == null || !trades.Any())
                return null;

            try
            {
                var sb = new System.Text.StringBuilder();
                foreach (var t in trades)
                {
                    sb.AppendFormat("{0:yyyy-MM-dd HH:mm:ss.fff}|{1}|{2};",
                        t.Time, t.Side == Side.Sell ? -t.Price : t.Price, t.Quantity);
                }

                return Compression.Compress(System.Text.Encoding.UTF8.GetBytes(sb.ToString()));
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.TraceError($"Failed to pack {trades.Count()} signal trades: {e.Message}");
                return null;
            }
        }

        private static bool IsSameInstruments(Selection instrument1, Selection instrument2)
        {
            if (instrument1 == null || instrument2 == null)
                return false;

            return instrument1.Symbol == instrument2.Symbol
                   && instrument1.DataFeed == instrument2.DataFeed
                   && instrument1.TimeFactor == instrument2.TimeFactor
                   && instrument1.Timeframe == instrument2.Timeframe
                   && instrument1.Level == instrument2.Level;
        }

        private static IEnumerable<IEnumerable<T>> CartesianProduct<T>(IEnumerable<IEnumerable<T>> sequences)
        {
            IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };
            return sequences.Aggregate(emptyProduct, (acc, seq) =>
                from accseq in acc
                from item in seq
                select accseq.Concat(new[] { item }));
        }

        #endregion

        #region Overrides

        public override int GetHashCode()
            => ID.GetHashCode() ^ Name.GetHashCode() ^ Owner.GetHashCode() ^ ShortName.GetHashCode();

        public override bool Equals(object obj)
        {
            var signal = obj as SignalBase;
            if (signal == null)
                return false;

            return signal.ID == ID && signal.Name == Name && signal.ShortName == ShortName && signal.Owner == Owner;
        }

        #endregion // Overrides

    }
}