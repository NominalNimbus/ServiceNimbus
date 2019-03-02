/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using CommonObjects.Enums;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCommonObjects.SQL
{
    public class DBSimulatedSymbols
    {
        private string _connection;

        public DBSimulatedSymbols(string connection)
        {
            _connection = connection;
        }

        public IEnumerable<SimulatedSymbol> GetSymbols()
        {
            var symbols = new List<SimulatedSymbol>();
            using (var conn = new SqlConnection(_connection))
            {
                var cmd = new SqlCommand("SELECT * FROM SimulatedSymbols", conn);

                try
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                symbols.Add(new SimulatedSymbol(reader.GetInt32(0), reader.GetString(1))
                                {
                                    StartPrice = reader.GetDecimal(2),
                                    Currency = reader.GetString(3),
                                    Margin = reader.GetDecimal(4),
                                    CommissionType = (ComisionType)reader.GetInt32(5),
                                    CommissionValue = reader.GetDecimal(6),
                                    ContractSize = reader.GetDecimal(7)
                                });
                            }
                        }
                    }
                }
                catch (Exception e)
                {

                }
            }
            return symbols;
        }
    }
}
