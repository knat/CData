using System;
using System.Collections.Generic;

namespace CData {
    public interface IObjectSet<TKey, TItem> : ICollection<TItem> {
        TKey GetKeyForItem(TItem item);
        new bool Add(TItem item);
        TItem this[TKey key] { get; }
        ICollection<TKey> Keys { get; }
        bool ContainsKey(TKey key);
        bool TryGetValue(TKey key, out TItem item);
        bool Remove(TKey key);
    }
    public abstract class ObjectSet<TKey, TItem> : IObjectSet<TKey, TItem> {
        protected ObjectSet() {
            _dict = new Dictionary<TKey, TItem>();
        }
        protected ObjectSet(IEqualityComparer<TKey> comparer) {
            _dict = new Dictionary<TKey, TItem>(comparer);
        }
        private readonly Dictionary<TKey, TItem> _dict;
        public abstract TKey GetKeyForItem(TItem item);
        public int Count {
            get {
                return _dict.Count;
            }
        }
        public bool Add(TItem item) {
            var key = GetKeyForItem(item);
            if (_dict.ContainsKey(key)) {
                return false;
            }
            _dict.Add(key, item);
            return true;
        }
        void ICollection<TItem>.Add(TItem item) {
            Add(item);
        }
        public TItem this[TKey key] {
            get {
                return _dict[key];
            }
        }
        public ICollection<TKey> Keys {
            get {
                return _dict.Keys;
            }
        }
        public bool ContainsKey(TKey key) {
            return _dict.ContainsKey(key);
        }
        public bool Contains(TItem item) {
            return _dict.ContainsKey(GetKeyForItem(item));
        }
        public bool TryGetValue(TKey key, out TItem item) {
            return _dict.TryGetValue(key, out item);
        }
        public bool Remove(TKey key) {
            return _dict.Remove(key);
        }
        public bool Remove(TItem item) {
            return _dict.Remove(GetKeyForItem(item));
        }
        public void Clear() {
            _dict.Clear();
        }
        public Dictionary<TKey, TItem>.ValueCollection.Enumerator GetEnumerator() {
            return _dict.Values.GetEnumerator();
        }
        IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() {
            return GetEnumerator();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
        public void CopyTo(TItem[] array, int arrayIndex) {
            _dict.Values.CopyTo(array, arrayIndex);
        }
        bool ICollection<TItem>.IsReadOnly {
            get { return false; }
        }
    }
}
