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

namespace ServerCommonObjects.SQL
{
    public static class DBMaintenance
    {
        private const string DbName = "TradingServer";

        public static string GetSecurityTableName(string symbol, string dataFeed, byte level = 0)
        {
            var normalizedSymbol = symbol.Replace("/", "");

            return level == 0
                ? $"{dataFeed}_{normalizedSymbol}_Ticks"
                : $"{dataFeed}_{normalizedSymbol}_Ticks_L2";
            //: $"{dataFeed}_{normalizedSymbol}_Ticks_L{level}";
        }

        public static void CreateSecurityTickTable(string connection, string symbol, string dataFeed, 
            byte level = 0, bool isArchive = false)
        {
            var tableName = GetSecurityTableName(symbol, dataFeed, level);
            CreateSecurityTickTable(connection, isArchive ? (tableName + "_Archive") : tableName, level);
        }

        public static string ArchiveTickData(string connection, byte level = 0)
        {
            var errors = new List<string>();
            var tables = new List<string>();
            using (var conn = new SqlConnection(connection))
            {
                SqlCommand cmd = null;
                if (level == 0)
                {
                    cmd = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES "
                        + "WHERE TABLE_NAME LIKE '%_Ticks'", conn);
                }
                else
                {
                    cmd = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES "
                        + "WHERE TABLE_NAME LIKE '%_Ticks_L" + level + "'", conn);
                }

                try
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                                tables.Add((string)reader[0]);
                        }
                    }
                }
                catch (Exception e)
                {
                    errors.Add("Failed to retrieve tick table names: " + e.Message);
                }
            }

            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < tables.Count; i++)
            {
                if (!tables[i].Contains("Simulated"))
                    CreateSecurityTickTable(connection, tables[i] + "_Archive", level);
                var error = MoveData(tables[i], tables[i] + "_Archive", connection, 1);
                if (!string.IsNullOrEmpty(error))
                    errors.Add(error);
            }
            watch.Stop();
            Logger.Info($"Archived tick data for {tables.Count} tables in {watch.Elapsed.TotalMinutes:0} minutes");

            return errors.Count > 0 ? string.Join(Environment.NewLine, errors) : null;
        }

        public static string ArchiveMinuteBars(string connection)
        {
            return MoveData("BarsMinute", "BarsMinute_Archive", connection, 1);
        }

        public static string ShrinkDbLog(string connection, byte newSizeInMb)
        {
            if (string.IsNullOrEmpty(connection))
                return "Connection string is empty";

            using (var conn = new SqlConnection(connection))
            {
                try
                {
                    string command = string.Format("ALTER DATABASE {0} SET RECOVERY SIMPLE; "
                        + "DBCC SHRINKFILE ({0}_Log, {1}); ALTER DATABASE {0} SET RECOVERY FULL;",
                        DbName, newSizeInMb);
                    using (var cmd = new SqlCommand(command, conn))
                    {
                        conn.Open();
                        cmd.CommandTimeout = 5 * 60;  //5 minutes
                        cmd.ExecuteNonQuery();
                        return null;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to shrink DB logs: " + e.Message);
                    return e.Message;
                }
                finally
                {
                    if (conn.State != System.Data.ConnectionState.Closed)
                        conn.Close();
                }
            }
        }

        private static void CreateSecurityTickTable(string connection, string tableName, byte level = 0)
        {
            using (var conn = new SqlConnection(connection))
            {
                var cmd = new SqlCommand("SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @name", conn);
                cmd.Parameters.AddWithValue("name", tableName);

                try
                {
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    if (result == null)  //no such table yet
                    {
                        if (level == 0)
                        {
                            cmd.CommandText = "CREATE TABLE [dbo].[" + tableName
                                + "]([Timestamp] [datetime2](7) NOT NULL, "
                                + "[Ask] [decimal](18, 8) NOT NULL, [Bid] [decimal](18, 8) NOT NULL, "
                                + "[AskSize] [int] NOT NULL, [BidSize] [int] NOT NULL, "
                                + "CONSTRAINT [PK_" + tableName + "] PRIMARY KEY CLUSTERED ([Timestamp] ASC))";
                        }
                        else
                        {
                            cmd.CommandText = "CREATE TABLE [dbo].[" + tableName
                                + "]([Timestamp] [datetime2](7) NOT NULL, [Level] [tinyint] NOT NULL, "
                                + "[Ask] [decimal](18, 8) NOT NULL, [Bid] [decimal](18, 8) NOT NULL, "
                                + "[AskSize] [int] NOT NULL, [BidSize] [int] NOT NULL, "
                                + "CONSTRAINT [PK_" + tableName + "] PRIMARY KEY CLUSTERED ([Timestamp] ASC, [Level] ASC))";
                        }
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    Logger.Error($"Failed to create '{tableName}' security table", e);
                }
            }
        }

        private static string MoveData(string fromTable, string toTable, string connection, byte intervalInMonths)
        {
            if (string.IsNullOrWhiteSpace(connection))
                return "Connection string is empty";

            using (var conn = new SqlConnection(connection))
            {
                const int TIMEOUT = 60 * 60;  //1 hour
                int rowsAffected = 0;
                if (intervalInMonths < 1)
                    intervalInMonths = 1;

                if (!fromTable.Contains("Simulated"))  //do not copy simulated ticks
                {
                    try
                    {
                        var command = string.Format("INSERT INTO [{1}] "
                            + "SELECT * FROM [{0}] "
                            + "WHERE [Timestamp] < CONVERT(Date, DATEADD(MONTH, -{2}, GETDATE()))",
                            fromTable, toTable, intervalInMonths);
                        using (var cmd = new SqlCommand(command, conn))
                        {
                            conn.Open();
                            cmd.CommandTimeout = TIMEOUT;
                            rowsAffected = cmd.ExecuteNonQuery();
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"Failed to archive data from {fromTable} table: {e.Message}");
                        if (conn.State != System.Data.ConnectionState.Closed)
                            conn.Close();
                        return e.Message;
                    }
                }

                if (rowsAffected > 0)
                {
                    try
                    {
                        var command = $"DELETE FROM [{fromTable}] " +
                                      $"WHERE [Timestamp] < CONVERT(Date, DATEADD(MONTH, -{intervalInMonths}, GETDATE()))";
                        using (var cmd = new SqlCommand(command, conn))
                        {
                            if (conn.State != System.Data.ConnectionState.Open)
                                conn.Open();
                            cmd.CommandTimeout = TIMEOUT;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"Failed to cleanup data in {fromTable} table: {e.Message}");
                        return e.Message;
                    }
                    finally
                    {
                        if (conn.State != System.Data.ConnectionState.Closed)
                            conn.Close();
                    }
                }

                if (conn.State != System.Data.ConnectionState.Closed)
                    conn.Close();
            }

            return null;
        }
    }
}
