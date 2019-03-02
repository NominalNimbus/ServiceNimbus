/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommonObjects;
using ServerCommonObjects;
using ServerCommonObjects.Classes;
using ServerCommonObjects.Interfaces;
using ServerCommonObjects.Managers;
using ServerCommonObjects.ServerClasses;
using ServerCommonObjects.SQL;
using OMS;
using PortfolioManager;
using RabbitMQ.Client;
using System.Configuration;
using Server.Queues;
using ScriptingManager;

namespace Server
{
    public class Core : ICore
    {
        #region Members and Properties

        private IConnection _rabbitConnection;
        private IModel _rabbitModel;

        private readonly List<string> _availableBrokers;
        private readonly List<string> _availableDataFeeds;
        private readonly List<IDataFeed> _availableDataFeedsInstances;
        private readonly IDataCacheManager _dataCacheManager;
        private readonly Dictionary<string, IUserInfo> _users;
        private readonly ScriptingServiceManager _codeManager;
        private readonly PortfolioSystem _portfolioSystem;
        private string _connectionString;
        private readonly IHistoryDataMultiTimeframeCache _multiTimeframeDataCache;
        private readonly System.Timers.Timer _tmrDbDataArchival;
        private double _minuteBarsProgress;
        private double _eodBarsProgress;
        private DateTime _prevDbArchivalTime;
        private readonly DBSignals _dbSignals;

        private readonly List<IWCFProcessorInfo> _processors;
        private SignalQueues _signalQueues;

        public IOMS OMS { get; }

        public List<string> AvailableDataFeeds => _availableDataFeeds.ToList();

        public List<string> AvailableBrokers => _availableBrokers.ToList();

        public bool IsTickCacheEnabled
        {
            get => _multiTimeframeDataCache.CacheTickData;
            set => _multiTimeframeDataCache.CacheTickData = value;
        }

        #endregion

        #region Managers

        public IFileManager FileManager { get; }
        public ISettingsManager SettingsManager { get; }

        #endregion // Managers

        #region Events

        public event NewTickHandler NewTick;
        public event ScriptingExitHandler ScriptingExit;
        public event ScriptingBacktestReportHandler BacktestReport;
        public event EventHandler<EventArgs<string>> Notification;
        public event SetSignalFlagHandler SignalFlag;
        public event RemoveIndicatorHandler RemoveIndicator;

        public event EventHandler<Tick> NewSingleTick;
        public event EventHandler<Tuple<string, string>> NewBar;

        #endregion

        public Core()
        {
            FileManager = new FileManager();
            SettingsManager = new JSONSettingsManager(FileManager);

            _tmrDbDataArchival = new System.Timers.Timer(10 * 60 * 1000);
            _tmrDbDataArchival.Elapsed += TimerDbDataArchivalOnElapsed;
            _availableBrokers = new List<string>();
            _availableDataFeeds = new List<string>();
            _dataCacheManager = new HistoryDataStoreCache();// DataCacheManager();
            _availableDataFeedsInstances = new List<IDataFeed>();
            _portfolioSystem = new PortfolioSystem();
            _users = new Dictionary<string, IUserInfo>();
            _processors = new List<IWCFProcessorInfo>();
            _dbSignals = new DBSignals();

            MessageRouter.gMessageRouter = new MessageRouter();
            MessageRouter.gMessageRouter.AddedProcessorSession += OnAddedProcessorSession;
            MessageRouter.gMessageRouter.RemovedProcessorSession += OnRemovedProcessorSession;

            _codeManager = new ScriptingServiceManager(this);
            //_codeManager.CodeExit += CodeManagerOnCodeExit;  //TODO

            _dataCacheManager.CachedDataProgress += DataCacheManagerOnCachedDataProgress;
            _multiTimeframeDataCache = new HistoryDataMultiTimeframeCache(_dataCacheManager);

            OMS = new OrderManager();
        }

