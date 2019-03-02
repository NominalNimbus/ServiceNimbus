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
using Com.Lmax.Api.Internal;
using Com.Lmax.Api.Internal.Protocol;
using Com.Lmax.Api.OrderBook;
using Com.Lmax.Api.Position;
using orderAlias = Com.Lmax.Api.Order;
using Com.Lmax.Api.Reject;
using Com.Lmax.Api.Account;

namespace Com.Lmax.Api
{
    ///<summary>
    /// Simple delegate for successful asynchronous call.
    ///</summary>
    public delegate void OnSuccess();

    ///<summary>
    /// Standard failure delegate for asynchronous calls.
    ///</summary>
    ///<param name="failureResponse">Contains the details of the failure</param>
    public delegate void OnFailure(FailureResponse failureResponse);

    ///<summary>
    /// Delegate for a successful login to the LMAX API.  Implementations can then use the session to interact with the API.
    ///</summary>
    ///<param name="session">The session to use when interacting with the API</param>
    public delegate void OnLogin(ISession session);

    ///<summary>
    /// Delagate for processing OrderBookEvents.  Events for all order books that have been subscribed to will be delivered to this delegate.
    ///</summary>
    ///<param name="onOrderBookEvent">Details of the order book event fired</param>
    public delegate void OnOrderBookEvent(OrderBookEvent onOrderBookEvent);

    ///<summary>
    /// Delagate for processing order book status events. Events for status changes of all order books that have been subscribed to will be delivered to this delegate.
    ///</summary>
    ///<param name="onOrderBookStatusEvent">Details of the order book status event fired</param>
    public delegate void OnOrderBookStatusEvent(OrderBookStatusEvent onOrderBookStatusEvent);

    ///<summary>
    /// Delegate to handle Exceptions
    ///</summary>
    ///<param name="exception">The exception that occurred</param>
    public delegate void OnException(Exception exception);


    ///<summary>
    /// Delegate for session disconnected.  The session can no longer be used. In order to continue, a new session must be created by logging in again.  
    ///</summary>
    public delegate void OnSessionDisconnected();

    ///<summary>
    /// Delegate to handle a response containing an instructionId - most usually in response to place/cancel/amend order requests
    ///</summary>
    ///<param name="instructionId">The instructionId returned by the LMAX API</param>
    public delegate void OnInstructionResponse(string instructionId);

    ///<summary>
    /// Delegate for processing events on all positions that have been subscribed to
    ///</summary>
    ///<param name="position">Contains details of the updated position</param>
    public delegate void OnPositionEvent(PositionEvent position);

    ///<summary>
    /// Delegate to deal with executions - for example, if an order has filled
    ///</summary>
    ///<param name="execution">The information about the execution that occurred</param>
    public delegate void OnExecutionEvent(orderAlias.Execution execution);

    ///<summary>
    /// Delegate to process changes in an order.
    ///</summary>
    ///<param name="order">The updated Order</param>
    public delegate void OnOrderEvent(orderAlias.Order order);

    ///<summary>
    /// If an instruction (for example, placing an order) is rejected, this delegate will be called.  
    /// An OnRejectionEvent delegate must be registered for the client to process rejections.
    ///</summary>
    ///<param name="instructionRejectedEvent">Details of why the instruction was rejected</param>
    public delegate void OnRejectionEvent(InstructionRejectedEvent instructionRejectedEvent);

    ///<summary>
    /// Delegate for dealing with updates to the user's account, for example if the account balance changes.
    ///</summary>
    ///<param name="accountState">The updated account state</param>
    public delegate void OnAccountStateEvent(AccountStateEvent accountState);

    ///<summary>
    /// Delegate that is called when historic market data events are received asynchronously.
    /// Note that the provided URIs require a valid authentication token which can be retrieved
    /// from <see cref="Session.Id"/>.
    ///</summary>
    ///<param name="instructionId">the ID of the instruction that requested this data.</param>
    ///<param name="uris">the URIs to the historic market data covering the requested date range.
    /// Empty if there is no data available for the requested date range.</param>
    public delegate void OnHistoricMarketDataEvent(string instructionId, List<Uri> uris);

    ///<summary>
    /// Delegate that is called market data asynchronous request is returned.
    /// Note that the provided URIs require a valid authentication token which can be retrieved
    /// from <see cref="Session.Id"/>.
    ///</summary>
    ///<param name="instructionId">the ID of the instruction that requested this data.</param>
    ///<param name="uris">the URIs to the market data covering the requested date range for the requested criteria.
    /// Empty if there is no data available for the requested date range.</param>
    public delegate void OnMarketDataEvent(string instructionId, List<Uri> uris);

    ///<summary>
    /// Delegate that is called when an asynchronous heartbeat event is received.
    ///</summary>
    ///<param name="token">the heartbeat token from the request that caused this heartbeat to be sent.</param>
    public delegate void OnHeartbeatReceivedEvent(string token);

    /// <summary>
    /// Delegate called when the search for instruments returns.  This returns a batch of instruments, 
    /// if the full number of search results is bigger than the batch size, the returned list will only 
    /// contain a subset of the results, and hasMoreResults will be true.
    /// </summary>
    /// <param name="instruments">A batch of search results.  If the full list of results is longer than the 
    /// batch size, this will be a partial set of results.</param>
    /// <param name="hasMoreResults">Set to true if there are more results to retrieve</param>
    public delegate void OnSearchResponse(List<Instrument> instruments, bool hasMoreResults);

