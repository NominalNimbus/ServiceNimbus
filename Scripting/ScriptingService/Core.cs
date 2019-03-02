/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Threading.Tasks;
using ServerCommonObjects;
using ScriptingService.Classes;
using System.Configuration;
using System.Reflection;
using System.IO;
using ServerCommonObjects.Classes;

namespace ScriptingService
{
    public class Core
    {
        private readonly Connector _connector;
        private readonly ScriptingManager _scriptingManager;

        public Core()
        {
            var config = new LocalConfigHelper(Assembly.GetExecutingAssembly().Location, "Scripting service");

            var rabbitMQUserName = config.GetString("rabbitMQUserName", "Username");
            var rabbitMQPassword = config.GetString("rabbitMQPassword", "Password");
            var rabbitMQVirtualHost = config.GetString("rabbitMQVirtualHost", "VirtualHost");
            var rabbitMQHostName = config.GetString("rabbitMQHostName", "HostName");
            var wcfIP = config.GetString("rabbitMQHostName", "WCFServerIP"); 
            var wcfPort = config.GetString("wcfPort", "WCFServerPort");

            _connector = new Connector(wcfIP, wcfPort);
            _scriptingManager = new ScriptingManager(_connector, rabbitMQUserName, rabbitMQPassword, rabbitMQVirtualHost, rabbitMQHostName);

            _connector.StartSignal += ConnectorOnStartSignal;
            _connector.SetSignalFlag += ConnectorOnSetSignalFlag;
            _connector.StartIndicator += ConnectorOnStartIndicator;
            _connector.UpdateStrategyParams += (serviceId, login, name, parameters) => _scriptingManager.UpdateSignalStrategyParams(login, name, parameters);
            _connector.RemoveIndicator += (login, name, id) => _scriptingManager.RemoveWorkingIndicator(name, login);
            _connector.NewTick += (sender, tick) => Task.Run(() => _scriptingManager.RecalculateOnNewTick(tick));
            _connector.NewBar += (sender, tuple) => Task.Run(() => _scriptingManager.RecalculateOnNewBar(tuple.Item1, tuple.Item2));

        }

        private void ConnectorOnStartSignal(object sender, StartSignalParameters startSignalParameters)
        {
            Task.Run(() =>
            {
                var signalBase = _scriptingManager.StartSignalExecution(startSignalParameters.Login,
                    startSignalParameters.SignalInitParams, startSignalParameters.AccountInfos, startSignalParameters.Files);
                if (signalBase == null)
                    return;

                var signal = ScriptingManager.CreateSignal(signalBase, startSignalParameters.SignalInitParams.FullName);
                if (signal == null)
                    return;

                _connector.SignalStarted(signal, startSignalParameters.Login, signalBase.GetActualAlerts());
            });
        }

        private void ConnectorOnStartIndicator(object sender, StartIndicatorParameters startIndicatorParameters)
        {
            Task.Run(() =>
            {
                if (startIndicatorParameters.Files != null)
                    _scriptingManager.SaveCustomIndicatorData(startIndicatorParameters.Login,
                        startIndicatorParameters.Name, startIndicatorParameters.Files);

                var indicator = _scriptingManager.StartIndicatorInstance(startIndicatorParameters.Login, startIndicatorParameters.Name,
                    startIndicatorParameters.PriceType, startIndicatorParameters.Parameters,
                    startIndicatorParameters.Selection);

                _connector.IndicatorStarted(indicator, startIndicatorParameters.Login, startIndicatorParameters.Name, startIndicatorParameters.OperationID);
            });
        }

        private void ConnectorOnSetSignalFlag(string processorID, string username, string path, SignalAction action)
        {
            Task.Run(() =>
            {
                var state = _scriptingManager.SetSignalFlag(username, path, action);
                _connector.SendFlagState(username, path, action, state);
            });
        }

        public void Start()
        {
            Console.WriteLine("Core started");
            _connector.RegisterService();
        }
    }
}
