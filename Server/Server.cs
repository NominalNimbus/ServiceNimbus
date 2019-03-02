/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using ServerCommonObjects;
using System.Collections.Generic;
using ServerCommonObjects.ServerClasses;
using System.IO;
using System.Linq;
using System;
using System.Net;
using System.Reflection;
using ServerCommonObjects.Interfaces;

namespace Server
{
    internal class Server
    {
        private readonly string _startupPath;
        private readonly string _connectionString;
        private readonly Authentication _authentication;
        private readonly MessageProcessor _messageProcessor;
        private readonly List<IDataFeed> _dataFeeds = new List<IDataFeed>();
        private readonly List<IServerServiceHost> _serviceHosts = new List<IServerServiceHost>();

        public Server()
        {
            ICore core = new Core();
            _connectionString = core.FileManager.LoadContent("DataBaseConnection.set");

            if (string.IsNullOrEmpty(_connectionString))
                throw new Exception("Connection string is empty. Please provide valid connection string and restart aplication.");

            _authentication = new Authentication(_connectionString);
            _messageProcessor = new MessageProcessor(core);

            _messageProcessor.Notification += MessageProcessor_OnNotification;
            MessageRouter.gMessageRouter.AddedSession += MsgRouter_OnSessionAdded;
            MessageRouter.gMessageRouter.RemovedSession += MsgRouter_OnSessionRemoved;

            _startupPath = Directory.GetCurrentDirectory();
        }

        public void Start()
        {
            Logger.Info("Starting...");

            var isOnline = CheckForInternetConnection();
            if (!isOnline)
            {
                Logger.Error("Internet connection is required. Please check your connection and restart Server application");
                return;
            }

            LoadDataFeeds();
            LoadServiceHosts();

            MessageRouter.gMessageRouter.Init(_authentication);
            _messageProcessor.Start(_dataFeeds, _connectionString);

            lock (MessageRouter.gMessageRouter)
            {
                foreach (var item in _dataFeeds)
                {
                    try
                    {
                        item.Start();
                        Logger.Info($"Datafeed {item.Name} initialized");
                    }
                    catch (Exception e)
                    {
                        Logger.Warning($"Failed to initialize {item.Name} feed.", e);
                    }
                }

                foreach (var item in _serviceHosts)
                {
                    try
                    {
                        item.Start();
                        Logger.Info($"Service {item.Name} initialized");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"Failed to initialize {item.Name} connection service.", ex);
                    }
                }
            }

            Logger.Info("Started");
        }

        public void Stop()
        {
            if (_messageProcessor == null)
                return;

            Logger.Info("Stopping...");

            _messageProcessor.Notification -= MessageProcessor_OnNotification;
            try
            {
                _messageProcessor.Stop();
            }
            catch
            {
                // ignored
            }

            foreach (var item in _dataFeeds)
            {
                try
                {
                    if (item.IsStarted)
                        item.Stop();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Cannot stop datafeed {item.Name}", ex);
                }
            }

            foreach (var item in _serviceHosts)
            {
                try
                {
                    item.Stop();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Cannot stop service host {item.Name}", ex);
                }
            }

            Logger.Info("Stopped");
        }

        private string[] GetFiles(string path)
        {
            try
            {
                return Directory.GetFiles(Path.Combine(_startupPath, path), "*.dll");
            }
            catch (Exception ex)
            {
                Logger.Error($"Cannot retrieve {path} libraries", ex);
                return new string[0];
            }
        }

        private void LoadDataFeeds()
        {
            var dlls = GetFiles("Datafeeds");
            if (dlls.Length == 0)
                return;

            var instances = ActivatorHelper<IDataFeed>.Activate(dlls, "DataFeed.dll");
            _dataFeeds.AddRange(instances);
        }

        private void LoadServiceHosts()
        {
            var dlls = GetFiles("ServiceHosts");
            if (dlls.Length == 0)
                return;

            var instances = ActivatorHelper<IServerServiceHost>.Activate(dlls, "ServiceHost.dll");
            _serviceHosts.AddRange(instances);
        }

        #region Event Handlers

        private void MessageProcessor_OnNotification(object sender, CommonObjects.EventArgs<string> e) =>
            Logger.Info(e.Value);

        private void MsgRouter_OnSessionRemoved(object sender, MessageRouter.MessageRouter_EventArgs e) =>
            Logger.Info($"Removed session: login = '{e.UserInfo.Login}' ID = '{e.ID}'");

        private void MsgRouter_OnSessionAdded(object sender, MessageRouter.MessageRouter_EventArgs e) =>
            Logger.Info($"Added session: login = '{e.UserInfo.Login}' ID = '{e.ID}'");

        #endregion

        #region Static Helpers

        private static bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead("http://www.google.com"))
                    return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Activator Helper

        private static class ActivatorHelper<T>
        {
            public static IEnumerable<T> Activate(IEnumerable<string> dlls, string suffix)
            {
                foreach (var item in dlls.Where(i => i.EndsWith(suffix)))
                {
                    var dll = Assembly.UnsafeLoadFrom(item);
                    var types = dll.GetTypes();

                    var host = Activate(types);
                    if (host != null)
                        yield return host;
                }
            }

            private static T Activate(IEnumerable<Type> types)
            {
                foreach (var type in types)
                {
                    if (type.GetInterface(typeof(T).Name) == null)
                        continue;

                    try
                    {
                        return (T)Activator.CreateInstance(type);
                    }
                    catch (TargetInvocationException ex)
                    {
                        if (ex.InnerException is ArgumentNullException argNullException)
                            Logger.Warning($"Failed to initialize {type.Name}. {argNullException.ParamName} is null or empty");
                        else
                            Logger.Error($"Failed to initialize {type.Name}", ex);
                    }
                }

                return default(T);
            }
        }

        #endregion

    }
}