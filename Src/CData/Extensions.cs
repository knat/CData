using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace CData {
    public static class Extensions {
        public const string SystemUri = "https://github.com/knat/CData";
        //
        //
        //
        private const int _stringBuilderCount = 4;
        private const int _stringBuilderCapacity = 128;
        private static readonly StringBuilder[] _stringBuilders = new StringBuilder[_stringBuilderCount];
        internal static StringBuilder AcquireStringBuilder() {
            var sbs = _stringBuilders;
            StringBuilder sb = null;
            lock (_stringBuilders) {
                for (var i = 0; i < _stringBuilderCount; ++i) {
                    sb = sbs[i];
                    if (sb != null) {
                        sbs[i] = null;
                        break;
                    }
                }
            }
            if (sb != null) {
                sb.Clear();
                return sb;
            }
            return new StringBuilder(_stringBuilderCapacity);
        }
        internal static void ReleaseStringBuilder(this StringBuilder sb) {
            if (sb != null && sb.Capacity <= _stringBuilderCapacity * 8) {
                var sbs = _stringBuilders;
                lock (_stringBuilders) {
                    for (var i = 0; i < _stringBuilderCount; ++i) {
                        if (sbs[i] == null) {
                            sbs[i] = sb;
                            return;
                        }
                    }
                }
            }
        }
        internal static string ToStringAndRelease(this StringBuilder sb) {
            var str = sb.ToString();
            ReleaseStringBuilder(sb);
            return str;
        }
        internal static string InvFormat(this string format, params string[] args) {
            return AcquireStringBuilder().AppendFormat(CultureInfo.InvariantCulture, format, args).ToStringAndRelease();
        }
        //
        //
        internal static bool TryInvParse(this string s, out decimal result) {
            return decimal.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, NumberFormatInfo.InvariantInfo, out result);
        }
        internal static string ToInvString(this decimal value) {
            return value.ToString(CultureInfo.InvariantCulture);
        }
        internal static bool TryInvParse(this string s, out long result) {
            return long.TryParse(s, NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo, out result);
        }
        internal static string ToInvString(this long value) {
            return value.ToString(CultureInfo.InvariantCulture);
        }
        internal static bool TryInvParse(this string s, out int result) {
            return int.TryParse(s, NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo, out result);
        }
        internal static string ToInvString(this int value) {
            return value.ToString(CultureInfo.InvariantCulture);
        }
        internal static bool TryInvParse(this string s, out short result) {
            return short.TryParse(s, NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo, out result);
        }
        internal static string ToInvString(this short value) {
            return value.ToString(CultureInfo.InvariantCulture);
        }
        internal static bool TryInvParse(this string s, out sbyte result) {
            return sbyte.TryParse(s, NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo, out result);
        }
        internal static string ToInvString(this sbyte value) {
            return value.ToString(CultureInfo.InvariantCulture);
        }
        internal static bool TryInvParse(this string s, out ulong result) {
            return ulong.TryParse(s, NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo, out result);
        }
        internal static string ToInvString(this ulong value) {
            return value.ToString(CultureInfo.InvariantCulture);
        }
        internal static bool TryInvParse(this string s, out uint result) {
            return uint.TryParse(s, NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo, out result);
        }
        internal static string ToInvString(this uint value) {
            return value.ToString(CultureInfo.InvariantCulture);
        }
        internal static bool TryInvParse(this string s, out ushort result) {
            return ushort.TryParse(s, NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo, out result);
        }
        internal static string ToInvString(this ushort value) {
            return value.ToString(CultureInfo.InvariantCulture);
        }
        internal static bool TryInvParse(this string s, out byte result) {
            return byte.TryParse(s, NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo, out result);
        }
        internal static string ToInvString(this byte value) {
            return value.ToString(CultureInfo.InvariantCulture);
        }
        internal static bool TryInvParse(this string s, out double result) {
            if (s == "INF") {
                result = double.PositiveInfinity;
            }
            else if (s == "-INF") {
                result = double.NegativeInfinity;
            }
            else if (s == "NaN") {
                result = double.NaN;
            }
            else if (!double.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, NumberFormatInfo.InvariantInfo, out result)) {
                return false;
            }
            return true;
        }
        internal static string ToInvString(this double value, out bool isLiteral) {
            if (double.IsPositiveInfinity(value)) {
                isLiteral = true;
                return "INF";
            }
            else if (double.IsNegativeInfinity(value)) {
                isLiteral = true;
                return "-INF";
            }
            else if (double.IsNaN(value)) {
                isLiteral = true;
                return "NaN";
            }
            isLiteral = false;
            return value.ToString(NumberFormatInfo.InvariantInfo);
        }
        internal static bool TryInvParse(this string s, out float result) {
            if (s == "INF") {
                result = float.PositiveInfinity;
            }
            else if (s == "-INF") {
                result = float.NegativeInfinity;
            }
            else if (s == "NaN") {
                result = float.NaN;
            }
            else if (!float.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, NumberFormatInfo.InvariantInfo, out result)) {
                return false;
            }
            return true;
        }
        internal static string ToInvString(this float value, out bool isLiteral) {
            if (float.IsPositiveInfinity(value)) {
                isLiteral = true;
                return "INF";
            }
            else if (float.IsNegativeInfinity(value)) {
                isLiteral = true;
                return "-INF";
            }
            else if (float.IsNaN(value)) {
                isLiteral = true;
                return "NaN";
            }
            isLiteral = false;
            return value.ToString(NumberFormatInfo.InvariantInfo);
        }
        internal static bool TryInvParse(this string s, out bool result) {
            if (s == "true") {
                result = true;
            }
            else if (s == "false") {
                result = false;
            }
            else {
                result = false;
                return false;
            }
            return true;
        }
        internal static string ToInvString(this bool value) {
            return value ? "true" : "false";
        }
        internal static bool TryInvParse(this string s, out Guid result) {
            return Guid.TryParseExact(s, "D", out result);
        }
        internal static string ToInvString(this Guid value) {
            return value.ToString("D");
        }
        internal static bool TryInvParse(this string s, out TimeSpan result) {
            return TimeSpan.TryParseExact(s, "c", DateTimeFormatInfo.InvariantInfo, out result);
        }
        internal static string ToInvString(this TimeSpan value) {
            return value.ToString("c");
        }
        private const string _dtoFormatString = "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz";
        internal static bool TryInvParse(this string s, out DateTimeOffset result) {
            return DateTimeOffset.TryParseExact(s, _dtoFormatString, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out result);
        }
        internal static string ToInvString(this DateTimeOffset value) {
            return value.ToString(_dtoFormatString, DateTimeFormatInfo.InvariantInfo);
        }
        //
        internal static object TryParse(TypeKind typeKind, string s, bool isReadOnly = false) {
            switch (typeKind) {
                case TypeKind.String:
                    return s;
                case TypeKind.IgnoreCaseString:
                    return new IgnoreCaseString(s, isReadOnly);
                case TypeKind.Decimal: {
                        decimal r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.Int64: {
                        long r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.Int32: {
                        int r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.Int16: {
                        short r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.SByte: {
                        sbyte r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.UInt64: {
                        ulong r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.UInt32: {
                        uint r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.UInt16: {
                        ushort r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.Byte: {
                        byte r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.Double: {
                        double r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.Single: {
                        float r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.Boolean: {
                        bool r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.Binary: {
                        Binary r;
                        if (Binary.TryFromBase64String(s, out r, isReadOnly)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.Guid: {
                        Guid r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.TimeSpan: {
                        TimeSpan r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.DateTimeOffset: {
                        DateTimeOffset r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;

                default:
                    throw new ArgumentException("Invalid type kind: " + typeKind.ToString());
            }
            return null;
        }
        //
        //
        internal static void GetLiteral(string value, StringBuilder sb) {
            var length = value.Length;
            if (length == 0) {
                sb.Append("\"\"");
            }
            else {
                sb.Append("@\"");
                for (var i = 0; i < length; ++i) {
                    var ch = value[i];
                    if (ch == '"') {
                        sb.Append("\"\"");
                    }
                    else {
                        sb.Append(ch);
                    }
                }
                sb.Append('"');
            }
        }
        internal static string ToLiteral(this string value) {
            var sb = AcquireStringBuilder();
            GetLiteral(value, sb);
            return sb.ToStringAndRelease();
        }

        //
        internal static int AggregateHash(int hash, int newValue) {
            unchecked {
                return hash * 31 + newValue;
            }
        }
        internal static int CombineHash(int a, int b) {
            unchecked {
                int hash = 17;
                hash = hash * 31 + a;
                hash = hash * 31 + b;
                return hash;
            }
        }
        internal static int CombineHash(int a, int b, int c) {
            unchecked {
                int hash = 17;
                hash = hash * 31 + a;
                hash = hash * 31 + b;
                hash = hash * 31 + c;
                return hash;
            }
        }
        //
        internal static void CreateAndAdd<T>(ref List<T> list, T item) {
            if (list == null) {
                list = new List<T>();
            }
            list.Add(item);
        }
        internal static int CountOrZero<T>(this List<T> list) {
            return list == null ? 0 : list.Count;
        }

        //
        internal const TypeKind AtomTypeStart = TypeKind.String;
        internal const TypeKind AtomTypeEnd = TypeKind.DateTimeOffset;
        internal static bool IsAtom(this TypeKind kind) {
            return kind >= AtomTypeStart && kind <= AtomTypeEnd;
        }
        internal static bool IsSimple(this TypeKind kind) {
            return IsAtom(kind) || kind == TypeKind.Enum;
        }
        internal static bool IsClrEnum(this TypeKind kind) {
            return kind >= TypeKind.Int64 && kind <= TypeKind.Byte;
        }
        internal static bool IsClrRef(this TypeKind kind) {
            return kind == TypeKind.Class || kind == TypeKind.String || kind == TypeKind.IgnoreCaseString || kind == TypeKind.Binary;
        }
        //
        //
        internal static ConstructorInfo TryGetParameterlessConstructor(TypeInfo ti) {
            foreach (var ci in ti.DeclaredConstructors) {
                if (ci.GetParameters().Length == 0) {
                    return ci;
                }
            }
            return null;
        }
        internal static ConstructorInfo GetParameterlessConstructor(TypeInfo ti) {
            var r = TryGetParameterlessConstructor(ti);
            if (r != null) return r;
            throw new ArgumentException("Cannot get parameterless constructor: " + ti.FullName);
        }
        internal static PropertyInfo TryGetPropertyInHierarchy(TypeInfo ti, string name) {
            while (true) {
                var pi = ti.GetDeclaredProperty(name);
                if (pi != null) {
                    return pi;
                }
                var baseType = ti.BaseType;
                if (baseType == null) {
                    return null;
                }
                ti = baseType.GetTypeInfo();
            }
        }
        internal static PropertyInfo GetPropertyInHierarchy(TypeInfo ti, string name) {
            var r = TryGetPropertyInHierarchy(ti, name);
            if (r != null) return r;
            throw new ArgumentException("Cannot get property: " + name);
        }
        internal static PropertyInfo GetProperty(TypeInfo ti, string name) {
            var r = ti.GetDeclaredProperty(name);
            if (r != null) return r;
            throw new ArgumentException("Cannot get property: " + name);
        }
        internal static FieldInfo TryGetFieldInHierarchy(TypeInfo ti, string name) {
            while (true) {
                var fi = ti.GetDeclaredField(name);
                if (fi != null) {
                    return fi;
                }
                var baseType = ti.BaseType;
                if (baseType == null) {
                    return null;
                }
                ti = baseType.GetTypeInfo();
            }
        }
        internal static FieldInfo GetFieldInHierarchy(TypeInfo ti, string name) {
            var r = TryGetFieldInHierarchy(ti, name);
            if (r != null) return r;
            throw new ArgumentException("Cannot get field: " + name);
        }
        internal static FieldInfo GetField(TypeInfo ti, string name) {
            var r = ti.GetDeclaredField(name);
            if (r != null) return r;
            throw new ArgumentException("Cannot get field: " + name);
        }
        internal static MethodInfo TryGetMethodInHierarchy(TypeInfo ti, string name) {
            while (true) {
                var mi = ti.GetDeclaredMethod(name);
                if (mi != null) {
                    return mi;
                }
                var baseType = ti.BaseType;
                if (baseType == null) {
                    return null;
                }
                ti = baseType.GetTypeInfo();
            }
        }
        internal static MethodInfo GetMethodInHierarchy(TypeInfo ti, string name) {
            var r = TryGetMethodInHierarchy(ti, name);
            if (r != null) return r;
            throw new ArgumentException("Cannot get method: " + name);
        }
        internal static MethodInfo GetMethod(TypeInfo ti, string name) {
            var r = ti.GetDeclaredMethod(name);
            if (r != null) return r;
            throw new ArgumentException("Cannot get method: " + name);
        }

    }
}
