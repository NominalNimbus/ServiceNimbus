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
using System.Threading;
using System.Threading.Tasks;
using CommonObjects;
using ServerCommonObjects;
using ServerCommonObjects.Classes;
using ServerCommonObjects.Interfaces;
using ServerCommonObjects.ServerClasses;
using Server.Commands;
using Server.Commands.Services;
using Server.Commands.Scripting;
using Server.Interfaces;

namespace Server
{
    public sealed class MessageProcessor : IDataFeedWorker, IRealTimeWorker, IHistoryWorker, IScriptingWorker, IPusher
    {
        #region Fields

        private const int MaxSendBarsCount = 10000;
        private readonly Dictionary<string, ICommand<RequestMessage>> _requestCommands;
        private readonly Dictionary<Security, Level1Subscribers> _level1Subscribers;
        private readonly Queue<RequestMessage> _requestMessages;
        private readonly Dictionary<string, Queue<Tick>> _ticks;
        private readonly List<HistoryDataRequest> _historicalRequests;

        private readonly ICore _core;
        private volatile bool _isReqWorkerActive;
        private int _processor;

        #endregion // Fields

        #region Properties

        public Dictionary<string, IDataFeed> DataFeeds { get; }

        public Queue<RemoveScriptingInstanceRequest> RemoveScriptingInstanceRequests { get; }

        public Queue<SaveScriptingDataRequest> SaveScriptingDataRequestMessages { get; }

        public List<HistoryDataRequest> HistoryRequest
        {
            get
            {
                lock (_historicalRequests)
                    return _historicalRequests;
            }
        }

        #endregion // Properties

        #region Event Declarations

        public event EventHandler<EventArgs<string>> Notification
        {
            add => _core.Notification += value;
            remove => _core.Notification -= value;
        }

        #endregion // Event Declarations

        #region Constructors

        public MessageProcessor(ICore core)
        {
            _core = core;

            _requestCommands = new Dictionary<string, ICommand<RequestMessage>>();
            DataFeeds = new Dictionary<string, IDataFeed>();
            _level1Subscribers = new Dictionary<Security, Level1Subscribers>();

            _requestMessages = new Queue<RequestMessage>();
            _ticks = new Dictionary<string, Queue<Tick>>();
            _historicalRequests = new List<HistoryDataRequest>();

            SaveScriptingDataRequestMessages = new Queue<SaveScriptingDataRequest>();
            RemoveScriptingInstanceRequests = new Queue<RemoveScriptingInstanceRequest>();

            RegisterCommands();
        }

        #endregion // Constructors

        #region Commands

