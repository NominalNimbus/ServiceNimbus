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
using System.Linq;
using CommonObjects;
using ServerCommonObjects.Enums;

namespace ServerCommonObjects.SQL
{
    public class DBOrders
    {
        private string _connectionString;

        public void Start(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void Stop()
        {
            _connectionString = string.Empty;
        }

        public IEnumerable<Order> GetOrderHistory(AccountInfo info, int count, int skip)
        {
            if (skip < 0 || count < 1)
                return new List<Order>(0);

            var result = new List<Order>();
            using (var aConnection = new SqlConnection(_connectionString))
            {
                //select N latest records of each symbol
                var cmd = new SqlCommand("WITH TOPRECORDS AS ( "
                    + "SELECT s.[Symbol], h.[Quantity], h.[ExecutedQuantity], h.[Status], h.[OrderId], h.[Price], "
                    + "h.[Type], h.[TIF], h.[Date], h.[BrokerID], h.[OpeningQuantity], h.[ClosingQuantity], h.[Origin], ROW_NUMBER() "
                    + "OVER(PARTITION BY h.[Symbol], h.[DataFeed] ORDER BY [Date] DESC) AS RowNumber FROM [Orders] AS h "
                        + "INNER JOIN[dbo].[Securities] AS s ON s.Symbol = h.Symbol AND s.DataFeed = h.DataFeed "
                        + "WHERE h.[UserName] = @userName AND h.[AccountId] = @account AND h.[BrokerName] = @broker) "
                        + "SELECT * FROM TOPRECORDS WHERE RowNumber > @start AND RowNumber <= @end", aConnection);
                cmd.Parameters.AddWithValue("start", skip);
                cmd.Parameters.AddWithValue("end", skip + count);
                cmd.Parameters.AddWithValue("userName", info.UserName);
                cmd.Parameters.AddWithValue("account", info.ID);
                cmd.Parameters.AddWithValue("broker", info.BrokerName);

                SqlTransaction transaction = null;
                try
                {
                    aConnection.Open();
                    transaction = aConnection.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted, "Orders Select");
                    cmd.Transaction = transaction;

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                            return result;

                        while (reader.Read())
                        {
                            try
                            {
                                var qty = (decimal)reader["Quantity"];
                                var execQty = (decimal)reader["ExecutedQuantity"];
                                var status = DBConverters.ParseOrderStatus((string)reader["Status"]);
                                var order = new Order((string)reader["OrderId"], (string)reader["Symbol"])
                                {
                                    BrokerID = (string)reader["BrokerID"],
                                    AvgFillPrice = status == Status.Filled ? (decimal)reader["Price"] : 0,
                                    Price = status == Status.Cancelled ? (decimal)reader["Price"] : 0,
                                    OrderSide = qty > 0 ? Side.Buy : Side.Sell,
                                    OrderType = DBConverters.ParseOrderType((string)reader["Type"]),
                                    BrokerName = info.BrokerName,
                                    AccountId = info.ID,
                                    TimeInForce = DBConverters.ParseTif((string)reader["TIF"]),
                                    OpenDate = (DateTime)reader["Date"],
                                    CancelledQuantity = status == Status.Cancelled ? Math.Abs(execQty) : 0,
                                    FilledQuantity = status == Status.Filled ? Math.Abs(execQty) : 0,
                                    OpeningQty = status == Status.Filled ? (decimal)reader["OpeningQuantity"] : 0,
                                    ClosingQty = status == Status.Filled ? (decimal)reader["ClosingQuantity"] : 0,
                                    Origin = reader["Origin"] as string
                                };

                                if (status == Status.Cancelled)
                                    order.CancelledQuantity = Math.Abs(qty);
                                else
                                    order.FilledQuantity = Math.Abs(qty);

                                result.Add(order);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Failed to parse historical order", ex);
                            }
                        }
                    }

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to load order history", e);
                    transaction?.Rollback();
                    return result;
                }
            }
            return result;
        }

