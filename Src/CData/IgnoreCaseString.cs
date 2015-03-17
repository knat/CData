using System;

namespace CData {
    public sealed class IgnoreCaseString : IEquatable<IgnoreCaseString>, IComparable<IgnoreCaseString> {
        public IgnoreCaseString(string value) {
            Value = value;
        }
        public static implicit operator IgnoreCaseString(string value) {
            if (value == null) return null;
            return new IgnoreCaseString(value);
        }
        public static implicit operator string(IgnoreCaseString obj) {
            if ((object)obj == null) return null;
            return obj._value;
        }
        private string _value;
        public string Value {
            get {
                return _value;
            }
            set {
                if (value == null) {
                    throw new ArgumentNullException("value");
                }
                _value = value;
            }
        }
        private static readonly StringComparer _stringComparer = StringComparer.OrdinalIgnoreCase;
        public bool Equals(IgnoreCaseString other) {
            if ((object)this == (object)other) return true;
            if ((object)other == null) return false;
            return _stringComparer.Equals(_value, other._value);
        }
        public override bool Equals(object obj) {
            return Equals(obj as IgnoreCaseString);
        }
        public override int GetHashCode() {
            return _stringComparer.GetHashCode(_value);
        }
        public static bool operator ==(IgnoreCaseString left, IgnoreCaseString right) {
            if ((object)left == null) {
                return (object)right == null;
            }
            return left.Equals(right);
        }
        public static bool operator !=(IgnoreCaseString left, IgnoreCaseString right) {
            return !(left == right);
        }
        public int CompareTo(IgnoreCaseString other) {
            if ((object)this == (object)other) return 0;
            if ((object)other == null) return 1;
            return _stringComparer.Compare(_value, other._value);
        }
        public override string ToString() {
            return _value;
        }
    }
}
