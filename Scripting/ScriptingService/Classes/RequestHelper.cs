/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ServerCommonObjects;
using ScriptingService.TradingService;

namespace ScriptingService.Classes
{
    public static class RequestHelper<T>
    {
        private static readonly ConcurrentDictionary<long, TaskCompletionSource<T>> Requests = new ConcurrentDictionary<long, TaskCompletionSource<T>>();

        public static async Task<T> ProceedRequest(long id, IWCFConnection service, RequestMessage message)
        {
            var result = default(T);
            var taskResult = new TaskCompletionSource<T>();

            if (!Requests.TryAdd(id, taskResult))
                return result;

            Send(service, message);

            try
            {
                result = await taskResult.Task;
            }
            catch (Exception ex)
            {
                Logger.Error("Connector.GetPortfolios -> ", ex);
            }

            return result;
        }

        public static void ProceedResponse(long id, T response)
        {
            if (id < 0)
                return;

            if (Requests.TryRemove(id, out var taskSource))
                taskSource.TrySetResult(response);
        }

        private static void Send(IWCFConnection service, RequestMessage requestMessage)
        {
            try
            {
                service.MessageIn(requestMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sending request error: {ex.Message}");
            }
        }
    }
}
