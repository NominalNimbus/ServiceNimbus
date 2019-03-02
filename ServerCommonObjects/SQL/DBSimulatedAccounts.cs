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
    public class DBSimulatedAccounts
    {
        private readonly string _connectionString;
        private readonly string _tableName;
        private readonly bool _isMarginAccount;
        
        public DBSimulatedAccounts(string connection, string table, bool isMarginAccount)
        {
            _connectionString = connection;
            _tableName = table;
            _isMarginAccount = isMarginAccount;
        }

        public bool VerifyAccount(string login, string account)
        {
            var command = $"SELECT TOP 1 [UserName] FROM [dbo].[{_tableName}]"
                                   + " WHERE [UserName] = @user AND [AccountName] = @account";

            using (var connection = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(command, connection))
            {
                cmd.Parameters.AddWithValue("user", login);
                cmd.Parameters.AddWithValue("account", account);
                connection.Open();
                var result = cmd.ExecuteScalar();
                return result != null && login.Equals(result as string, StringComparison.OrdinalIgnoreCase);
            }
        }

        public AccountInfo GetAccountDetails(string login, string account)
        {
            var command = "SELECT TOP 1 [AccountName], [UserName], [Currency], [Balance], " 
                            + (_isMarginAccount ? "[Margin], " :string.Empty) + 
                            $"[Profit] FROM [dbo].[{_tableName}] WHERE [UserName] = @user AND [AccountName] = @account";

            using (var connection = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(command, connection))
            {
                cmd.Parameters.AddWithValue("user", login);
                cmd.Parameters.AddWithValue("account", account);
                connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows || !reader.Read())
                        return null;

                    return new AccountInfo
                    {
                        ID = (string)reader["AccountName"],
                        UserName = (string)reader["UserName"],
                        Currency = (string)reader["Currency"],
                        Balance = (decimal)reader["Balance"],
                        Margin = _isMarginAccount ? (decimal)reader["Margin"] : 0m,
                        Profit = (decimal)reader["Profit"]
                    };
                }
            }
        }

        public void SaveAccountDetails(AccountInfo account)
        {
            var command = $"UPDATE [dbo].[{_tableName}] SET [Balance] = @balance, " +
                        (_isMarginAccount ? "[Margin] = @margin, " : string.Empty) +
                        "[Profit] = @profit  WHERE [UserName] = @user AND [AccountName] = @account";

            using (var connection = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(command, connection))
            {
                cmd.Parameters.AddWithValue("balance", Math.Round(account.Balance, 5));
                if (_isMarginAccount)
                {
                    cmd.Parameters.AddWithValue("margin", Math.Round(account.Margin, 5));
                }
                cmd.Parameters.AddWithValue("profit", Math.Round(account.Profit, 5));
                cmd.Parameters.AddWithValue("user", account.UserName);
                cmd.Parameters.AddWithValue("account", account.Account);

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
                    Logger.Error($"Failed to save {account?.UserName} simulated account details.", e);
                }
            }
        }

        public static List<string> GetAccounts(string connectionString, string table, string userName)
        {
            var accounts = new List<string>();
            var command = $"SELECT [AccountName] FROM [dbo].[{table}] WHERE [UserName] = @user";
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    using (var cmd = new SqlCommand(command, connection))
                    {
                        cmd.Parameters.AddWithValue("user", userName);
                        connection.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.HasRows)
                                return accounts;

                            while (reader.Read())
                            {
                                accounts.Add((string)reader["AccountName"]);
                            }
                        }
                    }
                }
                catch { }                
            }
            return accounts;
        }

        public static string CreateSimulatedAccount(string connectionString, string table, string userName, CreateSimulatedBrokerAccountInfo account, bool isMarginAccount)
        {
            var command = $"Insert into [dbo].[{table}] Values (@user, @account, @curency, @balance, { (isMarginAccount ? "@margin, " : string.Empty) }@profit)";
            using (var connection = new SqlConnection(connectionString))
            {
                using (var cmd = new SqlCommand(command, connection))
                {
                    cmd.Parameters.AddWithValue("user", userName);
                    cmd.Parameters.AddWithValue("account", account.AccountName);
                    cmd.Parameters.AddWithValue("curency", account.Currency);
                    cmd.Parameters.AddWithValue("balance", account.Ballance);
                    if (isMarginAccount)
                    {
                        cmd.Parameters.AddWithValue("margin", 0);
                    }
                    cmd.Parameters.AddWithValue("profit", 0);
                    try
                    {
                        connection.Open();
                        cmd.ExecuteNonQuery();
                    }
                    catch(Exception ex)
                    {
                        return ex.Message;
                    }
                }
            }
            return string.Empty;
        }
    }
}
