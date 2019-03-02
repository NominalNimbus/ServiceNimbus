/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CommonObjects;
using ServerCommonObjects;
using ServerCommonObjects.Classes;
using Scripting;
using Scripting.TechnicalIndicators;

namespace ScriptingManager
{
    public class ScriptingServiceManager
    {

        #region Fields

        private readonly IEntityStore<Signal> _signalStore;

        #endregion // Fields

        #region Properties

        private ICore Core { get; }

        #endregion // Properties

        #region Members

        private readonly Dictionary<string, List<ScriptingParameterBase>> _availableStandardIndicators;
        private readonly List<IndicatorBase> _standardIndicatorsInstances;
        private readonly Dictionary<string, List<IndicatorService>> _userWorkingIndicators;
        private readonly Dictionary<string, Dictionary<string, List<ScriptingParameterBase>>> _userIndicators;

        private readonly string _indicatorsFolderPath;
        private readonly string _signalsFolderPath;
        private readonly Dictionary<string, List<Signal>> _userSignals;
        private readonly Dictionary<string, List<SignalService>> _userWorkingSignals;

        public List<string> DefaultIndicators => _availableStandardIndicators.Keys.ToList();

        #endregion

        #region Initialization

        public ScriptingServiceManager(ICore core)
        {
            Core = core ?? throw new ArgumentNullException(nameof(core));
            _standardIndicatorsInstances = new List<IndicatorBase>
            {
                new SimpleMovingAverage(),
                new SmoothedMovingAverage (),
                new ExponentialMovingAverage(),
                new LinearWeightedMovingAverage(),
                new AcceleratorOscillator(),
                new AccumulationDistribution(),
                new AverageDirectionalMovement (),
                new Alligator(),
                new AwesomeOscillator(),
                new AverageTrueRange(),
                new BollingerBands(),
                new BearsPower(),
                new BullsPower(),
                new MarketFacilitationIndex(),
                new CommodityChannelIndex(),
                new Envelopes(),
                new ForceIndex(),
                new Gator(),
                new MACD(),
                new MoneyFlowIndex(),
                new Momentum(),
                new OnBalanceVolume(),
                new MovingAverageOfOscillator(),
                new RelativeStrengthIndex(),
                new RelativeVigorIndex(),
                new StandardDeviation(),
                new Volume(),
                new WPercentRange(),
                new ParabolicSAR(),
                new StochasticOscillator(),
                new PL(),
                new ZigZag()
            };

            _signalStore = new SignalStore(Core.FileManager);

            _userWorkingIndicators = new Dictionary<string, List<IndicatorService>>();
            _userIndicators = new Dictionary<string, Dictionary<string, List<ScriptingParameterBase>>>();
            _availableStandardIndicators = new Dictionary<string, List<ScriptingParameterBase>>();
            _userSignals = new Dictionary<string, List<Signal>>();
            _userWorkingSignals = new Dictionary<string, List<SignalService>>();

            foreach (var indicator in _standardIndicatorsInstances)
                _availableStandardIndicators.Add(indicator.Name, indicator.GetParameters().ToList());

            try
            {
                const string folder = "CustomIndicators";
                _indicatorsFolderPath = Directory.Exists(folder)
                    ? new DirectoryInfo(folder).FullName
                    : Directory.CreateDirectory(folder).FullName;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to create folder for custom indicators", ex);
            }

            try
            {
                const string folder = "CustomSignals";
                _signalsFolderPath = Directory.Exists(folder)
                    ? new DirectoryInfo(folder).FullName
                    : Directory.CreateDirectory(folder).FullName;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to create folder for custom Signals", ex);
            }
        }

        #endregion

        #region Signals