        private void RegisterCommands()
        {
            _requestCommands.Add(typeof(GetDataFeedListRequest).Name, new DataFeedListCommand(_core, this, this));
            _requestCommands.Add(typeof(TradingInfoRequest).Name, new BrokerListCommand(_core, this));
            _requestCommands.Add(typeof(BrokersAvailableSecuritiesRequest).Name, new SecuritiesListCommand(_core, this));
            _requestCommands.Add(typeof(BrokersLoginRequest).Name, new BrokerLoginCommand(_core, this));
            _requestCommands.Add(typeof(BrokerLogoutRequest).Name, new BrokerLogoutCommand(_core, this));
            _requestCommands.Add(typeof(SubscribeRequest).Name, new SubscribeCommand(_core, this, this));
            _requestCommands.Add(typeof(UnsubscribeRequest).Name, new UnsubscribeCommand(_core, this, this));
            _requestCommands.Add(typeof(HistoryDataRequest).Name, new HistoryCommand(_core, this, this, this));
            _requestCommands.Add(typeof(OrdersListRequest).Name, new OrdersHistoryCommand(_core, this));
            _requestCommands.Add(typeof(PlaceOrderRequest).Name, new PlaceOrderCommand(_core, this));
            _requestCommands.Add(typeof(CancelOrderRequest).Name, new CancelOrderCommand(_core, this));
            _requestCommands.Add(typeof(ModifyOrderRequest).Name, new ModifyOrderCommand(_core, this));
            _requestCommands.Add(typeof(PortfolioActionRequest).Name, new PortfolioActionCommand(_core, this));
            _requestCommands.Add(typeof(GetTickRequest).Name, new GetTickCommand(_core, this));
            _requestCommands.Add(typeof(GetAvailableDataFeedsRequest).Name, new GetAvailableDataFeedsCommand(_core, this));
            _requestCommands.Add(typeof(GetAvailableSymbolsRequest).Name, new GetAvailableSymbolsCommand(_core, this, this));
            _requestCommands.Add(typeof(ScriptingRequest).Name, new ScriptingCommand(_core, this));
            _requestCommands.Add(typeof(SignalDataRequest).Name, new SignalDataCommand(_core, this, this));
            _requestCommands.Add(typeof(BacktestResultsRequest).Name, new BacktestResultsCommand(_core, this));
            _requestCommands.Add(typeof(CreateUserIndicatorRequest).Name, new CreateUserIndicatorCommand(_core, this));
            _requestCommands.Add(typeof(CreateUserSignalRequest).Name, new CreateUserSignalCommand(_core, this));
            _requestCommands.Add(typeof(RemoveScriptingInstanceRequest).Name, new RemoveScriptingInstanceCommand(_core, this, this));
            _requestCommands.Add(typeof(SaveScriptingDataRequest).Name, new SaveScriptingDataCommand(_core, this, this));
            _requestCommands.Add(typeof(SignalActionRequest).Name, new SignalActionCommand(_core, this));
            _requestCommands.Add(typeof(UpdateStrategyParamsRequest).Name, new UpdateStrategyParamsCommand(_core, this));
            _requestCommands.Add(typeof(AddUserFilesRequest).Name, new AddUserFilesCommand(_core, this));
            _requestCommands.Add(typeof(RemoveUserFilesRequest).Name, new RemoveUserFilesCommand(_core, this));
            _requestCommands.Add(typeof(ScriptingReportRequest).Name, new ScriptingReportCommand(_core, this));
            _requestCommands.Add(typeof(ScriptingAlertRequest).Name, new ScriptingAlertCommand(_core, this));
            _requestCommands.Add(typeof(SeriesUpdatedRequest).Name, new SeriesUpdatedCommand(_core, this));
            _requestCommands.Add(typeof(StartedSignalExecutionRequest).Name, new StartedSignalExecutionCommand(_core, this));
            _requestCommands.Add(typeof(IndicatorStartedRequest).Name, new IndicatorStartedCommand(_core, this));
            _requestCommands.Add(typeof(ScriptingOutputRequest).Name, new ScriptingOutputCommand(_core, this));
            _requestCommands.Add(typeof(GetBarsRequest).Name, new GetBarsCommand(_core, this));
            _requestCommands.Add(typeof(GetPositionsRequest).Name, new GetPositionsCommand(_core, this));
            _requestCommands.Add(typeof(CancelAccountOrderRequest).Name, new CancelAccountOrderCommand(_core, this));
            _requestCommands.Add(typeof(ClosePositionRequest).Name, new ClosePositionCommand(_core, this));
            _requestCommands.Add(typeof(ModifyAccountOrderRequest).Name, new ModifyAccountOrderCommand(_core, this));
            _requestCommands.Add(typeof(PlaceAccountOrderRequest).Name, new PlaceAccountOrderCommand(_core, this));
            _requestCommands.Add(typeof(GetPortfoliosRequest).Name, new GetPortfoliosCommand(_core, this));
            _requestCommands.Add(typeof(GetAccountsRequest).Name, new GetAccountsCommand(_core, this));
            _requestCommands.Add(typeof(GetAvailableSecuritiesRequest).Name, new GetAvailableSecuritiesCommand(_core, this));
            _requestCommands.Add(typeof(GetOrderRequest).Name, new GetOrderCommand(_core, this));
            _requestCommands.Add(typeof(GetOrdersRequest).Name, new GetOrdersCommand(_core, this));
            _requestCommands.Add(typeof(NewBacktestResultsRequest).Name, new NewBacktestResultsCommand(_core, this));
            _requestCommands.Add(typeof(BacktestReportRequest).Name, new BacktestReportCommand(_core, this));
            _requestCommands.Add(typeof(SignalActionSettedRequest).Name, new SignalActionSettedCommand(_core, this));
            _requestCommands.Add(typeof(UpdateUserInfoRequest).Name, new UpdateUserInfoCommand(_core, this));
            _requestCommands.Add(typeof(CreateSimulatedBrokerAccountRequest).Name, new CreateSimulatedBrokerAccountCommand(_core, this));
        }

        #endregion // Private

        #region Start/Stop

