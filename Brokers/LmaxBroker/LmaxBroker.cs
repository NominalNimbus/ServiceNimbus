/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using Com.Lmax.Api;
using Com.Lmax.Api.Account;
using Com.Lmax.Api.Order;
using Com.Lmax.Api.OrderBook;
using Com.Lmax.Api.Position;
using Com.Lmax.Api.Reject;
using CommonObjects;
using ServerCommonObjects;
using ServerCommonObjects.Interfaces;
using CancelOrderRequest = Com.Lmax.Api.Order.CancelOrderRequest;
using Order = CommonObjects.Order;

namespace Brokers
{
    public abstract class LmaxBroker : AbstractBroker
    {
        private const ProductType _productType = ProductType.CFD_LIVE;

        private LmaxApi _api;
        private ISession _session;
        private DateTime _prevSessionBreak;
        private readonly System.Timers.Timer _tmrAccStateUpdate;
        public const string DefaultDataFeedName = "LMAX";


        public LmaxBroker(IDataFeed datafeed) : base(datafeed)
        {
            DataFeedName = datafeed.Name;
            AccountInfo = new AccountInfo
            {
                DataFeedName = datafeed.Name,
                BalanceDecimals = datafeed.BalanceDecimals,
                IsMarginAccount = true
            };

            _tmrAccStateUpdate = new System.Timers.Timer(3000);
            _tmrAccStateUpdate.Elapsed += (sender, args)
                => _session?.RequestAccountState(new AccountStateRequest(), () => { }, GeneralFailureCallback);
        }
        
        public abstract string Uri { get; }

        #region IBroker Implementation

        public override void Login(AccountInfo account)
        {
            AccountInfo = account;
            AccountInfo.IsMarginAccount = true;
            if (_api == null)
                _api = new LmaxApi(Uri);

            _api.Login(new Com.Lmax.Api.LoginRequest(AccountInfo.UserName, AccountInfo.Password, _productType), session =>
            {
                _session = session;
                AccountInfo.ID = _session.AccountDetails.AccountId.ToString();
                AccountInfo.Currency = _session.AccountDetails.Currency;

            }, LoginFailureCallback);
        }

        public override void Start()
        {
            base.Start();

            _session.EventStreamSessionDisconnected += OnSessionDisconnected;
            _session.AccountStateUpdated += OnAccountStateUpdated;
            _session.InstructionRejected += OnInstructionRejected;
            _session.InstructionFailed += OnInstructionFailed;
            _session.OrderChanged += OnOrderChanged;
            _session.OrderExecuted += OnOrderExecuted;
            _session.PositionChanged += OnPositionChanged;

            ThreadPool.QueueUserWorkItem(p =>
            {
                try
                {
                    _session.Subscribe(new AccountSubscriptionRequest(), () => { }, GeneralFailureCallback);
                    _session.Subscribe(new OrderSubscriptionRequest(), () => { }, GeneralFailureCallback);
                    _session.Subscribe(new ExecutionSubscriptionRequest(), () => { }, GeneralFailureCallback);
                    _session.Subscribe(new PositionSubscriptionRequest(), () => { }, GeneralFailureCallback);

                    foreach (var security in Securities)
                    {
                        _session.Subscribe(new OrderBookSubscriptionRequest(security.SecurityId),
                            () => { }, GeneralFailureCallback);
                        _session.Subscribe(new OrderBookStatusSubscriptionRequest(security.SecurityId),
                            () => { }, GeneralFailureCallback);
                    }

                    _session.Start();
                }
                catch (Exception ex)
                {
                    Logger.Error(Name + " broker startup failure", ex);
                }
            });

            _tmrAccStateUpdate.Start();
        }

        public override void Stop()
        {
            base.Stop();

            _tmrAccStateUpdate.Stop();
            if (_session != null)
            {
                _session.EventStreamSessionDisconnected -= OnSessionDisconnected;
                _session.AccountStateUpdated -= OnAccountStateUpdated;
                _session.InstructionRejected -= OnInstructionRejected;
                _session.OrderChanged -= OnOrderChanged;
                _session.OrderExecuted -= OnOrderExecuted;
                _session.PositionChanged -= OnPositionChanged;
                try { _session.Stop(); }
                catch (Exception ex) { Logger.Error(Name + " broker stop exception", ex); }
                _session.Logout(() => { }, GeneralFailureCallback);
                _session = null;
            }
        }

        public override void PlaceOrder(Order order)
        {
            var tradingError = CheckIfCanTrade();
            if (tradingError != null)
                OnOrderRejected(order, tradingError);
            else
                base.PlaceOrder(order);
        }

        public override void CancelOrder(Order order)
        {
            var tradingError = CheckIfCanTrade();
            if (tradingError != null)
            {
                OnOrderRejected(order, tradingError);
                return;
            }

            if (order.ServerSide)
            {
                base.CancelOrder(order);
            }
            else
            {
                var security = Securities.FirstOrDefault(i => i.Symbol.Equals(order.Symbol, IgnoreCase));
                if (security != null)
                {
                    _session.CancelOrder(new CancelOrderRequest(order.UserID + "_c",
                        security.SecurityId, order.UserID), id => { }, GeneralFailureCallback);
                }
            }
        }

