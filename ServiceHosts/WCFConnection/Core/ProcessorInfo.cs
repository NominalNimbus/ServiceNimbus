/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.ServiceModel;
using ServerCommonObjects;
using WCFServiceHost.Interfaces;

namespace WCFServiceHost.Core
{
    public class ProcessorInfo : IWCFProcessorInfo
    {

        #region Fields

        public readonly IContextChannel Chanel;
        public string ServiceID { get; }
        public string ID { get; set; }
        public object SessionObject { get; set; }

        private readonly IWCFCallback _callBack;

        #endregion

        #region Constructor

        public ProcessorInfo(string serviceID, string aSessionId, OperationContext aCtx)
        {
            ServiceID = serviceID;
            ID = aSessionId;
            _callBack = aCtx.GetCallbackChannel<IWCFCallback>();
            Chanel = aCtx.Channel;
            SessionObject = aCtx.Channel;
        }

        #endregion

        #region Messaging

        public void Send(ResponseMessage msg)
        {
            SendMessage(msg);
        }

        public void Disconnect()
        {
            SendMessage(new HeartbeatResponse("close session"));
            Chanel.Close();
        }

        public void Heartbeat()
        {
            SendMessage(new HeartbeatResponse(string.Empty));
        }

        private void SendMessage(ResponseMessage msg)
        {
            try
            {
                if (Chanel.State == CommunicationState.Opened)
                    _callBack.MessageOut(msg);
            }
            catch (Exception ex)
            {
                Logger.Error("Connection problem", ex);
            }
        }

        #endregion

        #region Overrides&Operators

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (!(obj is ProcessorInfo o))
                return false;

            return ServiceID.Equals(o.ServiceID, StringComparison.InvariantCulture);
        }

        private bool Equals(IWCFProcessorInfo other) => 
            string.Equals(ServiceID, other?.ServiceID) && string.Equals(ID, other?.ID);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((ServiceID != null ? ServiceID.GetHashCode() : 0) * 397) ^ (ID != null ? ID.GetHashCode() : 0);
            }
        }

        public static bool operator ==(ProcessorInfo a, ProcessorInfo b) => 
            !(a is null) && a.Equals(b);

        public static bool operator !=(ProcessorInfo a, ProcessorInfo b) => 
            !(a == b);

        #endregion

    }
}
