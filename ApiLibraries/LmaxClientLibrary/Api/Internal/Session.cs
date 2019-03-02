/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Net;
using System.Threading;
using Com.Lmax.Api.Account;
using Com.Lmax.Api.Internal.Protocol;
using Com.Lmax.Api.Internal.Xml;
using Com.Lmax.Api.MarketData;
using Com.Lmax.Api.Order;
using Com.Lmax.Api.OrderBook;

namespace Com.Lmax.Api.Internal
{
    // Disable these inspections because they're not .NET 2.0 compliant
    // ReSharper disable ConvertToLambdaExpression
    // ReSharper disable SuggestUseVarKeywordEvident
    public class Session : ISession
    {
        private const int Stopped = 0;
        private const int Running = 1;

        private readonly AccountDetails _accountDetails;
        private readonly IHttpInvoker _httpInvoker;
        private readonly string _baseUri;
        private readonly IXmlParser _xmlParser;
        private readonly DefaultHandler _eventHandler;
        private readonly string _sessionId;
        private readonly bool _restartStreamOnFailure;
        private readonly OrderBookEventHandler _orderBookEventHandler = new OrderBookEventHandler();
        private readonly OrderBookStatusEventHandler _orderBookStatusEventHandler = new OrderBookStatusEventHandler();
        private readonly OrderStateEventHandler _orderStateEventHandler = new OrderStateEventHandler();
        private readonly InstructionRejectedEventHandler _instructionRejectedEventHandler = new InstructionRejectedEventHandler();
        private readonly PositionEventHandler _positionEventHandler = new PositionEventHandler();
        private readonly AccountStateEventHandler _accountStateEventHandler = new AccountStateEventHandler();
        private readonly HeartbeatEventHandler _heartbeatEventHandler = new HeartbeatEventHandler();
        private readonly HistoricMarketDataEventHandler _historicMarketDataEventHandler = new HistoricMarketDataEventHandler();
        private readonly EventStreamHandler _eventStreamHandler;
        private volatile IConnection _streamConnection;

        /* _volatileRunFlag is intentionally not volatile.  Read with Thread.VolatileRead.  Write with Interlocked  */
        private Int32 _volatileRunFlag = Stopped;

        private delegate void OnSucessfulRequest();

        public event OnOrderBookEvent MarketDataChanged
        {
            add { _orderBookEventHandler.MarketDataChanged += value; }
            remove { _orderBookEventHandler.MarketDataChanged -= value; }
        }

        public event OnOrderBookStatusEvent OrderBookStatusChanged
        {
            add { _orderBookStatusEventHandler.OrderBookStatusChanged += value; }
            remove { _orderBookStatusEventHandler.OrderBookStatusChanged -= value; }
        }

        public event OnPositionEvent PositionChanged
        {
            add { _positionEventHandler.PositionEventListener += value; }
            remove { _positionEventHandler.PositionEventListener -= value; }
        }

        public event OnException EventStreamFailed;

        public event OnSessionDisconnected EventStreamSessionDisconnected;

        public event OnExecutionEvent OrderExecuted
        {
            add { _orderStateEventHandler.ExecutionEvent += value; }
            remove { _orderStateEventHandler.ExecutionEvent -= value; }
        }

        public event OnOrderEvent OrderChanged
        {
            add { _orderStateEventHandler.OrderEvent += value; }
            remove { _orderStateEventHandler.OrderEvent -= value; }
        }

        public event OnRejectionEvent InstructionRejected
        {
            add { _instructionRejectedEventHandler.RejectionEventListener += value; }
            remove { _instructionRejectedEventHandler.RejectionEventListener -= value; }
        }

        public event EventHandler<Tuple<OrderSpecification, string>> InstructionFailed;

        public event OnAccountStateEvent AccountStateUpdated
        {
            add { _accountStateEventHandler.AccountStateUpdated += value; }
            remove { _accountStateEventHandler.AccountStateUpdated -= value; }
        }

        public event OnHistoricMarketDataEvent HistoricMarketDataReceived
        {
            add { _historicMarketDataEventHandler.HistoricMarketDataReceived += value; }
            remove { _historicMarketDataEventHandler.HistoricMarketDataReceived -= value; }
        }

        public event OnHeartbeatReceivedEvent HeartbeatReceived
        {
            add { _heartbeatEventHandler.HeartbeatReceived += value; }
            remove { _heartbeatEventHandler.HeartbeatReceived -= value; }
        }

