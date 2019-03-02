/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Com.Lmax.Api.Internal
{
    public class DefaultHandler : Handler
    {
        public static readonly NumberFormatInfo NumberFormat = CultureInfo.InvariantCulture.NumberFormat;
        readonly IDictionary<string, Handler> _handlers = new Dictionary<string, Handler>();

        public DefaultHandler() : this("res")
        {
        }

        public DefaultHandler(string elementName) : base(elementName)
        {
            AddHandler(STATUS);
            AddHandler(MESSAGE);
        }

        public override bool IsOk
        {
            get { return OK == GetStringValue(STATUS); }
        }

        public String Status
        {
            get { return GetStringValue(STATUS); }
        }

        public override string Message
        {
            get { return GetStringValue(MESSAGE); }
        }

        public void AddHandler(string tag)
        {
            AddHandler(new Handler(tag));
        }

        public void AddHandler(Handler handler)
        {
            _handlers[handler.ElementName] = handler;
        }

        public override void Reset(string element)
        {
            Handler handler;
            if (_handlers.TryGetValue(element, out handler))
            {
                handler.Reset(element);
            }
        }

        public void ResetAll()
        {
            foreach (KeyValuePair<string, Handler> entry in _handlers)
            {
                entry.Value.Reset(entry.Key);
            }
        }

        protected string GetStringValue(string tag)
        {
            Handler handler;
            if (_handlers.TryGetValue(tag, out handler))
            {
                return handler.Content;
            }

            return null;
        }

        public bool TryGetValue(string tag, out decimal dec)
        {
            Handler handler;
            if (_handlers.TryGetValue(tag, out handler))
            {
                string content = handler.Content;
                if (content.Length > 0)
                {
                    
                    
                    dec = Convert.ToDecimal(content, NumberFormat);
                    
                    return true;
                }
            }

            dec = 0;
            return false;
        }

        public bool TryGetValue(string tag, out long longValue)
        {
            Handler handler;
            if (_handlers.TryGetValue(tag, out handler))
            {
                string content = handler.Content;
                if (content.Length > 0)
                {
                    longValue = Convert.ToInt64(content);
                    return true;
                }
            }

            longValue = 0;
            return false;
        }

        public bool TryGetValue(string tag, out bool boolValue)
        {
            Handler handler;
            if (_handlers.TryGetValue(tag, out handler))
            {
                string content = handler.Content;
                if (content.Length > 0)
                {
                    boolValue = Convert.ToBoolean(content);
                    return true;
                }
            }

            boolValue = false;
            return false;
        }

        public bool TryGetValue(string tag, out string stringValue)
        {
            Handler handler;
            if (_handlers.TryGetValue(tag, out handler))
            {
                string content = handler.Content;
                if (content.Length > 0)
                {
                    stringValue = content;
                    return true;
                }
            }
            stringValue = null;
            return false;
        }

        public int GetIntValue(string tag, int defaultValue)
        {
            long value;
            return (int) (TryGetValue(tag, out value) ? value : defaultValue);
        }

        public long GetLongValue(string tag, long defaultValue)
        {
            long value;
            return TryGetValue(tag, out value) ? value : defaultValue;
        }

        public decimal GetDecimalValue(string tag, decimal defaultValue)
        {
            decimal value;
            return TryGetValue(tag, out value) ? value : defaultValue;
        }

        public decimal? GetDecimalValue(string tag)
        {
            decimal value;
            if (TryGetValue(tag, out value))
            {
                return value;
            }
            
            return null;
        }

        public override Handler GetHandler(string qName)
        {
            Handler handler;
            if (_handlers.TryGetValue(qName, out handler))
            {
                return handler;
            }

            return this;
        }
    }
}

