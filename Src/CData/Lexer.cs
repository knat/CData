using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace CData {
    internal struct Token {
        public Token(int kind, int startIndex, int length, TextPosition startPosition, TextPosition endPosition, string value) {
            Kind = kind;
            StartIndex = startIndex;
            Length = length;
            StartPosition = startPosition;
            EndPosition = endPosition;
            Value = value;
        }
        public readonly int Kind;
        public readonly int StartIndex;
        public readonly int Length;
        public readonly TextPosition StartPosition;
        public readonly TextPosition EndPosition;
        public readonly string Value;//for TokenKind.Name to TokenKind.RealValue and TokenKind.Error
        public TokenKind TokenKind {
            get {
                return (TokenKind)Kind;
            }
        }
        public bool IsEndOfFile {
            get {
                return Kind == char.MaxValue;
            }
        }
        public bool IsError {
            get {
                return TokenKind == TokenKind.Error;
            }
        }
        public TextSpan ToTextSpan(string filePath) {
            return new TextSpan(filePath, this);
        }
    }
    internal enum TokenKind {
        Error = -1000,
        WhitespaceOrNewLine,
        SingleLineComment,
        MultiLineComment,
        Name,
        VerbatimName,// @name
        StringValue,
        VerbatimStringValue,// @"..."
        CharValue,// 'c'
        IntegerValue,// 123
        DecimalValue,// 123.45
        RealValue,// 123.45Ee+-12
        HashOpenBracket,// #[//DEL
        ColonColon,// ::
        EqualsEquals,// ==
        EqualsGreaterThan,// =>
        ExclamationEquals,// !=
        LessThanEquals,// <=
        LessThanLessThan,// <<
        GreaterThanEquals,// >=
        //no '>>'
        BarBar,// ||
        AmpersandAmpersand,// &&
        QuestionQuestion,// ??

    }
    internal sealed class Lexer {
        [ThreadStatic]
        private static Lexer _instance;
        private static Lexer Instance {
            get { return _instance ?? (_instance = new Lexer()); }
        }
        public static Lexer Get(TextReader reader) {
            return Instance.Set(reader);
        }
        private Lexer() {
            _buf = new char[_bufLength];
        }
        private const int _bufLength = 1024;
        private TextReader _reader;
        private readonly char[] _buf;
        private int _index, _count;
        private bool _isEOF;
        private int _totalIndex;
        private int _lastLine, _lastColumn, _line, _column;
        private const int _stringBuilderCapacity = 256;
        private StringBuilder _stringBuilder;
        private Lexer Set(TextReader reader) {
            if (reader == null) throw new ArgumentNullException("reader");
            _reader = reader;
            _index = _count = 0;
            _isEOF = false;
            _totalIndex = 0;
            _lastLine = _lastColumn = _line = _column = 1;
            if (_stringBuilder == null) {
                _stringBuilder = new StringBuilder(_stringBuilderCapacity);
            }
            return this;
        }
        public void Clear() {
            _reader = null;
            if (_stringBuilder != null && _stringBuilder.Capacity > _stringBuilderCapacity * 8) {
                _stringBuilder = null;
            }
        }
        private StringBuilder GetStringBuilder() {
            return _stringBuilder.Clear();
        }
        private char GetChar(int offset = 0) {
            var pos = _index + offset;
            if (pos < _count) {
                return _buf[pos];
            }
            if (_isEOF) {
                return char.MaxValue;
            }
            var remainCount = _count - _index;
            if (remainCount > 0) {
                for (var i = 0; i < remainCount; ++i) {
                    _buf[i] = _buf[_index + i];
                }
            }
            var retCount = _reader.Read(_buf, remainCount, _bufLength - remainCount);
            if (retCount == 0) {
                _isEOF = true;
            }
            _index = 0;
            _count = remainCount + retCount;
            return GetChar(offset);
        }
        private char GetNextChar() {
            return GetChar(1);
        }
        private char GetNextNextChar() {
            return GetChar(2);
        }
        private void AdvanceChar() {
            _lastLine = _line;
            _lastColumn = _column;
            if (_index < _count) {
                var ch = _buf[_index++];
                ++_totalIndex;
                if (IsNewLine(ch)) {
                    if (ch == '\r' && GetChar() == '\n') {
                        ++_index;
                        ++_totalIndex;
                    }
                    ++_line;
                    _column = 1;
                }
                else {
                    ++_column;
                }
            }
        }
        private enum StateKind : byte {
            None = 0,
            InWhitespaceOrNewLine,
            InSingleLineComment,
            InMultiLineComment,
            InName,
            InVerbatimName,
            InStringValue,
            InVerbatimStringValue,
            InCharValue,
            InNumericValueInteger,
            InNumericValueFraction,
            InNumericValueExponent,
        }
        private struct State {
            public State(StateKind kind, int index, int line, int column) {
                Kind = kind;
                Index = index;
                Line = line;
                Column = column;
            }
            public StateKind Kind;
            public readonly int Index;
            public readonly int Line;
            public readonly int Column;
        }
        private State CreateState(StateKind kind) {
            return new State(kind, _totalIndex, _line, _column);
        }
        private Token CreateToken(TokenKind tokenKind, State state, string value = null) {
            var startIndex = state.Index;
            return new Token((int)tokenKind, startIndex, _totalIndex - startIndex,
                new TextPosition(state.Line, state.Column), new TextPosition(_lastLine, _lastColumn), value);
        }
        private Token CreateTokenAndAdvanceChar(char ch) {
            var pos = new TextPosition(_line, _column);
            var token = new Token(ch, _totalIndex, _index < _count ? 1 : 0, pos, pos, null);
            AdvanceChar();
            return token;
        }
        private Token CreateErrorToken(string errorMessage) {
            var pos = new TextPosition(_line, _column);
            return new Token((int)TokenKind.Error, _totalIndex, _index < _count ? 1 : 0, pos, pos, errorMessage);
        }
        public Token GetToken() {
            var state = default(State);
            StringBuilder sb = null;
            while (true) {
                var ch = GetChar();
                var stateKind = state.Kind;
                if (stateKind == StateKind.InWhitespaceOrNewLine) {
                    if (IsWhitespace(ch) || IsNewLine(ch)) {
                        AdvanceChar();
                    }
                    else {
                        return CreateToken(TokenKind.WhitespaceOrNewLine, state);
                    }
                }
                else if (stateKind == StateKind.InSingleLineComment) {
                    if (IsNewLine(ch) || ch == char.MaxValue) {
                        return CreateToken(TokenKind.SingleLineComment, state);
                    }
                    else {
                        AdvanceChar();
                    }
                }
                else if (stateKind == StateKind.InMultiLineComment) {
                    if (ch == '*') {
                        AdvanceChar();
                        ch = GetChar();
                        if (ch == '/') {
                            AdvanceChar();
                            return CreateToken(TokenKind.MultiLineComment, state);
                        }
                    }
                    else if (ch == char.MaxValue) {
                        return CreateErrorToken("*/ expected.");
                    }
                    else {
                        AdvanceChar();
                    }
                }
                else if (stateKind == StateKind.InName || stateKind == StateKind.InVerbatimName) {
                    if (IsNamePartChar(ch)) {
                        sb.Append(ch);
                        AdvanceChar();
                    }
                    else {
                        return CreateToken(stateKind == StateKind.InName ? TokenKind.Name : TokenKind.VerbatimName, state, sb.ToString());
                    }
                }
                else if (stateKind == StateKind.InStringValue) {
                    if (ch == '\\') {
                        AdvanceChar();
                        Token errToken;
                        if (!ProcessCharEscSeq(sb, out errToken)) {
                            return errToken;
                        }
                    }
                    else if (ch == '"') {
                        AdvanceChar();
                        return CreateToken(TokenKind.StringValue, state, sb.ToString());
                    }
                    else if (IsNewLine(ch) || ch == char.MaxValue) {
                        return CreateErrorToken("\" expected.");
                    }
                    else {
                        sb.Append(ch);
                        AdvanceChar();
                    }
                }
                else if (stateKind == StateKind.InVerbatimStringValue) {
                    if (ch == '"') {
                        AdvanceChar();
                        ch = GetChar();
                        if (ch == '"') {
                            sb.Append('"');
                            AdvanceChar();
                        }
                        else {
                            return CreateToken(TokenKind.VerbatimStringValue, state, sb.ToString());
                        }
                    }
                    else if (ch == char.MaxValue) {
                        return CreateErrorToken("\" expected.");
                    }
                    else {
                        sb.Append(ch);
                        AdvanceChar();
                    }
                }
                else if (stateKind == StateKind.InCharValue) {
                    if (ch == '\\') {
                        AdvanceChar();
                        Token errToken;
                        if (!ProcessCharEscSeq(sb, out errToken)) {
                            return errToken;
                        }
                    }
                    else if (ch == '\'') {
                        if (sb.Length == 1) {
                            AdvanceChar();
                            return CreateToken(TokenKind.CharValue, state, sb.ToString());
                        }
                        else {
                            return CreateErrorToken("Character expected.");
                        }
                    }
                    else if (IsNewLine(ch) || ch == char.MaxValue) {
                        return CreateErrorToken("' expected.");
                    }
                    else {
                        if (sb.Length == 0) {
                            sb.Append(ch);
                            AdvanceChar();
                        }
                        else {
                            return CreateErrorToken("' expected.");
                        }
                    }
                }
                else if (stateKind == StateKind.InNumericValueInteger) {
                    if (IsDecDigit(ch)) {
                        sb.Append(ch);
                        AdvanceChar();
                    }
                    else if (ch == '.') {
                        var nextch = GetNextChar();
                        if (IsDecDigit(nextch)) {
                            state.Kind = StateKind.InNumericValueFraction;
                            sb.Append(ch);
                            sb.Append(nextch);
                            AdvanceChar();
                            AdvanceChar();
                        }
                        else {
                            return CreateToken(TokenKind.IntegerValue, state, sb.ToString());
                        }
                    }
                    else if (ch == 'E' || ch == 'e') {
                        sb.Append(ch);
                        AdvanceChar();
                        ch = GetChar();
                        if (ch == '+' || ch == '-') {
                            sb.Append(ch);
                            AdvanceChar();
                            ch = GetChar();
                        }
                        if (IsDecDigit(ch)) {
                            state.Kind = StateKind.InNumericValueExponent;
                            sb.Append(ch);
                            AdvanceChar();
                        }
                        else {
                            return CreateErrorToken("Decimal digit expected.");
                        }
                    }
                    else {
                        return CreateToken(TokenKind.IntegerValue, state, sb.ToString());
                    }
                }
                else if (stateKind == StateKind.InNumericValueFraction) {
                    if (IsDecDigit(ch)) {
                        sb.Append(ch);
                        AdvanceChar();
                    }
                    else if (ch == 'E' || ch == 'e') {
                        sb.Append(ch);
                        AdvanceChar();
                        ch = GetChar();
                        if (ch == '+' || ch == '-') {
                            sb.Append(ch);
                            AdvanceChar();
                            ch = GetChar();
                        }
                        if (IsDecDigit(ch)) {
                            state.Kind = StateKind.InNumericValueExponent;
                            sb.Append(ch);
                            AdvanceChar();
                        }
                        else {
                            return CreateErrorToken("Decimal digit expected.");
                        }
                    }
                    else {
                        return CreateToken(TokenKind.DecimalValue, state, sb.ToString());
                    }
                }
                else if (stateKind == StateKind.InNumericValueExponent) {
                    if (IsDecDigit(ch)) {
                        sb.Append(ch);
                        AdvanceChar();
                    }
                    else {
                        return CreateToken(TokenKind.RealValue, state, sb.ToString());
                    }
                }
                //
                //
                //
                else if (ch == char.MaxValue) {
                    return CreateTokenAndAdvanceChar(ch);
                }
                else if (IsWhitespace(ch) || IsNewLine(ch)) {
                    state = CreateState(StateKind.InWhitespaceOrNewLine);
                    AdvanceChar();
                }
                else if (ch == '/') {
                    var nextch = GetNextChar();
                    if (nextch == '/') {
                        state = CreateState(StateKind.InSingleLineComment);
                        AdvanceChar();
                        AdvanceChar();
                    }
                    else if (nextch == '*') {
                        state = CreateState(StateKind.InMultiLineComment);
                        AdvanceChar();
                        AdvanceChar();
                    }
                    else {
                        return CreateTokenAndAdvanceChar(ch);
                    }
                }
                else if (ch == '@') {
                    var nextch = GetNextChar();
                    if (nextch == '"') {
                        state = CreateState(StateKind.InVerbatimStringValue);
                        AdvanceChar();
                        AdvanceChar();
                        sb = GetStringBuilder();
                    }
                    else if (IsNameStartChar(nextch)) {
                        state = CreateState(StateKind.InVerbatimName);
                        AdvanceChar();
                        AdvanceChar();
                        sb = GetStringBuilder();
                        sb.Append(nextch);
                    }
                    else {
                        return CreateTokenAndAdvanceChar(ch);
                    }
                }
                else if (IsNameStartChar(ch)) {
                    state = CreateState(StateKind.InName);
                    AdvanceChar();
                    sb = GetStringBuilder();
                    sb.Append(ch);
                }
                else if (ch == '"') {
                    state = CreateState(StateKind.InStringValue);
                    AdvanceChar();
                    sb = GetStringBuilder();
                }
                else if (ch == '\'') {
                    state = CreateState(StateKind.InCharValue);
                    AdvanceChar();
                    sb = GetStringBuilder();
                }
                else if (IsDecDigit(ch)) {
                    state = CreateState(StateKind.InNumericValueInteger);
                    AdvanceChar();
                    sb = GetStringBuilder();
                    sb.Append(ch);
                }
                else if (ch == '.') {
                    var nextch = GetNextChar();
                    if (IsDecDigit(nextch)) {
                        state = CreateState(StateKind.InNumericValueFraction);
                        AdvanceChar();
                        AdvanceChar();
                        sb = GetStringBuilder();
                        sb.Append(ch);
                        sb.Append(nextch);
                    }
                    else {
                        return CreateTokenAndAdvanceChar(ch);
                    }
                }
                else if (ch == '#') {
                    var nextch = GetNextChar();
                    if (nextch == '[') {
                        state = CreateState(StateKind.None);
                        AdvanceChar();
                        AdvanceChar();
                        return CreateToken(TokenKind.HashOpenBracket, state);
                    }
                    else {
                        return CreateTokenAndAdvanceChar(ch);
                    }
                }
                else if (ch == ':') {
                    var nextch = GetNextChar();
                    if (nextch == ':') {
                        state = CreateState(StateKind.None);
                        AdvanceChar();
                        AdvanceChar();
                        return CreateToken(TokenKind.ColonColon, state);
                    }
                    else {
                        return CreateTokenAndAdvanceChar(ch);
                    }
                }
                else if (ch == '=') {
                    var nextch = GetNextChar();
                    if (nextch == '=') {
                        state = CreateState(StateKind.None);
                        AdvanceChar();
                        AdvanceChar();
                        return CreateToken(TokenKind.EqualsEquals, state);
                    }
                    else if (nextch == '>') {
                        state = CreateState(StateKind.None);
                        AdvanceChar();
                        AdvanceChar();
                        return CreateToken(TokenKind.EqualsGreaterThan, state);
                    }
                    else {
                        return CreateTokenAndAdvanceChar(ch);
                    }
                }
                else if (ch == '!') {
                    var nextch = GetNextChar();
                    if (nextch == '=') {
                        state = CreateState(StateKind.None);
                        AdvanceChar();
                        AdvanceChar();
                        return CreateToken(TokenKind.ExclamationEquals, state);
                    }
                    else {
                        return CreateTokenAndAdvanceChar(ch);
                    }
                }
                else if (ch == '<') {
                    var nextch = GetNextChar();
                    if (nextch == '=') {
                        state = CreateState(StateKind.None);
                        AdvanceChar();
                        AdvanceChar();
                        return CreateToken(TokenKind.LessThanEquals, state);
                    }
                    else if (nextch == '<') {
                        state = CreateState(StateKind.None);
                        AdvanceChar();
                        AdvanceChar();
                        return CreateToken(TokenKind.LessThanLessThan, state);
                    }
                    else {
                        return CreateTokenAndAdvanceChar(ch);
                    }
                }
                else if (ch == '>') {
                    var nextch = GetNextChar();
                    if (nextch == '=') {
                        state = CreateState(StateKind.None);
                        AdvanceChar();
                        AdvanceChar();
                        return CreateToken(TokenKind.GreaterThanEquals, state);
                    }
                    else {
                        return CreateTokenAndAdvanceChar(ch);
                    }
                }
                else if (ch == '|') {
                    var nextch = GetNextChar();
                    if (nextch == '|') {
                        state = CreateState(StateKind.None);
                        AdvanceChar();
                        AdvanceChar();
                        return CreateToken(TokenKind.BarBar, state);
                    }
                    else {
                        return CreateTokenAndAdvanceChar(ch);
                    }
                }
                else if (ch == '&') {
                    var nextch = GetNextChar();
                    if (nextch == '&') {
                        state = CreateState(StateKind.None);
                        AdvanceChar();
                        AdvanceChar();
                        return CreateToken(TokenKind.AmpersandAmpersand, state);
                    }
                    else {
                        return CreateTokenAndAdvanceChar(ch);
                    }
                }
                else if (ch == '?') {
                    var nextch = GetNextChar();
                    if (nextch == '?') {
                        state = CreateState(StateKind.None);
                        AdvanceChar();
                        AdvanceChar();
                        return CreateToken(TokenKind.QuestionQuestion, state);
                    }
                    else {
                        return CreateTokenAndAdvanceChar(ch);
                    }
                }


                else {
                    return CreateTokenAndAdvanceChar(ch);
                }
            }
        }
        private bool ProcessCharEscSeq(StringBuilder sb, out Token errToken) {
            var ch = GetChar();
            switch (ch) {
                case '\'': sb.Append('\''); break;
                case '"': sb.Append('"'); break;
                case '\\': sb.Append('\\'); break;
                case '0': sb.Append('\0'); break;
                case 'a': sb.Append('\a'); break;
                case 'b': sb.Append('\b'); break;
                case 'f': sb.Append('\f'); break;
                case 'n': sb.Append('\n'); break;
                case 'r': sb.Append('\r'); break;
                case 't': sb.Append('\t'); break;
                case 'v': sb.Append('\v'); break;
                case 'u': {
                        AdvanceChar();
                        int value = 0;
                        for (var i = 0; i < 4; ++i) {
                            ch = GetChar();
                            if (IsHexDigit(ch)) {
                                value <<= 4;
                                value |= HexValue(ch);
                                AdvanceChar();
                            }
                            else {
                                errToken = CreateErrorToken("Invalid character escape sequence.");
                                return false;
                            }
                        }
                        sb.Append((char)value);
                        errToken = default(Token);
                        return true;
                    }
                default:
                    errToken = CreateErrorToken("Invalid character escape sequence.");
                    return false;
            }
            AdvanceChar();
            errToken = default(Token);
            return true;
        }
        #region helpers
        private static bool IsNewLine(char ch) {
            return ch == '\r'
                || ch == '\n'
                || ch == '\u0085'
                || ch == '\u2028'
                || ch == '\u2029';
        }
        private static bool IsWhitespace(char ch) {
            return ch == ' '
                || ch == '\t'
                || ch == '\v'
                || ch == '\f'
                || ch == '\u00A0'
                || ch == '\uFEFF'
                || ch == '\u001A'
                || (ch > 255 && CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.SpaceSeparator);
        }
        private static bool IsDecDigit(char ch) {
            return ch >= '0' && ch <= '9';
        }
        private static bool IsHexDigit(char ch) {
            return (ch >= '0' && ch <= '9') ||
                   (ch >= 'A' && ch <= 'F') ||
                   (ch >= 'a' && ch <= 'f');
        }
        private static int DecValue(char ch) {
            return ch - '0';
        }
        private static int HexValue(char ch) {
            return (ch >= '0' && ch <= '9') ? ch - '0' : (ch & 0xdf) - 'A' + 10;
        }

        public static bool IsNameStartChar(char ch) {
            // identifier-start-character:
            //   letter-character
            //   _ (the underscore character U+005F)

            if (ch < 'a') // '\u0061'
            {
                if (ch < 'A') // '\u0041'
                {
                    return false;
                }

                return ch <= 'Z'  // '\u005A'
                    || ch == '_'; // '\u005F'
            }

            if (ch <= 'z') // '\u007A'
            {
                return true;
            }

            if (ch <= '\u007F') // max ASCII
            {
                return false;
            }

            return IsLetterChar(CharUnicodeInfo.GetUnicodeCategory(ch));
        }
        public static bool IsNamePartChar(char ch) {
            // identifier-part-character:
            //   letter-character
            //   decimal-digit-character
            //   connecting-character
            //   combining-character
            //   formatting-character

            if (ch < 'a') // '\u0061'
            {
                if (ch < 'A') // '\u0041'
                {
                    return ch >= '0'  // '\u0030'
                        && ch <= '9'; // '\u0039'
                }

                return ch <= 'Z'  // '\u005A'
                    || ch == '_'; // '\u005F'
            }

            if (ch <= 'z') // '\u007A'
            {
                return true;
            }

            if (ch <= '\u007F') // max ASCII
            {
                return false;
            }

            UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            return IsLetterChar(cat)
                || IsDecimalDigitChar(cat)
                || IsConnectingChar(cat)
                || IsCombiningChar(cat)
                || IsFormattingChar(cat);
        }
        private static bool IsLetterChar(UnicodeCategory cat) {
            // letter-character:
            //   A Unicode character of classes Lu, Ll, Lt, Lm, Lo, or Nl 
            //   A Unicode-escape-sequence representing a character of classes Lu, Ll, Lt, Lm, Lo, or Nl

            switch (cat) {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.OtherLetter:
                case UnicodeCategory.LetterNumber:
                    return true;
            }

            return false;
        }
        private static bool IsCombiningChar(UnicodeCategory cat) {
            // combining-character:
            //   A Unicode character of classes Mn or Mc 
            //   A Unicode-escape-sequence representing a character of classes Mn or Mc

            switch (cat) {
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.SpacingCombiningMark:
                    return true;
            }

            return false;
        }
        private static bool IsDecimalDigitChar(UnicodeCategory cat) {
            // decimal-digit-character:
            //   A Unicode character of the class Nd 
            //   A unicode-escape-sequence representing a character of the class Nd

            return cat == UnicodeCategory.DecimalDigitNumber;
        }
        private static bool IsConnectingChar(UnicodeCategory cat) {
            // connecting-character:  
            //   A Unicode character of the class Pc
            //   A unicode-escape-sequence representing a character of the class Pc

            return cat == UnicodeCategory.ConnectorPunctuation;
        }
        private static bool IsFormattingChar(UnicodeCategory cat) {
            // formatting-character:  
            //   A Unicode character of the class Cf
            //   A unicode-escape-sequence representing a character of the class Cf

            return cat == UnicodeCategory.Format;
        }
        #endregion helpers
    }


}
