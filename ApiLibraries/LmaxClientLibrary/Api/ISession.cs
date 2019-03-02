/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using Com.Lmax.Api.Account;
using Com.Lmax.Api.MarketData;
using Com.Lmax.Api.Order;
using Com.Lmax.Api.OrderBook;

namespace Com.Lmax.Api
{
    /// <summary>
    /// The main interface for interacting with the Lmax Trader platform.  This interface
    /// can be shared across multiple threads.
    /// </summary>
    public interface ISession
    {
        /// <summary>
        /// Readonly property contain the details of the users Account, id, locale etc. 
        /// </summary>
        AccountDetails AccountDetails { get; }

        /// <summary>
        /// Readonly property containing the web session id assigned to this session. 
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// 
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Fired whenever an order book event that represents a change in the market data
        /// occurs.  Orderbooks need to be subscribed to before events will be fired.
        /// A market data change includes changes to the best price and the depth.
        /// </summary>
        event OnOrderBookEvent MarketDataChanged;

        /// <summary>
        /// Fired whenever an order book event that represents a change in the order book
        /// status occurs. Use an OrderBookStatusSubscriptionRequest to subscribe to these
        /// events.
        /// </summary>
        event OnOrderBookStatusEvent OrderBookStatusChanged;

        /// <summary>
        /// Fired whenever an Exception occurs while reading from the event stream. This could
        /// occur, for example, due to the connection being dropped.
        /// </summary>
        event OnException EventStreamFailed;

        /// <summary>
        /// Fired whenever the session was disconnected on the server and the event stream closed.
        /// The session can no longer be used. In order to continue, a new session must be created by logging in again. 
        /// </summary>
        event OnSessionDisconnected EventStreamSessionDisconnected;

        /// <summary>
        /// Fired whenever a position changes, normally the result of a new order, fill
        /// cancel etc.
        /// </summary>
        event OnPositionEvent PositionChanged;

        /// <summary>
        /// Fired when a execution occurs as the result of an action that occurs on
        /// an order, e.g. order filling.
        /// </summary>
        event OnExecutionEvent OrderExecuted;

        /// <summary>
        /// Fired when there is a change to the state of an order
        /// </summary>
        event OnOrderEvent OrderChanged;

        /// <summary>
        /// Fired when an instruction id rejected.
        /// </summary>
        event OnRejectionEvent InstructionRejected;

        /// <summary>
        /// Fired when order request timed out.
        /// </summary>
        event EventHandler<Tuple<OrderSpecification, string>> InstructionFailed;

        /// <summary>
        /// Fired when an account state event is received.  May be the result of
        /// a request.  Use an AccountSubscriptionRequest to subscribe to AccountStateEvents.
        /// </summary>
        event OnAccountStateEvent AccountStateUpdated;

        ///<summary>
        /// Fired when the requested historic market data is received.
        /// Use RequestHistoricMarketData to request historic market data.
        ///</summary>
        event OnHistoricMarketDataEvent HistoricMarketDataReceived;

        ///<summary>
        /// Fired when a heartbeat is received.
        /// Use RequestHeartbeat to request a heartbeat be sent.
        ///</summary>
        event OnHeartbeatReceivedEvent HeartbeatReceived;

        /// <summary>
        /// Subscribe to the events from an OrderBook.  This includes Market Data, i.e. changes
        /// to the current best prices and the depth
        /// </summary>
        /// <param name="subscriptionRequest">
        /// An <see cref="ISubscriptionRequest"/> indicating the types of events to subscribe to.
        /// </param>
        /// <param name="successCallback">
        /// An <see cref="OnSuccess"/> that will be called if the subscription request is
        /// successful.
        /// </param>
        /// <param name="failureCallback">
        /// An <see cref="OnFailure"/> that will be called if the request fails, a <see cref="FailureResponse"/>
        /// will be provided containing the details of the failure.
        /// </param>
        void Subscribe(ISubscriptionRequest subscriptionRequest, OnSuccess successCallback, OnFailure failureCallback);

