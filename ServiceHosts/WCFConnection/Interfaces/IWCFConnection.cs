/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.ServiceModel;
using ServerCommonObjects;

namespace WCFServiceHost.Interfaces
{
    [ServiceContract(CallbackContract = typeof(IWCFCallback), Name = "IWCFConnection", SessionMode = SessionMode.Required)]
    public interface IWCFConnection
    {
        [OperationContract(IsInitiating = true)]
        [FaultContract(typeof(TradingServerException))]
        LoginResponse Login(LoginRequest message);

        [OperationContract(IsTerminating = true, IsInitiating = false, IsOneWay = true)]
        void LogOut();

        [OperationContract(IsInitiating = false, IsOneWay = true)]
        void MessageIn(RequestMessage message);

        //CalculateServer
        [OperationContract(IsInitiating = true)]
        [FaultContract(typeof(TradingServerException))]
        void RegisterProcessor(RegisterUCProcessorResponse message);
    }
}