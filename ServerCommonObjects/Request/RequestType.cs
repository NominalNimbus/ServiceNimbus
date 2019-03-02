/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;

namespace ServerCommonObjects
{
    public sealed class RequestType
    {
        public const string NONE = "None";
        public const string LOGIN = "Login";
        public const string LOGOUT = "Logout";
        public const string DATAFEED_LIST = "DataFeedList";
        public const string TRADING_INFO = "TradingInfo";
        public const string BROKERS_AVAILABLE_SECURITIES = "BrokersAvailableSecurities";
        public const string BROKERS_LOGIN = "BrokersLogin";
        public const string BROKERS_LOGOUT = "BrokerLogout";
        public const string SUBSCRIBE = "Subscribe";
        public const string UNSUBSCRIBE = "Unsubscribe";
        public const string HISTORY = "History";
        public const string ORDERS_LIST = "OrdersList";
        public const string PLACE_ORDER = "PlaceOrder";
        public const string CANCEL_ORDER = "CancelOrder";
        public const string MODIFY_ORDER = "ModifyOrder";
        public const string PORTFOLIO_ACTION = "PortfolioAction";
        
        public const string Scripting = "Scripting";
        public const string SIGNAL_DATA = "SignalData";
        public const string BACKTEST_RESULTS = "BacktestResults";
        public const string CREATE_USER_INDICATOR = "CreateUserIndicator";
        public const string CREATE_USER_SIGNAL = "CreateUserSignal";
        public const string REMOVE_SCRIPTING_INSTANCE = "RemoveScriptingInstance";
        public const string SAVE_SCRIPTING_DATA = "SaveScriptingData";
        public const string SIGNAL_ACTION = "SignalAction";
        public const string UPDATE_STRATEGY_PARAMS = "UpdateStrategyParams";
        public const string ADD_USER_FILES = "AddUserFiles";
        public const string REMOVE_USER_FILES = "RemoveUserFiles";
        public const string SCRIPTING_REPORT = "ScriptingReport";
        public const string STARTED_SIGNAL_EXECUTION = "StartedSignalExecution";
        
        public const string SCRIPTING_OUTPUT = "ScriptingOutput";
        public const string GET_BARS = "GetBars";
        public const string GET_POSITIONS = "GetPositions";
        public const string CANCEL_ACCOUNT_ORDER = "CancelAccountOrder";
        public const string CLOSE_POSITION = "ClosePosition";
        public const string MODIFY_ACCOUNT_ORDER = "ModifyAccountOrder";
        public const string PLACE_ACCOUNT_ORDER = "PlaceAccountOrder";
        public const string GET_PORTFOLIOS = "GetPortfolios";
        public const string GET_ACCOUNTS = "GetAccounts";
        public const string GET_AVAILABLE_SECURITIES = "GetAvailableSecurities";
        public const string GET_ORDER = "GetOrder";
        public const string GET_ORDERS = "GetOrders";
        public const string NEW_BACKTEST_RESULTS = "NewBacktestResults";
        public const string BACKTEST_RESPOT = "BacktestReport";
        public const string SIGNAL_ACTION_SETTED = "SignalActionSetted";
        public const string HEART_BEAT = "Heartbeat";
        public const string GET_TICK = "GetTick";
        public const string GET_AVAILABLE_DATAFEEDS = "GetAvailableDataFeeds";
        public const string GET_AVAILABLE_SYMBOLS = "GetAvailableSymbols";
        
        public const string SCRIPTING_ALERT = "ScriptingAlert";
        public const string SERIES_UPDATED = "SeriesUpdated";
        public const string INDICATOR_STARTED = "IndicatorStartedRequest";

