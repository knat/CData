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
                for (var i = 0; i < _indentCount; ++i) {
                    StringBuilder.Append(IndentString);
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
        public void AppendLine(string s) {
            Append(s);
            AppendLine();
        }
        public void AppendLine(char ch) {
            Append(ch);
            AppendLine();
        }
    }
    internal sealed class SavingContext : IndentedStringBuilder {
        public SavingContext(StringBuilder stringBuilder, string indentString, string newLineString) :
            base(stringBuilder, indentString, newLineString) {
        }
        private struct AliasUri {
            public AliasUri(string alias, string uri) {
                Alias = alias;
                Uri = uri;
            }
            public readonly string Alias, Uri;
        }
        private List<AliasUri> _aliasUriList;
        public string AddUri(string uri) {
            if (string.IsNullOrEmpty(uri)) {
                return null;
            }
            if (uri == Extensions.SystemUri) {
                return "sys";
            }
            if (_aliasUriList != null) {
                foreach (var au in _aliasUriList) {
                    if (au.Uri == uri) {
                        return au.Alias;
                    }
                }
            }
            else {
                _aliasUriList = new List<AliasUri>();
            }
            var alias = "a" + _aliasUriList.Count.ToInvString();
            _aliasUriList.Add(new AliasUri(alias, uri));
            return alias;
        }
        public void Append(FullName fullName) {
            var alias = AddUri(fullName.Uri);
            if (alias != null) {
                Append(alias);
                StringBuilder.Append(':');
            }
            Append(fullName.Name);
        }
        public void InsertRootElement(string alias, string name) {
            var sb = Extensions.AcquireStringBuilder();
            if (alias != null) {
                sb.Append(alias);
                sb.Append(':');
            }
            sb.Append(name);
            var auCount = _aliasUriList.CountOrZero();
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
