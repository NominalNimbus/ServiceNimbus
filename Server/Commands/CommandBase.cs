/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using System;
using System.Threading.Tasks;
using ServerCommonObjects;
using Server.Interfaces;

namespace Server.Commands
{
    internal abstract class CommandBase<T> : ICommand<RequestMessage> where T : class
    {
        #region Properties

        protected ICore Core { get; private set; }

        private IPusher Pusher { get; }

        #endregion // Properties

        #region Constructors

        public CommandBase(ICore core, IPusher pusher)
        {
            Core = core ?? throw new ArgumentNullException(nameof(core));
            Pusher = pusher ?? throw new ArgumentNullException(nameof(pusher));
        }

        #endregion // Constructors

        #region Abstracts

        protected abstract void ExecuteCommand(T request);

        #endregion // Abstracts

        #region Protected

        protected void PushResponse(ResponseMessage response)
            => Pusher.PushResponse(response);

        protected void PushToProcessor(ResponseMessage message, IWCFProcessorInfo processorInfo)
            => Pusher.PushToProcessor(message, processorInfo);

        protected bool PushStartCodeMessage(ResponseMessage message)
            => Pusher.PushStartCodeMessage(message);

        #endregion // Protected

        #region ICommand

        public void Execute(RequestMessage baseRequest)
        {
            if (baseRequest == null)
                return;

            Task.Run(() =>
            {
                try
                {
                    var request = baseRequest as T;
                    if (request == null)
                        return;

                    ExecuteCommand(request);
                }
                catch (Exception ex)
                {
                    Logger.Error("CommandBase.Execute -> ", ex);
                    baseRequest.User?.SendError(ex);
                }
            });
        }

        #endregion // ICommand
    }
}
