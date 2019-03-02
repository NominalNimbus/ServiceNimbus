/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Data;
using CommonObjects;

namespace ServerCommonObjects.SQL
{
    public class SqlBulkBarCopy : IDataReader
    {
        private BarWithInstrument _currentBar;
        private int _currentPos;
        private readonly List<BarWithInstrument> _bars;

        public int Depth { get; }
        public bool IsClosed { get; }
        public int RecordsAffected { get; }
        public int FieldCount { get { return 13; } }

        public SqlBulkBarCopy(List<BarWithInstrument> bars)
        {
            _bars = bars;
        }

        public object GetValue(int i)
        {
            switch (i)
            {
                case 0: return CommonHelper.GetTimeRoundToMinute(_currentBar.Date);
                case 1: return _currentBar.OpenBid;
                case 2: return _currentBar.OpenAsk;
                case 3: return _currentBar.HighBid;
                case 4: return _currentBar.HighAsk;
                case 5: return _currentBar.LowBid;
                case 6: return _currentBar.LowAsk;
                case 7: return _currentBar.CloseBid;
                case 8: return _currentBar.CloseAsk;
                case 9: return _currentBar.VolumeBid;
                case 10: return _currentBar.VolumeAsk;
                case 11: return _currentBar.Symbol;
                case 12: return _currentBar.DataFeed;
                default: return null;
            }
        }

        public bool Read()
        {
            if (_currentPos < _bars.Count)
            {
                _currentBar = _bars[_currentPos];
                _currentPos++;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Dispose()
        {
        }

        #region Unused IDataReader methods
        public string GetName(int i)
        {
            throw new NotImplementedException();
        }

        public string GetDataTypeName(int i)
        {
            throw new NotImplementedException();
        }

        public Type GetFieldType(int i)
        {
            throw new NotImplementedException();
        }

        public int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public int GetOrdinal(string name)
        {
            throw new NotImplementedException();
        }

        public bool GetBoolean(int i)
        {
            throw new NotImplementedException();
        }

        public byte GetByte(int i)
        {
            throw new NotImplementedException();
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public char GetChar(int i)
        {
            throw new NotImplementedException();
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public Guid GetGuid(int i)
        {
            throw new NotImplementedException();
        }

        public short GetInt16(int i)
        {
            throw new NotImplementedException();
        }

        public int GetInt32(int i)
        {
            throw new NotImplementedException();
        }

        public long GetInt64(int i)
        {
            throw new NotImplementedException();
        }

        public float GetFloat(int i)
        {
            throw new NotImplementedException();
        }

        public double GetDouble(int i)
        {
            throw new NotImplementedException();
        }

        public string GetString(int i)
        {
            throw new NotImplementedException();
        }

        public decimal GetDecimal(int i)
        {
            throw new NotImplementedException();
        }

        public DateTime GetDateTime(int i)
        {
            throw new NotImplementedException();
        }

        public IDataReader GetData(int i)
        {
            throw new NotImplementedException();
        }

        public bool IsDBNull(int i)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public DataTable GetSchemaTable()
        {
            throw new NotImplementedException();
        }

        public bool NextResult()
        {
            throw new NotImplementedException();
        }

        object IDataRecord.this[int i]
        {
            get { throw new NotImplementedException(); }
        }

        object IDataRecord.this[string name]
        {
            get { throw new NotImplementedException(); }
        }
        #endregion
    }
}
