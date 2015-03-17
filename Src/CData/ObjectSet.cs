using System;
using System.Collections.Generic;

namespace CData {
    public interface IObjectSet<TKey, TItem> : ICollection<TItem>, IDictionary<TKey, TItem> {
        TKey GetKeyForItem(TItem item);
        new bool Add(TItem item);
    }
    public abstract class ObjectSet<TKey, TItem> : Dictionary<TKey, TItem>, IObjectSet<TKey, TItem> {
        protected ObjectSet() { }
        protected ObjectSet(IEqualityComparer<TKey> comparer)
            : base(comparer) {
        }
        public abstract TKey GetKeyForItem(TItem item);
        public bool Add(TItem item) {
            var key = GetKeyForItem(item);
            if (ContainsKey(key)) return false;
            Add(key, item);
            return true;
        }
        void ICollection<TItem>.Add(TItem item) {
            Add(item);
        }
        public bool Remove(TItem item) {
            return Remove(GetKeyForItem(item));
        }
        public bool Contains(TItem item) {
            return ContainsKey(GetKeyForItem(item));
        }
        new public Dictionary<TKey, TItem>.ValueCollection.Enumerator GetEnumerator() {
            return Values.GetEnumerator();
        }
        IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() {
            return GetEnumerator();
        }
        public void CopyTo(TItem[] array, int arrayIndex) {
            Values.CopyTo(array, arrayIndex);
        }
        bool ICollection<TItem>.IsReadOnly {
            get { return false; }
        }
    }
}
