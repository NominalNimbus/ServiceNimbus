/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;

namespace ServerCommonObjects.Classes
{
    public static class SetValue
    {
        public static string String(string name, string value) =>
            !string.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(name);

        public static int Int(string name, string value) =>
            int.TryParse(value, out var intValue)? intValue : throw new ArgumentNullException(name);
    }
}
