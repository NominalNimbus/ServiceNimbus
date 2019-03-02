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

namespace ServerCommonObjects.SQL
{
    public class DBPortfolios
    {
        private string _connectionString;
        
        public void Start(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void Stop()
        {
            _connectionString = String.Empty;
        }

        #region Portfolios

        public int AddPortfolio(Portfolio portfolio)
        {
            using (var aConnection = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("INSERT INTO [dbo].[Portfolios]  ([User], [Name], [Currency])" 
                    + "VALUES(@user, @name, @currency) SELECT CONVERT(int, SCOPE_IDENTITY())", aConnection);

                cmd.Parameters.AddWithValue("user", portfolio.User);
                cmd.Parameters.AddWithValue("name", portfolio.Name);
                cmd.Parameters.AddWithValue("currency", portfolio.Currency);

                SqlTransaction transaction = null;

                int returnValue;
                try
                {
                    aConnection.Open();
                    transaction = aConnection.BeginTransaction();
                    cmd.Transaction = transaction;
                    returnValue = (int)cmd.ExecuteScalar();

                    if (returnValue <= 0)
                    {
                        transaction.Rollback();
                        return -1;
                    }

                    portfolio.ID = returnValue;

                    //add accounts
                    foreach (var account in portfolio.Accounts ?? new List<PortfolioAccount>(0))
                    {
                        account.ID = AddPortfolioAccount(portfolio.ID, account, aConnection, transaction);
                        if (account.ID < 1)
                        {
                            transaction.Rollback();
                            portfolio.ID = -1;
                            return -1;
                        }
                    }

                    //add strategies
                    foreach (var strategy in portfolio.Strategies ?? new List<PortfolioStrategy>(0))
                    {
                        strategy.ID = AddPortfolioStrategy(portfolio.ID, strategy, aConnection, transaction);
                        if (strategy.ID < 1)
                        {
                            transaction.Rollback();
                            portfolio.ID = -1;
                            return -1;
                        }
                    }

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to add portfolio", e);

                    transaction?.Rollback();

                    return -1;
                }

                return returnValue;
            }
        }

        public int GetPortfolioCount(IUserInfo info, int id = 0)
        {
            using (var aConnection = new SqlConnection(_connectionString))
            {
                var query = "SELECT COUNT(*) FROM [dbo].[Portfolios] WHERE [User] = @login";
                if (id > 0)
                    query += " AND [ID] = @id";
                var cmd = new SqlCommand(query, aConnection);
                cmd.Parameters.AddWithValue("login", info.Login);
                if (id > 0)
                    cmd.Parameters.AddWithValue("id", id);

                try
                {
                    aConnection.Open();
                    return (int)cmd.ExecuteScalar();
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to retrieve portfolio count", e);
                }
            }

            return 0;
        }

        public List<Portfolio> GetPortfolios(IUserInfo info, int id = 0)
        {
            var result = new List<Portfolio>();

            using (var aConnection = new SqlConnection(_connectionString))
            {
                var query1 = "SELECT * FROM [dbo].[Portfolios] WHERE [User] = @login";
                var query2 = query1 + " AND [ID] = @id";

                SqlCommand cmd = null;
                if (id > 0)
                {
                    cmd = new SqlCommand(query2, aConnection);
                    cmd.Parameters.AddWithValue("id", id);
                }
                else
                {
                    cmd = new SqlCommand(query1, aConnection);
                }

                cmd.Parameters.AddWithValue("login", info.Login);
                SqlTransaction transaction = null;
                try
                {
                    aConnection.Open();
                    transaction = aConnection.BeginTransaction();
                    cmd.Transaction = transaction;

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                            return result;

                        while (reader.Read())
                        {
                            try
                            {
                                result.Add(new Portfolio
                                {
                                    ID = (int)reader["ID"],
                                    Name = (string)reader["Name"],
                                    Currency = (string)reader["Currency"],
                                    User = (string)reader["User"]
                                });
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Failed to parse Portfolio.", ex);
                            }
                        }
                    }

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to load portfolios", e);
                    transaction?.Rollback();

                    return result;
                }
            }

            try
            {
                foreach (var portfolio in result)
                    portfolio.Accounts = GetPortfolioAccounts(portfolio.ID);
            }
            catch (Exception ex) { Logger.Error("Failed to load portfolios accounts", ex); }

            try
            {
                foreach (var portfolio in result)
                    portfolio.Strategies = GetPortfolioStrategies(portfolio.ID);
            }
            catch (Exception ex) { Logger.Error("Failed to load portfolios strategies", ex); }

            return result;
        }

        private bool UpdatePortfolio(Portfolio portfolio, SqlConnection aConnection, SqlTransaction transaction)
        {
            var cmd = new SqlCommand("UPDATE [dbo].[Portfolios] SET [Name] = @name, [Currency] = @currency WHERE [ID] = @id", aConnection);
            cmd.Parameters.AddWithValue("currency", portfolio.Currency);
            cmd.Parameters.AddWithValue("name", portfolio.Name);
            cmd.Parameters.AddWithValue("id", portfolio.ID);

            try
            {
                cmd.Transaction = transaction;
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Logger.Error("Failed to update portfolio", e);
                return false;
            }

            return true;
        }

