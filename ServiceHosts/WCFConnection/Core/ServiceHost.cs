/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Description;
using ServerCommonObjects.Classes;
using ServerCommonObjects.ServerClasses;
using WCFServiceHost.Interfaces;

namespace WCFServiceHost.Core
{
    public class ServiceHost : IServerServiceHost
    {

        #region Fields

        private readonly Uri[] _uris;
        private readonly NetTcpBinding _netTcpBinding;

        private TradingServerWCFService _serviceCore;
        private System.ServiceModel.ServiceHost _serviceHost;

        #endregion

        #region Properties

        public string Name => "WCF";

        private string IP { get; }
        private int Port { get; }
        private int DTPort { get; }

        #endregion

        #region Constructor

        public ServiceHost()
        {
            var config = new LocalConfigHelper(Assembly.GetExecutingAssembly().Location, "  WCF Service");

            IP = config.GetString(nameof(IP));
            Port = config.GetInt(nameof(Port));
            DTPort = config.GetInt(nameof(DTPort));

            _uris = new[]
            {
                new Uri($"net.tcp://{IP}:{Port}/TradingService"),
                new Uri($"http://{IP}:{DTPort}/TradingService")
            };

            var timeOut = new TimeSpan(0, 1, 0);
            _netTcpBinding = new NetTcpBinding
            {
                TransactionFlow = false,
                ReceiveTimeout = timeOut,
                SendTimeout = timeOut,
                OpenTimeout = timeOut,
                CloseTimeout = timeOut,
                Security = { Mode = SecurityMode.None },
                MaxBufferSize = int.MaxValue,
                MaxReceivedMessageSize = int.MaxValue,
                MaxBufferPoolSize = 1073741823,
                MaxConnections = 100
            };

            _netTcpBinding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;
            _netTcpBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
            _netTcpBinding.ReliableSession.InactivityTimeout = timeOut;
        }

        #endregion

        #region IServerServiceHost

        public void Start()
        {
            _serviceCore = new TradingServerWCFService();

            _serviceHost = new System.ServiceModel.ServiceHost(_serviceCore, _uris);
            _serviceHost.AddServiceEndpoint(typeof(IWCFConnection), _netTcpBinding, _uris[0]);
            _serviceHost.Description.Behaviors.Add(new ServiceMetadataBehavior());
            _serviceHost.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexHttpBinding(), "mex");
            _serviceHost.Open();
        }

        public void Stop()
        {
            _serviceCore = null;
            if (_serviceHost == null) return;

            if (_serviceHost.State == CommunicationState.Opened)
                _serviceHost.Close();

            _serviceHost = null;
        }

        #endregion

    }
}
