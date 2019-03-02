/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.ServiceModel;
using CommonObjects;
using ServerCommonObjects;
using WCFServiceHost.Interfaces;

namespace WCFServiceHost.Core
{
    public class UserInfo : IUserInfo
    {

        #region Fields

        private readonly IWCFCallback _callBack;
        public readonly IContextChannel Chanel;

        public string Login { get; }
        public string ID { get; }
        public object SessionObject { get; }
        public List<AccountInfo> Accounts { get; }

        #endregion

        #region Constructor

        public UserInfo(string login, string sessionId, OperationContext ctx)
        {
            Login = login;
            ID = sessionId;
            _callBack = ctx.GetCallbackChannel<IWCFCallback>();
            Chanel = ctx.Channel;
            SessionObject = ctx.Channel;
            Accounts = new List<AccountInfo>();
        }

        #endregion

        #region Messaging

        public void Send(ResponseMessage aResponse)
        {
            try
            {
                if (Chanel.State == CommunicationState.Opened)
                    _callBack.MessageOut(aResponse);
            }
            catch (Exception ex)
            {
                Logger.Error("Connection problem", ex);
            }
        }

        public void SendError(Exception e) =>
            Send(new ErrorMessageResponse(e));

        public void Heartbeat() => 
            Send(new HeartbeatResponse(string.Empty));

        public void Disconnect()
        {
            Send(new HeartbeatResponse("close session"));
            Chanel.Close();
        }

        public void DisconnectedByAnotherUser()
        {
            Send(new HeartbeatResponse("disconnected by another user"));
            Chanel.Close();
        }

        #endregion

        #region Overrides&Operators

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (!(obj is UserInfo o))
                return false;

            return Login.Equals(o.Login, StringComparison.InvariantCulture);
        }

        public override int GetHashCode() => 
            ID.GetHashCode() ^ Login.GetHashCode();

        public static bool operator ==(UserInfo a, UserInfo b) => 
            (a is null && b is null) || a?.Equals(b) == true;

        public static bool operator !=(UserInfo a, UserInfo b) => 
            !(a == b);

        #endregion

    }
}