        public void Start(List<IDataFeed> dataFeeds, string connectionString)
        {
            _connectionString = connectionString;

            lock (_availableDataFeeds)
            {
                _availableDataFeeds.Clear();
                _availableDataFeeds.AddRange(dataFeeds.Select(p => p.Name));
            }

            lock (_availableBrokers)
            {
                _availableBrokers.Clear();
                //_availableBrokers.AddRange(BrokerFactory.AvailableBrokers.Select(b=>b.BrokerName));
            }

            lock (_availableDataFeedsInstances)
            {
                _availableDataFeedsInstances.Clear();
                _availableDataFeedsInstances.AddRange(dataFeeds);

                foreach (var dataFeed in _availableDataFeedsInstances)
                    dataFeed.NewTick += DataFeedOnNewTick;
            }

            lock (_portfolioSystem)
                _portfolioSystem.Start(_connectionString);

            MessageRouter.gMessageRouter.RemovedSession += SessionRemoved;

            lock (_dataCacheManager)
                _dataCacheManager.Start(dataFeeds, _connectionString);

            OMS.Start(dataFeeds, _connectionString);

            _dataCacheManager.NewMinBar += DataCacheManagerOnNewMinBar;

            _tmrDbDataArchival.Start();
            _dbSignals.Start(connectionString);
            StartDbSignalQueue();
        }

        public void Stop()
        {
            _tmrDbDataArchival.Stop();

            lock (_availableDataFeeds)
                _availableDataFeeds.Clear();

            lock (_availableBrokers)
                _availableBrokers.Clear();

            lock (_dataCacheManager)
                _dataCacheManager.Stop();

            lock (_users)
                _users.Clear();

            lock (_portfolioSystem)
                _portfolioSystem.Stop();

            OMS.Stop();

            _connectionString = string.Empty;

            StopDBSignalQueue();

            MessageRouter.gMessageRouter.RemovedSession -= SessionRemoved;
        }

        #region Portfolio Manager

        public List<Portfolio> GetPortfolios(IUserInfo user)
        {
            var portfolios = _portfolioSystem.GetPortfolios(user);

            //get signals from directory structure
            if (portfolios != null && portfolios.Count > 0)
            {
                foreach (var p in portfolios)
                    foreach (var s in p.Strategies)
                    {
                        s.Signals = new string[0];
                        var dir = Path.Combine("CustomSignals", user.Login, p.Name, s.Name);
                        if (Directory.Exists(dir))
                            s.Signals = Directory.GetDirectories(dir).Select(CommonHelper.GetDirectoryName).ToArray();
                    }
            }

            return portfolios;
        }

        public int AddPortfolio(Portfolio portfolio, string user)
        {
            return _portfolioSystem.AddPortfolio(portfolio, user);
        }

        public bool RemovePortfolio(Portfolio portfolio)
        {
            return _portfolioSystem.RemovePortfolio(portfolio);
        }

        public bool UpdatePortfolio(IUserInfo user, Portfolio portfolio)
        {
            return _portfolioSystem.UpdatePortfolio(user, portfolio);
        }

        #endregion

        #region Data Cache

        public decimal GetTotalDailyAskVolume(Security security, int level)
        {
            return _dataCacheManager.GetTotalDailyAskVolume(security.Symbol, security.DataFeed, level);
        }

        public decimal GetTotalDailyBidVolume(Security security, int level)
        {
            return _dataCacheManager.GetTotalDailyBidVolume(security.Symbol, security.DataFeed, level);
        }

        public List<Security> GetDatafeedSecurities(string dataFeed)
        {
            return _dataCacheManager.GetDatafeedSecurities(dataFeed);
        }

        #endregion

        #region Scripting

        public IUserInfo GetUser(string userName)
        {
            lock (_users)
                return _users.TryGetValue(userName, out var user) ? user : null;
        }

        public List<IWCFProcessorInfo> GetAvailableProcessors()
        {
            lock (_processors)
                return new List<IWCFProcessorInfo>(_processors);
        }

        public void AddUserFiles(string user, string relativePath, byte[] zippedFiles)
        {
            _codeManager.AddUserFiles(user, relativePath, zippedFiles);
        }

        public void DeleteUserFiles(string user, string[] relativePaths)
        {
            _codeManager.DeleteUserFiles(user, relativePaths);
        }

