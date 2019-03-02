/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using CommonObjects;

namespace ServerCommonObjects.SQL
{
    public class DBSignals
    {
        private string _connectionString;

        public void Start(string connectionString)
        {
            _connectionString = connectionString;
        }

        public bool AddSignal(TradeSignal tradeSignal, string userLogin, string signalName, bool canRetry = false)
        {
            if (tradeSignal == null)
                return false; //TODO find out

            var res = false;
            using (var aConnection = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("INSERT INTO [dbo].[Signals] ([SignalID],[UserLogin],[SignalName],[Date])"
                                         + "VALUES(@signalID, @userLogin, @signalName, @date)", aConnection);

                cmd.Parameters.AddWithValue("signalID", tradeSignal.Id);
                cmd.Parameters.AddWithValue("userLogin", userLogin);
                cmd.Parameters.AddWithValue("signalName", signalName);
                cmd.Parameters.AddWithValue("date", tradeSignal.Time);

                SqlTransaction transaction = null;

                try
                {
                    aConnection.Open();
                    transaction = aConnection.BeginTransaction();
                    cmd.Transaction = transaction;

                    res = cmd.ExecuteNonQuery() > 0;

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    if (canRetry && e is SqlException)
                    {
                        var code = ((SqlException) e).Number;
                        if (code == 1205 || code == -2) //deadlock or timeout
                        {
                            Logger.Error(
                                $"Failed to add {tradeSignal.Instrument.Symbol} signal to DB (will retry): {e.Message}");
                            AddSignal(tradeSignal, userLogin, signalName);
                            return res;
                        }
                    }

                    Logger.Error($"Failed to add {tradeSignal.Instrument.Symbol} order to DB", e);

                    transaction?.Rollback();
                }
            }

            return res;
        }

        public List<ReportField> GetReport(string userName, string strategyName, DateTime startDate, DateTime endDate)
        {
            var result = new List<ReportField>();
            using (var aConnection = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand(
                    "SELECT o.Symbol, o.Type, o.Status, o.TIF, o.ExecutedQuantity, s.SignalName, s.Date as SignalDate,"
                    + " s.DataBaseEntry as SignalDBDate, o.Date, o.FilledDate, o.DataBaseEntry as OrderDBDate"
                    + " FROM[dbo].[Orders] o"
                    + " LEFT JOIN[dbo].[Signals] s ON o.SignalID = s.SignalID"
                    + " WHERE s.UserLogin = @userLogin AND s.SignalName = @signalName  AND s.Date BETWEEN @startDate AND @endDate", aConnection);

                cmd.Parameters.AddWithValue("userLogin", userName);
                cmd.Parameters.AddWithValue("signalName", strategyName);
                cmd.Parameters.AddWithValue("startDate", startDate);
                cmd.Parameters.AddWithValue("endDate", endDate);

                SqlTransaction transaction = null;
                try
                {
                    aConnection.Open();
                    transaction =
                        aConnection.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted, "Orders Select");
                    cmd.Transaction = transaction;

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                            return result;

                        while (reader.Read())
                        {
                            try
                            {
                                var qty = (decimal) reader["ExecutedQuantity"];

                                var field = new ReportField
                                {
                                    Symbol = (string) reader["Symbol"],
                                    TradeType = DBConverters.ParseOrderType((string) reader["Type"]),
                                    Status = DBConverters.ParseOrderStatus((string) reader["Status"]),
                                    TimeInForce = DBConverters.ParseTif((string) reader["TIF"]),
                                    Quantity = qty,
                                    Side = qty > 0 ? Side.Buy : Side.Sell,
                                    SignalName = (string) reader["SignalName"],
                                    SignalGeneratedDateTime = (DateTime) reader["SignalDate"],
                                    OrderFilledDate = (DateTime) reader["FilledDate"],
                                    OrderGeneratedDate = (DateTime) reader["Date"],
                                    DBOrderEntryDate = (DateTime)reader["OrderDBDate"],
                                    DBSignalEntryDate = (DateTime)reader["SignalDBDate"]
                                };

                                field.CalculateDiff();
                                result.Add(field);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Failed to parse report field", ex);
                            }
                        }
                    }

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to load report", e);
                    transaction?.Rollback();
                    return result;
                }
            }

            return result;
        }
    }
}