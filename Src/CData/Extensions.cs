using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace CData {
    public static class Extensions {
        internal const string SystemUri = "https://github.com/knat/CData";
        //
        //[System.Diagnostics.ConditionalAttribute("DEBUG")]
        //public static void PublicParameterlessConstructorRequired<T>() where T : new() { }
        //internal const string DefaultIndentString = "\t";
        //internal const string DefaultNewLineString = "\n";
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
        internal static bool TryInvParse(this string s, out byte[] result) {
            if (s.Length == 0) {
                result = EmptyBytes;
                return true;
            }
            try {
                result = Convert.FromBase64String(s);
                return true;
            }
            catch (FormatException) {
                result = null;
                return false;
            }
        }
        internal static readonly byte[] EmptyBytes = new byte[0];
        internal static string ToInvString(this byte[] value) {
            if (value.Length == 0) return string.Empty;
            return Convert.ToBase64String(value);
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
        internal static bool IsClass(this TypeKind kind) {
            return kind == TypeKind.Class;
        }
        internal static bool IsList(this TypeKind kind) {
            return kind == TypeKind.List;
        }
        internal static bool IsSet(this TypeKind kind) {
            return kind == TypeKind.Set;
        }
        internal static bool IsMap(this TypeKind kind) {
            return kind == TypeKind.Map;
        }
        internal static bool IsAtom(this TypeKind kind) {
            return kind >= TypeKind.String && kind <= TypeKind.DateTimeOffset;
        }
        //
        //
        internal static ConstructorInfo GetParameterlessConstructor(TypeInfo ti) {
            foreach (var ci in ti.DeclaredConstructors) {
                if (ci.GetParameters().Length == 0) {
                    return ci;
                }
            }
            return null;
        }
        internal static ConstructorInfo GetIEqualityComparerOfStringConstructor(TypeInfo ti) {
            foreach (var ci in ti.DeclaredConstructors) {
                var ps = ci.GetParameters();
                if (ps.Length == 1 && ps[0].ParameterType == IEqualityComparerOfStringType) {
                    return ci;
                }
            }
            return null;
        }
        internal static PropertyInfo GetClrProperty(TypeInfo ti, string name) {
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
        internal static FieldInfo GetClrField(TypeInfo ti, string name) {
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
        internal static MethodInfo GetClrMethod(TypeInfo ti, string name, Type para1Type, Type para2Type) {
            foreach (var mi in ti.GetDeclaredMethods(name)) {
                var ps = mi.GetParameters();
                if (ps.Length == 2 && ps[0].ParameterType == para1Type && ps[1].ParameterType == para2Type) {
                    return mi;
                }
            }
            return null;
        }
        internal static readonly Type IEqualityComparerType = typeof(IEqualityComparer<>);
        internal static readonly Type IEqualityComparerOfStringType = typeof(IEqualityComparer<string>);
        internal static readonly Type ListType = typeof(List<>);
        internal static readonly Type HashSetType = typeof(HashSet<>);
        internal static readonly Type DictionaryType = typeof(Dictionary<,>);
        internal static readonly Type BoolRefType = typeof(bool).MakeByRefType();
        internal static readonly Type DiagContextType = typeof(DiagContext);
        internal static readonly object BoolTrueValue = true;

    }
}