        public void AddHistoricalOrder(Order order, AccountInfo info, Status status, bool canRetry = false)
        {
            using (var aConnection = new SqlConnection(_connectionString))
            {
                ////simple insert
                var cmd = new SqlCommand("INSERT INTO [dbo].[Orders] ([OrderId],[BrokerID],[SignalID],[BrokerName],[DataFeed],[UserName],[AccountId],[Symbol],[Price],[Quantity],[ExecutedQuantity],[Status],"
                                         + "[Type],[TIF],[Date],[PlacedDate],[FilledDate],[OpeningQuantity],[ClosingQuantity],[Origin]) "
                                         + "VALUES (@orderId, @brokerId, @signalId, @brokerName, @dataFeed, @userName, @accountId, @symbol, @price, @qty, @execQty, @status, "
                                         + "@type, @tif, @openDate, @placedDate, @filledDate, @openingQty, @closingQty, @origin);", aConnection);

                //upsert via MERGE
                //var cmd1 = new SqlCommand("MERGE [dbo].[Orders] AS Target "
                //                         + "USING (VALUES(@orderId, @brokerId, @signalId, @brokerName, @accountId, @symbol, @datafeed, @price, @qty, @execQty, @status, "
                //                         + "@type, @tif, @openDate, @placedDate, @filledDate, @openingQty, @closingQty, @origin)) "
                //                         + "AS item([OrderId],[BrokerID],[SignalID],[BrokerName],[AccountId],[Symbol],[DataFeed],[Price],[Quantity],[ExecutedQuantity],"
                //                         + "[Status],[Type],[TIF],[Date],[PlacedDate],[FilledDate],[OpeningQuantity],[ClosingQuantity],[Origin]) "
                //                         + "ON (Target.OrderId = item.OrderId AND Target.BrokerID = item.BrokerID AND Target.SignalID = item.SignalID AND Target.AccountId = item.AccountId) "
                //                         + "WHEN MATCHED THEN "
                //                         + "UPDATE SET Target.Price = item.Price, Target.Quantity = item.Quantity, Target.ExecutedQuantity = item.ExecutedQuantity, "
                //                         + "Target.Status = item.Status, Target.[Type] = item.[Type], Target.TIF = item.TIF, Target.[Date] = item.[Date], "
                //                         + "Target.[PlacedDate] = item.[PlacedDate], Target.[FilledDate] = item.[FilledDate], "
                //                         + "Target.OpeningQuantity = item.OpeningQuantity, Target.ClosingQuantity = item.ClosingQuantity,Target.Origin = item.Origin "
                //                         + "WHEN NOT MATCHED BY TARGET THEN "
                //                         + "INSERT ([OrderId],[BrokerID],[SignalID],[BrokerName],[AccountId],[Symbol],[DataFeed],[Price],[Quantity],[ExecutedQuantity],[Status],"
                //                         + "[Type],[TIF],[Date],[PlacedDate],[FilledDate],[OpeningQuantity],[ClosingQuantity],[Origin]) "
                //                         + "VALUES (item.OrderId,item.BrokerID,item.SignalID,item.BrokerName,item.AccountId,item.Symbol,item.DataFeed,item.Price,"
                //                         + "item.Quantity,item.ExecutedQuantity,item.Status,item.[Type],item.TIF,item.[Date],item.[PlacedDate],item.[FilledDate],"
                //                         + "item.OpeningQuantity,item.ClosingQuantity,item.Origin);", aConnection);

                cmd.Parameters.AddWithValue("orderId", order.UserID);
                cmd.Parameters.AddWithValue("brokerId", order.BrokerID);
                cmd.Parameters.AddWithValue("brokerName", info.BrokerName);
                cmd.Parameters.AddWithValue("datafeed", order.DataFeedName);
                cmd.Parameters.AddWithValue("userName", info.UserName);
                cmd.Parameters.AddWithValue("accountId", info.ID);
                cmd.Parameters.AddWithValue("symbol", order.Symbol);
                cmd.Parameters.AddWithValue("qty", order.OrderSide == Side.Buy ? order.Quantity : -order.Quantity);
                cmd.Parameters.AddWithValue("openingQty", order.OpeningQty);
                cmd.Parameters.AddWithValue("closingQty", order.ClosingQty);
                cmd.Parameters.AddWithValue("status", DBConverters.OrderStatusToString(status));
                cmd.Parameters.AddWithValue("type", DBConverters.OrderTypeToString(order.OrderType));
                cmd.Parameters.AddWithValue("tif", DBConverters.TifToString(order.TimeInForce));
                cmd.Parameters.AddWithValue("openDate", order.OpenDate);
                cmd.Parameters.AddWithValue("placedDate", order.PlacedDate);
                cmd.Parameters.AddWithValue("filledDate", order.FilledDate);

                if (order.SignalID == null)
                    cmd.Parameters.AddWithValue("signalId", DBNull.Value);
                else
                    cmd.Parameters.AddWithValue("signalId", order.SignalID);

                if (string.IsNullOrWhiteSpace(order.Origin))
                    cmd.Parameters.AddWithValue("origin", DBNull.Value);
                else
                    cmd.Parameters.AddWithValue("origin", order.Origin);

                if (status == Status.Filled)
                {
                    cmd.Parameters.AddWithValue("execQty",
                        order.OrderSide == Side.Buy ? order.FilledQuantity : -order.FilledQuantity);
                    cmd.Parameters.AddWithValue("price", order.AvgFillPrice);
                }
                else if (status == Status.Cancelled)
                {
                    cmd.Parameters.AddWithValue("execQty",
                        order.OrderSide == Side.Buy ? order.CancelledQuantity : -order.CancelledQuantity);
                    cmd.Parameters.AddWithValue("price", order.Price);
                }

                SqlTransaction transaction = null;

                try
                {
                    aConnection.Open();
                    transaction = aConnection.BeginTransaction();
                    cmd.Transaction = transaction;
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                }
                catch (Exception e)
                {
                    if (canRetry && e is SqlException)
                    {
                        var code = ((SqlException)e).Number;
                        if (code == 1205 || code == -2)  //deadlock or timeout
                        {
                            Logger.Error($"Failed to add {order.Symbol} order to DB (will retry): {e.Message}");
                            AddHistoricalOrder(order, info, status);
                            return;
                        }
                    }

                    Logger.Error($"Failed to add {order.Symbol} order to DB", e);

                    transaction?.Rollback();
                }
            }
        }

        public bool AddUserActivity(IUserInfo user)
        {
            foreach (var account in user.Accounts.ToList())
            {
                using (var aConnection = new SqlConnection(_connectionString))
                {
                    var cmd = new SqlCommand("INSERT INTO [dbo].[UserActivity] ([User],[Broker],[Account],[Date])" +
                                             "VALUES(@user, @broker, @account, @date)", aConnection);

                    cmd.Parameters.AddWithValue("user", user.Login);
                    cmd.Parameters.AddWithValue("broker", account.BrokerName);
                    cmd.Parameters.AddWithValue("account", account.UserName);
                    cmd.Parameters.AddWithValue("date", DateTime.UtcNow);

                    SqlTransaction transaction = null;

                    try
                    {
                        aConnection.Open();
                        transaction = aConnection.BeginTransaction();
                        cmd.Transaction = transaction;
                        cmd.ExecuteNonQuery();
                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Failed to add [UserActivity] item.", e);
                        transaction?.Rollback();
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