        public bool UpdatePortfolio(Portfolio portfolio)
        {
            if (portfolio == null || portfolio.ID < 1)
                return false;

            ////short version (will change the portfolio ID!)
            //if (RemovePortfolio(portfolio))
            //    return AddPortfolio(portfolio) > 0;
            //return false;

            //long version
            SqlTransaction transaction = null;
            try
            {
                using (var aConnection = new SqlConnection(_connectionString))
                {
                    aConnection.Open();
                    transaction = aConnection.BeginTransaction();

                    if (UpdatePortfolio(portfolio, aConnection, transaction))
                    {
                        //synchronize accounts
                        var IDs = portfolio.Accounts.Select(i => i.ID).ToList();
                        if (IDs.Count == 0)
                            IDs.Add(0);
                        var cmd = new SqlCommand(
                            "DELETE FROM [dbo].[PortfolioAccounts] " +
                            $"WHERE [Portfolio_ID] = {portfolio.ID} AND [ID] NOT IN ({String.Join(",", IDs)})", aConnection);
                        try
                        {
                            cmd.Transaction = transaction;
                            cmd.ExecuteNonQuery();
                        }
                        catch { }

                        foreach (var item in portfolio.Accounts)
                        {
                            if (item.ID < 1)
                                AddPortfolioAccount(portfolio.ID, item, aConnection, transaction);
                            else
                                UpdatePortfolioAccount(item, aConnection, transaction);
                        }

                        //synchronize strategies
                        IDs = portfolio.Strategies.Select(i => i.ID).ToList();
                        if (IDs.Count == 0)
                            IDs.Add(0);
                        cmd = new SqlCommand(
                            "DELETE FROM [dbo].[PortfolioStrategies] " +
                            $"WHERE [Portfolio_ID] = {portfolio.ID} AND [ID] NOT IN ({String.Join(",", IDs)})", aConnection);
                        try
                        {
                            cmd.Transaction = transaction;
                            cmd.ExecuteNonQuery();
                        }
                        catch { }

                        foreach (var item in portfolio.Strategies)
                        {
                            if (item.ID < 1)
                                AddPortfolioStrategy(portfolio.ID, item, aConnection, transaction);
                            else
                                UpdatePortfolioStrategy(item, aConnection, transaction);
                        }

                    }

                    transaction.Commit();
                }
            }
            catch(Exception e)
            {
                Logger.Error("Failed to update portfolio", e);
                transaction?.Rollback();

                return false;
            }

            return true;
        }

        public bool RemovePortfolio(Portfolio portfolio)
        {
            using (var aConnection = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("DELETE FROM [dbo].[Portfolios] WHERE [ID] = @id", aConnection);

                cmd.Parameters.AddWithValue("id", portfolio.ID);
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
                    Logger.Error("Failed to remove portfolio", e);

                    transaction?.Rollback();

                    return false;
                }

                return true;
            }
        }

        #endregion

        #region Portfolio Accounts

        private int AddPortfolioAccount(int portfolioID, PortfolioAccount portfolioAcct, SqlConnection aConnection, SqlTransaction transaction)
        {
            var cmd = new SqlCommand("INSERT INTO [dbo].[PortfolioAccounts] ([Portfolio_ID], [Name], [BrokerName], [UserName], [Account])" +
                "VALUES(@portfolio_id, @name, @brokerName, @userName, @account) SELECT CONVERT(int, SCOPE_IDENTITY())", aConnection);
            cmd.Parameters.AddWithValue("portfolio_id", portfolioID);
            cmd.Parameters.AddWithValue("name", portfolioAcct.Name);
            cmd.Parameters.AddWithValue("brokerName", portfolioAcct.BrokerName);
            cmd.Parameters.AddWithValue("userName", portfolioAcct.UserName);
            cmd.Parameters.AddWithValue("account", portfolioAcct.Account);

            try
            {
                cmd.Transaction = transaction;
                return (int)cmd.ExecuteScalar();
            }
            catch (Exception e)
            {
                Logger.Error("Failed to insert portfolio account", e);
                return -1;
            }
        }

