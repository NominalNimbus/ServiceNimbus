/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Data.SqlClient;

namespace ServerCommonObjects.ServerClasses
{
    public class Authentication
    {
        private string ConnectionString { get; }

        public Authentication(string connectionString) => 
            ConnectionString = connectionString;

        public bool Login(string login, string password)
        {
            const string command = "SELECT TOP 1 [Login] FROM [Users] "
                                   + "WHERE [Login] = @login AND [Password] = @password AND [Active] = 1";

            using (var connection = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(command, connection))
            {
                cmd.Parameters.AddWithValue("login", login);
                cmd.Parameters.AddWithValue("password", password);
                try
                {
                    connection.Open();
                    var res = cmd.ExecuteScalar();
                    return !string.IsNullOrEmpty(res as string);
                }
                catch (Exception e)
                {
                    Logger.Info($"Failed to log {login} in", e);
                    return false;
                }
            }
        }
    }
}
