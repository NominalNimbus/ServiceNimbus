/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Media;

namespace CommonObjects
{
    [DataContract]
    [Serializable]
    public class Series
    {
        public Series()
        {
            Values = new List<SeriesValue>();
            Color = Colors.Red;
            Thickness = 1;
            Style = DrawShapeStyle.DRAW_LINE;
            ID = Guid.NewGuid().ToString();
            Name = string.Empty;
            Type = DrawStyle.STYLE_SOLID;
        }

        public Series(string name)
            : this()
        {
            Name = name;
        }
        
        [DataMember]
        public string ID { get; private set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public ColorValue Color { get; set; }

        [DataMember]
        public int Thickness { get; set; }

        [DataMember]
        public DrawStyle Type { get; set; }

        [DataMember]
        public DrawShapeStyle Style { get; set; }

        [DataMember]
        public List<SeriesValue> Values { get; private set; }

        public int Length => Values.Count;

        public void AppendOrUpdate(DateTime date, double value)
        {
            for (int i = Values.Count - 1; i > -1; i--)
            {
                if (Values[i].Date == date)
                {
                    Values[i].Value = value;
                    return;
                }

                if (Values[i].Date < date)
                    break;
            }

            Values.Add(new SeriesValue(date, value));
        }

        public void Shift(int shift, double emptyValue)
        {
            if (shift <= 0) 
                return;

            if (shift >= Values.Count)
                return;

            for (var i = shift; i < Values.Count; i++)
                Values[i].Value = Values[i - shift].Value;

            for (var i = 0; i < shift; i++)
                Values[i].Value = emptyValue;
        }
    }
}
