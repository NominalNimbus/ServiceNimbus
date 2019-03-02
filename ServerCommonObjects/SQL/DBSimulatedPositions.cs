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
    public class DBSimulatedPositions
    {
        private readonly string _connectionString;
        private readonly string _tableName;

        public DBSimulatedPositions(string connection, string table)
        {
            _connectionString = connection;
            _tableName = table;
        }

        public List<Position> GetPositions(string userName, string account, string broker, bool includeClosed = false)
        {
            if (IsAnyNullOrWhiteSpace(new[] { userName, account, broker }))
                return new List<Position>(0);

            string command = $"SELECT [Symbol], [Quantity], [Price], [Profit] FROM [dbo].[{_tableName}]"
                + " WHERE [UserName] = @userName AND [Account] = @account AND [BrokerName] = @broker";

            var positions = new List<Position>();
            using (var connection = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(command, connection))
            {
                cmd.Parameters.AddWithValue("userName", userName);
                cmd.Parameters.AddWithValue("account", account);
                cmd.Parameters.AddWithValue("broker", broker);
                connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows)
                        return positions;

                    while (reader.Read())
                    {
                        try
                        {
                            var qty = (decimal)reader["Quantity"];
                            if (qty != 0m || includeClosed)
                            {
                                positions.Add(new Position
                                {
                                    AccountId = account,
                                    BrokerName = broker,
                                    Symbol = (string)reader["Symbol"],
                                    PositionSide = qty < 0 ? Side.Sell : Side.Buy,
                                    Price = (decimal)reader["Price"],
                                    Profit = (decimal)reader["Profit"],
                                    Quantity = qty
                                });
                            }
                        }
                        catch(Exception e)
                        {
                            Logger.Error("Failed to parse position", e);
                        }
                    }
                }
            }

            return positions;
        }

        public void SavePosition(Position position, bool deleteIfEmpty = false)
        {
            if (position == null || IsAnyNullOrWhiteSpace(new[] { position.AccountId, position.BrokerName, position.Symbol }))
                return;

            if (position.Quantity == 0 && deleteIfEmpty)
            {
                DeletePosition(position.UserName, position.AccountId, position.BrokerName, position.Symbol);
                return;
            }

            var command = $"UPDATE [dbo].[{_tableName}]"
                + " SET [Quantity] = @qty, [Price] = @price, [Profit] = @profit"
                + " WHERE [Symbol] = @symbol AND [UserName] = @userName AND [Account] = @account AND [BrokerName] = @broker"
                + $" IF @@ROWCOUNT = 0 INSERT INTO [dbo].[{_tableName}]"
                + " ([Symbol], [UserName], [Account], [BrokerName], [Created], [Quantity], [Price], [Profit])"
                + " VALUES (@symbol, @userName, @account, @broker, @timestamp, @qty, @price, @profit)";

            using (var connection = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(command, connection))
            {
                cmd.Parameters.AddWithValue("symbol", position.Symbol);
                cmd.Parameters.AddWithValue("userName", position.UserName);
                cmd.Parameters.AddWithValue("account", position.AccountId);
                cmd.Parameters.AddWithValue("broker", position.BrokerName);
                cmd.Parameters.AddWithValue("timestamp", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("qty", position.Quantity);
                cmd.Parameters.AddWithValue("price", Math.Round(position.Price, 5));
                cmd.Parameters.AddWithValue("profit", Math.Round(position.Profit, 5));

                try
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        cmd.Transaction = transaction;
                        cmd.ExecuteNonQuery();
                        transaction.Commit();
                    }
                }
                catch (Exception e)
                {
                    Logger.Error($"Failed to save {position.BrokerName} position for {position.Symbol}", e);
                }
            }
        }

        private void DeletePosition(string userName, string account, string broker, string symbol)
        {
            if (IsAnyNullOrWhiteSpace(new[] { account, broker, symbol }))
                return;

            string command = $"DELETE FROM [dbo].[{_tableName}]"
                + " WHERE [Symbol] = @symbol AND [UserName] = @userName AND [Account] = @account AND [BrokerName] = @broker";

            using (var connection = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(command, connection))
            {
                cmd.Parameters.AddWithValue("symbol", symbol);
                cmd.Parameters.AddWithValue("userName", userName);
                cmd.Parameters.AddWithValue("account", account);
                cmd.Parameters.AddWithValue("broker", broker);

                try
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        cmd.Transaction = transaction;
                        cmd.ExecuteNonQuery();
                        transaction.Commit();
                    }
                }
                catch (Exception e)
                {
                    Logger.Error($"Failed to delete {broker} position for {symbol}", e);
                }
            }
        }

        private static bool IsAnyNullOrWhiteSpace(string[] strings)
        {
            if (strings == null || strings.Length == 0)
                return false;

            for (int i = 0; i < strings.Length; i++)
            {
                if (String.IsNullOrWhiteSpace(strings[i]))
                    return true;
            }

            return false;
        }
    }
}