        /// <summary>
        /// Place a market order onto the Lmax Trader platform.  This will response with an instruction id that can
        /// be used to trace the state of an order.  The response to this call will be return before the
        /// order is actually placed on the exchange.  The execution reports and order responses will
        /// come back through an asynchronous event.
        /// </summary>
        /// <param name="marketOrderSpecification">The specification of the market order to be placed
        /// contains instrumentId, quantity etc.</param>
        /// <param name="instructionCallback">A <see cref="OnInstructionResponse"/> called when an order has
        /// been validated and passed onto the broker.</param>
        /// <param name="failureCallback">A <see cref="OnFailure"/> called if the call to place
        /// the order has failed.</param>
        void PlaceMarketOrder(MarketOrderSpecification marketOrderSpecification, OnInstructionResponse instructionCallback,
                              OnFailure failureCallback);

        /// <summary>
        /// Place a limit order onto the Lmax Trader platform.  This will response with an instruction id that can
        /// be used to trace the state of an order.  The response to this call will be return before the
        /// order is actually placed on the exchange.  The execution reports and order responses will
        /// come back through an asynchronous event.
        /// </summary>
        /// <param name="limitOrderSpecification">The specification of the market order to be placed
        /// contains instrumentId, quantity etc.</param>
        /// <param name="instructionCallback">A <see cref="OnInstructionResponse"/> called when an order has
        /// been validated and passed onto the broker.</param>
        /// <param name="failureCallback">A <see cref="OnFailure"/> called if the call to place
        /// the order has failed.</param>
        void PlaceLimitOrder(LimitOrderSpecification limitOrderSpecification, OnInstructionResponse instructionCallback,
                             OnFailure failureCallback);

        /// <summary>
        /// Place a stop order onto the Lmax Trader platform.  This will response with an instruction id that can
        /// be used to trace the state of an order.  The response to this call will be return before the
        /// order is actually placed on the exchange.  The execution reports and order responses will
        /// come back through an asynchronous event.
        /// </summary>
        /// <param name="stopOrderSpecification">The specification of the stop order to be placed
        /// contains instrumentId, quantity etc.</param>
        /// <param name="instructionCallback">A <see cref="OnInstructionResponse"/> called when an order has
        /// been validated and passed onto the broker.</param>
        /// <param name="failureCallback">A <see cref="OnFailure"/> called if the call to place
        /// the order has failed.</param>
        void PlaceStopOrder(StopOrderSpecification stopOrderSpecification, OnInstructionResponse instructionCallback,
                             OnFailure failureCallback);

        /// <summary>
        /// Cancel an order that has already be placed in the exchange.  This will need the instruction id of
        /// the original order.  This request will also have its own instruction id which can be used to
        /// trace an rejections of the cancellation.
        /// </summary>
        /// <param name="cancelOrderRequest">An order cancellation request containing the original orders
        /// instruction id</param>
        /// <param name="instructionCallback">A <see cref="OnInstructionResponse"/> called when an order has
        /// been validated and passed onto the broker.</param>
        /// <param name="failureCallback">A <see cref="OnFailure"/> called if the call to place
        /// the order has failed.</param>
        void CancelOrder(CancelOrderRequest cancelOrderRequest, OnInstructionResponse instructionCallback, OnFailure failureCallback);

        /// <summary>
        /// Amend the stop loss and/or profit on an existing order.
        /// </summary>
        /// <param name="amendStopLossProfitRequest">An amend order request containing a reference
        /// to the original order and the new stop loss/profit offsets</param>
        /// <param name="instructionCallback">A <see cref="OnInstructionResponse"/> called when the amendment has
        /// been validated and passed onto the broker.</param>
        /// <param name="failureCallback">Will be called when the attempt to amend order fails. For further details <see cref="OnFailure"/> </param>
        void AmendStops(AmendStopLossProfitRequest amendStopLossProfitRequest, OnInstructionResponse instructionCallback, OnFailure failureCallback);

