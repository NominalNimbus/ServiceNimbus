/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using CommonObjects;
using ServerCommonObjects.Classes;
using System;

namespace ServerCommonObjects
{
    public sealed class DbSignal
    {
        public TradeSignal Signal { get; }
        public string Login { get; }
        public string ShortName { get; }

        public DbSignal(TradeSignal signal, string login, string shortName)
        {
            Signal = signal ?? throw new ArgumentNullException(nameof(signal));
            Login = !string.IsNullOrEmpty(login) ? login : throw new ArgumentNullException(nameof(login));
            ShortName = !string.IsNullOrEmpty(shortName) ? shortName : throw new ArgumentNullException(nameof(shortName));
        }
    }
}
