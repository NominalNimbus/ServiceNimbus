/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using CommonObjects;

namespace ServerCommonObjects.Classes
{
    public class OrderEventArgs : EventArgs
    {
        public Order Order { get; private set; }

        public OrderEventArgs(Order order)
        {
            Order = order;
        }
    }

    public class OrderRejectedEventArgs : OrderEventArgs
    {
        public string Message { get; private set; }

        public OrderRejectedEventArgs(Order order, string message = "") : base(order)
        {
            Message = message;
        }
    }
    
    public class UserOrderRejectedEventArgs : OrderRejectedEventArgs
    {
        public IUserInfo UserInfo { get; private set; }

        public UserOrderRejectedEventArgs(IUserInfo userInfo, Order order, string message = "")
            : base(order, message)
        {
            UserInfo = userInfo;
        }
    }
}
