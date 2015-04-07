using System;
using System.Text;

namespace CData {
    internal static class StringBuilderBuffer {
        private const int _count = 4;
        [ThreadStatic]
        private static readonly StringBuilder[] _sbs = new StringBuilder[_count];
        internal static StringBuilder Acquire() {
            var sbs = _sbs;
            StringBuilder sb = null;
            for (var i = 0; i < _count; ++i) {
                sb = sbs[i];
                if (sb != null) {
                    sbs[i] = null;
                    break;
                }
            }
            if (sb != null) {
                sb.Clear();
                return sb;
            }
            return new StringBuilder(128);
        }
        internal static void Release(StringBuilder sb) {
            if (sb != null && sb.Capacity <= 1024 * 8) {
                var sbs = _sbs;
                for (var i = 0; i < _count; ++i) {
                    if (sbs[i] == null) {
                        sbs[i] = sb;
                        return;
                    }
                }
            }
        }
        internal static string ToStringAndRelease(this StringBuilder sb) {
            var str = sb.ToString();
            Release(sb);
            return str;
        }

    }
}
