using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace CData {
    public abstract class ProgramMetadata {
        private volatile Dictionary<FullName, ClassTypeMetadata> _objectMap;
        protected abstract Dictionary<FullName, ClassTypeMetadata> GetObjectMap();
        public ClassTypeMetadata TryGetObject(FullName fullName) {
            ClassTypeMetadata obj;
            var map = _objectMap ?? (_objectMap = GetObjectMap());
            if (map.TryGetValue(fullName, out obj)) {
                return obj;
            }
            return null;
        }
    }

    public enum TypeKind : byte {
        None = 0,
        Class,
        List,
        AtomSet,
        ObjectSet,
        Map,
        //DO NOT CHANGE THE ORDER
        String,
        IgnoreCaseString,
        Decimal,
        Int64,
        Int32,
        Int16,
        SByte,
        UInt64,
        UInt32,
        UInt16,
        Byte,
        Double,
        Single,
        Boolean,
        Binary,
        Guid,
        TimeSpan,
        DateTimeOffset,
    }
    public abstract class TypeMetadata {
        protected TypeMetadata(TypeKind kind, bool isNullable, Type clrType) {
            Kind = kind;
            IsNullable = isNullable;
            ClrType = clrType;
        }
        public readonly TypeKind Kind;
        public readonly bool IsNullable;
        public readonly Type ClrType;
        public bool IsClass {
            get {
                return Kind == TypeKind.Class;
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
        public bool IsSet {
            get {
                return Kind == TypeKind.AtomSet || Kind == TypeKind.ObjectSet;
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
    public sealed class AtomTypeMetadata : TypeMetadata {
        public static AtomTypeMetadata Get(TypeKind kind, bool isNullable) {
            return isNullable ? _nullableMap[kind] : _map[kind];
        }
        private AtomTypeMetadata(TypeKind kind, bool isNullable, Type clrType)
            : base(kind, isNullable, clrType) {
        }
        private static readonly Dictionary<TypeKind, AtomTypeMetadata> _map = new Dictionary<TypeKind, AtomTypeMetadata> {
            { TypeKind.String, new AtomTypeMetadata(TypeKind.String , false, typeof(string)) },
            { TypeKind.IgnoreCaseString, new AtomTypeMetadata(TypeKind.IgnoreCaseString , false, typeof(IgnoreCaseString)) },
            { TypeKind.Decimal, new AtomTypeMetadata(TypeKind.Decimal , false, typeof(decimal)) },
            { TypeKind.Int64, new AtomTypeMetadata(TypeKind.Int64 , false, typeof(long)) },
            { TypeKind.Int32, new AtomTypeMetadata(TypeKind.Int32 , false, typeof(int)) },
            { TypeKind.Int16, new AtomTypeMetadata(TypeKind.Int16 , false, typeof(short)) },
            { TypeKind.SByte, new AtomTypeMetadata(TypeKind.SByte , false, typeof(sbyte)) },
            { TypeKind.UInt64, new AtomTypeMetadata(TypeKind.UInt64 , false, typeof(ulong)) },
            { TypeKind.UInt32, new AtomTypeMetadata(TypeKind.UInt32 , false, typeof(uint)) },
            { TypeKind.UInt16, new AtomTypeMetadata(TypeKind.UInt16 , false, typeof(ushort)) },
            { TypeKind.Byte, new AtomTypeMetadata(TypeKind.Byte , false, typeof(byte)) },
            { TypeKind.Double, new AtomTypeMetadata(TypeKind.Double , false, typeof(double)) },
            { TypeKind.Single, new AtomTypeMetadata(TypeKind.Single , false, typeof(float)) },
            { TypeKind.Boolean, new AtomTypeMetadata(TypeKind.Boolean , false, typeof(bool)) },
            { TypeKind.Binary, new AtomTypeMetadata(TypeKind.Binary , false, typeof(BinaryValue)) },
            { TypeKind.Guid, new AtomTypeMetadata(TypeKind.Guid , false, typeof(Guid)) },
            { TypeKind.TimeSpan, new AtomTypeMetadata(TypeKind.TimeSpan , false, typeof(TimeSpan)) },
            { TypeKind.DateTimeOffset, new AtomTypeMetadata(TypeKind.DateTimeOffset , false, typeof(DateTimeOffset)) },
        };
        private static readonly Dictionary<TypeKind, AtomTypeMetadata> _nullableMap = new Dictionary<TypeKind, AtomTypeMetadata> {
            { TypeKind.String, new AtomTypeMetadata(TypeKind.String , true, typeof(string)) },
            { TypeKind.IgnoreCaseString, new AtomTypeMetadata(TypeKind.IgnoreCaseString , true, typeof(IgnoreCaseString)) },
            { TypeKind.Decimal, new AtomTypeMetadata(TypeKind.Decimal , true, typeof(decimal?)) },
            { TypeKind.Int64, new AtomTypeMetadata(TypeKind.Int64 , true, typeof(long?)) },
            { TypeKind.Int32, new AtomTypeMetadata(TypeKind.Int32 , true, typeof(int?)) },
            { TypeKind.Int16, new AtomTypeMetadata(TypeKind.Int16 , true, typeof(short?)) },
            { TypeKind.SByte, new AtomTypeMetadata(TypeKind.SByte , true, typeof(sbyte?)) },
            { TypeKind.UInt64, new AtomTypeMetadata(TypeKind.UInt64 , true, typeof(ulong?)) },
            { TypeKind.UInt32, new AtomTypeMetadata(TypeKind.UInt32 , true, typeof(uint?)) },
            { TypeKind.UInt16, new AtomTypeMetadata(TypeKind.UInt16 , true, typeof(ushort?)) },
            { TypeKind.Byte, new AtomTypeMetadata(TypeKind.Byte , true, typeof(byte?)) },
            { TypeKind.Double, new AtomTypeMetadata(TypeKind.Double , true, typeof(double?)) },
            { TypeKind.Single, new AtomTypeMetadata(TypeKind.Single , true, typeof(float?)) },
            { TypeKind.Boolean, new AtomTypeMetadata(TypeKind.Boolean , true, typeof(bool?)) },
            { TypeKind.Binary, new AtomTypeMetadata(TypeKind.Binary , true, typeof(BinaryValue)) },
            { TypeKind.Guid, new AtomTypeMetadata(TypeKind.Guid , true, typeof(Guid?)) },
            { TypeKind.TimeSpan, new AtomTypeMetadata(TypeKind.TimeSpan , true, typeof(TimeSpan?)) },
            { TypeKind.DateTimeOffset, new AtomTypeMetadata(TypeKind.DateTimeOffset , true, typeof(DateTimeOffset?)) },
        };
    }
    public sealed class CollectionTypeMetadata : TypeMetadata {
        public CollectionTypeMetadata(TypeKind kind, bool isNullable, Type clrType,
            TypeMetadata itemOrValueType, AtomTypeMetadata mapKeyType, object objectSetKeySelector)
            : base(kind, isNullable, clrType) {
            ItemOrValueType = itemOrValueType;
            MapKeyType = mapKeyType;
            ObjectSetKeySelector = objectSetKeySelector;
            var ti = clrType.GetTypeInfo();
            ClrConstructor = Extensions.GetConstructor(ti, objectSetKeySelector == null ? 0 : 1);
            ClrAddMethod = Extensions.GetMethodInHierarchy(ti, "Add");
            if (kind == TypeKind.Map) {
                ClrContainsKeyMethod = Extensions.GetMethodInHierarchy(ti, "ContainsKey");
                ClrKeysProperty = Extensions.GetPropertyInHierarchy(ti, "Keys");
                ClrValuesProperty = Extensions.GetPropertyInHierarchy(ti, "Values");
            }
        }
        public readonly TypeMetadata ItemOrValueType;
        public readonly AtomTypeMetadata MapKeyType;//opt
        public readonly object ObjectSetKeySelector;//opt
        public readonly ConstructorInfo ClrConstructor;
        public readonly MethodInfo ClrAddMethod;
        public readonly MethodInfo ClrContainsKeyMethod;//for map
        public readonly PropertyInfo ClrKeysProperty;//for map
        public readonly PropertyInfo ClrValuesProperty;//for map
        public object CreateInstance() {
            return ClrConstructor.Invoke(ObjectSetKeySelector == null ? null : new object[] { ObjectSetKeySelector });
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
    public sealed class ClassTypeMetadata : TypeMetadata {
        public ClassTypeMetadata(bool isNullable, Type clrType,
            FullName fullName, bool isAbstract, ClassTypeMetadata baseClass, PropertyMetadata[] properties,
            string onLoadingName, string onLoadedName, ProgramMetadata program)
            : base(TypeKind.Class, isNullable, clrType) {
            FullName = fullName;
            IsAbstract = isAbstract;
            BaseClass = baseClass;
            _properties = properties;
            Program = program;
            var ti = clrType.GetTypeInfo();
            if (!isAbstract) {
                ClrConstructor = Extensions.GetConstructor(ti, 0);
            }
            if (baseClass == null) {
                ClrTextSpanProperty = Extensions.GetPropertyInHierarchy(ti, "__TextSpan");
            }
            if (onLoadingName != null) {
                ClrOnLoadingMethod = Extensions.GetMethod(ti, onLoadingName);
            }
            if (onLoadedName != null) {
                ClrOnLoadedMethod = Extensions.GetMethod(ti, onLoadedName);
            }
            if (properties != null) {
                foreach (var prop in properties) {
                    prop.GetClrPropertyOrField(ti);
                }
            }
        }
        public readonly FullName FullName;
        public readonly bool IsAbstract;
        public readonly ClassTypeMetadata BaseClass;
        private readonly PropertyMetadata[] _properties;
        public readonly ProgramMetadata Program;
        public readonly ConstructorInfo ClrConstructor;//for non abstract class
        public readonly PropertyInfo ClrTextSpanProperty;//for top class
        public readonly MethodInfo ClrOnLoadingMethod;//opt
        public readonly MethodInfo ClrOnLoadedMethod;//opt
        public bool IsEqualToOrDeriveFrom(ClassTypeMetadata other) {
            if (other == null) throw new ArgumentNullException("other");
            for (var info = this; info != null; info = info.BaseClass) {
                if (info == other) {
                    return true;
                }
            }
            return false;
        }
        public PropertyMetadata GetProperty(string name) {
            if (_properties != null) {
                foreach (var prop in _properties) {
                    if (prop.Name == name) {
                        return prop;
                    }
                }
            }
            if (BaseClass != null) {
                return BaseClass.GetProperty(name);
            }
            return null;
        }
        public void GetAllProperties(ref List<PropertyMetadata> propList) {
            if (BaseClass != null) {
                BaseClass.GetAllProperties(ref propList);
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
        public IEnumerable<PropertyMetadata> GetAllProperties() {
            if (BaseClass == null) {
                return _properties;
            }
            if (_properties == null) {
                return BaseClass.GetAllProperties();
            }
            List<PropertyMetadata> propList = null;
            GetAllProperties(ref propList);
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
                ClrProperty = Extensions.GetPropertyInHierarchy(ti, ClrName);
            }
            else {
                ClrField = Extensions.GetFieldInHierarchy(ti, ClrName);
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

    //public static class XXX {
    //    public static bool ObjectValue(Parser parser) {
    //    }
    //}
    //public class Class0 { }
    //interface IItf0 { }
    //abstract partial class Class1 : Class0 {
    //    public int Id { get; protected set; }

    //    //bool OnLoading( DiagContext context);
    //    partial void OnLoaded(ref bool success, DiagContext context);

    //    //partial void OnLoading() {
    //    //    throw new NotImplementedException();
    //    //}

    //    public static bool __TryCreate() {

    //        return true;
    //    }

    //}
    //public partial class Class1 : IItf0 {

    //}

}
