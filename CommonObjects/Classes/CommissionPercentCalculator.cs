/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonObjects
{
    public class CommissionPercentCalculator : ICommisionCalculator
    {
        public decimal Percent { get; set; }

        public CommissionPercentCalculator(decimal percent) =>
            Percent = percent * 0.01M;

        public decimal CalculateCommission(Order order, decimal fillCash, decimal fillQuantity) =>
            fillCash * Percent;
    }
}
