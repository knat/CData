using System;
using System.Text;

namespace CData {
    internal static class StringBuilderBuffer {
        private const int _stringBuilderCount = 4;
        private const int _stringBuilderCapacity = 128;
        [ThreadStatic]
        private static readonly StringBuilder[] _stringBuilders = new StringBuilder[_stringBuilderCount];
        internal static StringBuilder Acquire() {
            var sbs = _stringBuilders;
            StringBuilder sb = null;
            //lock (sbs) {
                for (var i = 0; i < _stringBuilderCount; ++i) {
                    sb = sbs[i];
                    if (sb != null) {
                        sbs[i] = null;
                        break;
                    }
                }
            //}
            if (sb != null) {
                sb.Clear();
                return sb;
            }
            return new StringBuilder(_stringBuilderCapacity);
        }
        internal static void Release(StringBuilder sb) {
            if (sb != null && sb.Capacity <= _stringBuilderCapacity * 8) {
                var sbs = _stringBuilders;
                //lock (sbs) {
                    for (var i = 0; i < _stringBuilderCount; ++i) {
                        if (sbs[i] == null) {
                            sbs[i] = sb;
                            return;
                        }
                    }
                //}
            }
        }
        internal static string ToStringAndRelease(this StringBuilder sb) {
            var str = sb.ToString();
            Release(sb);
            return str;
        }

    }
}