        public Session(AccountDetails accountDetails, string baseUri, IHttpInvoker httpInvoker, IXmlParser xmlParser,
                       string sessionId, bool restartStreamOnFailure)
        {
            _accountDetails = accountDetails;
            _baseUri = baseUri;
            _httpInvoker = httpInvoker;
            _xmlParser = xmlParser;
            _eventHandler = new DefaultHandler();
            _eventStreamHandler = new EventStreamHandler(new SaxContentHandler(_eventHandler));
            _sessionId = sessionId;
            _restartStreamOnFailure = restartStreamOnFailure;
            _eventHandler.AddHandler(_orderBookEventHandler);
            _eventHandler.AddHandler(_orderBookStatusEventHandler);
            _eventHandler.AddHandler(_orderStateEventHandler);
            _eventHandler.AddHandler(_instructionRejectedEventHandler);
            _eventHandler.AddHandler(_positionEventHandler);
            _eventHandler.AddHandler(_accountStateEventHandler);
            _eventHandler.AddHandler(_historicMarketDataEventHandler);
            _eventHandler.AddHandler(_heartbeatEventHandler);
        }

        public Session(AccountDetails accountDetails, string baseUri, IHttpInvoker httpInvoker, IXmlParser xmlParser,
             EventStreamHandler eventStreamHandler, DefaultHandler eventHandler, string sessionId, bool restartStreamOnFailure)
        {
            _accountDetails = accountDetails;
            _baseUri = baseUri;
            _httpInvoker = httpInvoker;
            _xmlParser = xmlParser;
            _eventHandler = eventHandler;
            _eventStreamHandler = eventStreamHandler;
            _sessionId = sessionId;
            _restartStreamOnFailure = restartStreamOnFailure;
        }

        public string Id
        {
            get { return _sessionId; }
        }

        public AccountDetails AccountDetails
        {
            get { return _accountDetails; }
        }

        public bool IsRunning
        {
            get { return Running == Thread.VolatileRead(ref _volatileRunFlag); }
        }

        public void Subscribe(ISubscriptionRequest subscriptionRequest, OnSuccess successCallback, OnFailure failureCallback)
        {
            Handler handler = new DefaultHandler();
            SendRequest(subscriptionRequest, handler, delegate { successCallback(); }, failureCallback);
        }

        public void PlaceMarketOrder(MarketOrderSpecification marketOrderSpecification, OnInstructionResponse instructionCallback, OnFailure failureCallback)
        {
            OrderResponseHandler handler = new OrderResponseHandler();
            SendRequest(marketOrderSpecification, handler, delegate { instructionCallback(handler.InstructionId); }, failureCallback);
        }

        public void PlaceLimitOrder(LimitOrderSpecification limitOrderSpecification, OnInstructionResponse instructionCallback, OnFailure failureCallback)
        {
            OrderResponseHandler handler = new OrderResponseHandler();
            SendRequest(limitOrderSpecification, handler, delegate { instructionCallback(handler.InstructionId); }, failureCallback);
        }

        public void PlaceStopOrder(StopOrderSpecification stopOrderSpecification, OnInstructionResponse instructionCallback, OnFailure failureCallback)
        {
            OrderResponseHandler handler = new OrderResponseHandler();
            SendRequest(stopOrderSpecification, handler, delegate { instructionCallback(handler.InstructionId); }, failureCallback);
        }

        public void CancelOrder(CancelOrderRequest cancelOrderRequest, OnInstructionResponse instructionCallback, OnFailure failureCallback)
        {
            OrderResponseHandler handler = new OrderResponseHandler();
            SendRequest(cancelOrderRequest, handler, delegate { instructionCallback(handler.InstructionId); }, failureCallback);
        }

        public void AmendStops(AmendStopLossProfitRequest amendStopLossProfitRequest, OnInstructionResponse instructionCallback, OnFailure failureCallback)
        {
            OrderResponseHandler handler = new OrderResponseHandler();
            SendRequest(amendStopLossProfitRequest, handler, delegate { instructionCallback(handler.InstructionId); }, failureCallback);
        }

        public void RequestAccountState(AccountStateRequest accountStateRequest, OnSuccess successCallback, OnFailure failureCallback)
        {
            Handler handler = new DefaultHandler();
            SendRequest(accountStateRequest, handler, delegate { successCallback(); }, failureCallback);
        }

        public void RequestHistoricMarketData(IHistoricMarketDataRequest historicMarketDataRequest, OnSuccess successCallback, OnFailure failureCallback)
        {
            Handler handler = new DefaultHandler();
            SendRequest(historicMarketDataRequest, handler, delegate { successCallback(); }, failureCallback);
        }

        public void RequestHeartbeat(HeartbeatRequest heartbeatRequest, OnSuccess successCallback, OnFailure failureCallback)
        {
            Handler handler = new DefaultHandler();
            SendRequest(heartbeatRequest, handler, delegate { successCallback(); }, failureCallback);
        }

