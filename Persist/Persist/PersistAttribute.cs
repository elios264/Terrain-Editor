using System;

namespace PersistDotNet.Persist
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class PersistAttribute : System.Attribute
    {
        /// <summary>
        /// name of the member, if not set the property or field name will be used
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// For complex types treats it as a reference only storing its id to be serialized elsewhere in the tree.
        /// For lists and dictionaries does the same thing but just for their values.
        /// </summary>
        public bool IsReference { get; set; }
        /// <summary>
        /// Opcional name for items on a dictionary or list
        /// </summary>
        public string ChildName { get; set; }
        /// <summary>
        /// Opctional name for key on dictionaries only
        /// </summary>
        public string KeyName { get; set; }
        /// <summary>
        /// Opctional name for value on dictionaries only
        /// </summary>
        public string ValueName { get; set; }

        /// <summary>
        /// Ignores the specified field since public properties are serialized by default
        /// </summary>
        public bool Ignore { get; set; }

        public PersistAttribute()
        {
        }
        public PersistAttribute(string name)
        {
            Name = name;
        }
    }
}