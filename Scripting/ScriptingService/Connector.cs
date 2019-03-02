/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using CommonObjects;
using ServerCommonObjects;
using ServerCommonObjects.Classes;
using ScriptingService.Classes;
using ScriptingService.TradingService;

namespace ScriptingService
{
    public class Connector
    {

        #region Members & Events

        private const int HeartbeatInterval = 15;

        private readonly Binding _binding;
        private readonly string _wcfIP;
        private readonly string _wcfPort;

        private long _id;
        private CallbackObject _callbackObject;
        private WCFConnectionClient _client;
        private InstanceContext _context;
        private EndpointAddress _endpoint;
        private Thread _heartbeatThread;
        private bool _keepHeartbeatAlive;

        public event EventHandler<StartSignalParameters> StartSignal;
        public event EventHandler<StartIndicatorParameters> StartIndicator;
        public event SetSignalFlagHandler SetSignalFlag;
        public event UpdateSignalStrategyParamsHandler UpdateStrategyParams;
        public event RemoveIndicatorHandler RemoveIndicator;

        public event EventHandler<Tick> NewTick;
        public event EventHandler<Tuple<string, string>> NewBar;

        private long NextValidId
        {
            get
            {
                if (_id == long.MaxValue)
                    _id = 0;

                return _id++;
            }
        }

        public List<string> AvailableDataFeeds { get; private set; }

        #endregion

        #region Initialization

        public Connector(string wcfIP, string wcfPort)
        {
            _wcfIP = wcfIP;
            _wcfPort = wcfPort;
            _binding = new NetTcpBinding("NetTcpBinding_IWCFConnection");
        }

        private void Initialize()
        {
            _callbackObject = new CallbackObject(this);
            _context = new InstanceContext(_callbackObject);
            _client = new WCFConnectionClient(_context, _binding, _endpoint);
        }