        public List<string> GetDefaultIndicators() =>
            _codeManager.DefaultIndicators;

        public Dictionary<string, List<ScriptingParameterBase>> GetAllIndicators(IUserInfo user)
        {
            return _codeManager.GetAllAllowedIndicators(user).ToDictionary(i => i.Key, i => i.Value.ToList());
        }

        public List<string> GetSignalFolderPaths(string user)
        {
            return _codeManager.GetSignalFolderPaths(user);
        }

        public Dictionary<string, byte[]> GetIndicatorFiles(IUserInfo user, string name) =>
            _codeManager.GetIndicatorFiles(user.Login, name);

        public List<Signal> GetAllSignals(IUserInfo user)
        {
            return _codeManager.GetSignals(user.Login);
        }

        public void AddSignal(IUserInfo user, Dictionary<string, byte[]> files, SignalInitParams p)
        {
            if (files == null || files.Count <= 0) return;

            _codeManager.SaveSignalData(user, p.FullName, files, out var errors);
            if (!string.IsNullOrWhiteSpace(errors))
                Logger.Error(errors);
        }

        public void SignalStarted(IUserInfo user, Signal signal, IWCFProcessorInfo processor)
        {
            if (signal.State == SignalState.Stopped)
                ScriptingExit(ScriptingType.Signal, signal.ID, user);

            lock (_users)
                _users[user.Login] = user;

            _codeManager.AddWorkingSignal(user.Login, signal, processor.ID);
            OMS.AddActiveSignal(user, signal.Name);
        }

        public void SaveBacktestResults(string user, string signal, List<BacktestResults> results) =>
            _codeManager.SaveBacktestResults(user, signal, results);

        public void IndicatorStarted(IUserInfo user, IWCFProcessorInfo processor, string indicatorName)
        {
            lock (_users)
                _users[user.Login] = user;

            _codeManager.AddWorkingIndicator(user.Login, indicatorName, processor.ID);
        }

        public void RemoveUserIndicator(string name, IUserInfo user)
        {
            var serviceID = _codeManager.ScriptingCodeServiceID(user.Login, name, ScriptingType.Indicator);
            var processor = GetProcessor(serviceID);
            if (processor == null) return;

            _codeManager.RemoveWorkingIndicator(name, user.Login);
            RemoveIndicator?.Invoke(user.Login, name, processor);
        }

        public void RemoveSignal(string path, IUserInfo user)
        {
            SetSignalFlag(user.Login, path, SignalAction.StopExecution);
            OMS.RemoveActiveSignal(user, path);
        }

        public List<ReportField> GetCodeReport(string userLogin, string signalName, DateTime fromTime, DateTime toTime) =>
            _dbSignals.GetReport(userLogin, signalName, fromTime, toTime);

        public List<ScriptingParameterBase> ValidateAndSaveCustomIndicator(IUserInfo user,
            string name, Dictionary<string, byte[]> dlls, out string errors)
        {
            return _codeManager.SaveCustomIndicatorData(user, name, dlls, out errors);
        }

        public Signal ValidateAndSaveSignal(IUserInfo user, string path, Dictionary<string, byte[]> files,
            out string errors)
        {
            return _codeManager.SaveSignalData(user, path, files, out errors);
        }

        public void SetSignalFlag(string login, string path, SignalAction action)
        {
            var serviceID = _codeManager.ScriptingCodeServiceID(login, path, ScriptingType.Signal);
            SignalFlag?.Invoke(serviceID, login, path, action);
        }

        public void SignalFlagSetted(string username, string path, SignalAction action, SignalState state)
        {
            var user = GetUser(username);
            if (action == SignalAction.StopExecution && state == SignalState.Stopped)
            {
                _codeManager.SignalServiceRemoved(username, path);
                OMS.RemoveActiveSignal(user, path);
            }
        }

        public string RemoveCustomIndicatorData(IUserInfo user, string indicatorName)
        {
            return _codeManager.RemoveCustomIndicatorData(user.Login, indicatorName);
        }