        public void Start(List<IDataFeed> dataFeeds, string connectionString)
        {
            foreach (var item in dataFeeds)
                DataFeeds.Add(item.Name, item);

            MessageRouter.gMessageRouter.RouteRequest += OnRouteRequest;
            MessageRouter.gMessageRouter.RemovedSession += OnRemovedSession;

            _core.OMS.AccountStateChanged += OmsOnAccountStateChanged;
            _core.OMS.OrdersChanged += OmsOnOrdersChanged;
            _core.OMS.OrdersUpdated += OmsOnOrdersUpdated;
            _core.OMS.PositionUpdated += OmsOnPositionUpdated;
            _core.OMS.PositionsChanged += OmsOnPositionsChanged;
            _core.OMS.OrderRejected += OmsOnOrderRejected;
            _core.OMS.HistoricalOrderAdded += OmsOnHistoricalOrderAdded;
            _core.NewTick += OnNewTick_DataFeed;
            _core.ScriptingExit += CoreOnScriptingExit;
            _core.BacktestReport += CoreOnBacktestReport;
            _core.NewSingleTick += CoreOnNewSingleTick;
            _core.NewBar += CoreOnNewBar;
            _core.SignalFlag += CoreOnSignalFlag;
            _core.RemoveIndicator += CoreOnRemoveIndicator;
            
            _core.Start(dataFeeds, connectionString);
        }

        public void Stop()
        {
            MessageRouter.gMessageRouter.RouteRequest -= OnRouteRequest;
            MessageRouter.gMessageRouter.RemovedSession -= OnRemovedSession;

            _core.OMS.AccountStateChanged -= OmsOnAccountStateChanged;
            _core.OMS.OrdersChanged -= OmsOnOrdersChanged;
            _core.OMS.OrdersUpdated -= OmsOnOrdersUpdated;
            _core.OMS.PositionUpdated -= OmsOnPositionUpdated;
            _core.OMS.PositionsChanged -= OmsOnPositionsChanged;
            _core.OMS.OrderRejected -= OmsOnOrderRejected;
            _core.ScriptingExit -= CoreOnScriptingExit;
            _core.OMS.HistoricalOrderAdded -= OmsOnHistoricalOrderAdded;
            _core.NewTick -= OnNewTick_DataFeed;
            _core.NewSingleTick -= CoreOnNewSingleTick;
            _core.NewBar -= CoreOnNewBar;
            _core.SignalFlag -= CoreOnSignalFlag;
            _core.RemoveIndicator -= CoreOnRemoveIndicator;

            _core.Stop();

            DataFeeds.Clear();

            lock (_requestMessages)
                _requestMessages.Clear();

            lock (_level1Subscribers)
                _level1Subscribers.Clear();

            lock (_historicalRequests)
                _historicalRequests.Clear();
        }

        #endregion // Start/Stop

        #region Request Handler

        private void RequestHandler()
        {
            _isReqWorkerActive = true;
            while (_isReqWorkerActive)
            {
                var request = default(RequestMessage);
                lock (_requestMessages)
                {
                    if (_requestMessages.Count > 0)
                        request = _requestMessages.Dequeue();
                }

                if (request == null)
                {
                    _isReqWorkerActive = false;
                    break;
                }

                ICommand<RequestMessage> command;
                var typeName = request.GetType().Name;

                lock (_requestCommands)
                {
                    if (!_requestCommands.TryGetValue(typeName, out command))
                        continue;
                }

                command.Execute(request);

                lock (_requestMessages)
                {
                    if (_requestMessages.Count == 0)
                        _isReqWorkerActive = false;
                }
            }
        }

        #endregion // Request Handler

        #region IPusher

        public void PushResponse(ResponseMessage response)
        {
            if (response == null)
                return;

            if (response is HistoryDataResponse historicalResponse)
            {
                PublishHistoricalData(historicalResponse);
            }
            else
            {
                response.User.Send(response);
            }
        }

        private void PublishHistoricalData(HistoryDataResponse histResponse)
        {
            var messageList = new List<HistoryDataResponse>();
            if (histResponse.Bars.Count > MaxSendBarsCount)
            {
                var i = 0;
                while (i < histResponse.Bars.Count)
                {
                    var sendCount = Math.Min(MaxSendBarsCount, histResponse.Bars.Count - i);
                    messageList.Add(new HistoryDataResponse
                    {
                        ID = histResponse.ID,
                        User = histResponse.User,
                        Bars = histResponse.Bars.GetRange(i, sendCount)
                    });
                    i += sendCount;
                }
            }
            else
            {
                messageList.Add(histResponse);
            }

            messageList.Last().Tail = true;
            foreach (var responseMessage in messageList)
                responseMessage?.User.Send(responseMessage);
        }

