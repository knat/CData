using System;

namespace CData {
    internal struct NameNode : IEquatable<NameNode> {
        public NameNode(string value, TextSpan textSpan) {
            Value = value;
            TextSpan = textSpan;
        }
        public readonly string Value;
        public readonly TextSpan TextSpan;
        public bool IsValid {
            get {
                return Value != null;
            }
        }
        public override string ToString() {
            return Value;
        }
        public bool Equals(NameNode other) {
            return Value == other.Value;
        }
        public override bool Equals(object obj) {
            return obj is NameNode && Equals((NameNode)obj);
        }
        public override int GetHashCode() {
            return Value != null ? Value.GetHashCode() : 0;
        }
        public static bool operator ==(NameNode left, NameNode right) {
            return left.Equals(right);
        }
        public static bool operator !=(NameNode left, NameNode right) {
            return !left.Equals(right);
        }
    }
    internal enum AtomValueKind : byte {
        None = 0,
        String,
        Boolean,
        Null,
        Integer,
        Decimal,
        Real,
    }
    internal struct AtomValueNode {
        public AtomValueNode(AtomValueKind kind, string value, TextSpan textSpan) {
            Kind = kind;
            Value = value;
            TextSpan = textSpan;
        }
        public readonly AtomValueKind Kind;
        public readonly string Value;
        public readonly TextSpan TextSpan;
        public bool IsValid {
            get {
                return Kind != AtomValueKind.None;
            }
        }
        public bool IsNull {
            get {
                return Kind == AtomValueKind.Null;
            }
        }
    }
}
