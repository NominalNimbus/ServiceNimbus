/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;

namespace CommonObjects
{
    public class EventArgs<T> : EventArgs
    {
        public EventArgs(T val)
        {
            Value = val;
        }

        public T Value { get; set; }
    }

    public class EventArgs<T1, T2> : EventArgs
    {
        public EventArgs(T1 val1, T2 val2)
        {
            Value1 = val1;
            Value2 = val2;
        }

        public T1 Value1 { get; private set; }
        public T2 Value2 { get; private set; }
    }
}