        public void RegisterService()
        {
            _endpoint = new EndpointAddress($"net.tcp://{_wcfIP}:{_wcfPort}/TradingService");
            Initialize();

            var serviceId = Guid.NewGuid().ToString();

            try
            {
                var registerUCProcessorRequest = new RegisterUCProcessorResponse { ServiceID = serviceId };

                _client.RegisterProcessor(registerUCProcessorRequest);
                Console.WriteLine("Connected to server");
                Send(new GetAvailableDataFeedsRequest());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            _keepHeartbeatAlive = true;
            _heartbeatThread = new Thread(HeartbeatThreadBody) { Name = "WCF Heartbeat", IsBackground = true };
            _heartbeatThread.Start();
        }

        #endregion

        #region Scripting Helpers

        public void ScriptingOutput(string username, List<Output> outputs)
        {
            Send(new ScriptingOutputRequest
            {
                Outputs = outputs,
                Username = username
            });
        }
        public void BacktestReport(List<BacktestResults> toList, float sBacktestProgress, string sOwner)
        {
            Send(new BacktestReportRequest
            {
                BacktestResults = toList,
                BacktestProgress = sBacktestProgress,
                Owner = sOwner
            });
        }

        public void SendBacktestResults(string user, string signal, List<BacktestResults> results)
        {
            Send(new NewBacktestResultsRequest
            {
                Username = user,
                Signal = signal,
                BacktestResults = results
            });
        }

        private void ProceedSignalState(string username, SignalAction action, string signalName) =>
            SetSignalFlag?.Invoke(null, username, signalName, action);

        public void SendFlagState(string username, string signalName, SignalAction action, SignalState state)
        {
            Send(new SignalActionSettedRequest
            {
                Username = username,
                Action = action,
                SignalName = signalName,
                State = state
            });
        }

        private void UpdateSignalStrategyParams(string login, string signalName, StrategyParams parameters) =>
            UpdateStrategyParams?.Invoke(null, login, signalName, parameters);

        public void SciriptingAlert(List<string> alerts, ScriptingType type, string codeId, string login)
        {
            Send(new ScriptingAlertRequest
            {
                Login = login,
                Alerts = alerts,
                CodeId = codeId,
                Type = type
            });
        }

        public void SeriesUpdated(List<SeriesForUpdate> seriesForUpdate, string login)
        {
            Send(new SeriesUpdatedRequest
            {
                Login = login,
                SeriesForUpdate = seriesForUpdate
            });
        }

        #endregion

        #region Helpers

        private void HeartbeatThreadBody()
        {
            do
            {
                Task.Factory.StartNew(() => Send(new HeartbeatRequest("Heartbeat")));

                for (var i = 0; i < HeartbeatInterval; i++)
                {
                    Thread.Sleep(1000);
                    if (!_keepHeartbeatAlive)
                        return;
                }
            }
            while (true);
        }

        #endregion

        #region History Provider
        
        public async Task<List<Bar>> GetBars(Selection selection)
        {
            var id = NextValidId;
            var request = new GetBarsRequest
            {
                Id = id,
                Selection = selection
            };

            return await RequestHelper<List<Bar>>.ProceedRequest(id, _client, request);
        }

        private void ProceedBarsResponse(long id, List<Bar> bars) => 
            RequestHelper<List<Bar>>.ProceedResponse(id, bars);

        #endregion // History Provider

        #region Tick Provider
        
        public async Task<Tick> GetTick(string dataFeed, string symbol, DateTime dateTime)
        {
            var id = NextValidId;
            var request = new GetTickRequest
            {
                Id = id,
                DataFeed = dataFeed,
                Symbol = symbol,
                DateTime = dateTime
            };

            return await RequestHelper<Tick>.ProceedRequest(id, _client, request);
        }

        private void OnNewTick(long id, Tick tick) => 
            RequestHelper<Tick>.ProceedResponse(id, tick);

        #endregion // Tick Provider

        #region BrokerHelpers
        
        public async Task<List<Position>> GetPositions(AccountInfo account, string symbol)
        {
            var id = NextValidId;
            var request = new GetPositionsRequest
            {
                Id = id,
                Symbol = symbol,
                Account = account
            };

            return await RequestHelper<List<Position>>.ProceedRequest(id, _client, request);
        }

        private void ProceedPositionsResponse(long id, List<Position> positions) => 
            RequestHelper<List<Position>>.ProceedResponse(id, positions);

        public void PlaceOrder(Order order, AccountInfo account, string username)
        {
            Send(new PlaceAccountOrderRequest
            {
                Order = order,
                Account = account,
                Username = username
            });
        }

        public void ModifyOrder(string orderId, decimal? sl, decimal? tp, bool isServerSide, AccountInfo account, string username)
        {
            Send(new ModifyAccountOrderRequest
            {
                OrderId = orderId,
                Sl = sl,
                Tp = tp,
                IsServerSide = isServerSide,
                Account = account,
                Username = username
            });
        }

        public void CancelOrder(string orderId, AccountInfo account, string username)
        {
            Send(new CancelAccountOrderRequest
            {
                OrderId = orderId,
                Account = account,
                Username = username
            });
        }

        public void ClosePosition(string symbol, AccountInfo account)
        {
            Send(new ClosePositionRequest
            {
                Symbol = symbol,
                Account = account
            });
        }
        
        public async Task<List<Portfolio>> GetPortfolios(string username)
        {
            var id = NextValidId;
            var request = new GetPortfoliosRequest
            {
                ID = id,
                Username = username
            };

            return await RequestHelper<List<Portfolio>>.ProceedRequest(id, _client, request);
        }

        private void ProceedPortfoliosResponse(long id, List<Portfolio> positions) => 
            RequestHelper<List<Portfolio>>.ProceedResponse(id, positions);

        public async Task<List<AccountInfo>> GetAccounts(string username)
        {
            var id = NextValidId;
            var request = new GetAccountsRequest
            {
                ID = id,
                Username = username
            };

            return await RequestHelper<List<AccountInfo>>.ProceedRequest(id, _client, request);
        }

        private void ProceedAccountsResponse(long id, List<AccountInfo> positions) => 
            RequestHelper<List<AccountInfo>>.ProceedResponse(id, positions);
        
        public async Task<List<Security>> GetAvailableSecurities(AccountInfo account)
        {
            var id = NextValidId;
            var request = new GetAvailableSecuritiesRequest
            {
                ID = id,
                AccountInfo = account
            };

            return await RequestHelper<List<Security>>.ProceedRequest(id, _client, request);
        }

        private void ProceedSecuritiesResponse(long id, List<Security> securities) =>
            RequestHelper<List<Security>>.ProceedResponse(id, securities);
        
        public async Task<List<Order>> GetOrders(string username, string accountID)
        {
            var id = NextValidId;
            var request = new GetOrdersRequest
            {
                ID = id,
                Username = username,
                AccountID = accountID
            };

            return await RequestHelper<List<Order>>.ProceedRequest(id, _client, request);
        }

        private void ProceedOrdersResponse(long id, List<Order> orders) =>
            RequestHelper<List<Order>>.ProceedResponse(id, orders);
        
        public async Task<Order> GetOrder(string username, string accountID, string orderId)
        {
            var id = NextValidId;
            var request = new GetOrderRequest
            {
                ID = id,
                Username = username,
                AccountID = accountID,
                OrderId = orderId
            };

            return await RequestHelper<Order>.ProceedRequest(id, _client, request);
        }

        private void ProceedOrderResponse(long id, Order order) =>
            RequestHelper<Order>.ProceedResponse(id, order);

        #endregion

        #region DataFeed Provider
        
        public async Task<List<string>> GetAvailableSymbols(string dataFeed)
        {
            var id = NextValidId;
            var request = new GetAvailableSymbolsRequest
            {
                Id = id,
                DataFeed = dataFeed
            };

            return await RequestHelper<List<string>>.ProceedRequest(id, _client, request);
        }

        private void OnAvailableSymbols(long id, List<string> symbols) => 
            RequestHelper<List<string>>.ProceedResponse(id, symbols);

        private void OnAvailableDataFeeds(List<string> availableDataFeeds)
        {
            if (availableDataFeeds == null)
                return;

            AvailableDataFeeds = availableDataFeeds;
        }

        #endregion // DataFeed Provider

        #region ScriptingWorkers

        private void ProceedRemoveIndicatorResponse(string login, string indicatorName)
            => RemoveIndicator?.Invoke(login, indicatorName, null);
        private void StartSignalExecution(StartSignalExecutionResponse response)
        {
            StartSignal?.Invoke(this, new StartSignalParameters
            {
                Id = response.Id,
                SignalInitParams = response.SignalInitParams,
                Files = response.Files,
                Login = response.UserName,
                AccountInfos = response.AccountInfos
            });
        }

        public void SignalStarted(Signal signal, string userName, List<string> alerts)
        {
            var signalStartedRequest = new StartedSignalExecutionRequest
            {
                UserName = userName,
                Signal = signal,
                SignalName = signal.Name,
                Alerts = alerts
            };

            Send(signalStartedRequest);
        }

        private void StartIndicatorExecution(StartIndicatorResponse response)
        {
            StartIndicator?.Invoke(this, new StartIndicatorParameters
            {
                OperationID = response.RequestID,
                Login = response.Login,
                Selection = response.Selection,
                Parameters = response.Parameters,
                Name = response.Name,
                PriceType = response.PriceType,
                Files = response.Dlls
            });
        }

        public void IndicatorStarted(ScriptingBase indicator, string login, string indicatorName, string reqID)
        {
            var indicatorStartedRequest = new IndicatorStartedRequest
            {
                Login = login,
                IndicatorName = indicatorName,
                RequestID = reqID,
                Indicator = indicator
            };

            Send(indicatorStartedRequest);
        }

        private void OnNewBar(string symbol, string datafeed) => NewBar?.Invoke(this, new Tuple<string, string>(symbol, datafeed));

        private void OnNewTick(Tick tick) => NewTick?.Invoke(this, tick);

        #endregion

        #region Sender
        
        private void Send(RequestMessage message)
        {
            try
            {
                _client.MessageIn(message);
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"Sending request error: {ex.Message}");
                if (_client.State == CommunicationState.Faulted)
                    TryReconnect();
            }
        }

