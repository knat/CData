using System;
using System.Collections.Generic;
using System.Text;

namespace CData {
    internal class IndentedStringBuilder {
        public IndentedStringBuilder(StringBuilder stringBuilder, string indentString, string newLineString) {
            if (stringBuilder == null) throw new ArgumentNullException("stringBuilder");
            if (string.IsNullOrEmpty(indentString)) throw new ArgumentNullException("indentString");
            if (string.IsNullOrEmpty(newLineString)) throw new ArgumentNullException("newLineString");
            StringBuilder = stringBuilder;
            StartIndex = stringBuilder.Length;
            IndentString = indentString;
            NewLineString = newLineString;
            _atNewLine = true;
        }
        public readonly StringBuilder StringBuilder;
        public readonly int StartIndex;
        public readonly string IndentString;
        public readonly string NewLineString;
        private int _indentCount;
        private bool _atNewLine;
        public int IndentCount {
            get {
                return _indentCount;
            }
        }
        public bool AtNewLine {
            get {
                return _atNewLine;
            }
        }
        public void PushIndent(int count = 1) {
            if ((_indentCount += count) < 0) throw new ArgumentOutOfRangeException("count");
        }
        public void PopIndent(int count = 1) {
            if ((_indentCount -= count) < 0) throw new ArgumentOutOfRangeException("count");
        }
        public void AppendIndents() {
            if (_atNewLine) {
                var count = _indentCount;
                var sb = StringBuilder;
                var s = IndentString;
                for (var i = 0; i < count; ++i) {
                    sb.Append(s);
                }
                _atNewLine = false;
            }
        }
        public void Append(string s) {
            AppendIndents();
            StringBuilder.Append(s);
        }
        public void Append(char ch) {
            AppendIndents();
            StringBuilder.Append(ch);
        }
        public void AppendLine() {
            StringBuilder.Append(NewLineString);
            _atNewLine = true;
        }
        //public void AppendLine(string s) {
        //    Append(s);
        //    AppendLine();
        //}
        //public void AppendLine(char ch) {
        //    Append(ch);
        //    AppendLine();
        //}
    }
    internal sealed class SavingContext : IndentedStringBuilder {
        public SavingContext(StringBuilder stringBuilder, string indentString, string newLineString) :
            base(stringBuilder, indentString, newLineString) {
            _aliasUriList = new List<AliasUri>();
        }
        private readonly List<AliasUri> _aliasUriList;
        private struct AliasUri {
            public AliasUri(string alias, string uri) {
                Alias = alias;
                Uri = uri;
            }
            public readonly string Alias, Uri;
        }
        public string AddUri(string uri) {
            foreach (var au in _aliasUriList) {
                if (au.Uri == uri) {
                    return au.Alias;
                }
            }
            var alias = "a" + _aliasUriList.Count.ToInvString();
            _aliasUriList.Add(new AliasUri(alias, uri));
            return alias;
        }
        public void Append(FullName fullName) {
            Append(AddUri(fullName.Uri));
            var sb = StringBuilder;
            sb.Append(':');
            sb.Append(fullName.Name);
        }
        public void InsertRootObjectHead(string alias, string name) {
            var sb = Extensions.AcquireStringBuilder();
            sb.Append(alias);
            sb.Append(':');
            sb.Append(name);
            var auCount = _aliasUriList.Count;
            if (auCount > 0) {
                sb.Append(" <");
                for (var i = 0; i < auCount; ++i) {
                    if (i > 0) {
                        sb.Append(' ');
                    }
                    var au = _aliasUriList[i];
                    sb.Append(au.Alias);
                    sb.Append(" = ");
                    sb.Append(au.Uri.ToLiteral());
                }
                sb.Append('>');
            }
            StringBuilder.Insert(StartIndex, sb.ToStringAndRelease());
        }

    }

}