        public void PushToProcessor(ResponseMessage message, IWCFProcessorInfo processorInfo) =>
            processorInfo?.Send(message);

        public bool PushStartCodeMessage(ResponseMessage message)
        {
            var processors = _core.GetAvailableProcessors();
            if (!processors.Any()) return false;

            if (_processor < processors.Count)
            {
                processors[_processor].Send(message);
                _processor++;
            }
            else
            {
                processors[0].Send(message);
                _processor = 0;
            }

            return true;
        }

        #endregion // IPusher

        #region IRealTimeWorker

        public void Level1Subscribe(Security instrument, string userId)
        {
            if (instrument == null || string.IsNullOrEmpty(userId))
                return;

            Level1Subscribers level1Subscribers;
            lock (_level1Subscribers)
            {
                if (!_level1Subscribers.TryGetValue(instrument, out level1Subscribers))
                {
                    level1Subscribers = new Level1Subscribers { Subscribers = new List<string>() };
                    _level1Subscribers.Add(instrument, level1Subscribers);
                }
            }

            lock (level1Subscribers)
            {
                if (!level1Subscribers.Subscribers.Contains(userId))
                    level1Subscribers.Subscribers.Add(userId);

                var tick = level1Subscribers.Tick;

                if (tick != null)
                    PushTick(userId, tick);
            }
        }

        public void Level1UnSubscribe(string sessionId, IEnumerable<Security> instrumnets)
        {
            if (string.IsNullOrEmpty(sessionId) || instrumnets == null)
                return;

            foreach (var instrument in instrumnets)
            {
                Level1Subscribers level1Subscribers;
                lock (_level1Subscribers)
                {
                    if (!_level1Subscribers.TryGetValue(instrument, out level1Subscribers))
                        continue;
                }

                if (level1Subscribers.Subscribers.Contains(sessionId))
                    level1Subscribers.Subscribers.Remove(sessionId);
            }
        }

        #endregion // IRealTimeWorker

        #region IDataFeedWorker

        public IDataFeed GetDataFeedByName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            DataFeeds.TryGetValue(name, out var dataFeed);
            return dataFeed;
        }

        #endregion // IDataFeedWorker

        #region IScriptingWorker

        public Dictionary<string, byte[]> GetSignalFiles(string user)
        {
            var paths = _core.GetSignalFolderPaths(user);
            if (paths == null || paths.Count == 0)
                return new Dictionary<string, byte[]>(0);

            var result = new Dictionary<string, byte[]>(); //file path -> file content
            foreach (var dir in paths)
            {
                foreach (var file in Directory.GetFiles(dir))
                {
                    if (Path.GetFileName(file) == "Backtest Results.xml")
                        continue;

                    //get path relative to user folder (eg. 'portfolio\strategy\signal\file.xml')
                    var levels = file.Split('\\');
                    if (levels.Length >= 4)
                    {
                        result.Add(string.Join("\\", levels.Skip(levels.Length - 4)),
                            CommonHelper.ReadFromFileAndCompress(file));
                    }
                }
            }

            return result;
        }

        #endregion // IScriptingWorker

        #region Event Handlers

        private void OnRouteRequest(object sender, MessageRouter.MessageRouter_EventArgs e)
        {
            lock (_requestMessages)
            {
                e.Request.User = e.UserInfo;
                e.Request.Processor = e.ProcessorInfo;

                _requestMessages.Enqueue(e.Request);
                if (!_isReqWorkerActive)
                {
                    Task.Run(() => RequestHandler());
                }
            }
        }

        private void OnRemovedSession(object sender, MessageRouter.MessageRouter_EventArgs e)
        {
            UnsubscribeSymbolsBySessionID(e.ID);
        }

        private void OmsOnAccountStateChanged(object sender, EventArgs<IUserInfo, AccountInfo> eventArgs)
        {
            ThreadPool.QueueUserWorkItem(p =>
            {
                var response = new AccountInfoChangedResponse
                {
                    User = eventArgs.Value1,
                    Account = eventArgs.Value2
                };
                PushResponse(response);
            });
        }