        public void SearchInstruments(SearchRequest searchRequest, OnSearchResponse searchCallback, OnFailure onFailure)
        {
            try
            {
                SearchResponseHandler handler = new SearchResponseHandler();
                Response response = _httpInvoker.GetInSession(_baseUri, searchRequest, _xmlParser, handler, _sessionId);
                if (response.IsOk)
                {
                    if (handler.IsOk)
                    {
                        searchCallback(handler.Instruments, handler.HasMoreResults);
                    }
                    else
                    {
                        onFailure(new FailureResponse(false, handler.Message, "", null));
                    }
                }
                else
                {
                    onFailure(new FailureResponse(true, "HttpStatus: " + response.Status + ", for: " + _baseUri + searchRequest.Uri));
                }
            }
            catch (Exception e)
            {
                onFailure(new FailureResponse(e, "URI: " + _baseUri + searchRequest.Uri));
            }
        }

        public void OpenUri(Uri uri, OnUriResponse uriCallback, OnFailure onFailure)
        {
            try
            {
                var connection = _httpInvoker.Connect(uri, _sessionId);
                try
                {
                    uriCallback(uri, connection.GetBinaryReader());
                }
                finally
                {
                    connection.Close();
                }
            }
            catch (UnexpectedHttpStatusCodeException e)
            {
                onFailure(new FailureResponse(true, "HttpStatus: " + e.StatusCode + ", for: " + uri.AbsoluteUri));
            }

        }

        public void Start()
        {
            int oldValue = Interlocked.CompareExchange(ref _volatileRunFlag, Running, Stopped);
            if (Running == oldValue)
            {
                throw new InvalidOperationException("Can not call start twice concurrently on the same session");
            }

            do
            {
                try
                {
                    _streamConnection = _httpInvoker.Connect(_baseUri, new StreamRequest(), _sessionId);
                    _eventStreamHandler.ParseEventStream(_streamConnection.GetTextReader());
                }
                catch (UnexpectedHttpStatusCodeException e)
                {
                    if (Running == Thread.VolatileRead(ref _volatileRunFlag) && HttpStatusCode.Forbidden == e.StatusCode)
                    {
                        if (null != EventStreamSessionDisconnected)
                        {
                            EventStreamSessionDisconnected();
                        }
                        Interlocked.CompareExchange(ref _volatileRunFlag, Stopped, Running);
                    }
                    else if (Running == Thread.VolatileRead(ref _volatileRunFlag) && null != EventStreamFailed)
                    {
                        EventStreamFailed(e);
                    }

                }
                catch (Exception e)
                {
                    if (Running == Thread.VolatileRead(ref _volatileRunFlag) && null != EventStreamFailed)
                    {
                        if (null != EventStreamSessionDisconnected)
                            EventStreamSessionDisconnected();

                        EventStreamFailed(e);
                    }
                }
                finally
                {
                    if (null != _streamConnection)
                    {
                        _streamConnection.Abort();
                        _streamConnection = null;
                    }
                }
            } while (_restartStreamOnFailure && Running == Thread.VolatileRead(ref _volatileRunFlag));
        }

        public void Stop()
        {
            Interlocked.CompareExchange(ref _volatileRunFlag, Stopped, Running);

            if (null != _streamConnection)
            {
                _streamConnection.Abort();
                _streamConnection = null;
            }
        }

        /// <summary>
        /// logout from the exchange.
        /// You should stop listening to events first.
        ///
        /// <param name="successCallback">Will be called when the logout request suceeds.</param> 
        /// <param name="onFailure">Will be called when the logout request fails.</param> 
        /// </summary>
        public void Logout(OnSuccess successCallback, OnFailure onFailure)
        {
            SendRequest(new LogoutRequest(), new DefaultHandler(), delegate { successCallback(); }, onFailure);
        }

        private void SendRequest(IRequest request, Handler handler, OnSucessfulRequest onSucessfulRequest, OnFailure failureCallback)
        {
            try
            {
                Response response = _httpInvoker.PostInSession(_baseUri, request, _xmlParser, handler, _sessionId);
                if (response.IsOk)
                {
                    if (handler.IsOk)
                        onSucessfulRequest();
                    else
                        failureCallback(new FailureResponse(false, handler.Message, handler.Content, null));
                }
                else
                {
                    failureCallback(new FailureResponse(true, "HttpStatus: " + response.Status + ", for: " + _baseUri + request.Uri));
                }
            }
            catch (Exception e)
            {
                if (request is OrderSpecification)
                    InstructionFailed?.Invoke(this, new Tuple<OrderSpecification, string>((OrderSpecification)request, e.Message));
                failureCallback(new FailureResponse(e, "URI: " + _baseUri + request.Uri));
            }
        }

        private class StreamRequest : IRequest
        {
            public string Uri
            {
                get { return "/push/stream"; }
            }

            /// <summary>
            /// Internal: Output this request.
            /// </summary>
            /// <param name="writer">The destination for the content of this request</param>
            public void WriteTo(IStructuredWriter writer)
            {
            }
        }
    }
    // ReSharper restore ConvertToLambdaExpression
    // ReSharper restore SuggestUseVarKeywordEvident
}
