/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using ServerCommonObjects;
using System;
using System.Collections.Generic;

namespace WebSocketServiceHost
{
    internal sealed class UserInfo : IUserInfo
    {
        #region Fields

        private readonly HeartbeatResponse _heartbeatResponse;
        private readonly ICoreServiceHost _core;

        #endregion // Fields

        #region Properties

        public string Login { get; }
        public string ID { get; }
        public object SessionObject => null;
        public List<CommonObjects.AccountInfo> Accounts { get; }

        #endregion // Properties

        #region Constructors

        public UserInfo(ICoreServiceHost core, string id, string login)
        {
            _core = core ?? throw new ArgumentNullException(nameof(core));
            _heartbeatResponse = new HeartbeatResponse(string.Empty);
            ID = string.IsNullOrEmpty(id) ? throw new ArgumentNullException(nameof(id)) : id;
            Login = string.IsNullOrEmpty(login) ? throw new ArgumentNullException(nameof(login)) : login;
            Accounts = new List<CommonObjects.AccountInfo>();
        }

        #endregion // Constructors

        #region IUserInfo

        public void Send(ResponseMessage aResponse)
        {
            var body = aResponse?.ToJson();
            if (string.IsNullOrEmpty(body))
                return;

            _core.WebSocketSender.Send(aResponse);
        }

        public void Disconnect()
            => _core.WebSocketServer.Disconect(ID);

        public void DisconnectedByAnotherUser()
            => _core.WebSocketServer.Disconect(ID);

        public void Heartbeat()
            => _core.WebSocketSender.Send(_heartbeatResponse);

        public void SendError(Exception e)
            => _core.WebSocketSender.Send(new ErrorMessageResponse(e));

        #endregion // IUserInfo

        #region Overrides

        public override int GetHashCode()
            => Login.GetHashCode() ^ ID.GetHashCode();

        public override bool Equals(object obj)
        {
            var userInfo = obj as UserInfo;
            if (userInfo == null)
                return false;

            return ID == userInfo.ID && Login == userInfo.Login && GetHashCode() == userInfo.GetHashCode();
        }

        #endregion // Overrides
    }
}
