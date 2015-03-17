using System;

namespace CData {
    public sealed class BinaryValue : IEquatable<BinaryValue> {
        public BinaryValue(byte[] value) {
            Value = value;
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
                if (value == null) {
                    throw new ArgumentNullException("value");
                }
                _value = value;
            }
        }
        public bool Equals(BinaryValue other) {
            if ((object)this == (object)other) return true;
            if ((object)other == null) return false;
            var xv = _value;
            var yv = other._value;
            if (xv == yv) return true;
            var xLength = xv.Length;
            if (xLength != yv.Length) return false;
            for (var i = 0; i < xLength; ++i) {
                if (xv[i] != yv[i]) return false;
            }
            return true;
        }
        public override bool Equals(object obj) {
            return Equals(obj as BinaryValue);
        }
        public override int GetHashCode() {
            var v = _value;
            var hash = 17;
            var count = Math.Min(v.Length, 7);
            for (var i = 0; i < count; ++i) {
                hash = Extensions.AggregateHash(hash, v[i]);
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
    }
}