        /// <summary>
        /// Request an update of the account state.  This will get the Lmax Trader platform to push an update
        /// of the account state back down the stream event channel.  It will not be returned directly.
        /// Use the AccountStateUpdated event to receive the update.
        /// </summary>
        /// <param name="accountStateRequest">The request for account state</param>
        /// <param name="successCallback"></param>
        /// <param name="failureCallback">A <see cref="OnFailure"/> called if the call to 
        ///   request the account state has failed.</param>
        void RequestAccountState(AccountStateRequest accountStateRequest, OnSuccess successCallback, OnFailure failureCallback);

        /// <summary>
        /// Get the historic market data for a specific instrument.  Will return a list of URLs that point to
        /// to CSV files that include the months that were requested.
        /// </summary>
        /// <param name="historicMarketDataRequest">The request for historic market data, will contain an instrument id
        ///   and a date range</param>
        /// <param name="successCallback">A <see cref="OnSuccess"/> called if the request is accepted and is being processed.</param>
        /// <param name="failureCallback">A <see cref="OnFailure"/> called if the call to 
        ///   request the historic market data has failed.</param>
        void RequestHistoricMarketData(IHistoricMarketDataRequest historicMarketDataRequest, OnSuccess successCallback, OnFailure failureCallback);

        ///<summary>
        /// Request that the server sends back a heartbeat event with the specified token.
        ///</summary>
        ///<param name="heartbeatRequest">The request for a heartbeat.</param>
        ///<param name="successCallback">A <see cref="OnSuccess"/> called if the request is accepted and is being processed.</param>
        ///<param name="failureCallback">A <see cref="OnFailure"/> called if the call to 
        ///   request the heartbeat has failed.</param>
        void RequestHeartbeat(HeartbeatRequest heartbeatRequest, OnSuccess successCallback, OnFailure failureCallback);

        /// <summary>
        /// Request the instruments.  The instruments will not returned in this call, but will
        /// cause the system to generate an asynchronous event(s) containing the information.
        /// Instrument information is paged, on the callback will be a boolean value indicating
        /// if there are any more values to be received.  The last instrumentId should be used
        /// as the offsetInstrumentId on the subsequent request.  The results will be returned
        /// ordered by instrument name.
        /// </summary>
        /// <param name="searchRequest">The request for instruments</param>
        /// <param name="searchCallback">Will be called if the request succeeds.</param>
        /// <param name="onFailure">A <see cref="OnFailure"/> called if the call to 
        /// request the instruments has failed.</param>
        void SearchInstruments(SearchRequest searchRequest,
                               OnSearchResponse searchCallback,
                               OnFailure onFailure);

        /// <summary>
        /// Open a data url from the Lmax Trader platform.
        /// </summary>
        /// <param name="uri">The uri of the file to open</param>
        /// <param name="uriCallback">Will be called if the request succeeds.</param>
        /// <param name="onFailure">A <see cref="OnFailure"/> called if the call fails.</param>
        void OpenUri(Uri uri, OnUriResponse uriCallback, OnFailure onFailure);

        /// <summary>
        /// Starts the event processing loop.  This method blocks while it is reading events from
        /// asynchronous stream interface.  This method should be the very last action from within
        /// the <see cref="OnLogin"/> callback.  The implementation will prevent this method from
        /// being called more than once.  It is not vaild to call this method multiple times from
        /// different threads.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the event processing loop.
        /// </summary>
        void Stop();

        /// <summary>
        /// Logout from the exchange.
        /// You should stop listening to events first.
        ///
        /// <param name="successCallback">Will be called when the logout request suceeds.</param> 
        /// <param name="onFailure">Will be called when the logout request fails.</param> 
        /// </summary>
        void Logout(OnSuccess successCallback, OnFailure onFailure);

    }
}
