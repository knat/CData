using System;
using System.Collections.Generic;

namespace CData {
    public sealed class Binary : IEquatable<Binary>, IList<byte> {
        private static readonly byte[] _emptyBytes = new byte[0];
        private byte[] _bytes;
        private int _count;
        private readonly bool _isReadOnly;
        public Binary(byte[] bytes, bool isReadOnly = true) {
            if (bytes == null) throw new ArgumentNullException("bytes");
            _bytes = bytes;
            _count = bytes.Length;
            _isReadOnly = isReadOnly;
        }
        public Binary()
            : this(_emptyBytes, false) {
        }
        public static implicit operator Binary(byte[] bytes) {
            if (bytes == null) return null;
            return new Binary(bytes);
        }
        public static bool TryFromBase64String(string s, out Binary result, bool isReadOnly = false) {
            if (s == null) throw new ArgumentNullException("s");
            if (s.Length == 0) {
                result = new Binary(_emptyBytes, isReadOnly);
                return true;
            }
            try {
                var bytes = Convert.FromBase64String(s);
                result = new Binary(bytes, isReadOnly);
                return true;
            }
            catch (FormatException) {
                result = null;
                return false;
            }
        }
        public string ToBase64String() {
            if (_count == 0) return string.Empty;
            return Convert.ToBase64String(_bytes, 0, _count);
        }
        public override string ToString() {
            return ToBase64String();
        }
        public byte[] ToBytes() {
            return (byte[])_bytes.Clone();
        }
        public bool IsReadOnly {
            get { return _isReadOnly; }
        }
        public Binary AsReadOnly() {
            if (_isReadOnly) return this;
            return new Binary(_bytes, true);
        }
        public int Count {
            get { return _count; }
        }
        private void ThrowIfReadOnly() {
            if (_isReadOnly) {
                throw new InvalidOperationException("The object is readonly.");
            }
        }
        public byte this[int index] {
            get {
                if (index >= _count) {
                    throw new ArgumentOutOfRangeException("index");
                }
                return _bytes[index];
            }
            set {
                ThrowIfReadOnly();
                if (index >= _count) {
                    throw new ArgumentOutOfRangeException("index");
                }
                _bytes[index] = value;
            }
        }
        public void Clear() {
            _count = 0;
        }
        public int IndexOf(byte item) {
            var count = _count;
            var bytes = _bytes;
            for (var i = 0; i < count; ++i) {
                if (bytes[i] == item) return i;
            }
            return -1;
        }
        public bool Contains(byte item) {
            return IndexOf(item) >= 0;
        }
        public void CopyTo(byte[] array, int arrayIndex) {
            Array.Copy(_bytes, 0, array, arrayIndex, _count);
        }
        public void Add(byte item) {
            Insert(_count, item);
        }
        public void Insert(int index, byte item) {
            ThrowIfReadOnly();
            var count = _count;
            if (index < 0 || index > count) {
                throw new ArgumentOutOfRangeException("index");
            }
            if (count == _bytes.Length) {
                if (count == 0) {
                    _bytes = new byte[8];
                }
                else {
                    Array.Resize(ref _bytes, count < 1024 * 8 ? count * 2 : count + 1024);
                }
            }
            if (index < count) {
                Array.Copy(_bytes, index, _bytes, index + 1, count - index);
            }
            _bytes[index] = item;
            ++_count;
        }
        public void RemoveAt(int index) {
            ThrowIfReadOnly();
            var count = _count;
            if (index < 0 || index >= count) {
                throw new ArgumentOutOfRangeException("index");
            }
            --count;
            if (index < count) {
                Array.Copy(_bytes, index + 1, _bytes, index, count - index);
            }
            _count = count;
        }
        public bool Remove(byte item) {
            ThrowIfReadOnly();
            var idx = IndexOf(item);
            if (idx >= 0) {
                RemoveAt(idx);
                return true;
            }
            return false;
        }
        public struct Enumerator : IEnumerator<byte> {
            internal Enumerator(Binary binary) {
                _binary = binary;
                _index = 0;
                _current = 0;
            }
            private readonly Binary _binary;
            private int _index;
            private byte _current;
            public bool MoveNext() {
                var bin = _binary;
                var idx = _index;
                if (idx < bin._count) {
                    _current = bin._bytes[idx];
                    ++_index;
                    return true;
                }
                return false;
            }
            public byte Current {
                get { return _current; }
            }
            object System.Collections.IEnumerator.Current {
                get { return _current; }
            }
            public void Reset() {
                _index = 0;
                _current = 0;
            }
            public void Dispose() {
            }
        }
        public Enumerator GetEnumerator() {
            return new Enumerator(this);
        }
        IEnumerator<byte> IEnumerable<byte>.GetEnumerator() {
            return GetEnumerator();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
        //
        public bool Equals(Binary other) {
            if ((object)this == (object)other) return true;
            if ((object)other == null) return false;
            var xBytes = _bytes;
            var yBytes = other._bytes;
            if (xBytes == yBytes) return true;
            var xCount = _count;
            if (xCount != other._count) return false;
            for (var i = 0; i < xCount; ++i) {
                if (xBytes[i] != yBytes[i]) return false;
            }
            return true;
        }
        public override bool Equals(object obj) {
            return Equals(obj as Binary);
        }
        public override int GetHashCode() {
            var bytes = _bytes;
            var count = Math.Min(_count, 7);
            var hash = 17;
            for (var i = 0; i < count; ++i) {
                hash = Extensions.AggregateHash(hash, bytes[i]);
            }
            return hash;
        }
        public static bool operator ==(Binary left, Binary right) {
            if ((object)left == null) {
                return (object)right == null;
            }
            return left.Equals(right);
        }
        public static bool operator !=(Binary left, Binary right) {
            return !(left == right);
        }

    }
}
