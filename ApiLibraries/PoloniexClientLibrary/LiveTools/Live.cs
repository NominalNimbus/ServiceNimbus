/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PoloniexAPI.MarketTools;
using SystemEx;
using WampSharp.V2;
using WampSharp.V2.Core.Contracts;
using WampSharp.V2.Realm;

namespace PoloniexAPI.LiveTools
{
    public class Live
    {
        private const string SUBJ_TICKER = "ticker";

        private long _sessionID;
        private IWampChannel _wampChannel;
        private Task _wampChannelOpenTask;

        private readonly Dictionary<string, IAsyncDisposable> _subscriptions = new Dictionary<string, IAsyncDisposable>();
        private readonly Dictionary<CurrencyPair, Quote> _tickers = new Dictionary<CurrencyPair, Quote>();

        public bool IsConnected { get; private set; }

        public event EventHandler<OrderBookItem> OnOrderBookUpdate;
        public event EventHandler<Quote> OnTickerUpdate;
        public event EventHandler<string> OnSessionError;

        #region Start/Stop
        public void Start()
        {
            _wampChannel = new DefaultWampChannelFactory().CreateJsonChannel(Helper.ApiUrlWssBase, "realm1");
            _wampChannel.RealmProxy.Monitor.ConnectionBroken += OnConnectionBroken;
            _wampChannel.RealmProxy.Monitor.ConnectionError += OnConnectionError;
            _wampChannel.RealmProxy.Monitor.ConnectionEstablished += OnConnectionEstablished;
            _wampChannelOpenTask = _wampChannel.Open();
        }

        public void Stop()
        {
            _wampChannel.RealmProxy.Monitor.ConnectionBroken -= OnConnectionBroken;
            _wampChannel.RealmProxy.Monitor.ConnectionError -= OnConnectionError;
            _wampChannel.RealmProxy.Monitor.ConnectionEstablished -= OnConnectionEstablished;

            foreach (var subscription in _subscriptions.Values)
                Task.Run(async() => await subscription.DisposeAsync()).Wait();
            _subscriptions.Clear();

            _wampChannel.Close();
            IsConnected = false;
        }

        private async Task Restart()
        {
            var subscriptions = new List<string>(_subscriptions.Keys);

            Stop();
            Start();

            for (var i = 0; i < subscriptions.Count; i++)
            {
                if (subscriptions[i] == SUBJ_TICKER)
                    await SubscribeTicker();
                else
                    await SubscribeOrderBook(subscriptions[i]);
            }
        }
        #endregion

        #region Connection Status Events
        private void OnConnectionEstablished(object sender, WampSessionCreatedEventArgs e)
        {
            IsConnected = true;
            _sessionID = e.SessionId;
        }

        private void OnConnectionError(object sender, WampSharp.Core.Listener.WampConnectionErrorEventArgs e)
        {
            IsConnected = false;
            OnSessionError?.Invoke(this, e.Exception.Message);
        }

        private async void OnConnectionBroken(object sender, WampSessionCloseEventArgs e)
        {
            IsConnected = false;
            if (e.CloseType != SessionCloseType.Disconnection)
            {
                OnSessionError?.Invoke(this, $"Reconnecting the broken session ({e.Reason ?? "no details provided"})");
                await Restart();
            }
        }
        #endregion

        #region Data Subscriptions
        public async Task SubscribeOrderBook(string symbol)
        {
            if (!String.IsNullOrWhiteSpace(symbol) && !_subscriptions.ContainsKey(symbol))
            {
                await _wampChannelOpenTask;
                var subscriber = new OrderBookSubscriber();
                subscriber.OnOrderBookUpdate += (s, e) => OnOrderBookUpdate?.Invoke(this, e);
                var topic = _wampChannel.RealmProxy.TopicContainer.GetTopicByUri(symbol);
                var obj = await topic.Subscribe(subscriber, new SubscribeOptions());
                _subscriptions[symbol] = obj;
            }
        }

        public async Task SubscribeTicker()
        {
            if (!_subscriptions.ContainsKey(SUBJ_TICKER))
            {
                await _wampChannelOpenTask;
                var subscriber = new TickSubscriber();
                subscriber.OnTickerUpdate += (s, e) =>
                {
                    _tickers[e.Symbol] = e;
                    OnTickerUpdate?.Invoke(this, e);
                };
                var obj = await _wampChannel.RealmProxy.Services.RegisterSubscriber(subscriber);
                _subscriptions[SUBJ_TICKER] = obj;
            }
        }

        public async Task UnsubscribeOrderBook(string symbol)
        {
            if (_subscriptions.ContainsKey(symbol))
            {
                await _subscriptions[symbol].DisposeAsync();
                _subscriptions.Remove(symbol);
            }
        }

        public async Task UnsubscribeTicker()
        {
            if (_subscriptions.ContainsKey(SUBJ_TICKER))
            {
                await _subscriptions[SUBJ_TICKER].DisposeAsync();
                _subscriptions.Remove(SUBJ_TICKER);
            }
        }
        #endregion
    }
}
