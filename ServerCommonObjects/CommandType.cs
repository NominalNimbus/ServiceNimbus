/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

namespace ServerCommonObjects
{
    public static class CommandType
    {
        public const string Login = "Login";
        public const string Logout = "Logout";
        public const string ProcessRequest = "ProcessRequest";

        public static string GetCommandType(RequestMessage request)
        {
            switch (request)
            {
                case LoginRequest _:
                    return Login;
                case LogoutRequest _:
                    return Logout;
                default:
                    return ProcessRequest;
            }
        }
    }
}
