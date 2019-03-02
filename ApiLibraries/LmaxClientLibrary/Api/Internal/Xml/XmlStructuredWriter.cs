/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Com.Lmax.Api.Internal.Xml
{
    public class XmlStructuredWriter : IStructuredWriter
    {
        private const int DefaultSize = 4096;
        private const string Left = "<";
        private const string Right = ">";
        private const string LeftClose = "</";
        private const string RightClose = "/>";
    
        private readonly byte[] _defaultData = new byte[DefaultSize];

        private byte[] _currentData;
        private int _length;

        public XmlStructuredWriter()
        {
            _currentData = _defaultData;
        }

        public IStructuredWriter StartElement(string name)
        {
            WriteOpenTag(name);
            return this;
        }

        public IStructuredWriter ValueOrEmpty(string name, string value)
        {
            if (null != value)
            {
                WriteTag(name, value);
            }
            else
            {
                WriteEmptyTag(name);
            }
        
            return this;
        }

        public IStructuredWriter ValueOrNone(string name, string value)
        {
            if (value != null)
            {
                WriteTag(name, value);
            }

            return this;
        }
    
        public IStructuredWriter ValueUTF8(string name, string value)
        {
            if (null != value)
            {
                WriteOpenTag(name);
                WriteBytes(Encoding.UTF8.GetBytes(Escape(value)));
                WriteCloseTag(name);
            }
            else
            {
                WriteEmptyTag(name);
            }
        
            return this;
        }
    
        public IStructuredWriter ValueOrEmpty(string name, long? value)
        {           
            if (value != null)
            {
                WriteTag(name, Convert.ToString(value, NumberFormatInfo.InvariantInfo));
            }
            else
            {
                WriteEmptyTag(name);
            }

            return this;
        }

        public IStructuredWriter ValueOrNone(string name, long? value)
        {
            if (value != null)
            {
                WriteTag(name, Convert.ToString(value, NumberFormatInfo.InvariantInfo));
            }            

            return this;
        }

        public IStructuredWriter ValueOrEmpty(String name, decimal? value)
        {
            if (value != null)
            {
                WriteTag(name, Convert.ToString(value, NumberFormatInfo.InvariantInfo));
            }
            else
            {
                WriteEmptyTag(name);
            }
        
            return this;
        }

        public IStructuredWriter ValueOrNone(String name, decimal? value)
        {
            if (value != null)
            {
                WriteTag(name, Convert.ToString(value, NumberFormatInfo.InvariantInfo));
            }
            

            return this;
        }

        public IStructuredWriter ValueOrEmpty(String name, bool value)
        {
            WriteTag(name, value ? "true" : "false");
            return this;
        }

        public IStructuredWriter WriteEmptyTag(string name)
        {
            WriteString(Left);
            WriteString(name);
            WriteString(RightClose);
        
            return this;
        }

        public IStructuredWriter EndElement(string name)
        {
            WriteCloseTag(name);
            return this;
        }

        public void Reset()
        {
            _length = 0;
            _currentData = _defaultData;
        }

        private void WriteString(string value)
        {
            int requiredLength = value.Length + _length;
        
            while (requiredLength > _currentData.Length)
            {
                IncreaseBufferSize();
            }
        
            for (int i = 0; i < value.Length; i++)
            {
                _currentData[_length++] = (byte) value[i];
            }
        }

        private void WriteBytes(byte[] value)
        {
            int valueLength = value.Length;
            while (valueLength + _length > _currentData.Length)
            {
                IncreaseBufferSize();
            }
        
            Array.Copy(value, 0, _currentData, _length, valueLength);
            _length += valueLength;
        }

        private void IncreaseBufferSize()
        {
            byte[] oldData = _currentData;
            _currentData = new byte[oldData.Length << 1];
            Array.Copy(oldData, _currentData, _length);
        }

        private void WriteTag(string name, string value)
        {
            WriteOpenTag(name);
            WriteString(value);
            WriteCloseTag(name);
        }

        private void WriteCloseTag(string name)
        {
            WriteString(LeftClose);
            WriteString(name);
            WriteString(Right);
        }

        private void WriteOpenTag(string name)
        {
            WriteString(Left);
            WriteString(name);
            WriteString(Right);
        }

        private static string Escape(string value)
        {
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
        }

        // ===============
        // Handling Output
        // ===============

        public void WriteTo(Stream output) 
        {
            output.Write(_currentData, 0, _length);
        }
    
        public override string ToString()
        {              
            MemoryStream stream = new MemoryStream();
            WriteTo(stream);
            return Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int) stream.Length);
        }        
    }    
}