        public Signal SaveSignalData(IUserInfo user, string path, Dictionary<string, byte[]> files, out string errors)
        {
            var error = RemoveSignalData(user, path);

            if (!string.IsNullOrEmpty(error))
            {
                errors = error;
                return null;
            }

            errors = string.Empty;
            var name = CommonHelper.GetDirectoryName(path);
            if (!files.Any(p => p.Key.EndsWith("\\" + name + ".dll")))
            {
                errors = "DLL for " + name + " signal is not found among supplied files";
                Logger.Error("Failed to create signal: " + errors);
                return null;
            }

            var root = Path.Combine(_signalsFolderPath, user.Login);
            var cleanedUpDirs = new List<string>();
            foreach (var file in files)
            {
                try
                {
                    var dir = Path.GetDirectoryName(Path.Combine(root, file.Key));
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    if (!cleanedUpDirs.Contains(dir))
                    {
                        cleanedUpDirs.Add(dir);
                        foreach (var f in Directory.GetFiles(dir))  //optional: delete old/existing files
                            File.Delete(f);
                    }

                    File.WriteAllBytes(Path.Combine(root, file.Key), Compression.Decompress(file.Value));
                }
                catch (Exception exc)
                {
                    Logger.Error("Failed to create signal", exc);
                    errors = exc.Message;
                    return null;
                }
            }

            try
            {
                errors = AddSignal(user.Login, path);
                var signalDir = Path.Combine(root, path);
                if (!string.IsNullOrEmpty(errors))
                {
                    foreach (var dll in Directory.GetFiles(signalDir, "*.dll"))
                        File.Delete(dll);
                }
                else
                {
                    List<Signal> signals;

                    lock (_userSignals)
                    {
                        if (!_userSignals.TryGetValue(user.Login, out signals))
                            return null;
                    }

                    lock (signals)
                        return signals.FirstOrDefault(i => i.Name == path);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to create instance of a signal", ex);
                errors = ex.Message;
            }

            return null;
        }

        public string RemoveSignalData(IUserInfo user, string path)
        {
            var login = user.Login;

            List<Signal> signals;
            lock (_userSignals)
            {
                _userSignals.TryGetValue(login, out signals);
            }

            if (signals != null)
            {
                lock (signals)
                    signals.RemoveAll(x => x.Name == path);
            }

            var userFolderPath = Path.Combine(_signalsFolderPath, login);
            if (!Directory.Exists(userFolderPath))
                return null;

            var signalFolderPath = Path.Combine(userFolderPath, path);
            try
            {
                if (Directory.Exists(signalFolderPath))
                    Directory.Delete(signalFolderPath, true);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message, ex);
                return "Failed to remove '" + path + "' signal";
            }

            return null;
        }

        private Signal LoadSignalFromStore(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            var configName = Path.GetFileName(path);
            var signal = _signalStore.GetEntity(Path.Combine(_signalsFolderPath, path, configName + FileExtensions.SIGNAL_CONFIG));
            return signal;
        }

