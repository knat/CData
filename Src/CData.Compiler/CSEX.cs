using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;


namespace CData.Compiler {
    internal sealed class CSharpNamespaceNameNode : List<string>, IEquatable<CSharpNamespaceNameNode> {
        public CSharpNamespaceNameNode() { }
        public CSharpNamespaceNameNode(IEnumerable<string> parts) : base(parts) { }
        private string[] _reversedNameParts;
        public string[] ReversedNameParts {
            get {
                if (_reversedNameParts == null) {
                    _reversedNameParts = this.ToArray();
                    Array.Reverse(_reversedNameParts);
                }
                return _reversedNameParts;
            }
        }

        public bool Equals(CSharpNamespaceNameNode other) {
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
            return Equals(obj as CSharpNamespaceNameNode);
        }
        public override int GetHashCode() {
            var hash = 17;
            var count = Math.Min(Count, 5);
            for (var i = 0; i < count; i++) {
                hash = Extensions.AggregateHash(hash, this[i].GetHashCode());
            }
            return hash;
        }
        public static bool operator ==(CSharpNamespaceNameNode left, CSharpNamespaceNameNode right) {
            if ((object)left == null) {
                return (object)right == null;
            }
            return left.Equals(right);
        }
        public static bool operator !=(CSharpNamespaceNameNode left, CSharpNamespaceNameNode right) {
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
        internal static AttributeData GetAttributeData(this ISymbol symbol, string[] nameParts) {
            foreach (var attData in symbol.GetAttributes()) {
                if (attData.AttributeClass.IsFullNameEquals(nameParts)) {
                    return attData;
                }
            }
            return null;
        }

    }

}