        public void CodeBacktestReport(List<BacktestResults> reports, float progress, string username)
        {
            lock (_users)
            {
                if (BacktestReport != null && _users.TryGetValue(username, out var user) && user != null)
                    BacktestReport(reports, progress, user);
            }
        }

        public string RemoveSignalData(IUserInfo user, string path)
        {
            return _codeManager.RemoveSignalData(user, path);
        }

        public List<Signal> GetWorkingSignalsAndUpdateUserInfo(IUserInfo user)
        {
            UpdateUserInfo(user);
            return _codeManager.GetWorkingSignals(user.Login);
        }

        public void UpdateUserInfo(IUserInfo user)
        {
            lock (_users)
                _users[user.Login] = user;
        }

        public string GetScriptingServiceID(string login, string signalName, ScriptingType type) =>
            _codeManager.ScriptingCodeServiceID(login, signalName, type);

        public IWCFProcessorInfo GetProcessor(string id)
        {
            lock (_processors)
                return _processors.FirstOrDefault(p => p.ID == id);
        }

        #endregion

        #region DataFeeds

        public void GetTick(string symbol, string dataFeedName, DateTime timestamp, NewTickHandler callback)
        {
            var tick = _dataCacheManager.GetTick(symbol, dataFeedName, timestamp);
            callback(tick);
        }

        public void GetHistory(Selection p, HistoryAnswerHandler callback)
        {
            var feed = _availableDataFeedsInstances
                .FirstOrDefault(i => i.Name.Equals(p.DataFeed, StringComparison.OrdinalIgnoreCase));
            if (feed == null)
            {
                Logger.Info($"Unable to retrieve historical data for {p.Symbol}: {p.DataFeed} feed is not available");
                callback(p, new List<Bar>());
                return;
            }

            _multiTimeframeDataCache.GetHistory(feed, p, callback);
        }
              
        #endregion

        #region Events

        private void OnAddedProcessorSession(object sender, MessageRouter.ScriptingProcessorEventArgs args)
        {
            lock (_processors)
                _processors.Add(args.ProcessorInfo);
        }

        private void OnRemovedProcessorSession(object sender, MessageRouter.ScriptingProcessorEventArgs args)
        {
            lock (_processors)
                _processors.Remove(args.ProcessorInfo);
        }

        private void DataFeedOnNewTick(Tick tick)
        {
            NewTick?.Invoke(tick);
            _multiTimeframeDataCache.Add2Cache(tick);
            NewSingleTick?.Invoke(this, tick);
        }

        private void DataCacheManagerOnCachedDataProgress(object sender, EventArgs<string, double> e)
        {
            if (e.Value1.StartsWith("minute", StringComparison.OrdinalIgnoreCase))
                _minuteBarsProgress = e.Value2;
            else if (e.Value1.StartsWith("eod", StringComparison.OrdinalIgnoreCase))
                _eodBarsProgress = e.Value2;
            else
                return;

            if (Notification != null)
            {
                var message = (_minuteBarsProgress < 99.99 || _eodBarsProgress < 99.99)
                    ? $"Cache progress: {_minuteBarsProgress:0}% of minute data, {_eodBarsProgress:0}% of EOD data"
                    : string.Empty;
                Notification(this, new EventArgs<string>(message));
            }
        }

        private void SessionRemoved(object sender, MessageRouter.MessageRouter_EventArgs e)
        {
            lock (_users)
            {
                if (_users.ContainsKey(e.UserInfo.Login))
                    _users.Remove(e.UserInfo.Login);
                //else return;  //optional
            }

            foreach (var indicatorsName in _codeManager.GetUserIndicatorsNames(e.UserInfo.Login))
                RemoveUserIndicator(indicatorsName, e.UserInfo);

            _codeManager.ClearWorkingIndicators(e.UserInfo);
            OMS.BrokerAccountsLogout(e.UserInfo, e.UserInfo.Accounts.ToList());
        }

        private void DataCacheManagerOnNewMinBar(object sender, EventArgs<Security> args)
        {
            if (args.Value != null)
                NewBar?.Invoke(this, new Tuple<string, string>(args.Value.Symbol, args.Value.DataFeed));
        }

        private void TimerDbDataArchivalOnElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //optional: do not run while filling data cache
            if (_minuteBarsProgress < 99.99 || _eodBarsProgress < 99.99)
                return;

            //run data every Sunday between 1 AM and 8 PM
            var now = DateTime.Now;
            if ((now - _prevDbArchivalTime.Date).TotalDays < 6)
                return;

            if (now.DayOfWeek != DayOfWeek.Sunday || now.Hour == 0 || now.Hour > 20)
                return;

            _tmrDbDataArchival.Enabled = false;

            try
            {
                //archive minute bars
                var errors = new List<string>(5);
                Logger.Info("Started data archivation...");
                Notification?.Invoke(this, new EventArgs<string>("Data archivation in progress..."));
                errors.Add(ServerCommonObjects.SQL.DBMaintenance.ArchiveMinuteBars(_connectionString));
                if (errors.Last() != null)
                    Logger.Error("Failed to archive minute bars: " + errors.Last());
                else
                    Logger.Info("Finished minute bars archivation");

                //archive tick data
                errors.Add(ServerCommonObjects.SQL.DBMaintenance.ArchiveTickData(_connectionString));
                if (errors.Last() != null)
                    Logger.Error("Failed to archive tick data: " + errors.Last());
                else
                    Logger.Info("Finished ticks archivation");

                //archive level2 tick data
                errors.Add(ServerCommonObjects.SQL.DBMaintenance.ArchiveTickData(_connectionString, 2));
                if (errors.Last() != null)
                    Logger.Error("Failed to archive L2 tick data: " + errors.Last());
                else
                    Logger.Info($"Finished L2 ticks archivation");

                //optional: cleanup (shrink) DB logs if no errors occured
                if (errors.All(i => i == null))
                {
                    const int NEW_LOG_SIZE = 50;
                    errors.Add(ServerCommonObjects.SQL.DBMaintenance.ShrinkDbLog(_connectionString, NEW_LOG_SIZE));
                    if (errors.Last() != null)
                        Logger.Warning("Failed to shrink DB logs: " + errors.Last());
                    else
                        Logger.Info("Shrinked DB log to " + NEW_LOG_SIZE + " MB");
                }

                Logger.Info("Finished data archivation");
                _prevDbArchivalTime = DateTime.Now;
                Notification?.Invoke(this, new EventArgs<string>(string.Empty));
            }
            catch (Exception error)
            {
                Logger.Error("Failed to archive data: ", error);
            }

            _tmrDbDataArchival.Enabled = true;
        }

        #endregion

        #region RabbitMQ

        private void StartDbSignalQueue()
        {
            var factory = new ConnectionFactory
            {
                 UserName = ConfigurationManager.AppSettings.Get("Username"),
                Password = ConfigurationManager.AppSettings.Get("Password"),
                VirtualHost = ConfigurationManager.AppSettings.Get("VirtualHost"),
                HostName = ConfigurationManager.AppSettings.Get("HostName")
            };

            _rabbitConnection = factory.CreateConnection();
            _rabbitModel = _rabbitConnection.CreateModel();
            _rabbitModel.ExchangeDeclare(QueuesHelper.DBSignalQueuesExchangeName, ExchangeType.Direct);
            _rabbitModel.QueueDeclare(QueuesHelper.DBSignalQueuesName, false, false, false, null);
            _rabbitModel.QueueBind(QueuesHelper.DBSignalQueuesName, QueuesHelper.DBSignalQueuesExchangeName, QueuesHelper.DBSignalQueuesName, null);

            _signalQueues = new SignalQueues(_dbSignals, _rabbitModel, QueuesHelper.DBSignalQueuesName);
            _signalQueues.Start();
        }

        private void StopDBSignalQueue()
        {
            _rabbitModel?.QueueDelete(QueuesHelper.DBSignalQueuesExchangeName);

            _rabbitModel?.Close();
            _rabbitConnection?.Close();
        }

        #endregion // RabbitMQ
    }
}