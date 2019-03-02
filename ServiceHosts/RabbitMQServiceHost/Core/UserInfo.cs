/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using ServerCommonObjects;

namespace RabbitMQServiceHost.Core
{
    internal class UserInfo : IUserInfo
    {

        #region Fields

        private readonly HostCore _core;
        private readonly HeartbeatResponse _heartbeatResponse;

        #endregion

        #region Properties

        public string Login { get; }
        public string ID { get; }
        public object SessionObject => null; // No need in SessionObject here
        public List<CommonObjects.AccountInfo> Accounts { get; }

        #endregion

        #region Constructors

        public UserInfo(HostCore core, string id, string login)
        {
            Accounts = new List<CommonObjects.AccountInfo>();
            ID = string.IsNullOrEmpty(id) ? throw new ArgumentNullException(nameof(id)) : id;
            Login = string.IsNullOrEmpty(login) ? throw new ArgumentNullException(nameof(login)) : login;

            _core = core;
            _heartbeatResponse = new HeartbeatResponse(string.Empty);
        }

        #endregion

        #region IUserInfo

        public void Send(ResponseMessage aResponse)
        {
            var body = aResponse?.ToJson();
            if (string.IsNullOrEmpty(body))
                return;

            _core.RabbitMQServer.Send(aResponse, ID);
        }

        public void Disconnect() { }

        public void DisconnectedByAnotherUser() { }

        public void Heartbeat() => 
            _core.RabbitMQServer.Send(_heartbeatResponse, ID);

        public void SendError(Exception e) => 
            _core.RabbitMQServer.Send(new ErrorMessageResponse(e), ID);

        #endregion

        #region Overrides

        public override int GetHashCode() => 
            Login.GetHashCode() ^ ID.GetHashCode();

        public override bool Equals(object obj)
        {
            if (!(obj is UserInfo userInfo))
                return false;

            return ID == userInfo.ID && Login == userInfo.Login && GetHashCode() == userInfo.GetHashCode();
        }

        #endregion // Overrides

    }
}
