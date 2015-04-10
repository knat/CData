using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace CData {
    public abstract class ProgramMetadata {
        private static readonly Dictionary<FullName, GlobalTypeMetadata> _globalTypeMap = new Dictionary<FullName, GlobalTypeMetadata>();
        private static readonly HashSet<string> _uriSet = new HashSet<string>();
        public static T GetGlobalType<T>(FullName fullName) where T : GlobalTypeMetadata {
            GlobalTypeMetadata globalType;
            _globalTypeMap.TryGetValue(fullName, out globalType);
            return globalType as T;
        }
        internal static bool IsUriDefined(string uri) {
            return _uriSet.Contains(uri);
        }
        protected void AddGlobalTypes(GlobalTypeMetadata[] globalTypes) {
            if (globalTypes != null) {
                lock (_globalTypeMap) {
                    foreach (var globalType in globalTypes) {
                        var fullName = globalType.FullName;
                        _globalTypeMap.Add(fullName, globalType);
                        _uriSet.Add(fullName.Uri);
                    }
                }
            }
        }
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
        Enumerable = 74,
        AnonymousClass = 75,
        Void = 76,
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
    public sealed class AnonymousClassMetadata : LocalTypeMetadata {
        public AnonymousClassMetadata(bool isNullable, AnonymousPropertyMetadata[] properties)
            : base(TypeKind.AnonymousClass, isNullable) {
            _properties = properties;
        }
        private readonly AnonymousPropertyMetadata[] _properties;//opt
        public AnonymousPropertyMetadata GetProperty(string name) {
            var props = _properties;
            if (props != null) {
                var length = props.Length;
                for (var i = 0; i < length; ++i) {
                    if (props[i].Name == name) {
                        return props[i];
                    }
                }
            }
            return null;
        }
    }
    public abstract class NamedLocalTypeMetadata {
        protected NamedLocalTypeMetadata(string name, LocalTypeMetadata type) {
            Name = name;
            Type = type;
        }
        public readonly string Name;
        public readonly LocalTypeMetadata Type;
    }
    public sealed class AnonymousPropertyMetadata : NamedLocalTypeMetadata {
        public AnonymousPropertyMetadata(string name, LocalTypeMetadata type)
            : base(name, type) {
        }
    }
    public sealed class VoidMetadata : LocalTypeMetadata {
        public static readonly VoidMetadata Instance = new VoidMetadata();
        private VoidMetadata() : base(TypeKind.Void, false) { }
    }
    public abstract class CollectionBaseMetadata : LocalTypeMetadata {
        protected CollectionBaseMetadata(TypeKind kind, bool isNullable, LocalTypeMetadata itemOrValueType)
            : base(kind, isNullable) {
            ItemOrValueType = itemOrValueType;
        }
        public readonly LocalTypeMetadata ItemOrValueType;
    }
    public sealed class EnumerableMetadata : CollectionBaseMetadata {
        public EnumerableMetadata(bool isNullable, LocalTypeMetadata itemOrValueType)
            : base(TypeKind.Enumerable, isNullable, itemOrValueType) {
        }
    }

    //for List, SimpleSet, ObjectSet, Map
    public sealed class CollectionMetadata : CollectionBaseMetadata {
        public CollectionMetadata(TypeKind kind, bool isNullable,
            LocalTypeMetadata itemOrValueType, GlobalTypeRefMetadata mapKeyType, object objectSetKeySelector, Type clrType)
            : base(kind, isNullable, itemOrValueType) {
            MapKeyType = mapKeyType;
            ObjectSetKeySelector = objectSetKeySelector;
            ClrType = clrType;
            var ti = clrType.GetTypeInfo();
            ClrConstructor = ReflectionExtensions.GetParameterlessConstructor(ti);
            ClrAddMethod = ReflectionExtensions.GetMethodInHierarchy(ti, "Add");
            if (kind == TypeKind.Map) {
                ClrContainsKeyMethod = ReflectionExtensions.GetMethodInHierarchy(ti, "ContainsKey");
                ClrGetEnumeratorMethod = ReflectionExtensions.GetMethodInHierarchy(ti, "GetEnumerator");
            }
            else if (kind == TypeKind.ObjectSet) {
                ClrKeySelectorProperty = ReflectionExtensions.GetPropertyInHierarchy(ti, "KeySelector");
            }
        }
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
        private static readonly Dictionary<TypeKind, GlobalTypeRefMetadata> _atomMap = new Dictionary<TypeKind, GlobalTypeRefMetadata> {
            { TypeKind.String, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.String), false) },
            { TypeKind.IgnoreCaseString, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.IgnoreCaseString), false) },
            { TypeKind.Char, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.Char), false) },
            { TypeKind.Decimal, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.Decimal), false) },
            { TypeKind.Int64, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.Int64), false) },
            { TypeKind.Int32, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.Int32), false) },
            { TypeKind.Int16, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.Int16), false) },
            { TypeKind.SByte, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.SByte), false) },
            { TypeKind.UInt64, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.UInt64), false) },
            { TypeKind.UInt32, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.UInt32), false) },
            { TypeKind.UInt16, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.UInt16), false) },
            { TypeKind.Byte, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.Byte), false) },
            { TypeKind.Double, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.Double), false) },
            { TypeKind.Single, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.Single), false) },
            { TypeKind.Boolean, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.Boolean), false) },
            { TypeKind.Binary, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.Binary), false) },
            { TypeKind.Guid, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.Guid), false) },
            { TypeKind.TimeSpan, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.TimeSpan), false) },
            { TypeKind.DateTimeOffset, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.DateTimeOffset), false) },
        };
        private static readonly Dictionary<TypeKind, GlobalTypeRefMetadata> _nullableAtomMap = new Dictionary<TypeKind, GlobalTypeRefMetadata> {
            { TypeKind.String, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.String), true) },
            { TypeKind.IgnoreCaseString, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.IgnoreCaseString), true) },
            { TypeKind.Char, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.Char), true) },
            { TypeKind.Decimal, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.Decimal), true) },
            { TypeKind.Int64, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.Int64), true) },
            { TypeKind.Int32, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.Int32), true) },
            { TypeKind.Int16, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.Int16), true) },
            { TypeKind.SByte, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.SByte), true) },
            { TypeKind.UInt64, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.UInt64), true) },
            { TypeKind.UInt32, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.UInt32), true) },
            { TypeKind.UInt16, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.UInt16), true) },
            { TypeKind.Byte, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.Byte), true) },
            { TypeKind.Double, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.Double), true) },
            { TypeKind.Single, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.Single), true) },
            { TypeKind.Boolean, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.Boolean), true) },
            { TypeKind.Binary, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.Binary), true) },
            { TypeKind.Guid, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.Guid), true) },
            { TypeKind.TimeSpan, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.TimeSpan), true) },
            { TypeKind.DateTimeOffset, new GlobalTypeRefMetadata(AtomMetadata.Get(TypeKind.DateTimeOffset), true) },
        };
    }
    public abstract class GlobalTypeMetadata : TypeMetadata {
        protected GlobalTypeMetadata(TypeKind kind, FullName fullName, FunctionMetadata[] functions)
            : base(kind) {
            FullName = fullName;
        }
        public readonly FullName FullName;
        protected FunctionMetadata[] _functions;//opt
    }
    public sealed class FunctionParameterMetadata : NamedLocalTypeMetadata {
        public FunctionParameterMetadata(string name, LocalTypeMetadata type)
            : base(name, type) {
        }
    }

    [Flags]
    public enum FunctionFlags {
        None = 0,
        Static = 1,
        Safe = 2,
    }

    public sealed class FunctionMetadata {
        public FunctionMetadata(string name, FunctionFlags flags, LocalTypeMetadata returnType, FunctionParameterMetadata[] parameters) {
            Name = name;
            Flags = flags;
            ReturnType = returnType;
            _parameters = parameters;
        }
        public readonly string Name;
        public readonly FunctionFlags Flags;
        public readonly LocalTypeMetadata ReturnType;
        private readonly FunctionParameterMetadata[] _parameters;//opt
        public bool IsStatic {
            get { return (Flags & FunctionFlags.Static) != 0; }
        }
        public bool IsSafe {
            get { return (Flags & FunctionFlags.Safe) != 0; }
        }

    }
    public sealed class AtomMetadata : GlobalTypeMetadata {
        public static AtomMetadata Get(TypeKind kind) {
            return _atomMap[kind];
        }
        private AtomMetadata(TypeKind kind) :
            base(kind, AtomExtensions.GetFullName(kind), null) {
        }
        private static readonly Dictionary<TypeKind, AtomMetadata> _atomMap;
        static AtomMetadata() {
            _atomMap = new Dictionary<TypeKind, AtomMetadata> {
                { TypeKind.String, new AtomMetadata(TypeKind.String) },
                { TypeKind.IgnoreCaseString, new AtomMetadata(TypeKind.IgnoreCaseString) },
                { TypeKind.Char, new AtomMetadata(TypeKind.Char) },
                { TypeKind.Decimal, new AtomMetadata(TypeKind.Decimal) },
                { TypeKind.Int64, new AtomMetadata(TypeKind.Int64) },
                { TypeKind.Int32, new AtomMetadata(TypeKind.Int32) },
                { TypeKind.Int16, new AtomMetadata(TypeKind.Int16) },
                { TypeKind.SByte, new AtomMetadata(TypeKind.SByte) },
                { TypeKind.UInt64, new AtomMetadata(TypeKind.UInt64) },
                { TypeKind.UInt32, new AtomMetadata(TypeKind.UInt32) },
                { TypeKind.UInt16, new AtomMetadata(TypeKind.UInt16) },
                { TypeKind.Byte, new AtomMetadata(TypeKind.Byte) },
                { TypeKind.Double, new AtomMetadata(TypeKind.Double) },
                { TypeKind.Single, new AtomMetadata(TypeKind.Single) },
                { TypeKind.Boolean, new AtomMetadata(TypeKind.Boolean) },
                { TypeKind.Binary, new AtomMetadata(TypeKind.Binary) },
                { TypeKind.Guid, new AtomMetadata(TypeKind.Guid) },
                { TypeKind.TimeSpan, new AtomMetadata(TypeKind.TimeSpan) },
                { TypeKind.DateTimeOffset, new AtomMetadata(TypeKind.DateTimeOffset) },
            };
            _atomMap[TypeKind.String]._functions = new FunctionMetadata[] {

            };

        }

    }

    public sealed class EnumMetadata : GlobalTypeMetadata {
        public EnumMetadata(FullName fullName, NameValuePair[] members)
            : base(TypeKind.Enum, fullName, null) {
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
        public ClassMetadata(FullName fullName, bool isAbstract, ClassMetadata baseClass, PropertyMetadata[] properties, FunctionMetadata[] functions, Type clrType)
            : base(TypeKind.Class, fullName, functions) {
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
                ClrConstructor = ReflectionExtensions.GetParameterlessConstructor(ti);
            }
            if (baseClass == null) {
                ClrMetadataProperty = ReflectionExtensions.GetProperty(ti, ReflectionExtensions.MetadataNameStr);
                ClrTextSpanProperty = ReflectionExtensions.GetProperty(ti, ReflectionExtensions.TextSpanNameStr);
            }
            ClrOnLoadingMethod = ti.GetDeclaredMethod(ReflectionExtensions.OnLoadingNameStr);
            ClrOnLoadedMethod = ti.GetDeclaredMethod(ReflectionExtensions.OnLoadedNameStr);
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
    public sealed class PropertyMetadata : NamedLocalTypeMetadata {
        public PropertyMetadata(string name, LocalTypeMetadata type, string clrName, bool isClrProperty)
            : base(name, type) {
            ClrName = clrName;
            IsClrProperty = isClrProperty;
        }
        public readonly string ClrName;
        public readonly bool IsClrProperty;
        public PropertyInfo ClrProperty { get; private set; }
        public FieldInfo ClrField { get; private set; }
        internal void GetClrPropertyOrField(TypeInfo ti) {
            if (IsClrProperty) {
                ClrProperty = ReflectionExtensions.GetProperty(ti, ClrName);
            }
            else {
                ClrField = ReflectionExtensions.GetField(ti, ClrName);
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

    public struct NameValuePair {
        public NameValuePair(string name, object value) {
            Name = name;
            Value = value;
        }
        public readonly string Name;
        public readonly object Value;
    }

}
