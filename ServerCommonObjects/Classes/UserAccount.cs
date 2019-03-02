/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using CommonObjects;

namespace ServerCommonObjects.Classes
{
    public class UserAccount
    {
        public IUserInfo UserInfo { get; private set; }
        public AccountInfo AccountInfo { get; private set; }

        public UserAccount(IUserInfo userInfo, AccountInfo accountInfo)
        {
            UserInfo = userInfo;
            AccountInfo = accountInfo;
        }
    }
}