        public override void ModifyOrder(Order order, decimal? sl, decimal? tp, bool isServerSide = false)
        {
            var tradingError = CheckIfCanTrade();
            if (tradingError != null)
            {
                OnOrderRejected(order, tradingError);
                return;
            }

            var isExisitingOrder = false;
            lock (Orders)
                isExisitingOrder = Orders.Contains(order);

            var unmodified = order.Clone() as Order;
            base.ModifyOrder(order, sl, tp, isServerSide);

            var security = Securities.FirstOrDefault(i => i.Symbol.Equals(order.Symbol, IgnoreCase));
            if (security == null || security.SecurityId < 1)
                return;

            if (isServerSide && isExisitingOrder && !unmodified.ServerSide && unmodified.OpenQuantity > 0
                && (unmodified.SLOffset.HasValue || unmodified.TPOffset.HasValue))
            {
                _session.AmendStops(new AmendStopLossProfitRequest(security.SecurityId,
                    unmodified.UserID + "_m", unmodified.UserID, null, null), id => { }, GeneralFailureCallback);
            }
            else if (!isServerSide && unmodified.OpenQuantity > 0 && (sl.HasValue || tp.HasValue))
            {
                _session.AmendStops(new AmendStopLossProfitRequest(security.SecurityId,
                    unmodified.UserID + "_m", unmodified.UserID, sl, tp), id => { }, GeneralFailureCallback);
            }
        }

        protected override void PlaceMarketOrder(Order order)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var qty = order.Quantity;
                if (qty > 0 && order.OrderSide == Side.Sell)
                    qty = -qty;

                var security = Securities.First(i => i.Symbol.Equals(order.Symbol, IgnoreCase));
                if (security == null || _session == null)
                {
                    OnOrderRejected(order, "Broker API error");
                    return; 
                }

