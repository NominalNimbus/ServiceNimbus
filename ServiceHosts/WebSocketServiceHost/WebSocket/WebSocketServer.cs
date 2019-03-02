/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using ServerCommonObjects;
using System;

using SuperWebSocket = SuperSocket.WebSocket.WebSocketServer;

namespace WebSocketServiceHost
{
    internal sealed class WebSocketServer : IWebSocketServer, ISender<ResponseMessage>
    {
        #region Fields

        private SuperWebSocket _webSocketServer;
        private IHostCommandManager _commandManager;

        #endregion // Fields

        #region Constructors

        public WebSocketServer(IHostCommandManager commandManager)
        {
            _commandManager = commandManager ?? throw new ArgumentNullException("Command manager is null.");
        }

        #endregion // Constructors

        #region Private

        private void WebSocketSubscribe()
        {
            _webSocketServer.NewMessageReceived += WebSocketServerNewMessageReceived;
            _webSocketServer.SessionClosed += WebSocketServerSessionClosed;
        }

        private void WebSocketUnSubscribe()
        {
            _webSocketServer.NewMessageReceived -= WebSocketServerNewMessageReceived;
            _webSocketServer.SessionClosed -= WebSocketServerSessionClosed;
        }

        #endregion // Private

        #region IWebSocketServer

        public void Start(string ip, int port)
        {
            _webSocketServer = new SuperWebSocket();
            WebSocketSubscribe();
            _webSocketServer.Setup(ip, port);
            _webSocketServer.Start();
        }

        public void Stop()
        {
            if (_webSocketServer == null)
                return;

            WebSocketUnSubscribe();
            _webSocketServer.Stop();
            _webSocketServer.Dispose();
            _webSocketServer = null;
        }

        #endregion // IWebSocketServer

        #region WebSocket Event Handlers

        private void WebSocketServerSessionClosed(SuperSocket.WebSocket.WebSocketSession session, SuperSocket.SocketBase.CloseReason value)
        {
            switch (value)
            {
                case SuperSocket.SocketBase.CloseReason.Unknown:
                case SuperSocket.SocketBase.CloseReason.ApplicationError:
                case SuperSocket.SocketBase.CloseReason.SocketError:
                case SuperSocket.SocketBase.CloseReason.ProtocolError:
                case SuperSocket.SocketBase.CloseReason.InternalError:

                    Logger.Error($"WebSocketServer.WebSocketServerSessionClosed -> {value}");
                    break;
                default:
                    break;
            }

            var logoutRequest = new LogoutRequest();
            _commandManager.OnNewRequest(session.SessionID, logoutRequest);
        }

        private void WebSocketServerNewMessageReceived(SuperSocket.WebSocket.WebSocketSession session, string value)
        {
            var requestBase = value.FromJson<BaseRequest>();
            if (requestBase == null || string.IsNullOrEmpty(requestBase.Type) || requestBase.Type == RequestType.NONE)
                return;

            var type = RequestType.GetRequestType(requestBase.Type);
            if (type == null)
                return;

            var request = value.FromJson(type) as RequestMessage;
            _commandManager.OnNewRequest(session.SessionID, request);
        }

        public void Disconect(string sessinId)
        {
            if (string.IsNullOrEmpty(sessinId))
                return;

            var session = _webSocketServer.GetSessionByID(sessinId);
            if (session != null)
                session.Close(SuperSocket.SocketBase.CloseReason.ClientClosing);
        }

        #endregion // WebSocket Event Handlers

        #region ISender

        public void Send(ResponseMessage message)
        {
            if (message == null || message.User == null)
            {
                Logger.Warning("WebSocketServer.Send -> message or user is null");
                return;
            }

            var session = _webSocketServer.GetSessionByID(message.User.ID);
            if (session == null)
            {
                Logger.Warning($"WebSocketServer.Send -> can not find a session for {message.User.Login}");
                return;
            }

            var response = message.ToJson();
            session.Send(response);
        }

        #endregion // ISender
    }
}
