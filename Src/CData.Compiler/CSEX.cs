using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CData.Compiler {
    internal sealed class CSNamespaceNameNode : List<string>, IEquatable<CSNamespaceNameNode> {
        public CSNamespaceNameNode() { }
        public CSNamespaceNameNode(IEnumerable<string> parts) : base(parts) { }
        private string[] _nameParts;
        public string[] NameParts {
            get {
                if (_nameParts == null) {
                    _nameParts = this.ToArray();
                    Array.Reverse(_nameParts);
                }
                return _nameParts;
            }
        }

        public bool Equals(CSNamespaceNameNode other) {
            if ((object)this == (object)other) return true;
            if ((object)other == null) return false;
            var count = Count;
            if (count != other.Count) {
                return false;
            }
            for (var i = 0; i < count; i++) {
                if (this[i] != other[i]) {
                    return false;
                }
            }
            return true;
        }
        public override bool Equals(object obj) {
            return Equals(obj as CSNamespaceNameNode);
        }
        public override int GetHashCode() {
            var hash = 17;
            var count = Math.Min(Count, 5);
            for (var i = 0; i < count; i++) {
                hash = Extensions.AggregateHash(hash, this[i].GetHashCode());
            }
            return hash;
        }
        public static bool operator ==(CSNamespaceNameNode left, CSNamespaceNameNode right) {
            if ((object)left == null) {
                return (object)right == null;
            }
            return left.Equals(right);
        }
        public static bool operator !=(CSNamespaceNameNode left, CSNamespaceNameNode right) {
            return !(left == right);
        }
        private string _string;
        public override string ToString() {
            if (_string == null) {
                var sb = Extensions.AcquireStringBuilder();
                for (var i = 0; i < Count; ++i) {
                    if (i > 0) {
                        sb.Append('.');
                    }
                    sb.Append(this[i]);
                }
                _string = sb.ToStringAndRelease();
            }
            return _string;
        }
        //
        private NameSyntax _csNonGlobalFullName;//@NS1.NS2
        internal NameSyntax CSNonGlobalFullName {
            get {
                if (_csNonGlobalFullName == null) {
                    foreach (var item in this) {
                        if (_csNonGlobalFullName == null) {
                            _csNonGlobalFullName = CS.IdName(item.EscapeId());
                        }
                        else {
                            _csNonGlobalFullName = CS.QualifiedName(_csNonGlobalFullName, item.EscapeId());
                        }
                    }
                }
                return _csNonGlobalFullName;
            }
        }
        private NameSyntax _csFullName;//global::@NS1.NS2
        internal NameSyntax CSFullName {
            get {
                if (_csFullName == null) {
                    foreach (var item in this) {
                        if (_csFullName == null) {
                            _csFullName = CS.GlobalAliasQualifiedName(item.EscapeId());
                        }
                        else {
                            _csFullName = CS.QualifiedName(_csFullName, item.EscapeId());
                        }
                    }
                }
                return _csFullName;
            }
        }
        private ExpressionSyntax _csFullExpr;//global::@NS1.NS2
        internal ExpressionSyntax CSFullExpr {
            get {
                if (_csFullExpr == null) {
                    foreach (var item in this) {
                        if (_csFullExpr == null) {
                            _csFullExpr = CS.GlobalAliasQualifiedName(item.EscapeId());
                        }
                        else {
                            _csFullExpr = CS.MemberAccessExpr(_csFullExpr, item.EscapeId());
                        }
                    }
                }
                return _csFullExpr;
            }
        }
    }
    internal static class ExtensionsEx {
        private volatile static char[] _dotCharArray = new char[] { '.' };
        internal static string[] SplitDot(this string s) {
            if (s == null) return null;
            return s.Split(_dotCharArray, StringSplitOptions.None);// StringSplitOptions.RemoveEmptyEntries);
        }

    }
    internal static class CSEX {
        internal static string[] GetIdsBySplitDot(string s) {
            var arr = s.SplitDot();
            if (arr == null || arr.Length == 0) return null;
            foreach (var name in arr) {
                if (!SyntaxFacts.IsValidIdentifier(name)) {
                    return null;
                }
            }
            return arr;
        }
        internal static TextSpan GetTextSpan(Location location) {
            if (location.IsInSource) {
                var csLineSpan = location.GetLineSpan();
                if (csLineSpan.IsValid) {
                    var csTextSpan = location.SourceSpan;
                    return new TextSpan(csLineSpan.Path, csTextSpan.Start, csTextSpan.Length,
                        ToTextPosition(csLineSpan.StartLinePosition), ToTextPosition(csLineSpan.EndLinePosition));
                }
            }
            return default(TextSpan);
        }
        private static TextPosition ToTextPosition(this LinePosition csPosition) {
            return new TextPosition(csPosition.Line + 1, csPosition.Character + 1);
        }
        //
        //
        private static readonly string[] _IgnoreCaseStringNameParts = new string[] { "IgnoreCaseString", "CData" };
        private static readonly string[] _BinaryNameParts = new string[] { "Binary", "CData" };
        private static readonly string[] _GuidNameParts = new string[] { "Guid", "System" };
        private static readonly string[] _TimeSpanNameParts = new string[] { "TimeSpan", "System" };
        private static readonly string[] _DateTimeOffsetNameParts = new string[] { "DateTimeOffset", "System" };
        internal static bool IsAtomType(TypeKind typeKind, ITypeSymbol typeSymbol) {
            switch (typeKind) {
                case TypeKind.String:
                    return typeSymbol.SpecialType == SpecialType.System_String;
                case TypeKind.IgnoreCaseString:
                    return typeSymbol.IsFullNameEqual(_IgnoreCaseStringNameParts);
                case TypeKind.Int64:
                    return typeSymbol.SpecialType == SpecialType.System_Int64;
                case TypeKind.Int32:
                    return typeSymbol.SpecialType == SpecialType.System_Int32;
                case TypeKind.Int16:
                    return typeSymbol.SpecialType == SpecialType.System_Int16;
                case TypeKind.SByte:
                    return typeSymbol.SpecialType == SpecialType.System_SByte;
                case TypeKind.UInt64:
                    return typeSymbol.SpecialType == SpecialType.System_UInt64;
                case TypeKind.UInt32:
                    return typeSymbol.SpecialType == SpecialType.System_UInt32;
                case TypeKind.UInt16:
                    return typeSymbol.SpecialType == SpecialType.System_UInt16;
                case TypeKind.Byte:
                    return typeSymbol.SpecialType == SpecialType.System_Byte;
                case TypeKind.Double:
                    return typeSymbol.SpecialType == SpecialType.System_Double;
                case TypeKind.Single:
                    return typeSymbol.SpecialType == SpecialType.System_Single;
                case TypeKind.Boolean:
                    return typeSymbol.SpecialType == SpecialType.System_Boolean;
                case TypeKind.Binary:
                    return typeSymbol.IsFullNameEqual(_BinaryNameParts);
                case TypeKind.Guid:
                    return typeSymbol.IsFullNameEqual(_GuidNameParts);
                case TypeKind.TimeSpan:
                    return typeSymbol.IsFullNameEqual(_TimeSpanNameParts);
                case TypeKind.DateTimeOffset:
                    return typeSymbol.IsFullNameEqual(_DateTimeOffsetNameParts);
                default:
                    throw new ArgumentException("Invalid type kind: " + typeKind.ToString());
            }
        }
        internal static bool IsAtomType(TypeKind typeKind, bool isNullable, ITypeSymbol typeSymbol) {
            if (!isNullable || typeKind == TypeKind.String || typeKind == TypeKind.IgnoreCaseString || typeKind == TypeKind.Binary) {
                return IsAtomType(typeKind, typeSymbol);
            }
            if (typeSymbol.SpecialType == SpecialType.System_Nullable_T) {
                return IsAtomType(typeKind, ((INamedTypeSymbol)typeSymbol).TypeArguments[0]);
            }
            return false;
        }


    }

}
