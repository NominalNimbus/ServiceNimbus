/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using NLog;

namespace ServerCommonObjects
{
    public static class Logger
    {
        private static NLog.Logger _logger;
        private static string _target;

        public static string Target => _target;

        static Logger()
        {
            _target = "TradingServer";
            try { _logger = LogManager.GetLogger(_target); }
            catch { }

            if (_logger == null)
                _logger = LogManager.GetCurrentClassLogger();
        }

        public static void Error(string msg, Exception e = null)
        {
            Log(LogLevel.Error, msg, e);
        }

        public static void Warning(string msg, Exception e = null)
        {
            Log(LogLevel.Warn, msg, e);
        }

        public static void Info(string msg, Exception e = null)
        {
            Log(LogLevel.Info, msg, e);
        }

        private static void Log(LogLevel level, string msg, Exception e = null)
        {
            if (!String.IsNullOrWhiteSpace(msg) && e != null)
                _logger.Log(level, e, msg);
            else if (!String.IsNullOrEmpty(msg))
                _logger.Log(level, msg);
            else if (e != null)
                _logger.Log(level, e);
        }
    }
}
