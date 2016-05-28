using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace PersistDotNet.Persist
{
    public abstract class Archive
    {
        private enum PersistType
        {
            Complex,
            List,
            Dictionary,
            Convertible,
        }

        private class PersistMember
        {
            public string Name { get; set; }

            public Type Type { get; }
            public Type DeclaringType { get; }
            public PersistType PersistType { get; }

            public bool IsReference { get; set; }

            public PersistMember KeyItemInfo { get; set; }
            public PersistMember ValueItemInfo { get; set; }
            public string ChildName { get; set; }

            public List<PersistMember> Children { get; set; } = new List<PersistMember>();

            public Func<object,object> GetValue { get; }
            public Action<object,object> SetValue { get; }

            public PersistMember(Type type)
            {
                Type = type;
                PersistType = GetPersistType(type);
            }
            public PersistMember(MemberInfo info)
            {
                PropertyInfo prp = info as PropertyInfo;
                FieldInfo fld = info as FieldInfo;

                Type = prp?.PropertyType ?? fld?.FieldType;
                DeclaringType = info.DeclaringType;
                PersistType = GetPersistType(Type);

                GetValue = owner => prp != null ? prp.GetValue(owner) : fld.GetValue(owner);

                if ((prp != null && prp.CanWrite == false) || (fld != null && fld.IsInitOnly))
                    return;

                SetValue = (owner, childValue) =>
                {
                    if (prp != null)
                        prp.SetValue(owner, childValue);
                    else
                        fld.SetValue(owner, childValue);
                };
            }

            public override string ToString()
            {
                return $"Name: {Name} \t Type:{Type}";
            }
            public static Type GetMemberType(MemberInfo info)
            {
                PropertyInfo prp = info as PropertyInfo;
                FieldInfo fld = info as FieldInfo;

                return prp?.PropertyType ?? fld?.FieldType;
            }
        }
        private struct MemberAttrib
        {
            public readonly MemberInfo Member;
            public readonly PersistAttribute Attribute;

            public MemberAttrib(MemberInfo m, PersistAttribute a)
            {
                Member = m;
                Attribute = a;
            }
        }

        public static string ClassKwd = "class";
        public static string ValueKwd = "value";
        public static string KeyKwd = "key";
        public static string ItemKwd = "item";
        public static string AddressKwd = "address";

        private ObjectIDGenerator m_generator;
        private readonly PersistMember m_mainInfo;
        private readonly Type[] m_polymorphicTypes;
        private readonly Dictionary<Type,Type> m_metaTypes;

        protected object LockObject => m_mainInfo;

        //Initialization
        protected Archive(Type mainType, Type[] polymorphicTypes)
        {
            m_mainInfo = new PersistMember(mainType);

            m_polymorphicTypes = polymorphicTypes ?? new Type[0];

            m_metaTypes = Assembly.GetAssembly(mainType)
                                  .GetTypes()
                                  .Where(type => System.Attribute.IsDefined(type,typeof(MetadataTypeAttribute)))
                                  .ToDictionary(type => type.GetCustomAttribute<MetadataTypeAttribute>().MetadataClassType, type => type);

            if (m_mainInfo.PersistType != PersistType.Complex)
            {
                throw new SerializationException("the root of the data to serialize can't implement IConvertible, IList or IDictionary");
            }

            var context = new Stack<PersistMember>(new[] {m_mainInfo});
            var definedMembers = new Dictionary<Type,List<PersistMember>>();

            while (context.Count > 0)
            {
                PersistMember current = context.Pop();

                foreach (var memberInfo in GetElegibleMembers(current.Type))
                {
                    //Create childInfo & add it to its parent
                    PersistMember childInfo = new PersistMember(memberInfo.Member)
                    {
                        Name = memberInfo.Attribute.Name ?? memberInfo.Member.Name,
                        IsReference = memberInfo.Attribute.IsReference
                    };
                    current.Children.Add(childInfo);

                    //Try to get its children or queue the creation of them
                    List<PersistMember> memberChildren;
                    if (definedMembers.TryGetValue(childInfo.Type, out memberChildren)) 
                    {
                        childInfo.Children = memberChildren;
                    }
                    else if (childInfo.PersistType == PersistType.Complex)
                    {
                        context.Push(childInfo);
                        definedMembers.Add(childInfo.Type, childInfo.Children);
                    }

                    //Handle convertible cases
                    if (childInfo.PersistType == PersistType.Convertible && memberInfo.Attribute.IsReference)
                    {
                        throw new SerializationException($"Cannot have a reference [Persist(IsReference=true)] on the simple type: {childInfo.Type} in property {memberInfo.Member.Name}!");
                    }
                    //Handle List cases
                    if (childInfo.PersistType == PersistType.List)
                    {
                        childInfo.IsReference = false;

                        var valueType = childInfo.Type?.GetEnumeratedType();
                        var valueItemInfo = new PersistMember(valueType)
                        {
                            IsReference = memberInfo.Attribute.IsReference,
                            Name = memberInfo.Attribute.ChildName ?? (valueType.IsGenericType ? ItemKwd : valueType.Name),
                        };
                        childInfo.ValueItemInfo = valueItemInfo;

                        List<PersistMember> typeMembers;
                        if (definedMembers.TryGetValue(valueType, out typeMembers))
                        {
                            valueItemInfo.Children = typeMembers;
                        }
                        else if (GetPersistType(valueItemInfo.Type) == PersistType.Complex)
                        {
                            context.Push(valueItemInfo);
                            definedMembers.Add(valueType, valueItemInfo.Children);
                        }
                    }
                    //Handle dictionary cases
                    else if (childInfo.PersistType == PersistType.Dictionary)
                    {
                        childInfo.IsReference = false;

                        var keyType = childInfo.Type?.GetGenericArguments()[0];
                        var valueType = childInfo.Type?.GetGenericArguments()[1];

                        var keyItemInfo = new PersistMember(keyType)
                        {
                            IsReference = false,
                            Name = memberInfo.Attribute.KeyName ?? (keyType.IsGenericType ? KeyKwd : keyType.Name),
                        };
                        var valueItemInfo = new PersistMember(valueType)
                        {
                            IsReference = memberInfo.Attribute.IsReference,
                            Name = memberInfo.Attribute.ValueName ?? (valueType.IsGenericType ? ValueKwd : valueType.Name),
                        };

                        childInfo.ChildName = memberInfo.Attribute.ChildName ?? ItemKwd;
                        childInfo.KeyItemInfo = keyItemInfo;
                        childInfo.ValueItemInfo = valueItemInfo;

                        List<PersistMember> typeMembers;
                        if (definedMembers.TryGetValue(valueType, out typeMembers))
                        {
                            valueItemInfo.Children = typeMembers;
                        }
                        else if (GetPersistType(valueItemInfo.Type) == PersistType.Complex)
                        {
                            context.Push(valueItemInfo);
                            definedMembers.Add(valueType, valueItemInfo.Children);
                        }

                        if (definedMembers.TryGetValue(keyType, out typeMembers))
                        {
                            keyItemInfo.Children = typeMembers;
                        }
                        else if (GetPersistType(keyItemInfo.Type) == PersistType.Complex)
                        {
                            context.Push(keyItemInfo);
                            definedMembers.Add(keyType, keyItemInfo.Children);
                        }
                    }
                }
            }

            if (Utils.HasCircularDependency(new[] {m_mainInfo}, member => member.Children.Where(m => !m.IsReference)))
            {
                throw new SerializationException("Could not initialize serializer because a circular dependency has been detected please use [Persist(IsReference = true)] to avoid this behaviour");
            }
        }
        
        //Methods called by TreeSerializer & BinarySerializer
        protected void WriteMain(string name, object data)
        {
            if (!m_mainInfo.Type.IsInstanceOfType(data))
            {
                throw new SerializationException($"the type {data.GetType().Name} does not match the constructed type of this serializer ({m_mainInfo.Type})");
            }

            m_generator = new ObjectIDGenerator();

            m_mainInfo.Name = name;
            Write(m_mainInfo, data);

            m_generator = null;
        }
        protected object ReadMain()
        {
            object result = null;
            Read(m_mainInfo,ref result);

            return result;
        }
        protected void ResolveMain(object obj)
        {
            Resolve(m_mainInfo, obj);
        }

        //TreeSerializer & BinarySerializer methods
        public abstract void Write(Stream target, string name, object data);
        public abstract object Read(Stream source);

        protected abstract bool BeginReadObject(string name);
        protected abstract void BeginWriteObject(string name);

        protected abstract void EndReadObject(object obj);
        protected abstract void EndWriteObject(long id);

        protected abstract object ReadValue(string name, Type type);
        protected abstract void WriteValue(string name, object data);

        protected abstract void WriteReference(string name, long id);
        protected abstract object ReadReference(string name);

        protected abstract int GetObjectChildrenCount(string name);

        //Helper Methods
        private void Resolve(PersistMember persistInfo,object owner)
        {
            switch (persistInfo.PersistType)
            {
                case PersistType.Convertible:
                    break;
                case PersistType.List:
                    if (BeginReadObject(persistInfo.Name))
                    {
                        var count = GetObjectChildrenCount(persistInfo.ValueItemInfo.Name);

                        for (int i = 0; i < count; i++)
                        {
                            if (persistInfo.ValueItemInfo.IsReference)
                            {
                                BeginReadObject(persistInfo.ValueItemInfo.Name);
                                ((IList) owner).Add(ReadReference(AddressKwd));
                                EndReadObject(null);
                            }
                            else
                            {
                                Resolve(persistInfo.ValueItemInfo, ((IList)owner)[i]);
                            }
                        }
                        EndReadObject(null);
                    }
                    break;
                case PersistType.Dictionary:
                    if (BeginReadObject(persistInfo.Name))
                    {
                        if (persistInfo.ValueItemInfo.IsReference)
                        {
                            var count = GetObjectChildrenCount(persistInfo.ChildName);
                            for (int i = 0; i < count; i++)
                            {
                                BeginReadObject(persistInfo.ChildName);
                                object keyValue = null;
                                Read(persistInfo.KeyItemInfo, ref keyValue);
                                object childValue = ReadReference(persistInfo.ValueItemInfo.Name);
                                EndReadObject(null);

                                ((IDictionary) owner).Add(keyValue, childValue);
                            }
                        }
                        else
                        {
                            foreach (DictionaryEntry subItem in (IDictionary)owner)
                            {
                                BeginReadObject(persistInfo.ChildName);
                                Resolve(persistInfo.KeyItemInfo, subItem.Key);
                                Resolve(persistInfo.ValueItemInfo, subItem.Value);
                                EndReadObject(null);
                            }
                        }
                        EndReadObject(null);
                    }
                    break;
                case PersistType.Complex:
                    if (BeginReadObject(persistInfo.Name))
                    {
                        foreach (var child in persistInfo.Children.Where(child => child.DeclaringType.IsInstanceOfType(owner)))
                        {
                            if (child.IsReference)
                                child.SetValue(owner, ReadReference(child.Name));
                            else
                                Resolve(child, child.GetValue(owner));
                        }

                        EndReadObject(null);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private void Read(PersistMember persistInfo, ref object owner)
        {
            if (persistInfo.PersistType == PersistType.Convertible || BeginReadObject(persistInfo.Name))
            {
                if (owner == null && persistInfo.PersistType != PersistType.Convertible)
                {
                    var classType = (string)ReadValue(ClassKwd, typeof(string));

                    owner = Activator.CreateInstance(classType == null
                            ? persistInfo.Type
                            : m_polymorphicTypes.Single(type => type.Name == classType));
                }


                int count;
                switch (persistInfo.PersistType)
                {
                    case PersistType.Convertible:
                        owner = ReadValue(persistInfo.Name, persistInfo.Type);
                        return;
                    case PersistType.List:
                        if (persistInfo.ValueItemInfo.IsReference)
                            break;

                        count = GetObjectChildrenCount(persistInfo.ValueItemInfo.Name);
                        for (int i = 0; i < count; i++)
                        {
                            object childValue = null;

                            if (persistInfo.ValueItemInfo.PersistType == PersistType.Convertible)
                            {
                                BeginReadObject(persistInfo.ValueItemInfo.Name);
                                childValue = ReadValue(ValueKwd, persistInfo.ValueItemInfo.Type);
                                EndReadObject(null);
                            }
                            else
                            {
                                Read(persistInfo.ValueItemInfo, ref childValue);
                            }

                            ((IList) owner).Add(childValue);
                        }
                        break;
                    case PersistType.Dictionary:
                        if (persistInfo.ValueItemInfo.IsReference)
                            break;

                        count = GetObjectChildrenCount(persistInfo.ChildName);
                        for (int i = 0; i < count; i++)
                        {
                            object keyValue = null;
                            object childValue = null;

                            BeginReadObject(persistInfo.ChildName);
                            Read(persistInfo.KeyItemInfo, ref keyValue);
                            Read(persistInfo.ValueItemInfo, ref childValue);
                            EndReadObject(null);

                            ((IDictionary) owner).Add(keyValue, childValue);
                        }
                        break;
                    case PersistType.Complex:
                        if (persistInfo.IsReference)
                            break;

                        Type ownerType = owner.GetType();
                        foreach (var child in persistInfo.Children.Where(child => child.DeclaringType.IsAssignableFrom(ownerType)))
                        {
                            object childValue = child.GetValue(owner);

                            Read(child, ref childValue);
                            child.SetValue?.Invoke(owner, childValue);
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }


                EndReadObject(owner);
            }
        }
        private void Write(PersistMember persistInfo, object persistValue)
        {
            if (persistValue == null)
                return;

            if (persistInfo.IsReference)
            {
                WriteReference(persistInfo.Name, UidOf(persistValue));
                return;
            }

            switch (persistInfo.PersistType)
            {
                case PersistType.Convertible:
                    WriteValue(persistInfo.Name, persistValue);
                    break;
                case PersistType.List:
                    BeginWriteObject(persistInfo.Name);

                    foreach (var subItem in (IList) persistValue)
                    {
                        if (persistInfo.ValueItemInfo.PersistType == PersistType.Convertible)
                        {
                            BeginWriteObject(persistInfo.ValueItemInfo.Name);
                            WriteValue(ValueKwd, (IConvertible) subItem);
                            EndWriteObject(-1);
                        }
                        else if (persistInfo.ValueItemInfo.IsReference)
                        {
                            BeginWriteObject(persistInfo.ValueItemInfo.Name);
                            WriteReference(AddressKwd, UidOf(subItem));
                            EndWriteObject(-1);
                        }
                        else
                        {
                            Write(persistInfo.ValueItemInfo, subItem);
                        }
                    }

                    EndWriteObject(UidOf(persistValue));
                    break;
                case PersistType.Dictionary:
                    BeginWriteObject(persistInfo.Name);

                    foreach (DictionaryEntry subItem in (IDictionary) persistValue)
                    {
                        BeginWriteObject(persistInfo.ChildName);
                        Write(persistInfo.KeyItemInfo, subItem.Key);
                        Write(persistInfo.ValueItemInfo, subItem.Value);
                        EndWriteObject(-1);
                    }

                    EndWriteObject(UidOf(persistValue));
                    break;
                case PersistType.Complex:
                    BeginWriteObject(persistInfo.Name);

                    if (persistValue.GetType() != persistInfo.Type)
                        WriteValue(ClassKwd, persistValue.GetType().Name);

                    foreach (var child in persistInfo.Children.Where(child => child.DeclaringType.IsInstanceOfType(persistValue)))
                        Write(child, child.GetValue(persistValue));

                    EndWriteObject(UidOf(persistValue));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private long UidOf(object o)
        {
            bool firstTime;
            return m_generator.GetId(o, out firstTime);
        }
        private IEnumerable<MemberAttrib> GetElegibleMembers(Type mainType)
        {
            const BindingFlags searchMode = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            var elegibleMembers = Enumerable.Empty<MemberAttrib>();
            var allDerivedTypes = m_polymorphicTypes.Where(mainType.IsAssignableFrom).SelectMany(dT =>
            {
                var typesTillMain = new List<Type>();

                while (dT != mainType)
                {
                    typesTillMain.Add(dT);
                    dT = dT.BaseType;
                }

                return typesTillMain;
            });

            foreach (var memberType in new[] {mainType}.Concat(allDerivedTypes))
            {
                var currentMemberType = memberType;

                var searchFlags = currentMemberType == mainType ? searchMode : searchMode | BindingFlags.DeclaredOnly;

                Type metaType;
                bool isMetaType = m_metaTypes.TryGetValue(currentMemberType, out metaType);

                var realFields = new FieldInfo[0];
                var realProperties = new PropertyInfo[0];

                if (isMetaType)
                {
                    realFields = currentMemberType.GetFields(searchFlags);
                    realProperties = currentMemberType.GetProperties(searchFlags);

                    currentMemberType = metaType;
                }

                var propertyMembers =
                    currentMemberType.GetProperties(searchFlags)
                        .Select( info => new { p = info, attr = (PersistAttribute)System.Attribute.GetCustomAttribute(info, typeof (PersistAttribute)) })
                        .Where(  info =>
                            {
                                return GetPersistType(PersistMember.GetMemberType(info.p)) == PersistType.Convertible
                                    ? info.p.SetMethod != null && info.p.SetMethod.IsPublic &&
                                      (info.attr == null || info.attr.Ignore == false)
                                    : (info.p.GetMethod.IsPublic
                                        ? info.attr == null || info.attr.Ignore == false
                                        : info.attr != null && info.attr.Ignore == false);
                            })
                        .Select(info => new MemberAttrib( isMetaType ? realProperties.Single(p => p.Name == info.p.Name) : info.p, info.attr ?? new PersistAttribute(info.p.Name)));

                var fieldMembers =
                    currentMemberType.GetFields(searchFlags)
                        .Select(info =>new { f = info, attr = (PersistAttribute) System.Attribute.GetCustomAttribute(info, typeof (PersistAttribute)) })
                        .Where(info => info.attr != null && info.attr.Ignore == false)
                        .Select(info => new MemberAttrib(isMetaType ? realFields.Single(p => p.Name == info.f.Name) : info.f, info.attr));

                elegibleMembers = elegibleMembers.Concat(propertyMembers.Concat(fieldMembers));
            }

            return elegibleMembers;
        }
        private static PersistType GetPersistType(Type type)
        {
            return typeof (IConvertible).IsAssignableFrom(type)
                ? PersistType.Convertible
                : (typeof (IList).IsAssignableFrom(type)
                    ? PersistType.List
                    : (typeof (IDictionary).IsAssignableFrom(type) ? PersistType.Dictionary : PersistType.Complex));
        }
    }
}