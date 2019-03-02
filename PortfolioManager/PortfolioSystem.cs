/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.Collections.Generic;
using System.Linq;
using CommonObjects;
using ServerCommonObjects;
using ServerCommonObjects.SQL;

namespace PortfolioManager
{
    public class PortfolioSystem
    {
        private readonly DBPortfolios _dbPortfolios;

        public PortfolioSystem()
        {
            _dbPortfolios = new DBPortfolios();
        }

        public void Start(string connectionString)
        {
            _dbPortfolios.Start(connectionString);
        }

        public void Stop()
        {
            _dbPortfolios.Stop();
        }

        public List<Portfolio> GetPortfolios(IUserInfo user)
        {
            return _dbPortfolios.GetPortfolios(user);
        }

        public int AddPortfolio(Portfolio portfolio, string user)
        {
            portfolio.User = user;
            return _dbPortfolios.AddPortfolio(portfolio);
        }

        public bool UpdatePortfolio(IUserInfo user, Portfolio portfolio)
        {
            if (_dbPortfolios.GetPortfolioCount(user, portfolio.ID) == 1)
                return _dbPortfolios.UpdatePortfolio(portfolio);
            else
                return false;
        }

        public bool RemovePortfolio(Portfolio portfolio)
        {
            return _dbPortfolios.RemovePortfolio(portfolio);
        }
    }
}