        public static Type GetRequestType(string type)
        {
            switch (type)
            {
                case LOGIN:
                    return typeof(LoginRequest);
                case LOGOUT:
                    return typeof(LogoutRequest);
                case DATAFEED_LIST:
                    return typeof(GetDataFeedListRequest);
                case TRADING_INFO:
                    return typeof(TradingInfoRequest);
                case BROKERS_AVAILABLE_SECURITIES:
                    return typeof(BrokersAvailableSecuritiesRequest);
                case BROKERS_LOGIN:
                    return typeof(BrokersLoginRequest);
                case BROKERS_LOGOUT:
                    return typeof(BrokerLogoutRequest);
                case SUBSCRIBE:
                    return typeof(SubscribeRequest);
                case UNSUBSCRIBE:
                    return typeof(UnsubscribeRequest);
                case HISTORY:
                    return typeof(HistoryDataRequest);
                case ORDERS_LIST:
                    return typeof(OrdersListRequest);
                case PLACE_ORDER:
                    return typeof(PlaceOrderRequest);
                case CANCEL_ORDER:
                    return typeof(CancelOrderRequest);
                case MODIFY_ORDER:
                    return typeof(ModifyOrderRequest);
                case PORTFOLIO_ACTION:
                    return typeof(PortfolioActionRequest);
                case Scripting:
                    return typeof(ScriptingRequest);
                case SIGNAL_DATA:
                    return typeof(SignalDataRequest);
                case BACKTEST_RESULTS:
                    return typeof(BacktestResultsRequest);
                case CREATE_USER_INDICATOR:
                    return typeof(CreateUserIndicatorRequest);
                case CREATE_USER_SIGNAL:
                    return typeof(CreateUserSignalRequest);
                case REMOVE_SCRIPTING_INSTANCE:
                    return typeof(RemoveScriptingInstanceRequest);
                case SAVE_SCRIPTING_DATA:
                    return typeof(SaveScriptingDataRequest);
                case SIGNAL_ACTION:
                    return typeof(SignalActionRequest);
                case UPDATE_STRATEGY_PARAMS:
                    return typeof(UpdateStrategyParamsRequest);
                case ADD_USER_FILES:
                    return typeof(AddUserFilesRequest);
                case REMOVE_USER_FILES:
                    return typeof(RemoveUserFilesRequest);
                case SCRIPTING_REPORT:
                    return typeof(ScriptingReportRequest);
                case STARTED_SIGNAL_EXECUTION:
                    return typeof(StartedSignalExecutionRequest);
                case SCRIPTING_OUTPUT:
                    return typeof(ScriptingOutput);
                case GET_BARS:
                    return typeof(GetBarsRequest);
                case GET_POSITIONS:
                    return typeof(GetPositionsRequest);
                case CANCEL_ACCOUNT_ORDER:
                    return typeof(CancelAccountOrderRequest);
                case CLOSE_POSITION:
                    return typeof(ClosePositionRequest);
                case MODIFY_ACCOUNT_ORDER:
                    return typeof(ModifyAccountOrderRequest);
                case PLACE_ACCOUNT_ORDER:
                    return typeof(PlaceAccountOrderRequest);
                case GET_PORTFOLIOS:
                    return typeof(GetPortfoliosRequest);
                case GET_ACCOUNTS:
                    return typeof(GetAccountsRequest);
                case GET_AVAILABLE_SECURITIES:
                    return typeof(GetAvailableSecuritiesRequest);
                case GET_ORDER:
                    return typeof(GetOrderRequest);
                case GET_ORDERS:
                    return typeof(GetOrdersRequest);
                case NEW_BACKTEST_RESULTS:
                    return typeof(NewBacktestResultsRequest);
                case BACKTEST_RESPOT:
                    return typeof(BacktestReportRequest);
                case SIGNAL_ACTION_SETTED:
                    return typeof(SignalActionSettedRequest);
                case HEART_BEAT:
                    return typeof(HeartbeatRequest);
                case GET_TICK:
                    return typeof(GetTickRequest);
                case GET_AVAILABLE_DATAFEEDS:
                    return typeof(GetAvailableDataFeedsRequest);
                case GET_AVAILABLE_SYMBOLS:
                    return typeof(GetAvailableSymbolsRequest);
                case SCRIPTING_ALERT:
                    return typeof(ScriptingAlertRequest);
                case SERIES_UPDATED:
                    return typeof(SeriesUpdatedRequest);
                case INDICATOR_STARTED:
                    return typeof(IndicatorStartedRequest);
                default:
                    return null;
            }
        }
    }
}
