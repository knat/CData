using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace CData {
    public enum TypeKind : byte {
        None = 0,
        Class = 1,
        Enum = 2,
        List = 10,
        AtomSet = 11,
        ObjectSet = 12,
        Map = 13,
        String = 50,
        IgnoreCaseString = 51,
        Decimal = 52,
        Int64 = 53,
        Int32 = 54,
        Int16 = 55,
        SByte = 56,
        UInt64 = 57,
        UInt32 = 58,
        UInt16 = 59,
        Byte = 60,
        Double = 61,
        Single = 62,
        Boolean = 63,
        Binary = 64,
        Guid = 65,
        TimeSpan = 66,
        DateTimeOffset = 67,
    }
    public abstract class TypeMetadata {
        protected TypeMetadata(TypeKind kind, bool isNullable) {
            Kind = kind;
            IsNullable = isNullable;
        }
        public readonly TypeKind Kind;
        public readonly bool IsNullable;
        public bool IsClass {
            get {
                return Kind == TypeKind.Class;
            }
        }
        public bool IsEnum {
            get {
                return Kind == TypeKind.Enum;
            }
        }
        public bool IsList {
            get {
                return Kind == TypeKind.List;
            }
        }
        public bool IsAtomSet {
            get {
                return Kind == TypeKind.AtomSet;
            }
        }
        public bool IsObjectSet {
            get {
                return Kind == TypeKind.ObjectSet;
            }
        }
        public bool IsMap {
            get {
                return Kind == TypeKind.Map;
            }
        }
        public bool IsAtom {
            get {
                return Kind.IsAtom();
            }
        }
    }
    public sealed class CollectionTypeMetadata : TypeMetadata {
        public CollectionTypeMetadata(TypeKind kind, bool isNullable,
            TypeMetadata itemOrValueType, AtomRefTypeMetadata mapKeyType, object objectSetKeySelector,
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
        public readonly TypeMetadata ItemOrValueType;
        public readonly AtomRefTypeMetadata MapKeyType;//opt
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
            if (IsObjectSet) {
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
    public sealed class AtomRefTypeMetadata : TypeMetadata {
        public static AtomRefTypeMetadata Get(TypeKind kind, bool isNullable) {
            return isNullable ? _nullableMap[kind] : _map[kind];
        }
        private AtomRefTypeMetadata(TypeKind kind, bool isNullable)
            : base(kind, isNullable) {
        }
        private static readonly Dictionary<TypeKind, AtomRefTypeMetadata> _map = new Dictionary<TypeKind, AtomRefTypeMetadata> {
            { TypeKind.String, new AtomRefTypeMetadata(TypeKind.String , false) },
            { TypeKind.IgnoreCaseString, new AtomRefTypeMetadata(TypeKind.IgnoreCaseString, false) },
            { TypeKind.Decimal, new AtomRefTypeMetadata(TypeKind.Decimal, false) },
            { TypeKind.Int64, new AtomRefTypeMetadata(TypeKind.Int64, false) },
            { TypeKind.Int32, new AtomRefTypeMetadata(TypeKind.Int32, false) },
            { TypeKind.Int16, new AtomRefTypeMetadata(TypeKind.Int16, false) },
            { TypeKind.SByte, new AtomRefTypeMetadata(TypeKind.SByte, false) },
            { TypeKind.UInt64, new AtomRefTypeMetadata(TypeKind.UInt64, false) },
            { TypeKind.UInt32, new AtomRefTypeMetadata(TypeKind.UInt32, false) },
            { TypeKind.UInt16, new AtomRefTypeMetadata(TypeKind.UInt16, false) },
            { TypeKind.Byte, new AtomRefTypeMetadata(TypeKind.Byte, false) },
            { TypeKind.Double, new AtomRefTypeMetadata(TypeKind.Double, false) },
            { TypeKind.Single, new AtomRefTypeMetadata(TypeKind.Single, false) },
            { TypeKind.Boolean, new AtomRefTypeMetadata(TypeKind.Boolean, false) },
            { TypeKind.Binary, new AtomRefTypeMetadata(TypeKind.Binary, false) },
            { TypeKind.Guid, new AtomRefTypeMetadata(TypeKind.Guid, false) },
            { TypeKind.TimeSpan, new AtomRefTypeMetadata(TypeKind.TimeSpan, false) },
            { TypeKind.DateTimeOffset, new AtomRefTypeMetadata(TypeKind.DateTimeOffset, false) },
        };
        private static readonly Dictionary<TypeKind, AtomRefTypeMetadata> _nullableMap = new Dictionary<TypeKind, AtomRefTypeMetadata> {
            { TypeKind.String, new AtomRefTypeMetadata(TypeKind.String, true) },
            { TypeKind.IgnoreCaseString, new AtomRefTypeMetadata(TypeKind.IgnoreCaseString, true) },
            { TypeKind.Decimal, new AtomRefTypeMetadata(TypeKind.Decimal, true) },
            { TypeKind.Int64, new AtomRefTypeMetadata(TypeKind.Int64, true) },
            { TypeKind.Int32, new AtomRefTypeMetadata(TypeKind.Int32, true) },
            { TypeKind.Int16, new AtomRefTypeMetadata(TypeKind.Int16, true) },
            { TypeKind.SByte, new AtomRefTypeMetadata(TypeKind.SByte, true) },
            { TypeKind.UInt64, new AtomRefTypeMetadata(TypeKind.UInt64, true) },
            { TypeKind.UInt32, new AtomRefTypeMetadata(TypeKind.UInt32, true) },
            { TypeKind.UInt16, new AtomRefTypeMetadata(TypeKind.UInt16, true) },
            { TypeKind.Byte, new AtomRefTypeMetadata(TypeKind.Byte, true) },
            { TypeKind.Double, new AtomRefTypeMetadata(TypeKind.Double, true) },
            { TypeKind.Single, new AtomRefTypeMetadata(TypeKind.Single, true) },
            { TypeKind.Boolean, new AtomRefTypeMetadata(TypeKind.Boolean, true) },
            { TypeKind.Binary, new AtomRefTypeMetadata(TypeKind.Binary, true) },
            { TypeKind.Guid, new AtomRefTypeMetadata(TypeKind.Guid, true) },
            { TypeKind.TimeSpan, new AtomRefTypeMetadata(TypeKind.TimeSpan, true) },
            { TypeKind.DateTimeOffset, new AtomRefTypeMetadata(TypeKind.DateTimeOffset, true) },
        };
    }
    public sealed class EntityRefTypeMetadata : TypeMetadata {
        public EntityRefTypeMetadata(TypeKind kind, bool isNullable, EntityMetadata entity)
            : base(kind, isNullable) {
            Entity = entity;
        }
        public readonly EntityMetadata Entity;
    }

    public abstract class EntityMetadata {
        private static readonly Dictionary<FullName, EntityMetadata> _map = new Dictionary<FullName, EntityMetadata>();
        private static void Add(FullName fullName, EntityMetadata entity) {
            lock (_map) {
                _map.Add(fullName, entity);
            }
        }
        public static T Get<T>(FullName fullName) where T : EntityMetadata {
            EntityMetadata entity;
            lock (_map) {
                _map.TryGetValue(fullName, out entity);
            }
            return entity as T;
        }
        protected EntityMetadata(FullName fullName, Type clrType) {
            Add(fullName, this);
            FullName = fullName;
            ClrType = clrType;
        }
        public readonly FullName FullName;
        public readonly Type ClrType;
    }
    public struct NameValuePair {
        public NameValuePair(string name, object value) {
            Name = name;
            Value = value;
        }
        public readonly string Name;
        public readonly object Value;
    }
    public sealed class EnumMetadata : EntityMetadata {
        public EnumMetadata(FullName fullName, Type clrType, bool isClrEnum, NameValuePair[] members)
            : base(fullName, clrType) {
            IsClrEnum = isClrEnum;
            if (!isClrEnum) {
                _clrFieldInfos = clrType.GetTypeInfo().DeclaredFields.ToArray();
            }
            _members = members;
        }
        public readonly bool IsClrEnum;
        private readonly FieldInfo[] _clrFieldInfos;//for non-clr enum
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
            if (IsClrEnum) {
                return Enum.GetName(ClrType, value);
            }
            foreach (var fi in _clrFieldInfos) {
                if (object.Equals(value, fi.GetValue(null))) {
                    return fi.Name;
                }
            }
            return null;
        }
    }
    public sealed class ClassMetadata : EntityMetadata {
        private static readonly HashSet<Assembly> _assemblySet = new HashSet<Assembly>();
        public static void Initialize(Assembly assembly) {
            if (assembly == null) throw new ArgumentNullException("assembly");
            bool added;
            lock (_assemblySet) {
                added = _assemblySet.Add(assembly);
            }
            if (added) {
                RunClassConstructors(assembly);
            }
        }
        private static void RunClassConstructors(Assembly assembly) {
            var att = assembly.GetCustomAttribute<ContractTypesAttribute>();
            if (att != null) {
                var types = att.Types;
                if (types != null) {
                    foreach (var type in types) {
                        if (type != null) {
                            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                        }
                    }
                }
            }
        }
        //
        public ClassMetadata(FullName fullName, Type clrType, bool isAbstract, ClassMetadata baseClass, PropertyMetadata[] properties)
            : base(fullName, clrType) {
            IsAbstract = isAbstract;
            BaseClass = baseClass;
            _properties = properties;
            TypeInfo ti = clrType.GetTypeInfo();
            Initialize(ti.Assembly);//??
            if (!isAbstract) {
                ClrConstructor = Extensions.GetParameterlessConstructor(ti);
            }
            if (properties != null) {
                foreach (var prop in properties) {
                    prop.GetClrPropertyOrField(ti);
                }
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
            if (other == null) throw new ArgumentNullException("other");
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
        public PropertyMetadata(string name, TypeMetadata type, string clrName, bool isClrProperty) {
            Name = name;
            Type = type;
            ClrName = clrName;
            IsClrProperty = isClrProperty;
        }
        public readonly string Name;
        public readonly TypeMetadata Type;
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
