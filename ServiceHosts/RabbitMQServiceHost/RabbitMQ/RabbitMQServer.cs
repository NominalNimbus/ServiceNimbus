/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Text;
using System.Threading.Tasks;
using ServerCommonObjects;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQServiceHost.Interfaces;

using static RabbitMQServiceHost.RabbitMQ.Constants;

namespace RabbitMQServiceHost.RabbitMQ
{
    public class RabbitMQServer : IRabbitMQServer
    {

        #region Fields

        private readonly IHostCommandManager _commandManager;

        private IConnection _connection;
        private IModel _model;
        private EventingBasicConsumer _consumer;

        #endregion

        #region Constructor

        public RabbitMQServer(IHostCommandManager commandManager)
        {
            _commandManager = commandManager;
        }

        #endregion

        #region IRabbitMQServer

        public void Start(string username, string password, string hostName, string virtualHost)
        {
            _connection = GenerateRabbitConnection(username, password, hostName, virtualHost);

            _model = _connection.CreateModel();
            _model.ExchangeDeclare(BaseExchange, ExchangeType.Direct);

            _consumer = new EventingBasicConsumer(_model);
            _consumer.Received += OnMessageReceived;

            Initialize();
        }

        public void Stop()
        {
            _model?.QueueDelete(RequestsQueue);

            _model?.Close();
            _connection?.Close();
        }

        public void Send(ResponseMessage response, string queueName)
        {
            try
            {
                var responseString = ConvertToByteArray(response);
                _model.BasicPublish(BaseExchange, queueName, null, responseString);
            }
            catch (Exception ex)
            {
                Logger.Error("RabbitMQHost. Basic publish error -> ", ex);
            }
        }

        #endregion

        #region Helpers

        private void Initialize()
        {
            _model.QueueDeclare(RequestsQueue, false, false, false, null);
            _model.QueueBind(RequestsQueue, BaseExchange, RequestsQueue, null);
            _model.BasicConsume(RequestsQueue, true, _consumer);
        }

        private void OnMessageReceived(object sender, BasicDeliverEventArgs ea)
        {
            Task.Run(() =>
            {
                var value = Encoding.UTF8.GetString(ea.Body);
                var requestBase = value.FromJson<BaseRequest>();
                if (string.IsNullOrEmpty(requestBase?.Type) || requestBase.Type == RequestType.NONE)
                    return;

                var type = RequestType.GetRequestType(requestBase.Type);
                if (type == null)
                    return;

                var request = value.FromJson(type) as RequestMessage;
                _commandManager.OnNewRequest(ea.BasicProperties.ReplyTo, request);
            });
        }

        private static IConnection GenerateRabbitConnection(string username, string password, string hostName, string virtualHost)
        {
            var factory = new ConnectionFactory
            {
                UserName = username,
                Password = password,
                VirtualHost = virtualHost,
                HostName = hostName
            };

            var connection = factory.CreateConnection();
            return connection;
        }

        private static byte[] ConvertToByteArray(ResponseMessage message)
        {
            var json = message.ToJson();
            return Encoding.UTF8.GetBytes(json);
        }

        #endregion

    }
}