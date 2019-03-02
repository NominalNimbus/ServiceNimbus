/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Windows.Media;

namespace CommonObjects
{
    [DataContract]
    [Serializable]
    public abstract class ScriptingParameterBase : ICloneable
    {
        [DataMember]
        public int ID { get; private set; }
        [DataMember]
        public string Category { get; set; }
        [DataMember]
        public string Name { get; set; }
        [IgnoreDataMember]
        public abstract string ValueAsString { get; }

        protected ScriptingParameterBase(string name, string category, int id)
        {
            Name = name;
            Category = category;
            ID = id;
        }

        public virtual object Clone()
        {
            return MemberwiseClone();
        }
    }

    [DataContract]
    [Serializable]
    public class IntParam : ScriptingParameterBase
    {
        [DataMember]
        public int Value { get; set; }
        [IgnoreDataMember]
        public override string ValueAsString => Value.ToString();
        [DataMember]
        public int MinValue { get; set; }
        [DataMember]
        public int MaxValue { get; set; }
        [DataMember]
        public int StartValue { get; set; }
        [DataMember]
        public int StopValue { get; set; }
        [DataMember]
        public int Step { get; set; }

        public IntParam(string name, string category, int ID) : base(name, category, ID)
        {
            MinValue = Int32.MinValue;
            MaxValue = Int32.MaxValue;
        }
    }

    [DataContract]
    [Serializable]
    public class DoubleParam : ScriptingParameterBase
    {
        [DataMember]
        public double Value { get; set; }
        [IgnoreDataMember]
        public override string ValueAsString => Value.ToString();
        [DataMember]
        public double MinValue { get; set; }
        [DataMember]
        public double MaxValue { get; set; }
        [DataMember]
        public double StartValue { get; set; }
        [DataMember]
        public double StopValue { get; set; }
        [DataMember]
        public double Step { get; set; }

        public DoubleParam(string name, string category, int ID) : base(name, category, ID)
        {
            MinValue = Double.MinValue;
            MaxValue = Double.MaxValue;
        }
    }

    [DataContract]
    [Serializable]
    public class StringParam : ScriptingParameterBase
    {
        [DataMember]
        public string Value { get; set; }
        [IgnoreDataMember]
        public override string ValueAsString => Value;
        [DataMember]
        public List<string> AllowedValues { get; set; }

        public StringParam(string name, string category, int ID) : base(name, category, ID)
        {
            Value = string.Empty;
            AllowedValues = new List<string>();
        }

        public override object Clone()
        {
            var res = MemberwiseClone() as StringParam;
            res.AllowedValues = new List<string>(AllowedValues);
            return res;
        }
    }

    [DataContract]
    [Serializable]
    public class BoolParam : ScriptingParameterBase
    {
        [DataMember]
        public bool Value { get; set; }

        [IgnoreDataMember]
        public override string ValueAsString => Value.ToString();

        public BoolParam(string name, string category, int ID)
            : base(name, category, ID)
        {
            
        }

        public override object Clone()
        {
            var res = MemberwiseClone() as BoolParam;
            return res;
        }
    }


    [DataContract]
    [Serializable]
    public class SeriesParam : ScriptingParameterBase
    {
        [DataMember]
        public ColorValue Color { get; set; }
        [DataMember]
        public int Thickness { get; set; }
        [IgnoreDataMember]
        public override string ValueAsString => $"{Thickness},{Color.ToString()}";

        public SeriesParam(string name, string category, int ID) : base(name, category, ID)
        {
            Color = Colors.Red;
            Thickness = 2;
        }

        public override object Clone()
        {
            var res = MemberwiseClone() as SeriesParam;
            res.Color = new ColorValue { ColorString = Color.ColorString };
            return res;
        }
    }

    [Serializable]
    [DataContract]
    public class ColorValue
    {
        [DataMember]
        public string ColorString { get; set; }

        public static explicit operator Color(ColorValue val)
        {
            if (String.IsNullOrEmpty(val.ColorString))
                return Colors.Red;

            try
            {
                var color = ColorConverter.ConvertFromString(val.ColorString);
                return (Color)color;
            }
            catch (FormatException)
            {
                return Colors.Red;
            }
        }

        public static implicit operator ColorValue(Color color)
        {
            return new ColorValue { ColorString = color.ToString() };
        }

        public override string ToString()
        {
            return ColorString;
        }
    }
}
