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
using ServerCommonObjects.ServerClasses;
using WCFServiceHost.Core;
using WCFServiceHost.Interfaces;

namespace WCFServiceHost
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class TradingServerWCFService : IWCFConnection
    {

        #region IWCFConnection Interface

        public void MessageIn(RequestMessage request) => 
            MessageRouter.gMessageRouter.ProcessRequest(GetSessionID(OperationContext.Current.SessionId), request);

        public void RegisterProcessor(RegisterUCProcessorResponse request)
        {
            try
            {
                var processorInfo = new ProcessorInfo(request.ServiceID, GetSessionID(OperationContext.Current.SessionId), OperationContext.Current);
                lock (MessageRouter.gMessageRouter)
                {
                    MessageRouter.gMessageRouter.AddProcessorSession(processorInfo.ID, processorInfo);
                    processorInfo.Chanel.Closed += ProcessorSessionClosed;
                    processorInfo.Chanel.Faulted += ProcessorSessionClosed;
                }

                Logger.Info($"New processor added: '{request.ServiceID}' id = '{processorInfo.ID}'");

            }
            catch (Exception ex)
            {
                throw new FaultException<TradingServerException>(new TradingServerException(ex.Message), new FaultReason(ex.Message));
            }
        }

        private void ProcessorSessionClosed(object sender, EventArgs eventArgs)
        {
            try
            {
                lock (MessageRouter.gMessageRouter)
                {
                    var processorInfo = (ProcessorInfo)MessageRouter.gMessageRouter.GetProcessorInfo(sender);

                    if (processorInfo != null)
                    {
                        MessageRouter.gMessageRouter.RemoveProcessorSession(processorInfo.ID);

                        processorInfo.Chanel.Closed -= ProcessorSessionClosed;
                        processorInfo.Chanel.Faulted -= ProcessorSessionClosed;

                        Logger.Info($"Processor removed: '{processorInfo.ServiceID}' id = '{processorInfo.ID}'");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new FaultException<TradingServerException>(new TradingServerException(ex.Message), new FaultReason(ex.Message));
            }
        }

        public LoginResponse Login(LoginRequest request)
        {
            var response = new LoginResponse();

            try
            {
                var loggedIn = MessageRouter.gMessageRouter.Authenticate(request.Login, request.Password);
                if (loggedIn)
                {
                    var userInfo = new UserInfo(request.Login, GetSessionID(OperationContext.Current.SessionId), OperationContext.Current);
                    //disconnect
                    var connectedUser = MessageRouter.gMessageRouter.GetUserInfoByLogin(request.Login);
                    if(connectedUser != null)//disconnect
                    {
                        connectedUser.DisconnectedByAnotherUser();
                        MessageRouter.gMessageRouter.RemoveSession(connectedUser.ID);
                    }

                    lock (MessageRouter.gMessageRouter)
                    {
                        MessageRouter.gMessageRouter.AddSession(userInfo.ID, userInfo);
                        userInfo.Chanel.Closed += ChanelClosed;
                        userInfo.Chanel.Faulted += ChanelClosed;
                    }
                    Logger.Info($"Login succeeded: user = '{request.Login}' id = '{userInfo.ID}'");
                }
                else
                {
                    Logger.Warning($"Login error: user = '{request.Login}'");
                    throw new ApplicationException("Login fault.");
                }
            }
            catch (Exception ex)
            {
                throw new FaultException<TradingServerException>(new TradingServerException(ex.Message), new FaultReason(ex.Message));
            }

            return response;
        }

        public void LogOut()
        {
            if (OperationContext.Current == null)
                return;

            var aID = OperationContext.Current.SessionId;

            try
            {
                UserInfo aUserInfo;

                lock (MessageRouter.gMessageRouter)
                {
                    aUserInfo = MessageRouter.gMessageRouter.RemoveSession(GetSessionID(aID)) as UserInfo;
                    if (aUserInfo != null)
                    {
                        aUserInfo.Chanel.Closed -= ChanelClosed;
                        aUserInfo.Chanel.Faulted -= ChanelClosed;
                    }
                }
                if (aUserInfo != null)
                    Logger.Info($"Logout: user = '{aUserInfo.Login}' id = '{aUserInfo.ID}'");
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private static void ChanelClosed(object sender, EventArgs e)
        {
            try
            {
                lock (MessageRouter.gMessageRouter)
                {
                    var aUserInfo = MessageRouter.gMessageRouter.GetUserInfo(sender);

                    if (aUserInfo != null)
                    {
                        MessageRouter.gMessageRouter.RemoveSession(aUserInfo.ID);
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        #endregion

        #region Helper Methods

        private static string GetSessionID(string id)
        {
            var arr = id.Split(':', ';');

            return arr[1] + ":" + arr[2];
        }

        #endregion

    }
}