                _session.PlaceMarketOrder(new MarketOrderSpecification(order.UserID, security.SecurityId,
                    qty, Converter.ToTIF(order.TimeInForce), order.SLOffset, order.TPOffset),
                    id => OnOrderPlaced(order), GeneralFailureCallback);
            });
        }

        private void OnOrderPlaced(Order order) => 
            order.PlacedDate = DateTime.UtcNow;
        
        protected override void PlaceLimitStopOrder(Order order)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var qty = order.Quantity;
                if (qty > 0 && order.OrderSide == Side.Sell)
                    qty = -qty;

                var security = Securities.First(i => i.Symbol.Equals(order.Symbol, IgnoreCase));
                if (security == null || _session == null)
                    return;

                if (order.OrderType == CommonObjects.OrderType.Limit)
                {
                    _session.PlaceLimitOrder(new LimitOrderSpecification(order.UserID, security.SecurityId,
                        order.Price, qty, Converter.ToTIF(order.TimeInForce), order.SLOffset, order.TPOffset),
                        id => OnOrderPlaced(order), this.GeneralFailureCallback);
                }
                else if (order.OrderType == CommonObjects.OrderType.Stop)
                {
                    _session.PlaceStopOrder(new StopOrderSpecification(order.UserID, security.SecurityId,
                        order.Price, qty, Converter.ToTIF(order.TimeInForce), order.SLOffset, order.TPOffset),
                        id => OnOrderPlaced(order), this.GeneralFailureCallback);
                }
            });
        }

        #endregion

        #region Callbacks

        private void OnInstructionRejected(InstructionRejectedEvent rejected)
        {
            var instrument = Securities.FirstOrDefault(i => i.SecurityId == rejected.InstrumentId);
            if (instrument != null)
            {
                OnOrderRejected(new Order(rejected.InstructionId, instrument.Symbol)
                {
                    AccountId = AccountInfo.ID,
                    BrokerName = Name
                }, rejected.Reason);
            }
        }

        private void OnInstructionFailed(object sender, Tuple<OrderSpecification, string> failed)
        {
            var instrument = Securities.FirstOrDefault(i => i.SecurityId == failed.Item1.InstrumentId);
            if (instrument != null)
            {
                var order = new Order(failed.Item1.InstructionId, instrument.Symbol)
                {
                    AccountId = AccountInfo.ID,
                    BrokerName = Name,
                    Quantity = failed.Item1.Quantity,
                    SLOffset = failed.Item1.StopLossPriceOffset,
                    TPOffset = failed.Item1.StopProfitPriceOffset
                };
                OnOrderRejected(order, failed.Item2);
            }
        }

        private void OnOrderExecuted(Execution execution)
        {
            var instrument = Securities.FirstOrDefault(i => i.SecurityId == execution.Order.InstrumentId);
            if (instrument != null)
                ProcessOrderExecution(Converter.ToCommonOrder(execution, instrument.Symbol, Name));
        }

        private void OnOrderChanged(Com.Lmax.Api.Order.Order order)
        {
            var instrument = Securities.FirstOrDefault(i => i.SecurityId == order.InstrumentId);
            if (instrument != null)
                ProcessOrderChange(Converter.ToCommonOrder(order, instrument.Symbol, Name));
        }

        private void OnPositionChanged(PositionEvent position)
        {
            var instrument = Securities.FirstOrDefault(i => i.SecurityId == position.InstrumentId);
            if (instrument == null)
                return;

            var positionDetails = new Position(instrument.Symbol)
            {
                PositionSide = position.OpenQuantity > 0 ? Side.Buy : Side.Sell,
                Price = position.OpenQuantity != 0m
                    ? Math.Abs(position.OpenCost / position.OpenQuantity) / instrument.ContractSize : 0,
                Quantity = position.OpenQuantity
            };

            ProcessPositionUpdate(positionDetails);
        }

        private void OnAccountStateUpdated(AccountStateEvent accountState)
        {
            if (_session == null)
                return;

            AccountInfo.Currency = _session.AccountDetails.Currency;
            AccountInfo.UserName = _session.AccountDetails.Username;
            AccountInfo.ID = _session.AccountDetails.AccountId.ToString();
            AccountInfo.Balance = accountState.Balance;
            AccountInfo.Margin = accountState.Margin;
            AccountInfo.Profit = accountState.UnrealisedProfitAndLoss;
            AccountInfo.Equity = AccountInfo.Balance + AccountInfo.Profit;
            OnAccountStateChanged();
        }

        private void OnSessionDisconnected()
        {
            _tmrAccStateUpdate.Stop();
            IsStarted = false;

            if (_session != null)
            {
                _session.Logout(() => { }, GeneralFailureCallback);
                _session.EventStreamSessionDisconnected -= OnSessionDisconnected;
                _session.AccountStateUpdated -= OnAccountStateUpdated;
                _session.InstructionRejected -= OnInstructionRejected;
                _session.OrderChanged -= OnOrderChanged;
                _session.OrderExecuted -= OnOrderExecuted;
                _session.PositionChanged -= OnPositionChanged;

                try { _session.Stop(); }
                catch (Exception ex) { Logger.Error(Name + " stop exception.", ex); }
                _session = null;
            }

            Logger.Error($"Reconnecting {Name} broker...");
            Reconnect();
        }

        private void ReloginCallback(ISession session)
        {
            _session = session;
            _session.AccountStateUpdated += OnAccountStateUpdated;
            _session.InstructionRejected += OnInstructionRejected;
            _session.OrderChanged += OnOrderChanged;
            _session.OrderExecuted += OnOrderExecuted;
            _session.PositionChanged += OnPositionChanged;

            _session.Subscribe(new AccountSubscriptionRequest(), () => { }, GeneralFailureCallback);
            _session.Subscribe(new OrderSubscriptionRequest(), () => { }, GeneralFailureCallback);
            _session.Subscribe(new ExecutionSubscriptionRequest(), () => { }, GeneralFailureCallback);
            _session.Subscribe(new PositionSubscriptionRequest(), () => { }, GeneralFailureCallback);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    IsStarted = true;
                    _session.Start();
                }
                catch (Exception ex)
                {
                    Logger.Error(Name + " start (re-login) failure.", ex);
                }
            });

            if (!_tmrAccStateUpdate.Enabled)
                _tmrAccStateUpdate.Start();
        }

        private void GeneralFailureCallback(FailureResponse response)
        {
            if (!IsStarted)
                return;

            LogFailureDetails(response, "general");

            if (response.Message.Contains("(403) Forbidden"))
            {
                if ((DateTime.Now - _prevSessionBreak).TotalMinutes >= 1)
                {
                    _prevSessionBreak = DateTime.Now;
                    OnSessionDisconnected();
                }
            }
        }

        private void ReconnectFailureCallback(FailureResponse response)
        {
            LogFailureDetails(response, "reconnection");
            ThreadPool.QueueUserWorkItem(_ => Reconnect(30));
        }

        private void LoginFailureCallback(FailureResponse response)
        {
            LogFailureDetails(response, "login");
            throw new InvalidCredentialException($"Login failed for {AccountInfo.UserName} ({Name} broker)",
                response.Exception);
        }

        #endregion

        #region Helper Methods

        private void LogFailureDetails(FailureResponse response, string failedAction)
        {
            var msg = $"{Name} {failedAction} failure: {response.Message} ({response.Description}). Exception details: {response.Exception?.Message}";
            Logger.Warning(msg);
        }

        private void Reconnect(byte delayInSeconds = 0)
        {
            if (delayInSeconds > 0)
                Thread.Sleep(delayInSeconds * 1000);

            if (_api == null)
                _api = new LmaxApi(Uri);

            _api.Login(new Com.Lmax.Api.LoginRequest(AccountInfo.UserName, AccountInfo.Password, _productType),
                ReloginCallback, ReconnectFailureCallback);
        }

        private string CheckIfCanTrade()
        {
            if (!IsStarted || _session == null)
                return "Trading is not available";

            return null;
        }

        #endregion
    }
}