        private List<PortfolioAccount> GetPortfolioAccounts(int portfolioId)
        {
            var result = new List<PortfolioAccount>();

            using (var aConnection = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT * FROM [dbo].[PortfolioAccounts] WHERE [Portfolio_ID] = @id", aConnection);
                cmd.Parameters.AddWithValue("id", portfolioId);

                SqlTransaction transaction = null;

                try
                {
                    aConnection.Open();
                    transaction = aConnection.BeginTransaction();
                    cmd.Transaction = transaction;

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                            return result;

                        while (reader.Read())
                        {
                            try
                            {
                                result.Add(new PortfolioAccount
                                {
                                    ID = (int)reader["ID"],
                                    Name = (string)reader["Name"],
                                    BrokerName = (string)reader["BrokerName"],
                                    UserName = (string)reader["UserName"],
                                    Account = (string)reader["Account"],
                                });
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Failed to read portfolio account", ex);
                            }
                        }
                    }

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to load portfolio accounts", e);

                    if (transaction != null) transaction.Rollback();

                    return result;
                }
            }
            return result;
        }
        
        private bool UpdatePortfolioAccount(PortfolioAccount portfolioItem, SqlConnection aConnection, SqlTransaction transaction)
        {
            var cmd = new SqlCommand("UPDATE [dbo].[PortfolioAccounts] SET [Name] = @name WHERE [ID] = @id", aConnection);
            cmd.Parameters.AddWithValue("name", portfolioItem.Name);
            cmd.Parameters.AddWithValue("id", portfolioItem.ID);

            try
            {
                cmd.Transaction = transaction;
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Logger.Error("Failed to update portfolio account", e);
                return false;
            }
            return true;
        }

        private bool RemovePortfolioAccount(int portfolioAcctID, SqlConnection aConnection, SqlTransaction transaction)
        {
            var cmd = new SqlCommand("DELETE FROM [dbo].[PortfolioAccounts] WHERE [ID] = @id", aConnection);
            cmd.Parameters.AddWithValue("id", portfolioAcctID);

            try
            {
                cmd.Transaction = transaction;
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Logger.Error("Failed to update portfolio account", e);
                return false;
            }

            return true;
        }

        #endregion

        #region Portfolio Strategies

        private int AddPortfolioStrategy(int portfolioID, PortfolioStrategy strategy, SqlConnection connection, SqlTransaction transaction)
        {
            var cmd = new SqlCommand("INSERT INTO [dbo].[PortfolioStrategies] ([Portfolio_ID], [StrategyName], " 
                + "[StrategySignals], [StrategyDataFeeds], [ExposedBalance]) "
                + "VALUES (@portfolioId, @strategyName, @signals, @datafeeds, @exposedBalance) SELECT CONVERT(int, SCOPE_IDENTITY())", connection);
            
            cmd.Parameters.AddWithValue("portfolioId", portfolioID);
            cmd.Parameters.AddWithValue("strategyName", strategy.Name);
            cmd.Parameters.AddWithValue("signals", strategy.Signals == null ? null : String.Join(",", strategy.Signals));
            cmd.Parameters.AddWithValue("datafeeds", strategy.DataFeeds == null ? null : String.Join(",", strategy.DataFeeds));
            cmd.Parameters.AddWithValue("exposedBalance", strategy.ExposedBalance);

            try
            {
                cmd.Transaction = transaction;
                return (int)cmd.ExecuteScalar();
            }
            catch (Exception e)
            {
                Logger.Error("Failed to insert portfolio strategy", e);
                return -1;
            }
        }

        private List<PortfolioStrategy> GetPortfolioStrategies(int portfolioId)
        {
            var result = new List<PortfolioStrategy>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT * FROM [dbo].[PortfolioStrategies] WHERE [Portfolio_ID] = @id", connection);
                cmd.Parameters.AddWithValue("id", portfolioId);

                SqlTransaction transaction = null;

                try
                {
                    connection.Open();
                    transaction = connection.BeginTransaction();
                    cmd.Transaction = transaction;

                    string s = null;
                    PortfolioStrategy strategy = null;
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                            return result;

                        while (reader.Read())
                        {
                            try
                            {
                                strategy = new PortfolioStrategy((int)reader["ID"], (string)reader["StrategyName"]);
                                s = (string)reader["StrategySignals"];
                                if (!String.IsNullOrEmpty(s))
                                    strategy.Signals = s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                s = (string)reader["StrategyDataFeeds"];
                                if (!String.IsNullOrEmpty(s))
                                    strategy.DataFeeds = s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                strategy.ExposedBalance = (decimal)reader["ExposedBalance"];
                                result.Add(strategy);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Failed to read portfolio strategy", ex);
                            }
                        }
                    }

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to load portfolio strategies", e);
                    if (transaction != null)
                        transaction.Rollback();
                }
            }
            return result;
        }

        private bool UpdatePortfolioStrategy(PortfolioStrategy strategy, SqlConnection connection, SqlTransaction transaction)
        {
            var cmd = new SqlCommand("UPDATE [dbo].[PortfolioStrategies] " 
                + "SET [StrategyName] = @name, [StrategySignals] = @signals, " 
                + "[StrategyDataFeeds] = @feeds, [ExposedBalance] = @exposedBalance WHERE [ID] = @id", connection);
            cmd.Parameters.AddWithValue("id", strategy.ID);
            cmd.Parameters.AddWithValue("name", strategy.Name);
            cmd.Parameters.AddWithValue("signals", strategy.Signals == null ? null : String.Join(",", strategy.Signals));
            cmd.Parameters.AddWithValue("feeds", strategy.DataFeeds == null ? null : String.Join(",", strategy.DataFeeds));
            cmd.Parameters.AddWithValue("exposedBalance", strategy.ExposedBalance);

            try
            {
                cmd.Transaction = transaction;
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Logger.Error("Failed to update portfolio strategy", e);
                return false;
            }
            return true;
        }

        #endregion
    }
}