        private void OmsOnOrdersChanged(object sender, EventArgs<UserAccount, List<Order>> eventArgs)
        {
            ThreadPool.QueueUserWorkItem(p =>
            {
                var orders = eventArgs.Value2;
                List<Order> threadSafeOrders;

                lock (orders)
                    threadSafeOrders = new List<Order>(orders);

                PushResponse(new OrdersChangedResponse
                {
                    User = eventArgs.Value1.UserInfo,
                    Orders = threadSafeOrders,
                    AccountID = eventArgs.Value1.AccountInfo.ID
                });
            });
        }

        private void OmsOnOrdersUpdated(object sender, EventArgs<UserAccount, List<Order>> eventArgs)
        {
            ThreadPool.QueueUserWorkItem(p =>
            {
                var orders = eventArgs.Value2;
                List<Order> threadSafeOrders;

                lock (orders)
                    threadSafeOrders = new List<Order>(orders);

                var response = new OrdersUpdatedResponse
                {
                    Orders = threadSafeOrders,
                    User = eventArgs.Value1.UserInfo,
                    AccountID = eventArgs.Value1.AccountInfo.ID,
                };
                PushResponse(response);
            });
        }

        private void OmsOnPositionUpdated(object sender, EventArgs<UserAccount, Position> eventArgs)
        {
            ThreadPool.QueueUserWorkItem(p => PushResponse(new PositionUpdatedResponse
            {
                User = eventArgs.Value1.UserInfo,
                AccountID = eventArgs.Value1.AccountInfo.ID,
                Position = eventArgs.Value2
            }));
        }

        private void OmsOnPositionsChanged(object sender, EventArgs<UserAccount, List<Position>> eventArgs)
        {
            ThreadPool.QueueUserWorkItem(p => PushResponse(new PositionChangedResponse
            {
                User = eventArgs.Value1.UserInfo,
                AccountID = eventArgs.Value1.AccountInfo.ID,
                Positions = eventArgs.Value2
            }));
        }

        private void OmsOnOrderRejected(object sender, UserOrderRejectedEventArgs eventArgs)
        {
            ThreadPool.QueueUserWorkItem(p => PushResponse(new OrderRejectionResponse
            {
                User = eventArgs.UserInfo,
                Order = eventArgs.Order,
                Msg = eventArgs.Message
            }));
        }

        private void OmsOnHistoricalOrderAdded(object sender, EventArgs<UserAccount, Order> eventArgs)
        {
            ThreadPool.QueueUserWorkItem(p => PushResponse(new HistoricalOrderResponse
            {
                User = eventArgs.Value1.UserInfo,
                AccountID = eventArgs.Value1.AccountInfo.ID,
                HistoricalOrder = eventArgs.Value2
            }));
        }

        #endregion

        #region DataFeeds

        private void OnNewTick_DataFeed(Tick tick)
        {
            Level1Subscribers subscribers;
            lock (_level1Subscribers)
            {
                _level1Subscribers.TryGetValue(tick.Symbol, out subscribers);
            }

            if (subscribers == null)
                return;
            List<string> users;

            lock (subscribers)
            {
                subscribers.Tick = tick;
                users = new List<string>(subscribers.Subscribers);
            }

            users.ForEach(user => PushTick(user, tick));
        }

        #endregion

        #region Message Processors

        private void TickMessageOutWorker(string aSessionID, Queue<Tick> aTicks)
        {
            const int aMaxPackSize = 20;

            var aUserInfo = MessageRouter.gMessageRouter.GetUserInfo(aSessionID);
            if (aUserInfo == null)
            {
                lock (aTicks)
                {
                    aTicks.Clear();

                    return;
                }
            }

            try
            {
                while (true)
                {
                    var aResponse = new TickDataResponse();
                    var bEmptyQueue = false;

                    aResponse.Tick = new List<Tick>();
                    aResponse.User = aUserInfo;
                    lock (aTicks)
                    {
                        while (aResponse.Tick.Count < aMaxPackSize)
                        {
                            if (aTicks.Count > 1)
                                aResponse.Tick.Add(aTicks.Dequeue());
                            else if (aTicks.Count == 1)
                            {
                                bEmptyQueue = true;
                                aResponse.Tick.Add(aTicks.Peek());
                                break;
                            }
                            else
                            {
                                bEmptyQueue = true;
                                break;
                            }
                        }
                    }

                    if (aResponse.Tick.Count > 0)
                    {
                        foreach (var tick in aResponse.Tick)
                        {
                            foreach (var level2 in tick.Level2)
                            {
                                level2.DailyLevel2AskSize = _core.GetTotalDailyAskVolume(tick.Symbol, level2.DomLevel);
                                level2.DailyLevel2BidSize = _core.GetTotalDailyBidVolume(tick.Symbol, level2.DomLevel);
                            }
                        }

                        aResponse.User.Send(aResponse);
                    }

                    if (bEmptyQueue)
                    {
                        lock (aTicks)
                        {
                            if (aTicks.Count > 0)
                                aTicks.Dequeue();
                            if (aTicks.Count == 0)
                                break;
                        }
                    }
                }
            }
            catch
            {
                lock (aTicks)
                {
                    aTicks.Clear();
                }
            }
        }

