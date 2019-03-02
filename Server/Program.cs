/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using System;
using System.Runtime.InteropServices;

namespace Server
{
    internal static class Program
    {
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(HandlerRoutine handler, bool add);
        private static Server _server;

        public delegate bool HandlerRoutine();

        private static void Main()
        {
            try
            {
                _server = new Server();
                _server.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            SetConsoleCtrlHandler(ConsoleCtrlCheck, true);
            Console.ReadKey();
        }
        
        private static bool ConsoleCtrlCheck()
        {
            _server?.Stop();
            return true;
        }
    }
}
