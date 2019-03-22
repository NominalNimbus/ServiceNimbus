/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PoloniexAPI.MarketTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocket4Net;

namespace PoloniexAPI.LiveTools
{
    public class LiveWebSocket
    {
        #region Variables

        private WebSocket _webSocket;

        #endregion //Variables

        #region public properties and events

        public bool IsConnected { get; private set; }

        public event EventHandler<OrderBookItem> OnOrderBookUpdate;
        public event EventHandler<Quote> OnTickerUpdate;
        public event EventHandler<string> OnSessionError;

        #endregion //public properties and events

        #region Public Members

        public void Start()
        {
            _webSocket = new WebSocket(Helper.Api2UrlWssBase);
            _webSocket.Opened += _webSocket_Opened;
            _webSocket.Closed += _webSocket_Closed;
            _webSocket.MessageReceived += _webSocket_MessageReceived;
            _webSocket.Error += _webSocket_Error;
            _webSocket.Open();
        }

        public void Stop()
        {
            UnsubscribeTicker();
            _webSocket.Opened -= _webSocket_Opened;
            _webSocket.Closed -= _webSocket_Closed;
            _webSocket.MessageReceived -= _webSocket_MessageReceived;
            _webSocket.Error -= _webSocket_Error;
            _webSocket.Close();
        }

        public void SubscribeOrderBook(string symbol)
        {
            //Send(new MessageIn(MessageHelper.Subscribe, symbol));
        }

        public void UnsubscribeOrderBook(string symbol)
        {
            //Send(new MessageIn(MessageHelper.Unsubscribe, symbol));
        }

        public void SubscribeTicker()
        {
            Send(new MessageIn(MessageHelper.Subscribe, MessageHelper.TickerData));
        }

        public void UnsubscribeTicker()
        {
            Send(new MessageIn(MessageHelper.Unsubscribe, MessageHelper.TickerData));
        }

        #endregion //Public Members

        #region Web Socket Events

        private  void _webSocket_Opened(object sender, EventArgs e)
        {
            IsConnected = true;
             SubscribeTicker();
        }

        private void _webSocket_Closed(object sender, EventArgs e)
        {
            Console.WriteLine($"{DateTime.Now}  Poloniex _webSocket_Closed");
            IsConnected = false;
            OnSessionError?.Invoke(this, "Web socket closed");
        }

        private void _webSocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var message = JsonConvert.DeserializeObject<JArray>(e.Message);
            if (message.Count == 0 || message[0].Type != JTokenType.Integer)
                return;

            var msgCode = message[0].Value<int>();
            switch (msgCode)
            {
                case MessageHelper.Heartbeat:

                    break;
                case MessageHelper.AccountNotifications:

                    break;
                case MessageHelper.TickerData:
                    if (message.Count > 2)
                    {
                        for (int i = 2; i < message.Count; i++)
                        {
                            if (message[i].Type != JTokenType.Array && message[i].HasValues)
                                continue;

                            var tick = TickData.TickDataFromMessage(message[i].Values());

                            if (tick != null)
                            {
                                OnTickerUpdate?.Invoke(this, tick.ToQuote());
                            }
                        }
                    }
                    break;
                default:
                //MessageHelper.ExchangeVolume24Hours:

                    break;
            }

        }

        private void _webSocket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            Console.WriteLine($"{DateTime.Now}  Poloniex _webSocket_Error");

            IsConnected = false;
            OnSessionError?.Invoke(this, e.Exception.Message);
        }

        #endregion //Web Socket Events

        #region Helper methods

        private void Send(MessageIn message)
        {
            var messageText = JsonConvert.SerializeObject(message);
            _webSocket.Send(messageText);
        }

        #endregion
    }

}