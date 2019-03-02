/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using CommonObjects;
using ServerCommonObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ServerCommonObjects.Classes;
using Scripting;
using Scripting.TechnicalIndicators;
using System.Text;
using RabbitMQ.Client;

namespace ScriptingService
{
    public class ScriptingManager
    {

        #region Members and Events

        private IConnection _rabbitConnection;
        private IModel _rabbitModel;

        private const StringComparison IgnoreCase = StringComparison.OrdinalIgnoreCase;

        private readonly Connector _connector;
        private readonly IDataProvider _dataProvider;

        private readonly string _indicatorsFolderPath;
        private readonly string _signalsFolderPath;

        private readonly HashSet<SignalBase> _userWorkingSignals;
        private readonly Dictionary<string, List<Signal>> _userSignals;
        private readonly Dictionary<string, Queue<Tick>> _signalTicks;
        private readonly List<string> _startedBacktestSignals;
        private readonly List<ScriptingAppDomainContext> _signalDomains;

        private readonly Dictionary<string, List<ScriptingParameterBase>> _availableStandardIndicators;
        private readonly Dictionary<string, Dictionary<string, List<ScriptingParameterBase>>> _userIndicators;
        private readonly Dictionary<IndicatorBase, Selection> _workingIndicators;
        private readonly List<IndicatorBase> _standardIndicatorsInstances;
        private readonly List<ScriptingAppDomainContext> _indicatorsDomains;

        private readonly Dictionary<string, int> _sentBacktestReports;

        private readonly Timer _tmrPeriodicSignals;
        private readonly Timer _tmrSignalsBacktest;
        private readonly Timer _tmrSignalsCheck;

        public event CodeExitHandler CodeExit;

        public List<string> DefaultIndicators => _availableStandardIndicators.Keys.ToList();

        #endregion

        #region Constructor

        public ScriptingManager(Connector connector, string rabbitMQUserName, string rabbitMQPassword, string rabbitMQVirtualHost, string rabbitMQHostName)
        {
            _connector = connector;
            _dataProvider = new DataProvider(_connector);

            _standardIndicatorsInstances = new List<IndicatorBase>
            {
                new SimpleMovingAverage(),
                new SmoothedMovingAverage (),
                new ExponentialMovingAverage(),
                new LinearWeightedMovingAverage(),
                new AcceleratorOscillator(),
                new AccumulationDistribution(),
                new AverageDirectionalMovement (),
                new Alligator(),
                new AwesomeOscillator(),
                new AverageTrueRange(),
                new BollingerBands(),
                new BearsPower(),
                new BullsPower(),
                new MarketFacilitationIndex(),
                new CommodityChannelIndex(),
                new Envelopes(),
                new ForceIndex(),
                new Gator(),
                new MACD(),
                new MoneyFlowIndex(),
                new Momentum(),
                new OnBalanceVolume(),
                new MovingAverageOfOscillator(),
                new RelativeStrengthIndex(),
                new RelativeVigorIndex(),
                new StandardDeviation(),
                new Volume(),
                new WPercentRange(),
                new ParabolicSAR(),
                new StochasticOscillator(),
                new PL()
            };

            _workingIndicators = new Dictionary<IndicatorBase, Selection>();
            _userIndicators = new Dictionary<string, Dictionary<string, List<ScriptingParameterBase>>>();
            _availableStandardIndicators = new Dictionary<string, List<ScriptingParameterBase>>();
            _indicatorsDomains = new List<ScriptingAppDomainContext>();

            _userWorkingSignals = new HashSet<SignalBase>();
            _userSignals = new Dictionary<string, List<Signal>>();
            _signalDomains = new List<ScriptingAppDomainContext>();
            _signalTicks = new Dictionary<string, Queue<Tick>>();

            _startedBacktestSignals = new List<string>();
            _sentBacktestReports = new Dictionary<string, int>();

            foreach (var indicator in _standardIndicatorsInstances)
                _availableStandardIndicators.Add(indicator.Name, indicator.GetParameters().ToList());

            try
            {
                const string folder = "CustomIndicators";
                _indicatorsFolderPath = Directory.Exists(folder)
                    ? new DirectoryInfo(folder).FullName
                    : Directory.CreateDirectory(folder).FullName;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to create folder for custom indicators", ex);
            }

            try
            {
                const string folder = "CustomSignals";
                _signalsFolderPath = Directory.Exists(folder)
                    ? new DirectoryInfo(folder).FullName
                    : Directory.CreateDirectory(folder).FullName;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to create folder for custom Signals", ex);
            }

            CreateDBSignalQueues(rabbitMQUserName, rabbitMQPassword, rabbitMQVirtualHost, rabbitMQHostName);

            _tmrPeriodicSignals = new Timer(RecalculateOnTimer, null, 0, 500);
            _tmrSignalsBacktest = new Timer(UpdateBacktestStatus, null, 0, Timeout.Infinite);
            _tmrSignalsCheck = new Timer(CheckTradeSignals, null, 0, Timeout.Infinite);
        }

