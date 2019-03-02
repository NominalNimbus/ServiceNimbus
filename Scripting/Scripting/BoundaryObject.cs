/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Lifetime;
using ServerCommonObjects;

namespace Scripting
{
    [Serializable]
    public class ScriptingAppDomainContext : IDisposable
    {
        private readonly string _user;
        private readonly string _path;
        private readonly string _baseFolderPath;
        private BoundaryObject _boundaryObject;
        private AppDomain _appDomain;
        private AppDomainArgs _args;

        public string UserName { get; private set; }
        public string ScriptingName { get; private set; }

        public ScriptingAppDomainContext(string user, string path, string baseFolderPath)
        {
            _user = user;
            _path = path;
            _baseFolderPath = baseFolderPath;
        }

        public IndicatorBase CreateIndicator()
        {
            InitializeDomain();
            return _boundaryObject.CreateScriptingInstance(_args) as IndicatorBase;
        }

        public SignalBase CreateSignal()
        {
            InitializeDomain();
            return _boundaryObject.CreateScriptingInstance(_args) as SignalBase;
        }
        
        private void InitializeDomain()
        {
            var domaininfo = new AppDomainSetup { PrivateBinPath = Path.Combine(_baseFolderPath, _user, _path + "\\") };

            //Create evidence for the new appdomain from evidence of the current application domain
            var evidence = AppDomain.CurrentDomain.Evidence;
            _appDomain = AppDomain.CreateDomain(Path.Combine(_user, _path), evidence, domaininfo);
            _appDomain.Load("Scripting");
            _boundaryObject = (BoundaryObject)_appDomain.CreateInstanceAndUnwrap(
                typeof(BoundaryObject).Assembly.FullName,
                typeof(BoundaryObject).FullName ?? throw new InvalidOperationException());

            _args = new AppDomainArgs
            {
                Folder = domaininfo.PrivateBinPath,
                Name = _path.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).Last(),
                User = _user
            };

            var test = _boundaryObject.CreateScriptingInstance(_args);
            if (test == null)
                throw new NoNullAllowedException("Failed to create scripting instance");
            test = null;

            UserName = _user;
            ScriptingName = _path;
        }

        public void Dispose()
        {
            if (_appDomain != null)
                AppDomain.Unload(_appDomain);
        }
    }

    internal class BoundaryObject : MarshalByRefObject
    {
        private Assembly _assembly;
        private string _name;
        private string _user;
        private string _folder;

        public IScripting CreateScriptingInstance(AppDomainArgs args)
        {
            try
            {
                if (_assembly == null || _name != args.Name || _user != args.User || _folder != args.Folder)
                {
#if DEBUG
                    var dll = File.ReadAllBytes(Path.Combine(args.Folder, args.Name + ".dll"));
                    var pdbFile = Path.Combine(args.Folder, args.Name + ".pdb");
                    _assembly = File.Exists(pdbFile) ? Assembly.Load(dll, File.ReadAllBytes(pdbFile)) : Assembly.Load(dll);
#else
                    _assembly = Assembly.Load(File.ReadAllBytes(Path.Combine(args.Folder, args.Name + ".dll")));
#endif
                    _name = args.Name;
                    _user = args.User;
                    _folder = args.Folder;
                }

                var instance = _assembly.CreateInstance(args.Name + "." + args.Name) as IScripting;

                //failed to create instance by name - try to retrieve the type by base type
                if (instance == null)
                {
                    var t = _assembly.GetTypes().FirstOrDefault(i => i.BaseType == typeof(SignalBase));  //|| i.BaseType == typeof(IndicatorBase));
                    if (t != null)
                        instance = _assembly.CreateInstance(t.FullName ?? throw new InvalidOperationException()) as IScripting;
                }

                if (instance != null)
                    instance.Owner = args.User;
                return instance;
            }
            catch (Exception e)
            {
                Logger.Error("Failed to create scripting instance", e);
                return null;
            }
        }

        public override object InitializeLifetimeService()
        {
            var lease = (ILease)base.InitializeLifetimeService();
            if (lease != null && lease.CurrentState == LeaseState.Initial)
                lease.InitialLeaseTime = TimeSpan.FromDays(256);
            return lease;
        }
    }

    public class AppDomainArgs : MarshalByRefObject
    {
        public string Name { get; set; }
        public string User { get; set; }
        public string Folder { get; set; }

        public override object InitializeLifetimeService()
        {
            var lease = (ILease)base.InitializeLifetimeService();
            if (lease != null && lease.CurrentState == LeaseState.Initial)
                lease.InitialLeaseTime = TimeSpan.FromDays(256);
            return lease;
        }
    }
}
