/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using ServerCommonObjects;
using ServerCommonObjects.ServerClasses;

namespace RabbitMQServiceHost.Messaging
{
    public class MessageManager : IMessageManager
    {

        #region Properties

        private static MessageRouter Router => MessageRouter.gMessageRouter;

        #endregion // Properties

        #region IMessageManager

        public void AddSession(IUserInfo userInfo)
        {
            if (userInfo == null)
                return;

            try
            {
                lock (Router)
                    Router.AddSession(userInfo.ID, userInfo);
            }
            catch (Exception ex)
            {
                Logger.Error("MessageManager.AddSession -> ", ex);
            }
        }

        public IUserInfo GetUserInfo(string id)
        {
            var userInfo = default(IUserInfo);
            if (string.IsNullOrEmpty(id))
                return null;

            try
            {
                lock (Router)
                    userInfo = Router.GetUserInfo(id);
            }
            catch (Exception ex)
            {
                Logger.Error("MessageManager.GetUserInfo -> ", ex);
            }

            return userInfo;
        }

        public bool IsUserConnected(string id)
        {
            var isConnected = false;
            try
            {
                lock (Router)
                    isConnected = Router.IsUserConnected(id);
            }
            catch (Exception ex)
            {
                Logger.Error("MessageManager.IsUserConnected -> ", ex);
            }

            return isConnected;
        }

        public IUserInfo RemoveSession(string id)
        {
            var userInfo = default(IUserInfo);
            if (string.IsNullOrEmpty(id))
                return null;

            try
            {
                lock (Router)
                    userInfo = Router.RemoveSession(id);
            }
            catch (Exception ex)
            {
                Logger.Error("MessageManager.RemoveSession -> ", ex);
            }

            return userInfo;
        }

        public void SendRequest(RequestMessage request)
        {
            if (request == null)
                return;

            try
            {
                lock (Router)
                    Router.ProcessRequest(request.User.ID, request);
            }
            catch (Exception ex)
            {
                Logger.Error("MessageManager.SendRequest -> ", ex);
            }
        }

        public bool ValidateCredentials(LoginRequest request)
        {
            var validateResult = false;
            if (request == null)
                return false;

            try
            {
                lock (Router)
                    validateResult = Router.Authenticate(request.Login, request.Password);
            }
            catch (Exception ex)
            {
                Logger.Error("MessageManager.ValidateCredentials -> ", ex);
            }

            return validateResult;
        }

        #endregion // IMessageManager

    }
}