        #endregion

        #region Signals

        public SignalBase StartSignalExecution(string userName, SignalInitParams p, List<PortfolioAccount> accountInfos, Dictionary<string, byte[]> files)
        {
            SaveSignalData(userName, p.FullName, files, out var errors);
            if (!string.IsNullOrWhiteSpace(errors))
            {
                Logger.Error(errors);
                return null;
            }

            SignalBase signalBase = null;
            try
            {
                signalBase = CreateSignalInstance(userName, p.FullName);
                var res1 = signalBase.SetParameters(p.Parameters);
                var res2 = signalBase.Init(new Broker(_connector, userName, accountInfos, signalBase),
                    _dataProvider, p.Selections, p.State, p.StrategyParameters);

                if (!res1 || !res2)
                    RemoveWorkingSignal(p.FullName, userName);

                AddWorkingSignal(signalBase, userName);

                if (p.State == SignalState.Backtesting && p.BacktestSettings != null)
                {
                    signalBase.StartBacktest(p.BacktestSettings);
                    Console.WriteLine($"Scripting backtest started {signalBase.ShortName}");
                    return signalBase;
                }

                Console.WriteLine($"Scripting started {signalBase.ShortName}");
                return signalBase;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message, ex);
                if (signalBase != null)
                    RemoveWorkingSignal(p.FullName, userName);

                return null;
            }
        }

