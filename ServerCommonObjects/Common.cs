/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.Collections.Generic;
using CommonObjects;

namespace ServerCommonObjects
{
    public delegate void HistoryAnswerHandler(Selection parameters, List<Bar> aBars);
    public delegate void NewTickHandler(Tick tick);
    public delegate void NewSecurityHandler(Security security);
    public delegate void CodeExitHandler(ScriptingType type, string codeName, string user);
    public delegate void ScriptingOutputHandler(List<Output> outputs, IUserInfo userInfo);
    public delegate void ScriptingExitHandler(ScriptingType type, string codeName, IUserInfo user);
    public delegate void ScriptingRemovedHandler(ScriptingType type, string codeName, IUserInfo user);
    public delegate void ScriptingBacktestReportHandler(List<BacktestResults> reports, float progress, IUserInfo user);
    public delegate void SetSignalFlagHandler(string processorID, string username, string path, SignalAction action);
    public delegate void RemoveIndicatorHandler(string login, string indicatorName, IWCFProcessorInfo processor);
    public delegate void UpdateSignalStrategyParamsHandler(string processorID, string login, string signalName, StrategyParams parameters);
}
