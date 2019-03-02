/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;

namespace ServerCommonObjects
{
    public static class DictionaryExtensions
    {
        public static bool TryGetValue<T, TKey>(this Dictionary<TKey, object> dictionary, TKey key, out T value)
        {
            var isSuccess = true;
            value = default(T);

            try
            {
                if (dictionary.TryGetValue(key, out var dictionaryValue))
                    value = (T)Convert.ChangeType(dictionaryValue, typeof(T));
            }
            catch (Exception)
            {
                isSuccess = false;
            }

            return isSuccess;
        }
    }
}
