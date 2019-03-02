/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using CommonObjects.Enums;

namespace ServerCommonObjects.SQL
{
    public class SimulatedSymbol
    {
        public int Id { get; set; }
        public string Symbol { get; set; }
        public decimal StartPrice { get; set; }
        public string Currency { get; set; }
        public decimal Margin { get; set; }
        public ComisionType CommissionType { get; set; }
        public decimal CommissionValue { get; set; }
        public decimal ContractSize { get; set; }

        public SimulatedSymbol(int id, string symbol)
        {
            Id = id;
            Symbol = symbol;
        }
    }
}