    /// <summary>
    /// Delegate called when the open uri request returns. 
    /// </summary>
    /// <param name="uri">The uri requested</param>
    /// <param name="reader">The response content</param>
    public delegate void OnUriResponse(Uri uri, BinaryReader reader);

    /// <summary>
    /// The time in force policy applicable to orders.
    /// </summary>
    public enum TimeInForce
    {
        /// <summary>
        /// An order with this value must fill completely, or it will be cancelled.
        /// </summary>
        FillOrKill, 
        /// <summary>
        /// An order with this value must fill, but it can partially fill
        /// </summary>
        ImmediateOrCancel, 
        /// <summary>
        /// Represents a limit order, which can be completely unmatched and will not be cancelled until the end of the day
        /// </summary>
        GoodForDay,
        /// <summary>
        /// Represents a limit order, which can be completely unmatched and will not be cancelled until manually cancelled
        /// </summary>
        GoodTilCancelled,
        /// <summary>
        /// Represent an unknown time in force value. This is a safety value which will be returned if a new time in force
        /// is added to the underlying protocol which the .NET API does not understand.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// The Top-Level interface for the Lmax API.  Provides the Login entry point, most
    /// of the heavy lifting occurs within Session, so most of the interations with
    /// the Lmax Trader platform happen through the <see cref="ISession"/>.
    /// </summary>
    public class LmaxApi
    {
        private readonly IHttpInvoker _httpInvoker;
        private readonly IXmlParser _xmlParser;
        private readonly string _baseUri;

        /// <summary>
        /// Internal: Exposed for testing, use LmaxApi(string urlBase) instead.
        /// </summary>
        public LmaxApi(string baseUri, IHttpInvoker httpInvoker, IXmlParser xmlParser)
        {
            _baseUri = baseUri;
            _httpInvoker = httpInvoker;
            _xmlParser = xmlParser;
        }

        /// <summary>
        /// Construct an LmaxApi with the specific Lmax Trader platform to connect to.
        /// For testing: 'https://testapi.lmaxtrader.com' and production: 'https://api.lmaxtrader.com'.
        /// </summary>
        /// <param name="urlBase">
        /// A <see cref="String"/> that contains the url of the system to connect to.
        /// </param>
        public LmaxApi(String urlBase) :
            this(urlBase, new HttpInvoker(), new SaxParser())
        {
        }

        /// <summary>
        /// Construct an LmaxApi with the specific Lmax Trader platform to connect to.
        /// For testing: 'https://testapi.lmaxtrader.com' and production: 'https://api.lmaxtrader.com'.
        /// </summary>
        /// <param name="urlBase">
        /// A <see cref="String"/> that contains the url of the system to connect to.
        /// </param>
        /// <param name="clientIdentifier">Identifies the client in HTTP requests for diagnostic purposes (25 characters permitted).</param>
        public LmaxApi(String urlBase, String clientIdentifier) :
            this(urlBase, new HttpInvoker(TruncateClientId(clientIdentifier)), new SaxParser())
        {
        }

        /// <summary>
        /// Login to the Lmax Trader platform.  The appropriate handler will be called back
        /// on success or failure.  The loginCallback should be the main entry point into
        /// your trading application.  From that point you should add listeners to the
        /// session, subscribe to resources that you're interested in, e.g. OrderBooks
        /// and call Start on the <see cref="ISession"/>.
        /// </summary>
        /// <param name="loginRequest">
        /// A <see cref="LoginRequest"/> that contains your login credentials.
        /// </param>
        /// <param name="loginCallback">
        /// A <see cref="OnLogin"/> callback, fired when you are successfully logged in.
        /// </param>
        /// <param name="failureCallback">
        /// A <see cref="OnFailure"/> callback, fired when there is a login failure.
        /// </param>
        public void Login(LoginRequest loginRequest, OnLogin loginCallback, OnFailure failureCallback)
        {
            LoginResponseHandler handler = new LoginResponseHandler();

            try
            {
                string sessionId;
                Response response = _httpInvoker.Invoke(_baseUri, loginRequest, _xmlParser, handler, out sessionId);
                if (response.IsOk)
                {
                    if (handler.IsOk)
                    {
                        loginCallback(new Session(handler.AccountDetails, _baseUri, _httpInvoker, _xmlParser, sessionId, true));
                    }
                    else
                    {
                        failureCallback(new FailureResponse(false, handler.FailureType, handler.Message, null));
                    }
                }
                else
                {
                    failureCallback(new FailureResponse(true, "HttpStatus: " + response.Status + ", for: " + _baseUri + loginRequest.Uri));
                }
            }
            catch (Exception e)
            {
                failureCallback(new FailureResponse(e, "URI: " + _baseUri + loginRequest.Uri));
            }
        }

        private static string TruncateClientId(string clientIdentifier)
        {
            if (clientIdentifier == null)
            {
                return "";
            }
            else
            {
                int length = clientIdentifier.Length;
                if (length < 25)
                {
                    return clientIdentifier;
                }
                else
                {
                    return clientIdentifier.Substring(0, 25);
                }
            }
        }
    }
}
