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
using ServerCommonObjects.Classes;
using ServerCommonObjects.Interfaces;
using OMS;

namespace ServerCommonObjects
{
    public interface ICore
    {
        IFileManager FileManager { get; }


        #region Events

        event NewTickHandler NewTick;
        event ScriptingExitHandler ScriptingExit;
        event ScriptingBacktestReportHandler BacktestReport;
        event EventHandler<EventArgs<string>> Notification;
        event SetSignalFlagHandler SignalFlag;
        event RemoveIndicatorHandler RemoveIndicator;

        event EventHandler<Tick> NewSingleTick;
        event EventHandler<Tuple<string, string>> NewBar;

        #endregion

        bool IsTickCacheEnabled { get; set; }
        List<string> AvailableDataFeeds { get; }
        List<string> AvailableBrokers { get; }
        IUserInfo GetUser(string userName);


        IOMS OMS { get; }

        #region Portfolio manager

        List<Portfolio> GetPortfolios(IUserInfo user);

        int AddPortfolio(Portfolio portfolio, string user);

        bool RemovePortfolio(Portfolio portfolio);

        bool UpdatePortfolio(IUserInfo user, Portfolio portfolio);

        #endregion

        #region Data cache

        decimal GetTotalDailyAskVolume(Security security, int level);
        decimal GetTotalDailyBidVolume(Security security, int level);
        List<Security> GetDatafeedSecurities(string dataFeed);

        #endregion

        #region ScriptingManager

        List<IWCFProcessorInfo> GetAvailableProcessors();
        void SaveBacktestResults(string user, string signal, List<BacktestResults> results);
        void CodeBacktestReport(List<BacktestResults> reports, float progress, string username);

        void AddUserFiles(string user, string relativePath, byte[] zippedFiles);
        void DeleteUserFiles(string user, string[] relativePaths);

        List<string> GetDefaultIndicators();
        Dictionary<string, List<ScriptingParameterBase>> GetAllIndicators(IUserInfo user);
        List<Signal> GetAllSignals(IUserInfo user);
        List<string> GetSignalFolderPaths(string user);
        Dictionary<string, byte[]> GetIndicatorFiles(IUserInfo indicatorRequestUser, string indicatorRequestName);

        void AddSignal(IUserInfo user, Dictionary<string, byte[]> files, SignalInitParams parameters);
        void SignalStarted(IUserInfo user, Signal signal, IWCFProcessorInfo processor);
        void IndicatorStarted(IUserInfo user, IWCFProcessorInfo processor, string indicatorName);

        void RemoveUserIndicator(string name, IUserInfo user);
        void RemoveSignal(string path, IUserInfo user);

        List<ScriptingParameterBase> ValidateAndSaveCustomIndicator(IUserInfo user, string name, Dictionary<string, byte[]> DLLs, out string errors);
        string RemoveCustomIndicatorData(IUserInfo user, string name);

        Signal ValidateAndSaveSignal(IUserInfo user, string path, Dictionary<string, byte[]> files, out string errors);
        void SignalFlagSetted(string username, string path, SignalAction action, SignalState state);
        string RemoveSignalData(IUserInfo user, string path);

        List<Signal> GetWorkingSignalsAndUpdateUserInfo(IUserInfo user);
        void UpdateUserInfo(IUserInfo user);
        string GetScriptingServiceID(string login, string signalName, ScriptingType type);
        IWCFProcessorInfo GetProcessor(string id);

        #endregion

        void Start(List<IDataFeed> dataFeeds, string connectionString);
        void Stop();

        #region DataFeeds

        void GetHistory(Selection parameters, HistoryAnswerHandler callback);
        void GetTick(string symbol, string dataFeedName, DateTime timestamp, NewTickHandler callback);
        List<ReportField> GetCodeReport(string userLogin, string signalName, DateTime fromTime, DateTime toTime);

        #endregion

    }
}
