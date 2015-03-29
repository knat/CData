using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace CData {
    public abstract class AssemblyMetadata {
        private static readonly Dictionary<FullName, GlobalTypeMetadata> _globalTypeMap = new Dictionary<FullName, GlobalTypeMetadata>();
        public static T GetGlobalType<T>(FullName fullName) where T : GlobalTypeMetadata {
            GlobalTypeMetadata globalType;
            lock (_globalTypeMap) {
                _globalTypeMap.TryGetValue(fullName, out globalType);
            }
            return globalType as T;
        }
        protected AssemblyMetadata(GlobalTypeMetadata[] globalTypes) {
            if (globalTypes != null) {
                lock (_globalTypeMap) {
                    foreach (var globalType in globalTypes) {
                        _globalTypeMap.Add(globalType.FullName, globalType);
                    }
                }
            }
            GlobalTypes = globalTypes;
        }
        internal readonly GlobalTypeMetadata[] GlobalTypes;
    }
    public enum TypeKind : byte {
        None = 0,
        String = 1,
        IgnoreCaseString = 2,
        Char = 3,
        Decimal = 4,
        Int64 = 5,
        Int32 = 6,
        Int16 = 7,
        SByte = 8,
        UInt64 = 9,
        UInt32 = 10,
        UInt16 = 11,
        Byte = 12,
        Double = 13,
        Single = 14,
        Boolean = 15,
        Binary = 16,
        Guid = 17,
        TimeSpan = 18,
        DateTimeOffset = 19,
        Class = 50,
        Enum = 51,
        List = 70,
        SimpleSet = 71,
        ObjectSet = 72,
        Map = 73,
    }
    public abstract class TypeMetadata {
        protected TypeMetadata(TypeKind kind) {
            Kind = kind;
        }
        public readonly TypeKind Kind;
    }
    public abstract class LocalTypeMetadata : TypeMetadata {
        protected LocalTypeMetadata(TypeKind kind, bool isNullable)
            : base(kind) {
            IsNullable = isNullable;
        }
        public readonly bool IsNullable;
    }
    public sealed class CollectionMetadata : LocalTypeMetadata {
        public CollectionMetadata(TypeKind kind, bool isNullable,
            LocalTypeMetadata itemOrValueType, GlobalTypeRefMetadata mapKeyType, object objectSetKeySelector,
            Type clrType)
            : base(kind, isNullable) {
            ItemOrValueType = itemOrValueType;
            MapKeyType = mapKeyType;
            ObjectSetKeySelector = objectSetKeySelector;
            ClrType = clrType;
            var ti = clrType.GetTypeInfo();
            ClrConstructor = Extensions.GetParameterlessConstructor(ti);
            ClrAddMethod = Extensions.GetMethodInHierarchy(ti, "Add");
            if (kind == TypeKind.Map) {
                ClrContainsKeyMethod = Extensions.GetMethodInHierarchy(ti, "ContainsKey");
                ClrGetEnumeratorMethod = Extensions.GetMethodInHierarchy(ti, "GetEnumerator");
            }
            else if (kind == TypeKind.ObjectSet) {
                ClrKeySelectorProperty = Extensions.GetPropertyInHierarchy(ti, "KeySelector");
            }
        }
        public readonly LocalTypeMetadata ItemOrValueType;
        public readonly GlobalTypeRefMetadata MapKeyType;//opt
        public readonly object ObjectSetKeySelector;//opt
        public readonly Type ClrType;
        public readonly ConstructorInfo ClrConstructor;
        public readonly MethodInfo ClrAddMethod;
        public readonly MethodInfo ClrContainsKeyMethod;//for map
        public readonly MethodInfo ClrGetEnumeratorMethod;//for map
        public readonly PropertyInfo ClrKeySelectorProperty;//for object set
        public object CreateInstance() {
            var obj = ClrConstructor.Invoke(null);
            if (Kind == TypeKind.ObjectSet) {
                ClrKeySelectorProperty.SetValue(obj, ObjectSetKeySelector);
            }
            return obj;
        }
        public void InvokeAdd(object obj, object item) {
            ClrAddMethod.Invoke(obj, new object[] { item });
        }
        public bool InvokeBoolAdd(object obj, object item) {
            return (bool)ClrAddMethod.Invoke(obj, new object[] { item });
        }
        public bool InvokeContainsKey(object obj, object key) {
            return (bool)ClrContainsKeyMethod.Invoke(obj, new object[] { key });
        }
        public void InvokeAdd(object obj, object key, object value) {
            ClrAddMethod.Invoke(obj, new object[] { key, value });
        }
        public IDictionaryEnumerator GetMapEnumerator(object obj) {
            return ClrGetEnumeratorMethod.Invoke(obj, null) as IDictionaryEnumerator;
        }
    }
    public sealed class GlobalTypeRefMetadata : LocalTypeMetadata {
        public GlobalTypeRefMetadata(GlobalTypeMetadata globalType, bool isNullable)
            : base(globalType.Kind, isNullable) {
            GlobalType = globalType;
        }
        public readonly GlobalTypeMetadata GlobalType;
        public static GlobalTypeRefMetadata GetAtom(TypeKind kind, bool isNullable) {
            return isNullable ? _nullableAtomMap[kind] : _atomMap[kind];
        }
        private GlobalTypeRefMetadata(TypeKind kind, bool isNullable)
            : base(kind, isNullable) {
        }
        private static readonly Dictionary<TypeKind, GlobalTypeRefMetadata> _atomMap = new Dictionary<TypeKind, GlobalTypeRefMetadata> {
            { TypeKind.String, new GlobalTypeRefMetadata(TypeKind.String , false) },
            { TypeKind.IgnoreCaseString, new GlobalTypeRefMetadata(TypeKind.IgnoreCaseString, false) },
            { TypeKind.Char, new GlobalTypeRefMetadata(TypeKind.Char , false) },
            { TypeKind.Decimal, new GlobalTypeRefMetadata(TypeKind.Decimal, false) },
            { TypeKind.Int64, new GlobalTypeRefMetadata(TypeKind.Int64, false) },
            { TypeKind.Int32, new GlobalTypeRefMetadata(TypeKind.Int32, false) },
            { TypeKind.Int16, new GlobalTypeRefMetadata(TypeKind.Int16, false) },
            { TypeKind.SByte, new GlobalTypeRefMetadata(TypeKind.SByte, false) },
            { TypeKind.UInt64, new GlobalTypeRefMetadata(TypeKind.UInt64, false) },
            { TypeKind.UInt32, new GlobalTypeRefMetadata(TypeKind.UInt32, false) },
            { TypeKind.UInt16, new GlobalTypeRefMetadata(TypeKind.UInt16, false) },
            { TypeKind.Byte, new GlobalTypeRefMetadata(TypeKind.Byte, false) },
            { TypeKind.Double, new GlobalTypeRefMetadata(TypeKind.Double, false) },
            { TypeKind.Single, new GlobalTypeRefMetadata(TypeKind.Single, false) },
            { TypeKind.Boolean, new GlobalTypeRefMetadata(TypeKind.Boolean, false) },
            { TypeKind.Binary, new GlobalTypeRefMetadata(TypeKind.Binary, false) },
            { TypeKind.Guid, new GlobalTypeRefMetadata(TypeKind.Guid, false) },
            { TypeKind.TimeSpan, new GlobalTypeRefMetadata(TypeKind.TimeSpan, false) },
            { TypeKind.DateTimeOffset, new GlobalTypeRefMetadata(TypeKind.DateTimeOffset, false) },
        };
        private static readonly Dictionary<TypeKind, GlobalTypeRefMetadata> _nullableAtomMap = new Dictionary<TypeKind, GlobalTypeRefMetadata> {
            { TypeKind.String, new GlobalTypeRefMetadata(TypeKind.String, true) },
            { TypeKind.IgnoreCaseString, new GlobalTypeRefMetadata(TypeKind.IgnoreCaseString, true) },
            { TypeKind.Char, new GlobalTypeRefMetadata(TypeKind.Char , true) },
            { TypeKind.Decimal, new GlobalTypeRefMetadata(TypeKind.Decimal, true) },
            { TypeKind.Int64, new GlobalTypeRefMetadata(TypeKind.Int64, true) },
            { TypeKind.Int32, new GlobalTypeRefMetadata(TypeKind.Int32, true) },
            { TypeKind.Int16, new GlobalTypeRefMetadata(TypeKind.Int16, true) },
            { TypeKind.SByte, new GlobalTypeRefMetadata(TypeKind.SByte, true) },
            { TypeKind.UInt64, new GlobalTypeRefMetadata(TypeKind.UInt64, true) },
            { TypeKind.UInt32, new GlobalTypeRefMetadata(TypeKind.UInt32, true) },
            { TypeKind.UInt16, new GlobalTypeRefMetadata(TypeKind.UInt16, true) },
            { TypeKind.Byte, new GlobalTypeRefMetadata(TypeKind.Byte, true) },
            { TypeKind.Double, new GlobalTypeRefMetadata(TypeKind.Double, true) },
            { TypeKind.Single, new GlobalTypeRefMetadata(TypeKind.Single, true) },
            { TypeKind.Boolean, new GlobalTypeRefMetadata(TypeKind.Boolean, true) },
            { TypeKind.Binary, new GlobalTypeRefMetadata(TypeKind.Binary, true) },
            { TypeKind.Guid, new GlobalTypeRefMetadata(TypeKind.Guid, true) },
            { TypeKind.TimeSpan, new GlobalTypeRefMetadata(TypeKind.TimeSpan, true) },
            { TypeKind.DateTimeOffset, new GlobalTypeRefMetadata(TypeKind.DateTimeOffset, true) },
        };
    }
    public abstract class GlobalTypeMetadata : TypeMetadata {
        protected GlobalTypeMetadata(TypeKind kind, FullName fullName)
            : base(kind) {
            FullName = fullName;
        }
        public readonly FullName FullName;
    }
    public struct NameValuePair {
        public NameValuePair(string name, object value) {
            Name = name;
            Value = value;
        }
        public readonly string Name;
        public readonly object Value;
    }
    public sealed class EnumMetadata : GlobalTypeMetadata {
        public EnumMetadata(FullName fullName, NameValuePair[] members)
            : base(TypeKind.Enum, fullName) {
            _members = members;
        }
        private readonly NameValuePair[] _members;
        public object GetMemberValue(string name) {
            var members = _members;
            if (members != null) {
                var length = members.Length;
                for (var i = 0; i < length; ++i) {
                    if (members[i].Name == name) {
                        return members[i].Value;
                    }
                }
            }
            return null;
        }
        public string GetMemberName(object value) {
            var members = _members;
            if (members != null) {
                var length = members.Length;
                for (var i = 0; i < length; ++i) {
                    if (members[i].Value.Equals(value)) {
                        return members[i].Name;
                    }
                }
            }
            return null;
        }
    }
    public sealed class ClassMetadata : GlobalTypeMetadata {
        public ClassMetadata(FullName fullName, bool isAbstract, ClassMetadata baseClass, PropertyMetadata[] properties, Type clrType)
            : base(TypeKind.Class, fullName) {
            IsAbstract = isAbstract;
            BaseClass = baseClass;
            _properties = properties;
            ClrType = clrType;
            TypeInfo ti = clrType.GetTypeInfo();
            if (properties != null) {
                foreach (var prop in properties) {
                    prop.GetClrPropertyOrField(ti);
                }
            }
            if (!isAbstract) {
                ClrConstructor = Extensions.GetParameterlessConstructor(ti);
            }
            if (baseClass == null) {
                ClrMetadataProperty = Extensions.GetProperty(ti, Extensions.MetadataNameStr);
                ClrTextSpanProperty = Extensions.GetProperty(ti, Extensions.TextSpanNameStr);
            }
            ClrOnLoadingMethod = ti.GetDeclaredMethod(Extensions.OnLoadingNameStr);
            ClrOnLoadedMethod = ti.GetDeclaredMethod(Extensions.OnLoadedNameStr);
        }
        public readonly bool IsAbstract;
        public readonly ClassMetadata BaseClass;
        private readonly PropertyMetadata[] _properties;
        public readonly Type ClrType;
        public readonly ConstructorInfo ClrConstructor;//for non abstract class
        public readonly PropertyInfo ClrMetadataProperty;//for top class
        public readonly PropertyInfo ClrTextSpanProperty;//for top class
        public readonly MethodInfo ClrOnLoadingMethod;//opt
        public readonly MethodInfo ClrOnLoadedMethod;//opt
        public bool IsEqualToOrDeriveFrom(ClassMetadata other) {
            for (var cls = this; cls != null; cls = cls.BaseClass) {
                if (cls == other) {
                    return true;
                }
            }
            return false;
        }
        public PropertyMetadata GetPropertyInHierarchy(string name) {
            var props = _properties;
            if (props != null) {
                var length = props.Length;
                for (var i = 0; i < length; ++i) {
                    if (props[i].Name == name) {
                        return props[i];
                    }
                }
            }
            if (BaseClass != null) {
                return BaseClass.GetPropertyInHierarchy(name);
            }
            return null;
        }
        public void GetPropertiesInHierarchy(ref List<PropertyMetadata> propList) {
            if (BaseClass != null) {
                BaseClass.GetPropertiesInHierarchy(ref propList);
            }
            if (_properties != null) {
                if (propList == null) {
                    propList = new List<PropertyMetadata>(_properties);
                }
                else {
                    propList.AddRange(_properties);
                }
            }
        }
        public IEnumerable<PropertyMetadata> GetPropertiesInHierarchy() {
            if (BaseClass == null) {
                return _properties;
            }
            if (_properties == null) {
                return BaseClass.GetPropertiesInHierarchy();
            }
            List<PropertyMetadata> propList = null;
            GetPropertiesInHierarchy(ref propList);
            return propList;
        }
        public object CreateInstance() {
            //FormatterServices.GetUninitializedObject()
            return ClrConstructor.Invoke(null);
        }
        public ClassMetadata GetMetadata(object obj) {
            var md = this;
            for (; md.BaseClass != null; md = md.BaseClass) ;
            return (ClassMetadata)md.ClrMetadataProperty.GetValue(obj);
        }
        public void SetTextSpan(object obj, TextSpan value) {
            var md = this;
            for (; md.BaseClass != null; md = md.BaseClass) ;
            md.ClrTextSpanProperty.SetValue(obj, value);
        }
        public bool InvokeOnLoad(bool isLoading, object obj, DiagContext context) {
            if (BaseClass != null) {
                if (!BaseClass.InvokeOnLoad(isLoading, obj, context)) {
                    return false;
                }
            }
            var mi = isLoading ? ClrOnLoadingMethod : ClrOnLoadedMethod;
            if (mi != null) {
                if (!(bool)mi.Invoke(obj, new object[] { context })) {
                    return false;
                }
            }
            return true;
        }
    }
    public sealed class PropertyMetadata {
        public PropertyMetadata(string name, LocalTypeMetadata type, string clrName, bool isClrProperty) {
            Name = name;
            Type = type;
            ClrName = clrName;
            IsClrProperty = isClrProperty;
        }
        public readonly string Name;
        public readonly LocalTypeMetadata Type;
        public readonly string ClrName;
        public readonly bool IsClrProperty;
        public PropertyInfo ClrProperty { get; private set; }
        public FieldInfo ClrField { get; private set; }
        internal void GetClrPropertyOrField(TypeInfo ti) {
            if (IsClrProperty) {
                ClrProperty = Extensions.GetProperty(ti, ClrName);
            }
            else {
                ClrField = Extensions.GetField(ti, ClrName);
            }
        }
        public object GetValue(object obj) {
            if (IsClrProperty) {
                return ClrProperty.GetValue(obj);
            }
            else {
                return ClrField.GetValue(obj);
            }
        }
        public void SetValue(object obj, object value) {
            if (IsClrProperty) {
                ClrProperty.SetValue(obj, value);
            }
            else {
                ClrField.SetValue(obj, value);
            }
        }
    }


}
