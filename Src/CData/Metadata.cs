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
        Decimal = 3,
        Int64 = 4,
        Int32 = 5,
        Int16 = 6,
        SByte = 7,
        UInt64 = 8,
        UInt32 = 9,
        UInt16 = 10,
        Byte = 11,
        Double = 12,
        Single = 13,
        Boolean = 14,
        Binary = 15,
        Guid = 16,
        TimeSpan = 17,
        DateTimeOffset = 18,
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
                ClrKeysProperty = Extensions.GetPropertyInHierarchy(ti, "Keys");
                ClrValuesProperty = Extensions.GetPropertyInHierarchy(ti, "Values");
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
        public readonly PropertyInfo ClrKeysProperty;//for map
        public readonly PropertyInfo ClrValuesProperty;//for map
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
        public IEnumerable GetMapKeys(object obj) {
            return (IEnumerable)ClrKeysProperty.GetValue(obj);
        }
        public IEnumerable GetMapValues(object obj) {
            return (IEnumerable)ClrValuesProperty.GetValue(obj);
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
        protected GlobalTypeMetadata(TypeKind kind, FullName fullName, Type clrType)
            : base(kind) {
            FullName = fullName;
            ClrType = clrType;
        }
        public readonly FullName FullName;
        public readonly Type ClrType;
    }
    internal struct NameValuePair {
        public NameValuePair(string name, object value) {
            Name = name;
            Value = value;
        }
        public readonly string Name;
        public readonly object Value;
    }
    public sealed class EnumMetadata : GlobalTypeMetadata {
        public EnumMetadata(FullName fullName, Type clrType, bool isClrEnum)
            : base(TypeKind.Enum, fullName, clrType) {
            IsClrEnum = isClrEnum;
            NameValuePair[] members;
            if (isClrEnum) {
                var names = Enum.GetNames(clrType);
                var values = Enum.GetValues(clrType);
                var length = names.Length;
                members = new NameValuePair[length];
                for (var i = 0; i < length; ++i) {
                    members[i] = new NameValuePair(names[i], values.GetValue(i));
                }
            }
            else {
                var list = new List<NameValuePair>();
                foreach (var fi in clrType.GetTypeInfo().DeclaredFields) {
                    list.Add(new NameValuePair(fi.Name, fi.GetValue(null)));
                }
                members = list.ToArray();
            }
            _members = members;
        }
        public readonly bool IsClrEnum;//true for Int64 to Byte
        private readonly NameValuePair[] _members;
        public object GetMemberValue(string name) {
            var members = _members;
            var length = members.Length;
            for (var i = 0; i < length; ++i) {
                if (members[i].Name == name) {
                    return members[i].Value;
                }
            }
            return null;
        }
        public string GetMemberName(object value) {
            var members = _members;
            var length = members.Length;
            for (var i = 0; i < length; ++i) {
                if (members[i].Value.Equals(value)) {
                    return members[i].Name;
                }
            }
            return null;
        }
    }
    public sealed class ClassMetadata : GlobalTypeMetadata {
        public ClassMetadata(FullName fullName, Type clrType, bool isAbstract, ClassMetadata baseClass, PropertyMetadata[] properties)
            : base(TypeKind.Class, fullName, clrType) {
            IsAbstract = isAbstract;
            BaseClass = baseClass;
            _properties = properties;
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
                ClrTextSpanProperty = Extensions.GetProperty(ti, "__TextSpan");
            }
            ClrOnLoadingMethod = ti.GetDeclaredMethod("OnLoading");
            ClrOnLoadedMethod = ti.GetDeclaredMethod("OnLoaded");
        }
        public readonly bool IsAbstract;
        public readonly ClassMetadata BaseClass;
        private readonly PropertyMetadata[] _properties;
        public readonly ConstructorInfo ClrConstructor;//for non abstract class
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
            return ClrConstructor.Invoke(null);
        }
        public void SetTextSpan(object obj, TextSpan value) {
            if (BaseClass != null) {
                BaseClass.SetTextSpan(obj, value);
            }
            else {
                ClrTextSpanProperty.SetValue(obj, value);
            }
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
