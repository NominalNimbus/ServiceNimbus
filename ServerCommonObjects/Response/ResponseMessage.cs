/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.Drawing;
using System.Runtime.Serialization;
using CommonObjects;
using ServerCommonObjects.Classes;
using ServerCommonObjects.Enums;

namespace ServerCommonObjects
{
    /// <summary>
    /// base class for a response message
    /// </summary>
    [DataContract]
    [KnownType(typeof(GetDataFeedListResponse))]
    [KnownType(typeof(HistoryDataResponse))]
    [KnownType(typeof(TickDataResponse))]
    [KnownType(typeof(HeartbeatResponse))]
    [KnownType(typeof(ErrorMessageResponse))]
    [KnownType(typeof(Security))]
    [KnownType(typeof(AccountInfoChangedResponse))]
    [KnownType(typeof(HistoricalOrdersListResponse))]
    [KnownType(typeof(OrdersUpdatedResponse))]
    [KnownType(typeof(OrdersChangedResponse))]
    [KnownType(typeof(TradingInfoResponse))]
    [KnownType(typeof(BrokerLoginResponse))]
    [KnownType(typeof(BrokersAvailableSecuritiesResponse))]
    [KnownType(typeof(PositionChangedResponse))]
    [KnownType(typeof(BrokerLogoutResponse))]
    [KnownType(typeof(PortfolioAccount))]
    [KnownType(typeof(Portfolio))]
    [KnownType(typeof(AvailableBrokerInfo))]
    [KnownType(typeof(PortfolioAction))]
    [KnownType(typeof(PortfolioStrategy))]
    [KnownType(typeof(Status))]
    [KnownType(typeof(PortfolioActionResponse))]
    [KnownType(typeof(OrderRejectionResponse))]
    [KnownType(typeof(PositionUpdatedResponse))]
    [KnownType(typeof(HistoricalOrderResponse))]
        
    //Scripting
    [KnownType(typeof(ScriptingResponse))]
    [KnownType(typeof(ScriptingParameterBase))]
    [KnownType(typeof(ColorValue))]
    [KnownType(typeof(SignalDataResponse))]
    [KnownType(typeof(ScriptingDataSavedResponse))]
    [KnownType(typeof(IntParam))]
    [KnownType(typeof(DoubleParam))]
    [KnownType(typeof(StringParam))]
    [KnownType(typeof(BoolParam))]
    [KnownType(typeof(SeriesParam))]
    [KnownType(typeof(DrawShapeStyle))]
    [KnownType(typeof(DrawStyle))]
    [KnownType(typeof(Timeframe))]
    [KnownType(typeof(StartMethod))]
    [KnownType(typeof(ScriptingType))]
    [KnownType(typeof(Selection))]
    [KnownType(typeof(Series))]
    [KnownType(typeof(Color))]
    [KnownType(typeof(Indicator))]
    [KnownType(typeof(ScriptingBase))]
    [KnownType(typeof(Signal))]
    [KnownType(typeof(ScriptingInstanceCreatedResponse))]
    [KnownType(typeof(IndicatorSeriesUpdatedResponse))]
    [KnownType(typeof(SeriesForUpdate))]
    [KnownType(typeof(SeriesValue))]
    [KnownType(typeof(ScriptingMessageResponse))]
    [KnownType(typeof(ScriptingExitResponse))]
    [KnownType(typeof(ScriptingInstanceUnloadedResponse))]
    [KnownType(typeof(ScriptingDataRemoveResponse))]
    [KnownType(typeof(SignalActionResponse))]
    [KnownType(typeof(BacktestReportMessage))]
    [KnownType(typeof(ScriptingOutput))]
    [KnownType(typeof(ScriptingReportResponse))]

    [KnownType(typeof(GetLastTickResponse))]
    [KnownType(typeof(RegisterUCProcessorResponse))]
    [KnownType(typeof(StartSignalExecutionResponse))]
    [KnownType(typeof(StopSignalExecutionResponse))]
    [KnownType(typeof(NewSingleTickResponse))]
    [KnownType(typeof(NewBarResponse))]
    [KnownType(typeof(GetBarsResponse))]
    [KnownType(typeof(GetPositionsResponse))]
    [KnownType(typeof(GetPortfoliosResponse))]
    [KnownType(typeof(GetAccountsResponse))]
    [KnownType(typeof(GetAvailableSecuritiesResponse))]
    [KnownType(typeof(GetOrderResponse))]
    [KnownType(typeof(GetOrdersResponse))]
    [KnownType(typeof(SetSignalActionResponse))]
    [KnownType(typeof(UpdateSignalStrategyParamsResponse))]
    [KnownType(typeof(StartIndicatorResponse))]
    [KnownType(typeof(RemoveIndicatorResponse))]
    [KnownType(typeof(GetAvailableDataFeedsResponse))]
    [KnownType(typeof(GetAvailableSymbolsResponse))]
    [KnownType(typeof(GetTickResponse))]

    [KnownType(typeof(CreateSimulatedBrokerAccountInfo))]
    [KnownType(typeof(CreateSimulatedBrokerAccountResponse))]
    public class ResponseMessage
    {
        /// <summary>
        /// user/session info to send response
        /// </summary>
        public IUserInfo User { get; set; }
    }
}