        #endregion

        #region CallbackObject

        private class CallbackObject : IWCFCallback, IDisposable
        {
            private Connector _referenceHolder;

            public CallbackObject(Connector referenceHolder)
            {
                _referenceHolder = referenceHolder;
            }

            public void Dispose()
            {
                _referenceHolder = null;
            }

            public void MessageOut(ResponseMessage message)
            {
                switch (message)
                {
                    case StartSignalExecutionResponse startSignalResponse:
                        _referenceHolder.StartSignalExecution(startSignalResponse);
                        break;
                    case NewBarResponse newBarResponse:
                        _referenceHolder.OnNewBar(newBarResponse.Symbol, newBarResponse.DataFeed);
                        break;
                    case NewSingleTickResponse newTickResponse:
                        _referenceHolder.OnNewTick(newTickResponse.Tick);
                        break;
                    case SetSignalActionResponse getOrdersResponse:
                        _referenceHolder.ProceedSignalState(getOrdersResponse.Username, getOrdersResponse.Action, getOrdersResponse.SignalName);
                        break;
                    case UpdateSignalStrategyParamsResponse updateSignalStrategyParamsResponse:
                        _referenceHolder.UpdateSignalStrategyParams(updateSignalStrategyParamsResponse.Login, updateSignalStrategyParamsResponse.SignalName, updateSignalStrategyParamsResponse.Parameters);
                        break;
                    case StartIndicatorResponse startIndicatorResponse:
                        _referenceHolder.StartIndicatorExecution(startIndicatorResponse);
                        break;

                    //DataProvider/Broker
                    case GetAvailableSymbolsResponse symbolResponse:
                        _referenceHolder.OnAvailableSymbols(symbolResponse.Id, symbolResponse.Symbols);
                        break;
                    case GetAvailableDataFeedsResponse dataFeedsResponse:
                        _referenceHolder.OnAvailableDataFeeds(dataFeedsResponse.AvailableDataFeeds);
                        break;
                    case GetTickResponse tickResponse:
                        _referenceHolder.OnNewTick(tickResponse.Id, tickResponse.Tick);
                        break;
                    case GetBarsResponse getBarsResponse:
                        _referenceHolder.ProceedBarsResponse(getBarsResponse.Id, getBarsResponse.Bars);
                        break;
                    case GetPositionsResponse getPositionsResponse:
                        _referenceHolder.ProceedPositionsResponse(getPositionsResponse.Id, getPositionsResponse.Positions);
                        break;
                    case GetPortfoliosResponse getPortfoliosResponse:
                        _referenceHolder.ProceedPortfoliosResponse(getPortfoliosResponse.ID, getPortfoliosResponse.Portfolios);
                        break;
                    case GetAccountsResponse getAccountsResponse:
                        _referenceHolder.ProceedAccountsResponse(getAccountsResponse.ID, getAccountsResponse.Accounts);
                        break;
                    case GetAvailableSecuritiesResponse getAvailableSecuritiesResponse:
                        _referenceHolder.ProceedSecuritiesResponse(getAvailableSecuritiesResponse.ID, getAvailableSecuritiesResponse.Securities);
                        break;
                    case GetOrderResponse getOrderResponse:
                        _referenceHolder.ProceedOrderResponse(getOrderResponse.ID, getOrderResponse.Order);
                        break;
                    case GetOrdersResponse getOrdersResponse:
                        _referenceHolder.ProceedOrdersResponse(getOrdersResponse.ID, getOrdersResponse.Orders);
                        break;
                    case RemoveIndicatorResponse removeIndicatorResponse:
                        _referenceHolder.ProceedRemoveIndicatorResponse(removeIndicatorResponse.Login, removeIndicatorResponse.IndicatorName);
                        break;
                }
            }
        }

        #endregion

        private void TryReconnect()
        {
            RegisterService();
        }

    }
}