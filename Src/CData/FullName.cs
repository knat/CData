using System;

namespace CData {
    public struct FullName : IEquatable<FullName> {
        public FullName(string uri, string name) {
            Uri = uri;
            Name = name;
        }
        public readonly string Uri;
        public readonly string Name;
        public bool IsValid {
            get {
                return Name != null;
            }
        }
        public override string ToString() {
            if (IsValid) {
                return "{" + Uri + "}" + Name;
            }
            return null;
        }
        public bool Equals(FullName other) {
            return Uri == other.Uri && Name == other.Name;
        }
        public override bool Equals(object obj) {
            return obj is FullName && Equals((FullName)obj);
        }
        public override int GetHashCode() {
            if (IsValid) {
                return Extensions.CombineHash(Uri.GetHashCode(), Name.GetHashCode());
            }
            return 0;
        }
        public static bool operator ==(FullName left, FullName right) {
            return left.Equals(right);
        }
        public static bool operator !=(FullName left, FullName right) {
            return !left.Equals(right);
        }
    }
}