        private void SaveSignalData(string userName, string path, Dictionary<string, byte[]> files, out string errors)
        {
            errors = string.Empty;
            var name = CommonHelper.GetDirectoryName(path);
            if (!files.Any(p => p.Key.EndsWith("\\" + name + ".dll")))
            {
                errors = "DLL for " + name + " signal is not found among supplied files";
                Logger.Error("Failed to create signal: " + errors);
                return;
            }

            var root = Path.Combine(_signalsFolderPath, userName);
            var cleanedUpDirs = new List<string>();
            foreach (var file in files)
            {
                try
                {
                    var dir = Path.GetDirectoryName(Path.Combine(root, file.Key));
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    if (!cleanedUpDirs.Contains(dir))
                    {
                        cleanedUpDirs.Add(dir);
                        foreach (var f in Directory.GetFiles(dir)) //optional: delete old/existing files
                            File.Delete(f);
                    }

                    File.WriteAllBytes(Path.Combine(root, file.Key), Compression.Decompress(file.Value));
                }
                catch (Exception exc)
                {
                    Logger.Error("Failed to create signal", exc);
                    errors = exc.Message;
                    return;
                }
            }

            try
            {
                errors = ValidateAndAddSignalToList(userName, path);
                var signalDir = Path.Combine(root, path);
                if (!string.IsNullOrEmpty(errors))
                {
                    foreach (var dll in Directory.GetFiles(signalDir, "*.dll"))
                        File.Delete(dll);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to create instance of a signal", ex);
                errors = ex.Message;
            }

            Console.WriteLine($"Scripting saved {path}");
        }

        private void AddWorkingSignal(SignalBase signal, string userName)
        {
            var key = userName + "|" + signal.Name;
            if (_sentBacktestReports.ContainsKey(key))
                _sentBacktestReports[key] = 0;

            lock (_userWorkingSignals)
                _userWorkingSignals.Add(signal);

            lock (_startedBacktestSignals)
            {
                if (signal.State == SignalState.Backtesting)
                {
                    if (!_startedBacktestSignals.Contains(key))
                        _startedBacktestSignals.Add(key);
                }
            }
        }

        private SignalBase CreateSignalInstance(string userName, string signalName)
        {
            var signal = default(SignalBase);
            lock (_userWorkingSignals)
            {
                signal = _userWorkingSignals.FirstOrDefault(x => x.Name == signalName && x.Owner == userName);
                if (signal != null)
                    return signal;
            }

            var context = new ScriptingAppDomainContext(userName, signalName, _signalsFolderPath);

            try
            {
                signal = context.CreateSignal();
                signal.Name = signalName;
                lock (_signalDomains)
                    _signalDomains.Add(context);

                return signal;
            }
            catch (Exception ex)
            {
                lock (_signalDomains)
                {
                    if (signal != null)
                        _signalDomains.Remove(context);
                }

                try { context.Dispose(); }
                catch (Exception disposingEx)
                {
                    Logger.Error("Disposing error", disposingEx);
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                Logger.Error(ex.Message, ex);

                return null;
            }
        }

        public static Signal CreateSignal(SignalBase signalBase, string fullName) => new Signal
        {
            Name = fullName,
            ID = signalBase.ID,
            Parameters = signalBase.GetParameters(),
            Selections = signalBase.Selections,
            State = signalBase.State
        };

        private void RemoveWorkingSignal(string path, string user)
        {
            var dom = default(ScriptingAppDomainContext);
            lock (_userWorkingSignals)
            {
                _userWorkingSignals.RemoveWhere(x =>
                {
                    if (x.Name == path && x.Owner == user)
                    {
                        x.Exit(null);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                });
            }

            lock (_signalDomains)
            {
                dom = _signalDomains.FirstOrDefault(i => i.ScriptingName == path && i.UserName == user);
                if (dom != null)
                    _signalDomains.Remove(dom);
            }

            try
            {
                dom?.Dispose();

                Console.WriteLine($"Scripting removed (path) {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Dispose ex: {ex.Message}");
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private void RemoveSignalData(string login, string path)
        {
            RemoveWorkingSignal(path, login);

            lock (_userSignals)
            {
                if (_userSignals.TryGetValue(login, out var signals))
                {
                    var signalToRemove = signals.FirstOrDefault(i => i.Name == path);
                    if (signalToRemove != null)
                        signals.Remove(signalToRemove);
                }
            }

            var userFolderPath = Path.Combine(_signalsFolderPath, login);
            if (!Directory.Exists(userFolderPath)) return;

            var signalFolderPath = Path.Combine(userFolderPath, path);
            try
            {
                if (Directory.Exists(signalFolderPath))
                    Directory.Delete(signalFolderPath, true);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message, ex);
            }
        }

        public SignalState SetSignalFlag(string username, string path, SignalAction action)
        {
            lock (_userWorkingSignals)
            {
                var signal = _userWorkingSignals.FirstOrDefault(x => x.Name == path && x.Owner == username);
                if (signal == null)
                    return default(SignalState);

                switch (action)
                {
                    case SignalAction.StopExecution:
                        signal.SetSignalState(SignalState.Stopped);
                        RemoveSignalData(username, path);
                        return SignalState.Stopped;

                    case SignalAction.SetSimulatedOn: signal.SetSignalState(SignalState.RunningSimulated); break;
                    case SignalAction.SetSimulatedOff: signal.SetSignalState(SignalState.Running); break;
                    case SignalAction.PauseBacktest: signal.SetSignalState(SignalState.BacktestingPaused); break;
                    case SignalAction.ResumeBacktest: signal.SetSignalState(SignalState.Backtesting); break;
                }
                return signal.State;
            }
        }

        private string ValidateAndAddSignalToList(string login, string path)
        {
            try
            {
                lock (_userSignals)
                {
                    if (_userSignals.TryGetValue(login, out var signals))
                    {
                        var signalToRemove = signals.FirstOrDefault(i => i.Name == path);
                        if (signalToRemove != null)
                            signals.Remove(signalToRemove);
                    }
                }

                var context = new ScriptingAppDomainContext(login, path, _signalsFolderPath);
                try
                {
                    var signal = context.CreateSignal();
                    if (signal != null)
                    {
                        lock (_userSignals)
                        {
                            if (!_userSignals.ContainsKey(login))
                                _userSignals.Add(login, new List<Signal>());

                            _userSignals[login].Add(new Signal
                            {
                                Name = path,
                                ID = signal.ID,
                                State = signal.State,
                                Parameters = signal.GetParameters(),
                                Selections = signal.Selections
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to create new signal", ex);
                    return "Failed to create signal: " + path;
                }
                finally
                {
                    try { context.Dispose(); }
                    catch (Exception ex) { Logger.Error("Failed to dispose signal context", ex); }

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }


                return string.Empty;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to validate/create user signal", ex);
                return ex.Message;
            }
        }

        private void EnqueueSignalTicks(string signalName, string signalOwner, IList<Tick> ticks)
        {
            if (ticks == null || !ticks.Any())
                return;

            Queue<Tick> ticksQueue;

            var key = new StringBuilder();
            key.Append(signalOwner);
            key.Append("|");
            key.Append(signalName);

            var reskey = key.ToString();

            lock (_signalTicks)
            {
                if (!_signalTicks.TryGetValue(reskey, out ticksQueue))
                {
                    ticksQueue = new Queue<Tick>();
                    _signalTicks.Add(reskey, ticksQueue);
                }
            }

            lock (ticksQueue)
            {
                for (var i = 0; i < ticks.Count && i < 100; i++)
                    ticksQueue.Enqueue(ticks[i]);

                while (ticksQueue.Count > 99)  //limit to 100 ticks in queue
                    ticksQueue.Dequeue();
            }
        }

        private List<Tick> GetSignalTicksQueue(string signalName, string signalOwner)
        {
            var key = new StringBuilder();
            key.Append(signalOwner);
            key.Append("|");
            key.Append(signalName);

            var reskey = key.ToString();

            lock (_signalTicks)
            {
                if (!_signalTicks.TryGetValue(reskey, out var queue))
                    return new List<Tick>(0);

                var list = new List<Tick>(queue.Count);
                while (queue.Count > 0)
                    list.Add(queue.Dequeue());

                return list;
            }
        }

        public void UpdateSignalStrategyParams(string login, string signalName, StrategyParams parameters)
        {
            lock (_userWorkingSignals)
            {
                foreach (var signal in _userWorkingSignals)
                {
                    if (signal.Name == signalName && signal.Owner == login)
                        signal.SetStrategyParameters(parameters);
                }
            }
        }

        private void CheckTradeSignals(object state)
        {
            lock (_userWorkingSignals)
            {
                foreach (var workingSignal in _userWorkingSignals)
                {
                    var signals = workingSignal.GetTradeSignals();
                    foreach (var tradeSignal in signals)
                    {
                        var dbSignal = new DbSignal(tradeSignal, workingSignal.Owner, workingSignal.ShortName);
                        var serializedObj = dbSignal.ToJson();
                        if (string.IsNullOrEmpty(serializedObj))
                        {
                            Logger.Info("ScriptingManager.CheckTradeSignals -> can't seriealize obj.");
                            continue;
                        }

                        var body = Encoding.UTF8.GetBytes(serializedObj);

                        try
                        {
                            _rabbitModel.BasicPublish(QueuesHelper.DBSignalQueuesExchangeName, QueuesHelper.DBSignalQueuesName, null, body);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("BasicPublish -> ", ex);
                        }

                        var outputs = workingSignal.GetOutputs();
                        if (outputs.Count > 0)
                            _connector.ScriptingOutput(workingSignal.Owner, outputs);
                    }
                }

                _tmrSignalsCheck.Change(1000, Timeout.Infinite);
            }
        }

        private void CreateDBSignalQueues(string username, string password, string virtualHost, string hostName)
        {
            var factory = new ConnectionFactory
            {
                UserName = username,
                Password = password,
                VirtualHost = virtualHost,
                HostName = hostName
            };

            _rabbitConnection = factory.CreateConnection();
            _rabbitModel = _rabbitConnection.CreateModel();
        }


        #endregion

        #region Backtest

        private void UpdateBacktestStatus(object state)
        {
            var finished = new List<Tuple<string, string>>();
            lock (_userWorkingSignals)
            {
                foreach (var s in _userWorkingSignals)
                {
                    //check backtest progress
                    var key = s.Owner + "|" + s.Name;
                    if (_startedBacktestSignals.Contains(key))
                    {
                        var gotNewResults = false;
                        var progress = 0F;
                        if (!_sentBacktestReports.ContainsKey(key)  //no reports have been sent yet
                            || _sentBacktestReports[key] > s.BacktestResults.Count)  //or backtest was restarted
                        {
                            gotNewResults = true;
                            _sentBacktestReports[key] = s.BacktestResults.Count;
                            progress = s.BacktestProgress;
                            _connector.BacktestReport(s.BacktestResults.ToList(), s.BacktestProgress, s.Owner);
                        }
                        else if (_sentBacktestReports[key] < s.BacktestResults.Count)  //got new reports to send
                        {
                            gotNewResults = true;
                            var idx = _sentBacktestReports[key];
                            var count = s.BacktestResults.Count - idx;
                            _sentBacktestReports[key] = s.BacktestResults.Count;
                            progress = s.BacktestProgress;
                            _connector.BacktestReport(s.BacktestResults.GetRange(idx, count), s.BacktestProgress, s.Owner);
                        }

                        if (gotNewResults && s.BacktestResults.Any())
                            SaveBacktestResults(s.Owner, s.Name, s.BacktestResults);

                        if (progress >= 100F)
                        {
                            _startedBacktestSignals.Remove(key);
                            finished.Add(new Tuple<string, string>(s.Name, s.Owner));
                            s.SetSignalState(SignalState.Stopped);
                            CodeExit?.Invoke(ScriptingType.Signal, s.Name, s.Owner);
                        }
                    }

                    //check for alerts
                    var alerts = s.GetActualAlerts();
                    if (alerts != null && alerts.Count > 0)
                        _connector.SciriptingAlert(alerts, ScriptingType.Signal, s.Name, s.Owner);
                }
            }

            foreach (var item in finished)
                RemoveWorkingSignal(item.Item1, item.Item2);

            _tmrSignalsBacktest.Change(1000, Timeout.Infinite);
        }

        private void SaveBacktestResults(string user, string signal, List<BacktestResults> results) =>
            _connector.SendBacktestResults(user, signal, results);

        #endregion

        #region Recalculation

        private void RecalculateOnTimer(object state)
        {
            var signals = new Dictionary<SignalBase, Selection>();

            lock (_userWorkingSignals)
            {
                var now = DateTime.UtcNow;
                foreach (var signalBase in _userWorkingSignals)
                {
                    if (signalBase.IsBusy() || !signalBase.IsReadyToRun || signalBase.StartMethod != StartMethod.Periodic) continue;
                    var period = Math.Max(signalBase.ExecutionPeriod, 500);

                    if ((now - signalBase.PreviousRunTime).TotalMilliseconds >= period)
                        signals.Add(signalBase, null);
                }
            }

            if (signals.Count > 0)
                RecalculateSignals(signals);
        }

        public void RecalculateOnNewTick(Tick tick)
        {
            List<IndicatorBase> indicators;
            var signals = new Dictionary<SignalBase, Selection>();

            lock (_workingIndicators)
            {
                indicators = _workingIndicators
                    .Where(i => !i.Key.StartOnNewBar
                        && i.Value.Symbol.Equals(tick.Symbol.Symbol, IgnoreCase)
                        && i.Value.DataFeed.Equals(tick.DataFeed, IgnoreCase))
                    .Select(i => i.Key).ToList();
            }

            lock (_userWorkingSignals)
            {
                foreach (var item in _userWorkingSignals)
                {
                    if (item.IsReadyToRun && item.StartMethod == StartMethod.NewTick)
                    {
                        foreach (var i in item.Selections)
                        {
                            if (i.Symbol.Equals(tick.Symbol.Symbol, IgnoreCase)
                                && i.DataFeed.Equals(tick.DataFeed, IgnoreCase))
                            {
                                EnqueueSignalTicks(item.Name, item.Owner, new List<Tick> { tick });
                                signals.Add(item, i);
                                break;
                            }
                        }
                    }
                }
            }

            if (indicators.Any())
                RecalculateIndicators(indicators);

            if (signals.Any())
                RecalculateSignals(signals);
        }

        public void RecalculateOnNewBar(string symbol, string dataFeed)
        {
            List<IndicatorBase> indicators;
            var signals = new Dictionary<SignalBase, Selection>();

            lock (_workingIndicators)
            {
                indicators = _workingIndicators
                    .Where(i => i.Key.StartOnNewBar
                        && i.Value.Symbol.Equals(symbol, IgnoreCase)
                        && i.Value.DataFeed.Equals(dataFeed, IgnoreCase))
                    .Select(i => i.Key).ToList();
            }

            lock (_userWorkingSignals)
            {
                foreach (var item in _userWorkingSignals)
                {
                    if (item.IsReadyToRun && item.StartMethod == StartMethod.NewBar)
                    {
                        foreach (var i in item.Selections)
                        {
                            if (i.Symbol.Equals(symbol, IgnoreCase)
                                && i.DataFeed.Equals(dataFeed, IgnoreCase))
                            {
                                signals.Add(item, i);
                                break;
                            }
                        }
                    }
                }
            }

            if (indicators.Any())
                RecalculateIndicators(indicators);

            if (signals.Any())
                RecalculateSignals(signals);
        }

        private void RecalculateIndicators(IEnumerable<IndicatorBase> indicators)
        {
            foreach (var ind in indicators)
            {
                try
                {
                    var count = ind.Calculate();
                    if (count == 0)
                        return;

                    var series = new List<SeriesForUpdate>();

                    foreach (var sr in ind.Series.ToList())
                    {
                        series.Add(new SeriesForUpdate
                        {
                            SeriesID = sr.ID,
                            IndicatorName = ind.Name
                        });

                        var seriesCopy = sr.Values.ToList();
                        if (count >= seriesCopy.Count)
                        {
                            foreach (var update in sr.Values.ToList())
                                series.Last().Values[update.Date] = update.Value;
                        }
                        else
                        {
                            for (var j = seriesCopy.Count - count - 1; j < seriesCopy.Count; j++)
                                series.Last().Values[seriesCopy[j].Date] = seriesCopy[j].Value;
                        }
                    }
                    _connector.SeriesUpdated(series.ToList(), ind.Owner);

                    var alerts = ind.GetActualAlerts();
                    if (alerts.Count > 0)
                        _connector.SciriptingAlert(alerts, ScriptingType.Indicator, ind.Name, ind.Owner);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message, ex);
                }
            }
        }

        private void RecalculateSignals(IDictionary<SignalBase, Selection> signals)
        {
            foreach (var item in signals)
            {
                if (item.Key.IsBusy())
                    continue;

                try
                {
                    var signal = item.Key;
                    var queuedTicks = signal.StartMethod == StartMethod.NewTick
                        ? GetSignalTicksQueue(signal.Name, signal.Owner)
                        : null;

                    signal.Start(item.Value, queuedTicks);

                    if (signal.State == SignalState.Stopped)
                    {
                        var exitAlerts = signal.GetActualAlerts();
                        if (exitAlerts != null && exitAlerts.Count > 0)
                            _connector.SciriptingAlert(exitAlerts, ScriptingType.Signal, signal.Name, signal.Owner);

                        CodeExit?.Invoke(ScriptingType.Signal, signal.Name, signal.Owner);
                        return;
                    }

                    var alerts = signal.GetActualAlerts();
                    if (alerts != null && alerts.Count > 0)
                        _connector.SciriptingAlert(alerts, ScriptingType.Signal, signal.Name, signal.Owner);
                }
                catch (AppDomainUnloadedException)
                {
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message, e);
                }
            }
        }

        #endregion

        #region Indicators

        public Indicator StartIndicatorInstance(string login, string name, PriceType priceType, List<ScriptingParameterBase> parameters, Selection selection)
        {
            var instance = CreateIndicatorInstance(login, name);
            if (string.IsNullOrEmpty(instance.Owner))
                instance.Owner = login;

            instance.PriceType = priceType;
            instance.SetParameters(parameters);
            instance.Init(selection, _dataProvider);
            AddWorkingIndicator(instance, selection);

            Console.WriteLine($"Indicator started {instance.Name}");

            return new Indicator
            {
                Name = instance.Name,
                DisplayName = instance.DisplayName,
                ID = instance.ID,
                IsOverlay = instance.IsOverlay,
                Parameters = parameters.ToList(),
                Series = instance.Series.ToList()
            };
        }

        private IndicatorBase CreateIndicatorInstance(string userName, string indicatorName)
        {
            var indicator = _standardIndicatorsInstances.FirstOrDefault(p => p.Name.Equals(indicatorName));
            if (indicator != null)
                return (IndicatorBase)Activator.CreateInstance(indicator.GetType());

            lock (_userIndicators)
            {
                if (!_userIndicators.TryGetValue(userName, out var dictionary) || !dictionary.ContainsKey(indicatorName))
                    return null;
            }

            var context = new ScriptingAppDomainContext(userName, indicatorName, _indicatorsFolderPath);
            var newIndicator = default(IndicatorBase);

            try
            {
                newIndicator = context.CreateIndicator();
                lock (_indicatorsDomains)
                    _indicatorsDomains.Add(context);

                return newIndicator;
            }
            catch (Exception ex)
            {
                if (newIndicator != null)
                {
                    lock (_userIndicators)
                        _indicatorsDomains.Remove(context);
                }

                try { context.Dispose(); }
                catch (Exception disposingEx)
                {
                    Logger.Error("Disposing error", disposingEx);
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                Logger.Error(ex.Message, ex);
                return null;
            }
        }

        private void AddWorkingIndicator(IndicatorBase indicator, Selection selection)
        {
            lock (_workingIndicators)
                _workingIndicators.Add(indicator, selection);
        }

        public void RemoveWorkingIndicator(string name, string userName)
        {
            lock (_workingIndicators)
            {
                var indicator = _workingIndicators.FirstOrDefault(p => p.Key.Name == name && p.Key.Owner == userName);
                if (indicator.Key != null)
                {
                    _workingIndicators.Remove(indicator.Key);
                    Console.WriteLine($"Indicator {indicator.Key.Name} was removed");
                }
            }

            lock (_userIndicators)
            {
                var dom = _indicatorsDomains.FirstOrDefault(i => i.UserName == userName && i.ScriptingName == name);
                if (dom != null)
                {
                    _indicatorsDomains.Remove(dom);
                    try { dom.Dispose(); }
                    catch { }

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
            }
        }

        public void SaveCustomIndicatorData(string login, string name, Dictionary<string, byte[]> dlls)
        {
            var error = RemoveCustomIndicatorData(login, name);

            if (!string.IsNullOrEmpty(error)) return;

            if (dlls.Count(p => p.Key.Equals(name + ".dll")) == 0)
            {
                Logger.Error($"DLL with indicator name {name} is not found.");
                return;
            }

            if (!dlls.Any(p => p.Key.EndsWith(".dll")))
            {
                Logger.Error("List of dll`s contain invalid data.");
                return;
            }

            var userFolder = Path.Combine(_indicatorsFolderPath, login);
            var indicatorFolder = Path.Combine(userFolder, name);

            try
            {
                if (!Directory.Exists(userFolder))
                    Directory.CreateDirectory(userFolder);

                if (Directory.Exists(indicatorFolder))
                    Directory.Delete(indicatorFolder, true);

                Directory.CreateDirectory(indicatorFolder);

                foreach (var dll in dlls)
                {
                    var data = Compression.Decompress(dll.Value);
                    File.WriteAllBytes(Path.Combine(indicatorFolder, dll.Key), data);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to create custom indicator", ex);
                return;
            }

            try
            {
                ValidateAndAddUserIndicatorToList(login, name);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to create instance of custom indicator", ex);
            }
        }

        private string RemoveCustomIndicatorData(string login, string name)
        {
            var userFolderPath = Path.Combine(_indicatorsFolderPath, login);
            ScriptingAppDomainContext dom;
            lock (_userIndicators)
                dom = _indicatorsDomains.FirstOrDefault(i => i.UserName == login && i.ScriptingName == name);

            if (dom != null)
            {
                lock (_userIndicators)
                    _indicatorsDomains.Remove(dom);

                lock (_workingIndicators)
                {
                    var indicator = _workingIndicators.FirstOrDefault(p => p.Key.Name == name && p.Key.Owner == login);
                    if (indicator.Key != null)
                        _workingIndicators.Remove(indicator.Key);
                }

                try { dom.Dispose(); }
                catch (Exception ex) { Logger.Error(ex.Message, ex); }
            }

            lock (_userIndicators)
            {
                if (_userIndicators.TryGetValue(login, out var dictionary))
                    dictionary.Remove(name);
            }

            if (!Directory.Exists(userFolderPath))
                return null;

            try
            {
                var indicatorFolderPath = Path.Combine(userFolderPath, name);
                if (Directory.Exists(indicatorFolderPath))
                    Directory.Delete(indicatorFolderPath, true);
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message, ex);
                return "Failed to remove user indicator " + name;
            }
        }

        private void ValidateAndAddUserIndicatorToList(string user, string name)
        {
            try
            {
                Dictionary<string, List<ScriptingParameterBase>> dictionary;

                lock (_userIndicators)
                {
                    if (!_userIndicators.TryGetValue(user, out dictionary))
                    {
                        dictionary = new Dictionary<string, List<ScriptingParameterBase>>();
                        _userIndicators.Add(user, dictionary);
                    }
                }

                lock (dictionary)
                    dictionary.Remove(name);

                var context = new ScriptingAppDomainContext(user, name, _indicatorsFolderPath);

                try
                {
                    var indicator = context.CreateIndicator();

                    lock (dictionary)
                        dictionary.Add(name, indicator.GetParameters().ToList());
                }
                catch (Exception ex)
                {
                    Logger.Error("ValidateUserIndicator - failed to create new indicator.", ex);
                }
                finally
                {
                    try
                    {
                        context.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("ValidateUserIndicator - failed to dispose indicator context.", ex);
                    }
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ValidateUserIndicator - common exception.", ex);
            }
        }

        #endregion

    }
}
