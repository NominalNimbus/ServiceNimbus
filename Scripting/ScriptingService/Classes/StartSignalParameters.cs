/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.Collections.Generic;
using CommonObjects;
using ServerCommonObjects;

namespace ScriptingService.Classes
{
    public class StartSignalParameters
    {
        public string Id { get; set; }
        public string Login { get; set; }
        public List<PortfolioAccount> AccountInfos { get; set; }
        public SignalInitParams SignalInitParams { get; set; }
        public Dictionary<string, byte[]> Files { get; set; }
    }
}
