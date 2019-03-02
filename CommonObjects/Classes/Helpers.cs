/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Threading;

namespace CommonObjects.Classes
{
    public static class Helpers
    {
        public static void ProceedThroughQueue<TQueue, TType>(TQueue queue, TType message, Action<TType> method) where TQueue : Queue<TType>
        {
            bool isEmpty;
            lock (queue)
            {
                isEmpty = queue.Count == 0;
                queue.Enqueue(message);
            }

            if (isEmpty)
                ThreadPool.QueueUserWorkItem(action => DequeHelper(queue, method));
        }

        private static void DequeHelper<TQueue, TType>(TQueue queue, Action<TType> action) where TQueue : Queue<TType>
        {
            while (true)
            {
                TType item;

                lock (queue)
                    item = queue.Peek();

                action(item);

                lock (queue)
                {
                    queue.Dequeue();

                    if (queue.Count == 0)
                        return;
                }
            }
        }
    }
}
