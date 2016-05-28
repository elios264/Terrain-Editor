using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace PersistDotNet.Persist
{

    public abstract class TreeSerializer : Archive
    {
        private class ParentElement : Element
        {
            private readonly Element m_realElement;

            public override string Name
            {
                get { return m_realElement.Name; }
            }
            public override long Id
            {
                get { return m_realElement.Id; }
                set { m_realElement.Id = value; }
            }

            public override bool IsArray
            {
                get { return m_realElement.IsArray; }
                set { m_realElement.IsArray = value; }
            }

            public override List<Attribute> Attributes
            {
                get { return m_realElement.Attributes; }
            }
            public override List<Element> Elements
            {
                get { return m_realElement.Elements; }
            }

            public ParentElement(Element realElement) : base(string.Empty)
            {
                m_realElement = realElement;
            }
        }

        private Element m_root;
        private readonly Stack<Element> m_context;
        private readonly HashSet<long> m_writeReferences;
        private readonly Dictionary<string, object> m_readReferences;

        private Element Current
        {
            get { return m_context.Peek(); }
        }

        protected TreeSerializer(Type type, Type[] polymorphicTypes) : base(type, polymorphicTypes)
        {
            m_context = new Stack<Element>();
            m_writeReferences = new HashSet<long>();
            m_readReferences = new Dictionary<string, object>();
        }

        protected abstract void WriteElement(Stream target, Element root);
        protected abstract Element ParseElement(Stream source);

        public override void Write(Stream target, string name, object data)
        {
            lock (LockObject)
            {
                WriteMain(name, data);
                ResolveWriteReferences();
                WriteElement(target, m_root);
                m_root = null;
            }
        }
        protected override void BeginWriteObject(string name)
        {
            if (m_context.Count == 0)
            {
                m_root = new Element(name);
                m_context.Push(m_root);
            }
            else if (!string.IsNullOrEmpty(name))
            {
                var element = new Element(name);
                Current.Elements.Add(element);
                m_context.Push(element);
            }
            else
            {
                m_context.Push(new ParentElement(Current));
            }
        }
        protected override void EndWriteObject(long id)
        {
            m_context.Pop().Id = id;
        }
        protected override void WriteReference(string name, long id)
        {
            Current.Attributes.Add(new Attribute(name, id));
            m_writeReferences.Add(id);
        }
        protected override void WriteValue(string name, object data)
        {
            Current.Attributes.Add(new Attribute(name,(IConvertible)data));
        }


        public override object Read(Stream source)
        {
            var firstStep = ParseElement(source);
            var secondStep = new Element(firstStep);

            lock (LockObject)
            {
                m_root = firstStep;

                var result = ReadMain();
                if (m_readReferences.Count > 0) //Resolve if there are pending references
                {
                    m_root = secondStep;

                    ResolveMain(result);

                    m_readReferences.Clear();
                }

                m_root = null;

                return result;
            }
        }
        protected override bool BeginReadObject(string name)
        {
            if (m_context.Count == 0)
            {
                m_context.Push(m_root);
                return true;
            }

            if (string.IsNullOrEmpty(name))
            {
                m_context.Push(new ParentElement(Current));
                return true;
            }

            var cur =  Current.IsArray ? Current.Elements.FirstOrDefault() : Current.Elements.FirstOrDefault(element => element.Name == name);
            if (cur != null)
            {
                Current.Elements.Remove(cur);
                m_context.Push(cur);
                return true;
            }

            return false;
        }
        protected override void EndReadObject(object value)
        {
            var objId = Current.Attributes.FirstOrDefault(attr => attr.Name == AddressKwd)?.Value;

            if (!(Current is ParentElement) &&  value!= null && objId != null)
            {
                m_readReferences.Add(objId,value);
            }

            m_context.Pop();
        }
        protected override object ReadValue(string name, Type type)
        {
            var value = Current.Attributes.FirstOrDefault(attr => attr.Name == name)?.Value;

            if (value != null)
            {
                return typeof (Enum).IsAssignableFrom(type) 
                    ? Enum.Parse(type, value) 
                    : Convert.ChangeType(value, type);
            }

            return null;
        }
        protected override object ReadReference(string name)
        {
            var id = Current.Attributes.FirstOrDefault(attr => attr.Name == name)?.Value;

            object reference;

            if (m_readReferences.TryGetValue(id, out reference))
            {
                return reference;
            }

            throw new SerializationException("unresolved reference " + id);
        }

        protected override int GetObjectChildrenCount(string name)
        {
            return Current.IsArray 
                ? Current.Elements.Count
                : Current.Elements.Count(e => e.Name == name);
        }

        private void ResolveWriteReferences()
        {
            if (m_writeReferences.Count == 0)
                return;

            m_context.Push(m_root);

            while (m_context.Count > 0)
            {
                Element e = m_context.Pop();

                if (e.Id > 0 && m_writeReferences.Contains(e.Id))
                {
                    e.Attributes.Add(new Attribute(AddressKwd, e.Id));
                    m_writeReferences.Remove(e.Id);
                }

                foreach (var element in e.Elements)
                {
                    m_context.Push(element);
                }
            }

            if (m_writeReferences.Count > 0)
            {
                throw new SerializationException("unresolved reference");
            }
        }
    }
}