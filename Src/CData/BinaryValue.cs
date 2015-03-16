using System;

namespace CData {
    public sealed class BinaryValue : IEquatable<BinaryValue> {
        public BinaryValue() { }
        public BinaryValue(byte[] value) {
            _value = value;
        }
        public static implicit operator BinaryValue(byte[] value) {
            if (value == null) return null;
            return new BinaryValue(value);
        }
        public static implicit operator byte[](BinaryValue obj) {
            if ((object)obj == null) return null;
            return obj._value;
        }
        private byte[] _value;
        public byte[] Value {
            get {
                return _value;
            }
            set {
                _value = value;
            }
        }
        private static bool ValueEquals(byte[] x, byte[] y) {
            if (x == y) {
                return true;
            }
            var xLength = x.Length;
            if (xLength != y.Length) {
                return false;
            }
            for (var i = 0; i < xLength; ++i) {
                if (x[i] != y[i]) {
                    return false;
                }
            }
            return true;
        }
        public bool Equals(BinaryValue other) {
            if ((object)this == (object)other) return true;
            if ((object)other == null) return false;
            return ValueEquals(_value, other._value);
        }
        public override bool Equals(object obj) {
            return Equals(obj as BinaryValue);
        }
        public override int GetHashCode() {
            var hash = 17;
            var count = Math.Min(_value.Length, 7);
            for (var i = 0; i < count; ++i) {
                hash = Extensions.AggregateHash(hash, _value[i]);
            }
            return hash;
        }
        public static bool operator ==(BinaryValue left, BinaryValue right) {
            if ((object)left == null) {
                return (object)right == null;
            }
            return left.Equals(right);
        }
        public static bool operator !=(BinaryValue left, BinaryValue right) {
            return !(left == right);
        }
        public override string ToString() {
            if (_value == null) return null;
            return _value.ToInvString();
        }
    }
}
