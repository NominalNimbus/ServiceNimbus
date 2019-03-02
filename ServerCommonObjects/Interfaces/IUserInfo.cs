/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using CommonObjects;

namespace ServerCommonObjects
{
    /// <summary>
    /// defines methods and properties common for all sessions managed by TradingServer
    /// </summary>
    public interface IUserInfo
    {
        /// <summary>
        /// user name
        /// </summary>
        string Login { get; }
        /// <summary>
        /// session ID, must be unique
        /// </summary>
        string ID { get; }
        /// <summary>
        /// session object, generally session context
        /// </summary>
        object SessionObject { get; }
        /// <summary>
        /// Broker accounts
        /// </summary>
        List<AccountInfo> Accounts { get; }
        /// <summary>
        /// send response message to client
        /// </summary>
        /// <param name="aResponse">response message</param>
        void Send(ResponseMessage aResponse);
        /// <summary>
        /// disconnect session
        /// </summary>
        void Disconnect();
        /// <summary>
        /// disconnect by another user 
        /// </summary>
        void DisconnectedByAnotherUser();
        /// <summary>
        /// send heartbeat
        /// </summary>
        void Heartbeat();
        /// <summary>
        /// sends error info
        /// </summary>
        /// <param name="e">exception object</param>
        void SendError(Exception e);
    }
}