        private void InitUserSignal(string user, string path)
        {
            var context = new ScriptingAppDomainContext(user, path, _signalsFolderPath);

            try
            {
                var signalBase = context.CreateSignal();
                if (signalBase != null)
                {
                    var signalList = new List<Signal>();
                    var signal = new Signal
                    {
                        Name = path,
                        ID = signalBase.ID,
                        State = signalBase.State,
                        Parameters = signalBase.GetParameters(),
                        Selections = signalBase.Selections
                    };

                    signalList.Add(signal);

                    lock (_userSignals)
                        _userSignals.Add(user, signalList);

                    _signalStore.AddEntity(Path.Combine(_signalsFolderPath, user, path, signalBase.Name + FileExtensions.SIGNAL_CONFIG), signal);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to create new signal", ex);
            }
            finally
            {
                try
                {
                    context.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to dispose signal context", ex);
                }
            }
        }

        private void AddSignalToList(string user, string path, ICollection<Signal> signals)
        {
            Signal existingSignal;

            lock (signals)
                existingSignal = signals.FirstOrDefault(x => x.Name == path);

            var context = new ScriptingAppDomainContext(user, path, _signalsFolderPath);

            try
            {
                var signal = context.CreateSignal();
                if (signal != null)
                {
                    var newSignal = new Signal
                    {
                        Name = path,
                        ID = signal.ID,
                        State = signal.State,
                        Parameters = signal.GetParameters(),
                        Selections = signal.Selections
                    };

                    if (existingSignal != null)
                    {
                        _signalStore.UpdateEntity(Path.Combine(_signalsFolderPath, user, path, signal.Name + FileExtensions.SIGNAL_CONFIG), newSignal);
                    }
                    else
                    {
                        lock (signals)
                            signals.Add(newSignal);

                        _signalStore.AddEntity(Path.Combine(_signalsFolderPath, user, path, signal.Name + FileExtensions.SIGNAL_CONFIG), newSignal);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to create new signal", ex);
            }
            finally
            {
                try
                {
                    context.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to dispose signal context", ex);
                }
            }
        }

        private void AddSignal(string user, string path, Signal signal)
        {
            List<Signal> signals;
            lock (_userSignals)
            {
                if (!_userSignals.TryGetValue(user, out signals))
                {
                    signals = new List<Signal> { signal };
                    _userSignals.Add(user, signals);
                }
            }

            lock (signals)
            {
                var exist = false;
                for (var i = 0; i < signals.Count; i++)
                {
                    if (signals[i].Name != path) continue;

                    signals[i] = signal;
                    exist = true;
                    break;
                }

                if (!exist)
                    signals.Add(signal);
            }
        }

        private string AddSignal(string user, string path)
        {
            try
            {
                if (Monitor.TryEnter(_userSignals, 5000))
                {
                    if (_userSignals.TryGetValue(user, out var signals))
                    {
                        Monitor.Exit(_userSignals);
                        AddSignalToList(user, path, signals);
                    }
                    else
                    {
                        Monitor.Exit(_userSignals);
                        InitUserSignal(user, path);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ScriptingServiceManager.AddSignal -> ", ex);
            }
            finally
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            return string.Empty;
        }

        public void AddUserFiles(string user, string relativePath, byte[] zippedFiles)
        {
            var root = GetUserFolderPath(user);
            if (Directory.Exists(root))
                CommonHelper.UnzipContent(Path.Combine(root, relativePath), zippedFiles);
        }

        public void DeleteUserFiles(string user, string[] relativePaths)
        {
            if (relativePaths == null || relativePaths.Length == 0)
                return;

            var root = GetUserFolderPath(user);
            if (!Directory.Exists(root))
                return;

            var rootDir = new DirectoryInfo(root);
            foreach (var paths in relativePaths)
            {
                var dir = Path.Combine(root, paths);
                if (Directory.Exists(dir) && new DirectoryInfo(dir).FullName != rootDir.FullName)
                {
                    try { Directory.Delete(dir, true); }
                    catch (Exception e) { Logger.Warning($"Failed to delete '{dir}' directory: {e.Message}"); }
                }
            }
        }

        public List<Signal> GetSignals(string user)
        {
            lock (_userSignals)
            {
                if (!_userSignals.ContainsKey(user))
                    LoadUserSignals(user);

                return _userSignals.ContainsKey(user) ? _userSignals[user].ToList() : null;
            }
        }

        private void LoadUserSignals(string user)
        {
            var userFolderPath = Path.Combine(_signalsFolderPath, user);
            if (!Directory.Exists(userFolderPath))
                return;

            if (userFolderPath[userFolderPath.Length - 1] != '\\')
                userFolderPath += "\\";

            var paths = new List<string>();
            foreach (var path in GetSignalFolderPaths(user))
            {
                var p = path.Substring(userFolderPath.Length);
                if (p.Count(c => c == '\\') == 2 && !paths.Contains(p))
                    paths.Add(p);
            }

            foreach (var fullName in paths)
            {
                try
                {
                    var signal = LoadSignalFromStore(Path.Combine(user, fullName));
                    if (signal != null)
                    {
                        AddSignal(user, fullName, signal);
                        continue;
                    }

                    AddSignal(user, fullName);
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to add user signal to list: " + e.Message);
                }
            }
        }

        public void AddWorkingSignal(string userLogin, Signal signal, string processorID)
        {
            lock (_userWorkingSignals)
            {
                var ss = new SignalService(signal, processorID);
                if (_userWorkingSignals.TryGetValue(userLogin, out var list))
                    list.Add(ss);
                else
                    _userWorkingSignals[userLogin] = new List<SignalService> { ss };
            }
        }

        public List<Signal> GetWorkingSignals(string user)
        {
            lock (_userWorkingSignals)
            {
                return _userWorkingSignals.TryGetValue(user, out var list)
                    ? list.Select(s => s.Signal).ToList()
                    : new List<Signal>(0);
            }
        }

        private string GetUserFolderPath(string user) =>
            string.IsNullOrWhiteSpace(user) ? string.Empty : Path.Combine(_signalsFolderPath, user);

        public List<string> GetSignalFolderPaths(string user)
        {
            var userFolderPath = GetUserFolderPath(user);
            if (!Directory.Exists(userFolderPath))
                return new List<string>(0);

            var paths = new List<string>();
            foreach (var dll in Directory.GetFiles(userFolderPath, "*.dll", SearchOption.AllDirectories))
            {
                var path = dll.Remove(dll.LastIndexOf('\\'));
                if (!paths.Contains(path))
                    paths.Add(path);
            }

            return paths;
        }

        public void SaveBacktestResults(string user, string signal, List<BacktestResults> results)
        {
            var path = Path.Combine(GetUserFolderPath(user), signal);
            if (!Directory.Exists(path))
                return;

            var file = Path.Combine(path, "Backtest Results.xml");
            if (File.Exists(file))
            {
                try { File.Delete(file); }
                catch { }
            }

            if (results == null || results.Count == 0)
                return;

            try
            {
                using (var serializer = new System.Xml.XmlTextWriter(file, System.Text.Encoding.UTF8))
                {
                    var xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(List<BacktestResults>));
                    xmlSerializer.Serialize(serializer, results);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to serialize backtest results to {signal}: {e.Message}");
            }
        }

        public void SignalServiceRemoved(string user, string path)
        {
            lock (_userWorkingSignals)
            {
                if (_userWorkingSignals.TryGetValue(user, out var list))
                {
                    var signalService = list.FirstOrDefault(s => s.Signal.Name == path);
                    list.Remove(signalService);
                }
            }
        }

        public string ScriptingCodeServiceID(string login, string path, ScriptingType type)
        {
            if (type == ScriptingType.Signal)
            {
                lock (_userWorkingSignals)
                {
                    if (_userWorkingSignals.TryGetValue(login, out var list))
                    {
                        var signalService = list.FirstOrDefault(s => s.Signal.Name == path);
                        return signalService?.ServiceID;
                    }
                    return string.Empty;
                }
            }

            lock (_userWorkingIndicators)
            {
                if (_userWorkingIndicators.TryGetValue(login, out var list))
                {
                    var signalService = list.FirstOrDefault(s => s.Name == path);
                    return signalService?.ServiceID;
                }
                return string.Empty;
            }

        }

        #endregion

        #region Indicators

        public void AddWorkingIndicator(string login, string indicatorName, string serviceID)
        {
            var ss = new IndicatorService(indicatorName, serviceID);
            lock (_userWorkingIndicators)
            {
                if (_userWorkingIndicators.TryGetValue(login, out var list))
                    list.Add(ss);
                else
                    _userWorkingIndicators[login] = new List<IndicatorService> { ss };
            }
        }

        public void RemoveWorkingIndicator(string name, string userName)
        {
            lock (_userWorkingIndicators)
            {
                var userIndicators = _userWorkingIndicators.FirstOrDefault(p => p.Key == userName);
                if (userIndicators.Value == null) return;

                userIndicators.Value.RemoveAll(i => i.Name == name);
            }
        }

        public void ClearWorkingIndicators(IUserInfo eUserInfo)
        {
            lock (_userWorkingIndicators)
                _userWorkingIndicators.Remove(eUserInfo.Login);
        }

        public List<ScriptingParameterBase> SaveCustomIndicatorData(IUserInfo user, string name, Dictionary<string, byte[]> dlls, out string errors)
        {
            var error = RemoveCustomIndicatorData(user.Login, name);

            if (!string.IsNullOrEmpty(error))
            {
                errors = error;
                return new List<ScriptingParameterBase>();
            }

            errors = string.Empty;

            if (dlls.Count(p => p.Key.Equals(name + ".dll")) == 0)
            {
                errors = $"DLL with indicator name {name} is not found.";
                return new List<ScriptingParameterBase>();
            }

            if (!dlls.Any(p => p.Key.EndsWith(".dll")))
            {
                errors = "List of dll`s contain invalid data.";
                return new List<ScriptingParameterBase>();
            }

            var userFolder = Path.Combine(_indicatorsFolderPath, user.Login);
            var indicatorFolder = Path.Combine(userFolder, name);

            try
            {
                if (!Directory.Exists(userFolder))
                    Directory.CreateDirectory(userFolder);

                if (Directory.Exists(indicatorFolder))
                    Directory.Delete(indicatorFolder, true);

                Directory.CreateDirectory(indicatorFolder);

                foreach (var dll in dlls)
                {
                    var data = Compression.Decompress(dll.Value);
                    File.WriteAllBytes(Path.Combine(indicatorFolder, dll.Key), data);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to create custom indicator", ex);
                errors = ex.Message;
                return new List<ScriptingParameterBase>();
            }

            try
            {
                errors = ValidateAndAddUserIndicatorToList(user.Login, name);

                if (!string.IsNullOrEmpty(errors))
                {
                    if (Directory.Exists(indicatorFolder))
                        Directory.Delete(indicatorFolder, true);

                    return new List<ScriptingParameterBase>();
                }

                lock (_userIndicators)
                {
                    if (_userIndicators.ContainsKey(user.Login) && _userIndicators[user.Login].ContainsKey(name))
                        return _userIndicators[user.Login][name].ToList();
                }

                return new List<ScriptingParameterBase>();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to create instance of custom indicator", ex);
                errors = ex.Message;
                return new List<ScriptingParameterBase>();
            }
        }

        public Dictionary<string, byte[]> GetIndicatorFiles(string userLogin, string name)
        {
            var indicatorFolder = Path.Combine(_indicatorsFolderPath, userLogin, name);

            return !Directory.Exists(Path.Combine(_indicatorsFolderPath, userLogin, name))
                ? null
                : Directory.GetFiles(indicatorFolder, "*.dll", SearchOption.TopDirectoryOnly)
                    .ToDictionary(Path.GetFileName, dll => Compression.Compress(File.ReadAllBytes(dll)));
        }

        public string RemoveCustomIndicatorData(string login, string name)
        {
            var userFolderPath = Path.Combine(_indicatorsFolderPath, login);

            lock (_userWorkingIndicators)
            {
                var userIndicators = _userWorkingIndicators.FirstOrDefault(p => p.Key == login);
                if (userIndicators.Value == null) return string.Empty;

                userIndicators.Value.RemoveAll(i => i.Name == name);
            }

            lock (_userIndicators)
            {
                if (_userIndicators.ContainsKey(login) && _userIndicators[login].ContainsKey(name))
                    _userIndicators[login].Remove(name);
            }


            if (!Directory.Exists(userFolderPath))
                return null;

            try
            {
                var indicatorFolderPath = Path.Combine(userFolderPath, name);
                if (Directory.Exists(indicatorFolderPath))
                    Directory.Delete(indicatorFolderPath, true);
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message, ex);
                return "Failed to remove user indicator " + name;
            }
        }

        public Dictionary<string, List<ScriptingParameterBase>> GetAllAllowedIndicators(IUserInfo user)
        {
            var ind = _availableStandardIndicators.ToDictionary(indicator => indicator.Key, indicator => new List<ScriptingParameterBase>(indicator.Value));

            lock (_userIndicators)
            {
                if (!_userIndicators.ContainsKey(user.Login))
                    LoadUserCustomIndicators(user);

                if (!_userIndicators.ContainsKey(user.Login))
                    return ind;

                foreach (var indicator in _userIndicators[user.Login])
                    ind.Add(indicator.Key, new List<ScriptingParameterBase>(indicator.Value));
            }

            return ind;
        }

        private void LoadUserCustomIndicators(IUserInfo user)
        {
            var userFolderPath = Path.Combine(_indicatorsFolderPath, user.Login);

            if (!Directory.Exists(userFolderPath))
                return;

            var indicatorsDirs = Directory.GetDirectories(userFolderPath);

            foreach (var indicatorsDir in indicatorsDirs)
            {
                try
                {
                    var info = new DirectoryInfo(indicatorsDir);
                    ValidateAndAddUserIndicatorToList(user.Login, info.Name);
                }
                catch (Exception ex)
                {
                    Logger.Error("Cannot load custom indicators", ex);
                }
            }
        }
        private string ValidateAndAddUserIndicatorToList(string user, string name)
        {
            try
            {
                lock (_userIndicators)
                {
                    if (!_userIndicators.ContainsKey(user))
                        _userIndicators.Add(user, new Dictionary<string, List<ScriptingParameterBase>>());

                    var oldInstance = _userIndicators[user].FirstOrDefault(p => p.Key.Equals(name));

                    if (!string.IsNullOrEmpty(oldInstance.Key))
                        _userIndicators[user].Remove(oldInstance.Key);

                    var context = new ScriptingAppDomainContext(user, name, _indicatorsFolderPath);

                    try
                    {
                        var indicator = context.CreateIndicator();
                        _userIndicators[user].Add(name, indicator.GetParameters().ToList());
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("ValidateUserIndicator - failed to create new indicator.", ex);
                        return "Failed to create indicator: " + name;
                    }
                    finally
                    {
                        try
                        {
                            context.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("ValidateUserIndicator - failed to dispose indicator context.", ex);
                        }
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                Logger.Error("ValidateUserIndicator - common exception.", ex);
                return ex.Message;
            }
        }

        public IEnumerable<string> GetUserIndicatorsNames(string login)
        {
            lock (_userWorkingIndicators)
            {
                if (_userWorkingIndicators.TryGetValue(login, out var list))
                    return list.Select(i => i.Name);
            }

            return new List<string>(0);
        }

        #endregion

        #region Helper Classes

        private class SignalService
        {
            public Signal Signal { get; }
            public string ServiceID { get; }

            public SignalService(Signal signal, string serviceID)
            {
                Signal = signal;
                ServiceID = serviceID;
            }
        }

        private class IndicatorService
        {
            public string Name { get; }
            public string ServiceID { get; }

            public IndicatorService(string name, string serviceID)
            {
                Name = name;
                ServiceID = serviceID;
            }
        }

        #endregion

    }
}