        private void BroadcastToProcessors(ResponseMessage message)
        {
            var processors = _core.GetAvailableProcessors();
            foreach (var processor in processors)
                processor.Send(message);
        }

        private void PushTick(string sessionID, Tick tick)
        {
            Queue<Tick> messages;

            lock (_ticks)
            {
                if (!_ticks.TryGetValue(sessionID, out messages))
                {
                    messages = new Queue<Tick>();
                    _ticks.Add(sessionID, messages);
                }
            }

            lock (messages)
            {
                messages.Enqueue(tick);
                if (messages.Count == 1)
                    ThreadPool.QueueUserWorkItem(o => TickMessageOutWorker(sessionID, messages));
            }
        }

        #endregion

        #region Request Messages

        private void UnsubscribeSymbolsBySessionID(string aSessionID)
        {
            var unsubscribed = new List<Security>();
            lock (_level1Subscribers)
            {
                foreach (var pair in _level1Subscribers)
                {
                    lock (pair.Value.Subscribers)
                    {
                        if (pair.Value.Subscribers.Remove(aSessionID))
                        {
                            if (pair.Value.Subscribers.Count == 0)
                                unsubscribed.Add(pair.Key);
                        }
                    }
                }
            }

            foreach (var security in unsubscribed)
            {
                try
                {
                    GetDataFeedByName(security.DataFeed)?.UnSubscribe(security);
                }
                catch (NullReferenceException)
                {
                }
                catch (Exception e)
                {
                    Logger.Error("UnsubscribeSymbolsBySessionID", e);
                }
            }
        }

        #endregion

        #region Scripting

        private void CoreOnScriptingExit(ScriptingType type, string codeId, IUserInfo user)
        {
            ThreadPool.QueueUserWorkItem(p => PushResponse(new ScriptingExitResponse
            {
                User = user,
                ScriptingType = type,
                Id = codeId
            }));
        }

        private void CoreOnScriptingRemoved(ScriptingType type, string name, IUserInfo user)
        {
            ThreadPool.QueueUserWorkItem(p => PushResponse(new ScriptingInstanceUnloadedResponse
            {
                User = user,
                Name = name,
                ScriptingType = type
            }));
        }

        private void CoreOnSignalFlag(string processorID, string username, string path, SignalAction action)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                var serviceID = _core.GetScriptingServiceID(username, path, ScriptingType.Signal);
                var service = _core.GetProcessor(serviceID);
                if (service == null) return;

                PushToProcessor(new SetSignalActionResponse
                {
                    Username = username,
                    Action = action,
                    SignalName = path
                }, service);
            });
        }

        private void CoreOnRemoveIndicator(string login, string indicatorName, IWCFProcessorInfo processor)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                if (processor == null) return;

                PushToProcessor(new RemoveIndicatorResponse
                {
                    Login = login,
                    IndicatorName = indicatorName
                }, processor);
            });
        }

        private void CoreOnBacktestReport(List<BacktestResults> reports, float progress, IUserInfo user)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                PushResponse(new BacktestReportMessage
                {
                    User = user,
                    BacktestProgress = progress,
                    BacktestResults = reports
                });
            });
        }

        private void CoreOnNewBar(object sender, Tuple<string, string> tuple)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                BroadcastToProcessors(new NewBarResponse
                {
                    Symbol = tuple.Item1,
                    DataFeed = tuple.Item2
                });
            });
        }

        private void CoreOnNewSingleTick(object sender, Tick tick)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                BroadcastToProcessors(new NewSingleTickResponse
                {
                    Tick = tick
                });
            });
        }

        #endregion
    }
}