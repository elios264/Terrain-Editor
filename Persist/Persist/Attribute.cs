using System;
using System.Globalization;

namespace PersistDotNet.Persist
{
    public class Attribute
    {
        public string Name { get; }
        public string Value { get; }

        public Attribute(string name, IConvertible value)
        {
            Name = name;
            Value = value.ToString(CultureInfo.InvariantCulture);
        }
    }
}