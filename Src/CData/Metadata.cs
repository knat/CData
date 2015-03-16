using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CData {
    public abstract class ProgramMetadata {
        //
        private volatile Dictionary<FullName, ClassTypeMetadata> _classTypeMap;
        protected abstract Dictionary<FullName, ClassTypeMetadata> GetClassTypeMap();
        public ClassTypeMetadata TryGetClassType(FullName fullName) {
            ClassTypeMetadata info;
            var map = _classTypeMap ?? (_classTypeMap = GetClassTypeMap());
            if (map.TryGetValue(fullName, out info)) {
                return info;
            }
            return null;
        }
    }

    public enum TypeKind : byte {
        None = 0,
        Class,
        List,
        Set,
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
        protected TypeMetadata(TypeKind kind, bool isNullable, string displayName, Type clrType) {
            Kind = kind;
            IsNullable = isNullable;
            DisplayName = displayName;
            ClrType = clrType;
        }
        public readonly TypeKind Kind;
        public readonly bool IsNullable;
        public readonly string DisplayName;
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
        public bool IsSet {
            get {
                return Kind == TypeKind.Set;
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
        public AtomTypeMetadata(TypeKind kind, bool isNullable, string displayName, Type clrType)
            : base(kind, isNullable, displayName, isNullable ? _nullableClrTypeMap[kind] : _clrTypeMap[kind]) {
        }
        private static readonly Dictionary<TypeKind, Type> _clrTypeMap = new Dictionary<TypeKind, Type> {
            { TypeKind.String, typeof(string) },
            { TypeKind.IgnoreCaseString, typeof(string) },
            { TypeKind.Decimal, typeof(decimal) },
            { TypeKind.Int64, typeof(long) },
            { TypeKind.Int32, typeof(int) },
            { TypeKind.Int16, typeof(short) },
            { TypeKind.SByte, typeof(sbyte) },
            { TypeKind.UInt64, typeof(ulong) },
            { TypeKind.UInt32, typeof(uint) },
            { TypeKind.UInt16, typeof(ushort) },
            { TypeKind.Byte, typeof(byte) },
            { TypeKind.Double, typeof(double) },
            { TypeKind.Single, typeof(float) },
            { TypeKind.Boolean, typeof(bool) },
            { TypeKind.Binary, typeof(BinaryValue) },
            { TypeKind.Guid, typeof(Guid) },
            { TypeKind.TimeSpan, typeof(TimeSpan) },
            { TypeKind.DateTimeOffset, typeof(DateTimeOffset) },
        };
        private static readonly Dictionary<TypeKind, Type> _nullableClrTypeMap = new Dictionary<TypeKind, Type> {
            { TypeKind.String, typeof(string) },
            { TypeKind.IgnoreCaseString, typeof(string) },
            { TypeKind.Decimal, typeof(decimal?) },
            { TypeKind.Int64, typeof(long?) },
            { TypeKind.Int32, typeof(int?) },
            { TypeKind.Int16, typeof(short?) },
            { TypeKind.SByte, typeof(sbyte?) },
            { TypeKind.UInt64, typeof(ulong?) },
            { TypeKind.UInt32, typeof(uint?) },
            { TypeKind.UInt16, typeof(ushort?) },
            { TypeKind.Byte, typeof(byte?) },
            { TypeKind.Double, typeof(double?) },
            { TypeKind.Single, typeof(float?) },
            { TypeKind.Boolean, typeof(bool?) },
            { TypeKind.Binary, typeof(BinaryValue) },
            { TypeKind.Guid, typeof(Guid?) },
            { TypeKind.TimeSpan, typeof(TimeSpan?) },
            { TypeKind.DateTimeOffset, typeof(DateTimeOffset?) },
        };
    }
    [Flags]
    public enum CollectionTypeFlags {
        None = 0,
        NeedIgnoreCaseComparer = 1,
        ClrMap = 2,
    }
    public abstract class CollectionTypeMetadata : TypeMetadata {
        protected CollectionTypeMetadata(TypeKind kind, bool isNullable, string displayName, Type clrType,
            CollectionTypeFlags flags)
            : base(kind, isNullable, displayName, clrType) {
            Flags = flags;
            var ti = clrType.GetTypeInfo();
            if (NeedIgnoreCaseComparer) {
                ClrConstructor = Extensions.GetIEqualityComparerOfStringConstructor(ti);
            }
            else {
                ClrConstructor = Extensions.GetParameterlessConstructor(ti);
            }
            if (IsClrMap) {
                ClrContainsKeyMethod = ti.GetDeclaredMethod("ContainsKey");
            }
            ClrAddMethod = ti.GetDeclaredMethod("Add");
        }
        public readonly CollectionTypeFlags Flags;
        public readonly ConstructorInfo ClrConstructor;
        public readonly MethodInfo ClrContainsKeyMethod;//for clr map
        public readonly MethodInfo ClrAddMethod;
        public bool NeedIgnoreCaseComparer {
            get {
                return (Flags & CollectionTypeFlags.NeedIgnoreCaseComparer) != 0;
            }
        }
        public bool IsClrMap {
            get {
                return (Flags & CollectionTypeFlags.ClrMap) != 0;
            }
        }
        public object CreateInstance() {
            if (NeedIgnoreCaseComparer) {
                return ClrConstructor.Invoke(new object[] { StringComparer.OrdinalIgnoreCase });
            }
            return ClrConstructor.Invoke(null);
        }
        public bool InvokeAddMethod(object obj, object key, object value) {
            if ((bool)ClrContainsKeyMethod.Invoke(obj, new object[] { key })) {
                return false;
            }
            ClrAddMethod.Invoke(obj, new object[] { key, value });
            return true;
        }
    }
    public abstract class ItemCollectionTypeMetadata : CollectionTypeMetadata {
        protected ItemCollectionTypeMetadata(TypeKind kind, bool isNullable, string displayName, Type clrType,
            CollectionTypeFlags flags, TypeMetadata itemType)
            : base(kind, isNullable, displayName, clrType, flags) {
            ItemType = itemType;
        }
        public readonly TypeMetadata ItemType;
    }
    public sealed class ListTypeMetadata : ItemCollectionTypeMetadata {
        public ListTypeMetadata(bool isNullable, string displayName, TypeMetadata itemType)
            : base(TypeKind.List, isNullable, displayName, Extensions.ListType.MakeGenericType(itemType.ClrType), CollectionTypeFlags.None, itemType) {
        }
        public void InvokeAddMethod(object obj, object item) {
            ClrAddMethod.Invoke(obj, new object[] { item });
        }
    }
    public sealed class SetTypeMetadata : ItemCollectionTypeMetadata {
        public SetTypeMetadata(bool isNullable, string displayName, TypeMetadata itemType, AtomTypeMetadata classKeyType)
            : base(TypeKind.Set, isNullable, displayName,
            classKeyType == null ? Extensions.HashSetType.MakeGenericType(itemType.ClrType) :
                Extensions.DictionaryType.MakeGenericType(classKeyType.ClrType, itemType.ClrType),
            ((itemType.Kind == TypeKind.IgnoreCaseString || (classKeyType != null && classKeyType.Kind == TypeKind.IgnoreCaseString)) ? CollectionTypeFlags.NeedIgnoreCaseComparer : CollectionTypeFlags.None) |
                (classKeyType != null ? CollectionTypeFlags.ClrMap : CollectionTypeFlags.None),
            itemType) {
            ClassKeyType = classKeyType;
        }
        public readonly AtomTypeMetadata ClassKeyType;//opt
        public bool InvokeAddMethod(object obj, object item) {
            return (bool)ClrAddMethod.Invoke(obj, new object[] { item });
        }
    }
    public sealed class MapTypeMetadata : CollectionTypeMetadata {
        public MapTypeMetadata(bool isNullable, string displayName, AtomTypeMetadata keyType, TypeMetadata valueType)
            : base(TypeKind.Map, isNullable, displayName,
            Extensions.DictionaryType.MakeGenericType(keyType.ClrType, valueType.ClrType),
            CollectionTypeFlags.ClrMap | ((keyType.Kind == TypeKind.IgnoreCaseString) ? CollectionTypeFlags.NeedIgnoreCaseComparer : CollectionTypeFlags.None)) {
            KeyType = keyType;
            ValueType = valueType;
        }
        public readonly AtomTypeMetadata KeyType;
        public readonly TypeMetadata ValueType;
    }
    public sealed class ClassTypeMetadata : TypeMetadata {
        public ClassTypeMetadata(bool isNullable, string displayName, Type clrType, ProgramMetadata program,
            ClassTypeMetadata baseClass, PropertyMetadata[] properties)
            : base(TypeKind.Class, isNullable, displayName, clrType) {
            Program = program;
            BaseClass = baseClass;
            _properties = properties;
            var ti = clrType.GetTypeInfo();
            if (!(IsAbstract = ti.IsAbstract)) {
                if ((ClrConstructor = Extensions.GetParameterlessConstructor(ti)) == null) {
                    throw new InvalidOperationException("Parameterless constructor required for class: " + ti.FullName);
                }
            }
            ClrOnLoadingMethod = Extensions.GetClrMethod(ti, "OnLoading", Extensions.BoolRefType, Extensions.DiagContextType);
            ClrOnLoadedMethod = Extensions.GetClrMethod(ti, "OnLoaded", Extensions.BoolRefType, Extensions.DiagContextType);
            if (properties != null) {
                foreach (var prop in properties) {
                    prop.GetClrPropertyOrField(ti);
                }
            }
        }
        public readonly ProgramMetadata Program;
        public readonly ClassTypeMetadata BaseClass;
        public readonly bool IsAbstract;
        private readonly PropertyMetadata[] _properties;
        public readonly ConstructorInfo ClrConstructor;//for non abstract class
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
        public object CreateInstance() {
            return ClrConstructor.Invoke(null);
        }
        public bool InvokeOnLoadMethod(bool isLoading, object obj, DiagContext context) {
            if (BaseClass != null) {
                if (!BaseClass.InvokeOnLoadMethod(isLoading, obj, context)) {
                    return false;
                }
            }
            var mi = isLoading ? ClrOnLoadingMethod : ClrOnLoadedMethod;
            if (mi != null) {
                var paras = new object[2] { Extensions.BoolTrueValue, context };
                mi.Invoke(obj, paras);
                if (!(bool)paras[0]) {
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
                if ((ClrProperty = Extensions.GetClrProperty(ti, ClrName)) == null) {
                    throw new InvalidOperationException("Cannot get CLR property: " + ClrName);
                }
            }
            else {
                if ((ClrField = Extensions.GetClrField(ti, ClrName)) == null) {
                    throw new InvalidOperationException("Cannot get CLR field: " + ClrName);
                }
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
    public class Class0 { }
    interface IItf0 { }
    abstract partial class Class1 : Class0 {
        public int Id { get; protected set; }

        partial void OnLoading(ref bool success, DiagContext context);
        partial void OnLoaded(ref bool success, DiagContext context);

        //partial void OnLoading() {
        //    throw new NotImplementedException();
        //}

        public static bool __TryCreate() {

            return true;
        }

    }
    public partial class Class1 : IItf0 {

    }

}
