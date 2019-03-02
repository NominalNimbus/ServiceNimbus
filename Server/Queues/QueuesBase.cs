/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using System;
using System.Text;
using ServerCommonObjects;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Server.Queues
{
    internal abstract class QueuesBase<TItem> where TItem : class
    {
        #region Fields

        private readonly string _queuesName;

        #endregion // Fields

        #region Properties

        private IModel RabbitModel { get; }

        #endregion // Properties

        #region Constructors

        protected QueuesBase(IModel rabbitModel, string queues)
        {
            RabbitModel = rabbitModel;
            _queuesName = queues;
        }

        #endregion // Constructors

        #region Public

        public void Start() => 
            InitQueue();

        #endregion // Public

        #region Abstracts

        protected abstract void OnNewItem(TItem item);

        #endregion // Abstracts

        #region Private

        private void InitQueue()
        {
            RabbitModel.QueueDeclare(_queuesName, false, false, false, null);
            var consumer = new EventingBasicConsumer(RabbitModel);

            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body);
                if (string.IsNullOrEmpty(message))
                    return;

                var obj = message.FromJson<TItem>();
                if (obj == null)
                    return;

                try
                {
                    OnNewItem(obj);
                }
                catch (Exception ex)
                {
                    Logger.Error("QueuesBase.InitQueue -> ", ex);
                }
            };

            RabbitModel.BasicConsume(_queuesName, true, consumer);
        }

        #endregion // Private
    }
}
