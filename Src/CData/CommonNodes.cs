﻿using System;

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
    internal struct QualifiableNameNode {
        public QualifiableNameNode(NameNode alias, NameNode name) {
            Alias = alias;
            Name = name;
        }
        public readonly NameNode Alias;//opt
        public readonly NameNode Name;
        public bool IsQualified {
            get {
                return Alias.IsValid;
            }
        }
        public bool IsValid {
            get {
                return Name.IsValid;
            }
        }
        public TextSpan TextSpan {
            get {
                return Name.TextSpan;
            }
        }
        public override string ToString() {
            if (IsQualified) {
                return Alias.Value + "::" + Name.Value;
            }
            return Name.Value;
        }
    }
    internal enum AtomValueKind : byte {
        None = 0,
        String,
        Char,
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
    internal struct UriAliasNode {
        public UriAliasNode(string uri, string alias) {
            Uri = uri;
            Alias = alias;
        }
        public readonly string Uri;
        public readonly string Alias;//opt
    }

}
