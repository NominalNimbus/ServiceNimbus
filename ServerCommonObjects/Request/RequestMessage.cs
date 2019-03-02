/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.Runtime.Serialization;
using CommonObjects;
using CommonObjects.Classes;
using ServerCommonObjects.Classes;
using Newtonsoft.Json;

namespace ServerCommonObjects
{
    /// <summary>
    /// base class for a request message
    /// </summary>
    [DataContract]
    [KnownType(typeof(LoginRequest))]
    [KnownType(typeof(UpdateUserInfoRequest))]
    [KnownType(typeof(GetDataFeedListRequest))]
    [KnownType(typeof(SubscribeRequest))]
    [KnownType(typeof(UnsubscribeRequest))]
    [KnownType(typeof(HistoryDataRequest))]
    [KnownType(typeof(HeartbeatRequest))]
    [KnownType(typeof(OrdersListRequest))]
    [KnownType(typeof(PlaceOrderRequest))]
    [KnownType(typeof(CancelOrderRequest))]
    [KnownType(typeof(ModifyOrderRequest))]
    [KnownType(typeof(TradingInfoRequest))]
    [KnownType(typeof(BrokersLoginRequest))]
    [KnownType(typeof(BrokerLogoutRequest))]
    [KnownType(typeof(PortfolioAction))]
    [KnownType(typeof(PortfolioActionRequest))]
    [KnownType(typeof(BrokersAvailableSecuritiesRequest))]
    [KnownType(typeof(AddUserFilesRequest))]
    [KnownType(typeof(RemoveUserFilesRequest))]
    [KnownType(typeof(ScriptingRequest))]
    [KnownType(typeof(ColorValue))]
    [KnownType(typeof(CreateUserIndicatorRequest))]
    [KnownType(typeof(CreateUserSignalRequest))]
    [KnownType(typeof(Selection))]
    [KnownType(typeof(SignalDataRequest))]
    [KnownType(typeof(BacktestResultsRequest))]
    [KnownType(typeof(SaveScriptingDataRequest))]
    [KnownType(typeof(SignalActionRequest))]
    [KnownType(typeof(UpdateStrategyParamsRequest))]
    [KnownType(typeof(ScriptingParameterBase))]
    [KnownType(typeof(IntParam))]
    [KnownType(typeof(DoubleParam))]
    [KnownType(typeof(StringParam))]
    [KnownType(typeof(BoolParam))]
    [KnownType(typeof(SeriesParam))]
    [KnownType(typeof(DrawShapeStyle))]
    [KnownType(typeof(DrawStyle))]
    [KnownType(typeof(Timeframe))]
    [KnownType(typeof(ScriptingType))]
    [KnownType(typeof(StartMethod))]
    [KnownType(typeof(Series))]
    [KnownType(typeof(ScriptingBase))]
    [KnownType(typeof(Signal))]
    [KnownType(typeof(Indicator))]
    [KnownType(typeof(SeriesValue))]
    [KnownType(typeof(RemoveScriptingInstanceRequest))]
    [KnownType(typeof(ScriptingOutput))]
    [KnownType(typeof(ScriptingReportRequest))]

    [KnownType(typeof(GetLastTickRequest))]
    [KnownType(typeof(StartedSignalExecutionRequest))]
    [KnownType(typeof(ScriptingOutputRequest))]
    [KnownType(typeof(GetBarsRequest))]
    [KnownType(typeof(GetPositionsRequest))]
    [KnownType(typeof(CancelAccountOrderRequest))]
    [KnownType(typeof(ClosePositionRequest))]
    [KnownType(typeof(ModifyAccountOrderRequest))]
    [KnownType(typeof(PlaceAccountOrderRequest))]

    [KnownType(typeof(GetPortfoliosRequest))]
    [KnownType(typeof(GetAccountsRequest))]
    [KnownType(typeof(GetAvailableSecuritiesRequest))]
    [KnownType(typeof(GetOrderRequest))]
    [KnownType(typeof(GetOrdersRequest))]
    [KnownType(typeof(NewBacktestResultsRequest))]
    [KnownType(typeof(BacktestReportRequest))]
    [KnownType(typeof(SignalActionSettedRequest))]
    [KnownType(typeof(IndicatorStartedRequest))]
    [KnownType(typeof(SeriesUpdatedRequest))]
    [KnownType(typeof(ScriptingAlertRequest))]
    [KnownType(typeof(GetAvailableDataFeedsRequest))]
    [KnownType(typeof(GetTickRequest))]
    [KnownType(typeof(GetAvailableSymbolsRequest))]

    [KnownType(typeof(CreateSimulatedBrokerAccountInfo))]
    [KnownType(typeof(CreateSimulatedBrokerAccountRequest))]
    public class RequestMessage
    {
        /// <summary>
        /// identifies user/session info where the request received
        /// </summary>
        [IgnoreDataMember]
        [JsonIgnore]
        public IUserInfo User { get; set; }

        /// <summary>
        /// identifies processor/session info where the request received
        /// </summary>
        [IgnoreDataMember]
        [JsonIgnore]
        public IWCFProcessorInfo Processor { get; set; }
    }

    public class BaseRequest
    {
        public string Type { get; set; } = RequestType.NONE;
    }
}