/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using System;
using ServerCommonObjects;
using ServerCommonObjects.SQL;
using RabbitMQ.Client;

namespace Server.Queues
{
    internal sealed class SignalQueues : QueuesBase<DbSignal>
    {
        #region Fields

        private readonly DBSignals _dbSignal;

        #endregion // Fields

        #region Constructors

        public SignalQueues(DBSignals dbSignal, IModel rabbitModel, string queues) : base(rabbitModel, queues)
        {
            _dbSignal = dbSignal ?? throw new ArgumentNullException(nameof(dbSignal));
        }

        #endregion // Constructors

        #region QueuesBase

        protected override void OnNewItem(DbSignal item)
        {
            if (item == null)
                return;

            _dbSignal.AddSignal(item.Signal, item.Login, item.ShortName);
        }

        #endregion // QueuesBase
    }

}
