/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using Newtonsoft.Json;
using System;

namespace ServerCommonObjects
{
    public static class JsonExtensions
    {
        private static JsonSerializerSettings _serializerConfig = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };

        /// <summary>
        ///     Serialize object to json (Safe extension).
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string ToJson(this object data)
        {
            var result = string.Empty;
            try
            {
                result = JsonConvert.SerializeObject(data, _serializerConfig);
            }
            catch
            {
                // Ignore serialize exception
            }

            return result;
        }

        /// <summary>
        ///     Deserialize from json to T (Safe extension).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static T FromJson<T>(this string data)
        {
            var result = default(T);
            try
            {
                result = JsonConvert.DeserializeObject<T>(data, _serializerConfig);
            }
            catch (Exception)
            {
                // Ignore deserialize exception
            }

            return result;
        }

        /// <summary>
        ///     Deserialize from json to T (Safe exstension).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object FromJson(this string data, Type type)
        {
            var result = default(object);
            try
            {
                result = JsonConvert.DeserializeObject(data, type, _serializerConfig);
            }
            catch (Exception)
            {
                // Ignore deserialize exception
            }

            return result;
        }
    }
}
