using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace CData {
    public abstract class ProgramMd {
        private static readonly Dictionary<FullName, GlobalTypeMd> _globalTypeMap = new Dictionary<FullName, GlobalTypeMd>();
        private static readonly HashSet<string> _uriSet = new HashSet<string>();
        public static T GetGlobalType<T>(FullName fullName) where T : GlobalTypeMd {
            GlobalTypeMd globalType;
            _globalTypeMap.TryGetValue(fullName, out globalType);
            return globalType as T;
        }
        internal static bool IsUriDefined(string uri) {
            return _uriSet.Contains(uri);
        }
        protected void AddGlobalTypes(GlobalTypeMd[] globalTypes) {
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
    public abstract class TypeMd {
        protected TypeMd(TypeKind kind) {
            Kind = kind;
        }
        public readonly TypeKind Kind;
    }
    public abstract class LocalTypeMd : TypeMd {
        protected LocalTypeMd(TypeKind kind, bool isNullable)
            : base(kind) {
            IsNullable = isNullable;
        }
        public readonly bool IsNullable;
    }
    public sealed class AnonymousClassMd : LocalTypeMd {
        public AnonymousClassMd(bool isNullable, AnonymousClassPropertyMd[] properties)
            : base(TypeKind.AnonymousClass, isNullable) {
            _properties = properties;
        }
        private readonly AnonymousClassPropertyMd[] _properties;//opt
        public AnonymousClassPropertyMd GetProperty(string name) {
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
    public abstract class NamedLocalTypeMd {
        protected NamedLocalTypeMd(string name, LocalTypeMd type) {
            Name = name;
            Type = type;
        }
        public readonly string Name;
        public readonly LocalTypeMd Type;
    }
    public sealed class AnonymousClassPropertyMd : NamedLocalTypeMd {
        public AnonymousClassPropertyMd(string name, LocalTypeMd type)
            : base(name, type) {
        }
    }
    public sealed class VoidMd : LocalTypeMd {
        public static readonly VoidMd Instance = new VoidMd();
        private VoidMd() : base(TypeKind.Void, false) { }
    }
    public abstract class CollectionBaseMd : LocalTypeMd {
        protected CollectionBaseMd(TypeKind kind, bool isNullable, LocalTypeMd itemOrValueType)
            : base(kind, isNullable) {
            ItemOrValueType = itemOrValueType;
        }
        public readonly LocalTypeMd ItemOrValueType;
    }
    public sealed class EnumerableMd : CollectionBaseMd {
        public EnumerableMd(bool isNullable, LocalTypeMd itemOrValueType)
            : base(TypeKind.Enumerable, isNullable, itemOrValueType) {
        }
    }

    //for List, SimpleSet, ObjectSet, Map
    public sealed class CollectionMd : CollectionBaseMd {
        public CollectionMd(TypeKind kind, bool isNullable,
            LocalTypeMd itemOrValueType, GlobalTypeRefMd mapKeyType, object objectSetKeySelector, Type clrType)
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
        public readonly GlobalTypeRefMd MapKeyType;//opt
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
    public sealed class GlobalTypeRefMd : LocalTypeMd {
        public GlobalTypeRefMd(GlobalTypeMd globalType, bool isNullable)
            : base(globalType.Kind, isNullable) {
            GlobalType = globalType;
        }
        public readonly GlobalTypeMd GlobalType;
        public static GlobalTypeRefMd GetAtom(TypeKind kind, bool isNullable = false) {
            return isNullable ? _nullableAtomMap[kind] : _atomMap[kind];
        }
        private static readonly Dictionary<TypeKind, GlobalTypeRefMd> _atomMap = new Dictionary<TypeKind, GlobalTypeRefMd> {
            { TypeKind.String, new GlobalTypeRefMd(AtomMd.Get(TypeKind.String), false) },
            { TypeKind.IgnoreCaseString, new GlobalTypeRefMd(AtomMd.Get(TypeKind.IgnoreCaseString), false) },
            { TypeKind.Char, new GlobalTypeRefMd(AtomMd.Get(TypeKind.Char), false) },
            { TypeKind.Decimal, new GlobalTypeRefMd(AtomMd.Get(TypeKind.Decimal), false) },
            { TypeKind.Int64, new GlobalTypeRefMd(AtomMd.Get(TypeKind.Int64), false) },
            { TypeKind.Int32, new GlobalTypeRefMd(AtomMd.Get(TypeKind.Int32), false) },
            { TypeKind.Int16, new GlobalTypeRefMd(AtomMd.Get(TypeKind.Int16), false) },
            { TypeKind.SByte, new GlobalTypeRefMd(AtomMd.Get(TypeKind.SByte), false) },
            { TypeKind.UInt64, new GlobalTypeRefMd(AtomMd.Get(TypeKind.UInt64), false) },
            { TypeKind.UInt32, new GlobalTypeRefMd(AtomMd.Get(TypeKind.UInt32), false) },
            { TypeKind.UInt16, new GlobalTypeRefMd(AtomMd.Get(TypeKind.UInt16), false) },
            { TypeKind.Byte, new GlobalTypeRefMd(AtomMd.Get(TypeKind.Byte), false) },
            { TypeKind.Double, new GlobalTypeRefMd(AtomMd.Get(TypeKind.Double), false) },
            { TypeKind.Single, new GlobalTypeRefMd(AtomMd.Get(TypeKind.Single), false) },
            { TypeKind.Boolean, new GlobalTypeRefMd(AtomMd.Get(TypeKind.Boolean), false) },
            { TypeKind.Binary, new GlobalTypeRefMd(AtomMd.Get(TypeKind.Binary), false) },
            { TypeKind.Guid, new GlobalTypeRefMd(AtomMd.Get(TypeKind.Guid), false) },
            { TypeKind.TimeSpan, new GlobalTypeRefMd(AtomMd.Get(TypeKind.TimeSpan), false) },
            { TypeKind.DateTimeOffset, new GlobalTypeRefMd(AtomMd.Get(TypeKind.DateTimeOffset), false) },
        };
        private static readonly Dictionary<TypeKind, GlobalTypeRefMd> _nullableAtomMap = new Dictionary<TypeKind, GlobalTypeRefMd> {
            { TypeKind.String, new GlobalTypeRefMd(AtomMd.Get(TypeKind.String), true) },
            { TypeKind.IgnoreCaseString, new GlobalTypeRefMd(AtomMd.Get(TypeKind.IgnoreCaseString), true) },
            { TypeKind.Char, new GlobalTypeRefMd(AtomMd.Get(TypeKind.Char), true) },
            { TypeKind.Decimal, new GlobalTypeRefMd(AtomMd.Get(TypeKind.Decimal), true) },
            { TypeKind.Int64, new GlobalTypeRefMd(AtomMd.Get(TypeKind.Int64), true) },
            { TypeKind.Int32, new GlobalTypeRefMd(AtomMd.Get(TypeKind.Int32), true) },
            { TypeKind.Int16, new GlobalTypeRefMd(AtomMd.Get(TypeKind.Int16), true) },
            { TypeKind.SByte, new GlobalTypeRefMd(AtomMd.Get(TypeKind.SByte), true) },
            { TypeKind.UInt64, new GlobalTypeRefMd(AtomMd.Get(TypeKind.UInt64), true) },
            { TypeKind.UInt32, new GlobalTypeRefMd(AtomMd.Get(TypeKind.UInt32), true) },
            { TypeKind.UInt16, new GlobalTypeRefMd(AtomMd.Get(TypeKind.UInt16), true) },
            { TypeKind.Byte, new GlobalTypeRefMd(AtomMd.Get(TypeKind.Byte), true) },
            { TypeKind.Double, new GlobalTypeRefMd(AtomMd.Get(TypeKind.Double), true) },
            { TypeKind.Single, new GlobalTypeRefMd(AtomMd.Get(TypeKind.Single), true) },
            { TypeKind.Boolean, new GlobalTypeRefMd(AtomMd.Get(TypeKind.Boolean), true) },
            { TypeKind.Binary, new GlobalTypeRefMd(AtomMd.Get(TypeKind.Binary), true) },
            { TypeKind.Guid, new GlobalTypeRefMd(AtomMd.Get(TypeKind.Guid), true) },
            { TypeKind.TimeSpan, new GlobalTypeRefMd(AtomMd.Get(TypeKind.TimeSpan), true) },
            { TypeKind.DateTimeOffset, new GlobalTypeRefMd(AtomMd.Get(TypeKind.DateTimeOffset), true) },
        };
    }
    public abstract class GlobalTypeMd : TypeMd {
        protected GlobalTypeMd(TypeKind kind, FullName fullName, PropertyMd[] properties, FunctionMd[] functions)
            : base(kind) {
            FullName = fullName;
            _properties = properties;
            _functions = functions;
        }
        public readonly FullName FullName;
        protected PropertyMd[] _properties;//opt
        protected FunctionMd[] _functions;//opt

    }

    [Flags]
    public enum PropertyFlags {
        None = 0,
        Index = 1,
        Static = 2,
        ReadOnly = 4,
        Extension = 8,
    }
    public class PropertyMd : NamedLocalTypeMd {
        public PropertyMd(string name, LocalTypeMd type, PropertyFlags flags, ParameterMd[] parameters = null)
            : base(name, type) {
            Flags = flags;
            _parameters = parameters;
        }
        public readonly PropertyFlags Flags;
        private readonly ParameterMd[] _parameters;//opt
        public bool IsIndex {
            get { return (Flags & PropertyFlags.Index) != 0; }
        }
        public bool IsStatic {
            get { return (Flags & PropertyFlags.Static) != 0; }
        }
        public bool IsReadOnly {
            get { return (Flags & PropertyFlags.ReadOnly) != 0; }
        }
        public int ParameterCount {
            get { return _parameters == null ? 0 : _parameters.Length; }
        }

    }

    public sealed class ParameterMd : NamedLocalTypeMd {
        public ParameterMd(string name, LocalTypeMd type)
            : base(name, type) {
        }
    }


    [Flags]
    public enum FunctionFlags {
        None = 0,
        Static = 1,
        Unsafe = 2,
        Extension = 4,
    }

    public sealed class FunctionMd {
        public FunctionMd(string name, FunctionFlags flags, LocalTypeMd returnType, ParameterMd[] parameters) {
            Name = name;
            Flags = flags;
            ReturnType = returnType;
            _parameters = parameters;
        }
        public readonly string Name;
        public readonly FunctionFlags Flags;
        public readonly LocalTypeMd ReturnType;
        private readonly ParameterMd[] _parameters;//opt
        public bool IsStatic {
            get { return (Flags & FunctionFlags.Static) != 0; }
        }
        public bool IsUnsafe {
            get { return (Flags & FunctionFlags.Unsafe) != 0; }
        }
        public int ParameterCount {
            get { return _parameters == null ? 0 : _parameters.Length; }
        }

    }

    public sealed class AtomMd : GlobalTypeMd {
        public static AtomMd Get(TypeKind kind) {
            return _map[kind];
        }
        public static AtomMd Get(string name) {
            TypeKind kind;
            if (_nameMap.TryGetValue(name, out kind)) {
                return _map[kind];
            }
            return null;
        }
        private AtomMd(TypeKind kind)
            : base(kind, AtomExtensions.GetFullName(kind), null, null) {
        }
        private static readonly Dictionary<string, TypeKind> _nameMap;
        private static readonly Dictionary<TypeKind, AtomMd> _map;
        static AtomMd() {
            _nameMap = new Dictionary<string, TypeKind> {
                { TypeKind.String.ToString(), TypeKind.String },
                { TypeKind.IgnoreCaseString.ToString(), TypeKind.IgnoreCaseString },
                { TypeKind.Char.ToString(), TypeKind.Char },
                { TypeKind.Decimal.ToString(), TypeKind.Decimal },
                { TypeKind.Int64.ToString(), TypeKind.Int64 },
                { TypeKind.Int32.ToString(), TypeKind.Int32 },
                { TypeKind.Int16.ToString(), TypeKind.Int16 },
                { TypeKind.SByte.ToString(), TypeKind.SByte },
                { TypeKind.UInt64.ToString(), TypeKind.UInt64 },
                { TypeKind.UInt32.ToString(), TypeKind.UInt32 },
                { TypeKind.UInt16.ToString(), TypeKind.UInt16 },
                { TypeKind.Byte.ToString(), TypeKind.Byte },
                { TypeKind.Double.ToString(), TypeKind.Double },
                { TypeKind.Single.ToString(), TypeKind.Single },
                { TypeKind.Boolean.ToString(), TypeKind.Boolean },
                { TypeKind.Binary.ToString(), TypeKind.Binary },
                { TypeKind.Guid.ToString(), TypeKind.Guid },
                { TypeKind.TimeSpan.ToString(), TypeKind.TimeSpan },
                { TypeKind.DateTimeOffset.ToString(), TypeKind.DateTimeOffset },
            };
            var map = new Dictionary<TypeKind, AtomMd> {
                { TypeKind.String, new AtomMd(TypeKind.String) },
                { TypeKind.IgnoreCaseString, new AtomMd(TypeKind.IgnoreCaseString) },
                { TypeKind.Char, new AtomMd(TypeKind.Char) },
                { TypeKind.Decimal, new AtomMd(TypeKind.Decimal) },
                { TypeKind.Int64, new AtomMd(TypeKind.Int64) },
                { TypeKind.Int32, new AtomMd(TypeKind.Int32) },
                { TypeKind.Int16, new AtomMd(TypeKind.Int16) },
                { TypeKind.SByte, new AtomMd(TypeKind.SByte) },
                { TypeKind.UInt64, new AtomMd(TypeKind.UInt64) },
                { TypeKind.UInt32, new AtomMd(TypeKind.UInt32) },
                { TypeKind.UInt16, new AtomMd(TypeKind.UInt16) },
                { TypeKind.Byte, new AtomMd(TypeKind.Byte) },
                { TypeKind.Double, new AtomMd(TypeKind.Double) },
                { TypeKind.Single, new AtomMd(TypeKind.Single) },
                { TypeKind.Boolean, new AtomMd(TypeKind.Boolean) },
                { TypeKind.Binary, new AtomMd(TypeKind.Binary) },
                { TypeKind.Guid, new AtomMd(TypeKind.Guid) },
                { TypeKind.TimeSpan, new AtomMd(TypeKind.TimeSpan) },
                { TypeKind.DateTimeOffset, new AtomMd(TypeKind.DateTimeOffset) },
            };
            _map = map;
            map[TypeKind.String]._properties = new PropertyMd[] {
                //char this[int index] { get; }
                new PropertyMd(null, GlobalTypeRefMd.GetAtom(TypeKind.Char), PropertyFlags.Index | PropertyFlags.ReadOnly,
                    new[] { new ParameterMd("index", GlobalTypeRefMd.GetAtom(TypeKind.Int32)) }),
                //int Length { get; }
                new PropertyMd("Length", GlobalTypeRefMd.GetAtom(TypeKind.Int32), PropertyFlags.ReadOnly),

            };
            map[TypeKind.String]._functions = new FunctionMd[] {
                //bool Contains(string value);
                new FunctionMd("Contains", FunctionFlags.None, GlobalTypeRefMd.GetAtom(TypeKind.Boolean), 
                    new[] { new ParameterMd("value", GlobalTypeRefMd.GetAtom(TypeKind.String)) }),
                //bool StartsWith(string value);
                new FunctionMd("StartsWith", FunctionFlags.None, GlobalTypeRefMd.GetAtom(TypeKind.Boolean), 
                    new[] { new ParameterMd("value", GlobalTypeRefMd.GetAtom(TypeKind.String)) }),
                //bool EndsWith(string value);
                new FunctionMd("EndsWith", FunctionFlags.None, GlobalTypeRefMd.GetAtom(TypeKind.Boolean), 
                    new[] { new ParameterMd("value", GlobalTypeRefMd.GetAtom(TypeKind.String)) }),
                //string Substring(int startIndex);
                new FunctionMd("Substring", FunctionFlags.None, GlobalTypeRefMd.GetAtom(TypeKind.String), 
                    new[] { new ParameterMd("startIndex", GlobalTypeRefMd.GetAtom(TypeKind.Int32)) }),
                //string Substring(int startIndex, int length);
                new FunctionMd("Substring", FunctionFlags.None, GlobalTypeRefMd.GetAtom(TypeKind.String), 
                    new[] { new ParameterMd("startIndex", GlobalTypeRefMd.GetAtom(TypeKind.Int32)),
                            new ParameterMd("length", GlobalTypeRefMd.GetAtom(TypeKind.Int32))
                    }),

            };

        }

    }

    public sealed class EnumPropertyMd : PropertyMd {
        public EnumPropertyMd(string name, LocalTypeMd type, object value)
            : base(name, type, PropertyFlags.Static | PropertyFlags.ReadOnly) {
            Value = value;
        }
        public readonly object Value;
    }
    public sealed class EnumMd : GlobalTypeMd {
        public EnumMd(FullName fullName, EnumPropertyMd[] properties)
            : base(TypeKind.Enum, fullName, properties, null) {
            _enumProperties = properties;
        }
        private readonly EnumPropertyMd[] _enumProperties;
        public object GetPropertyValue(string name) {
            var props = _enumProperties;
            if (props != null) {
                var length = props.Length;
                for (var i = 0; i < length; ++i) {
                    if (props[i].Name == name) {
                        return props[i].Value;
                    }
                }
            }
            return null;
        }
        public string GetPropertyName(object value) {
            var props = _enumProperties;
            if (props != null) {
                var length = props.Length;
                for (var i = 0; i < length; ++i) {
                    if (props[i].Value.Equals(value)) {
                        return props[i].Name;
                    }
                }
            }
            return null;
        }
    }
    public sealed class ClassMd : GlobalTypeMd {
        public ClassMd(FullName fullName, bool isAbstract, ClassMd baseClass,
            ClassPropertyMd[] properties, FunctionMd[] functions, Type clrType)
            : base(TypeKind.Class, fullName, properties, functions) {
            IsAbstract = isAbstract;
            BaseClass = baseClass;
            _classProperties = properties;
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
        public readonly ClassMd BaseClass;
        private readonly ClassPropertyMd[] _classProperties;
        public readonly Type ClrType;
        public readonly ConstructorInfo ClrConstructor;//for non abstract class
        public readonly PropertyInfo ClrMetadataProperty;//for top class
        public readonly PropertyInfo ClrTextSpanProperty;//for top class
        public readonly MethodInfo ClrOnLoadingMethod;//opt
        public readonly MethodInfo ClrOnLoadedMethod;//opt
        public bool IsEqualToOrDeriveFrom(ClassMd other) {
            for (var cls = this; cls != null; cls = cls.BaseClass) {
                if (cls == other) {
                    return true;
                }
            }
            return false;
        }
        public ClassPropertyMd GetPropertyInHierarchy(string name) {
            var props = _classProperties;
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
        public void GetPropertiesInHierarchy(ref List<ClassPropertyMd> propList) {
            if (BaseClass != null) {
                BaseClass.GetPropertiesInHierarchy(ref propList);
            }
            if (_classProperties != null) {
                if (propList == null) {
                    propList = new List<ClassPropertyMd>(_classProperties);
                }
                else {
                    propList.AddRange(_classProperties);
                }
            }
        }
        public IEnumerable<ClassPropertyMd> GetPropertiesInHierarchy() {
            if (BaseClass == null) {
                return _classProperties;
            }
            if (_classProperties == null) {
                return BaseClass.GetPropertiesInHierarchy();
            }
            List<ClassPropertyMd> propList = null;
            GetPropertiesInHierarchy(ref propList);
            return propList;
        }
        public object CreateInstance() {
            //FormatterServices.GetUninitializedObject()
            return ClrConstructor.Invoke(null);
        }
        public ClassMd GetMetadata(object obj) {
            var md = this;
            for (; md.BaseClass != null; md = md.BaseClass) ;
            return (ClassMd)md.ClrMetadataProperty.GetValue(obj);
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
    public sealed class ClassPropertyMd : PropertyMd {
        public ClassPropertyMd(string name, LocalTypeMd type, string clrName, bool isClrProperty)
            : base(name, type, PropertyFlags.None) {
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
