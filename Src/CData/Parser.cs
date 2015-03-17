using System;
using System.Collections.Generic;
using System.IO;

namespace CData {
    internal struct NameNode : IEquatable<NameNode> {
        public NameNode(string value, TextSpan textSpan) {
            Value = value;
            TextSpan = textSpan;
        }
        public readonly string Value;
        public readonly TextSpan TextSpan;
        public bool IsValid {
            get {
                return Value != null;
            }
        }
        public override string ToString() {
            return Value;
        }
        public bool Equals(NameNode other) {
            return Value == other.Value;
        }
        public override bool Equals(object obj) {
            return obj is NameNode && Equals((NameNode)obj);
        }
        public override int GetHashCode() {
            return Value != null ? Value.GetHashCode() : 0;
        }
        public static bool operator ==(NameNode left, NameNode right) {
            return left.Equals(right);
        }
        public static bool operator !=(NameNode left, NameNode right) {
            return !left.Equals(right);
        }
    }
    internal enum AtomValueKind : byte {
        None = 0,
        String,
        Boolean,
        Null,
        Integer,
        Decimal,
        Real,
    }
    internal struct AtomValueNode {
        public AtomValueNode(AtomValueKind kind, string value, TextSpan textSpan) {
            Kind = kind;
            Value = value;
            TextSpan = textSpan;
        }
        public readonly AtomValueKind Kind;
        public readonly string Value;
        public readonly TextSpan TextSpan;
        public bool IsValid {
            get {
                return Kind != AtomValueKind.None;
            }
        }
        public bool IsNull {
            get {
                return Kind == AtomValueKind.Null;
            }
        }
    }

    internal abstract class ParserBase {
        protected virtual void Set(string filePath, TextReader reader, DiagContext context) {
            if (filePath == null) throw new ArgumentNullException("filePath");
            if (context == null) throw new ArgumentNullException("context");
            _lexer = Lexer.Get(reader);
            _token = null;
            _filePath = filePath;
            _context = context;
        }
        protected virtual void Clear() {
            if (_lexer != null) {
                _lexer.Clear();
            }
            _token = null;
            _filePath = null;
            _context = null;
        }
        private Lexer _lexer;
        private Token? _token;
        protected string _filePath;
        protected DiagContext _context;
        //
        protected sealed class ParsingException : Exception { }
        protected static readonly ParsingException _parsingException = new ParsingException();
        protected void ErrorDiag(int code, string errMsg, TextSpan textSpan) {
            _context.AddDiag(DiagSeverity.Error, code, errMsg, textSpan);
        }
        protected void ErrorDiag(DiagMsg diagMsg, TextSpan textSpan) {
            _context.AddDiag(DiagSeverity.Error, diagMsg, textSpan);
        }
        protected void Throw() {
            throw _parsingException;
        }
        protected void ErrorDiagAndThrow(string errMsg, TextSpan textSpan) {
            ErrorDiag((int)DiagCode.Parsing, errMsg, textSpan);
            Throw();
        }
        protected void ErrorDiagAndThrow(string errMsg, Token token) {
            ErrorDiagAndThrow(errMsg ?? token.Value, token.ToTextSpan(_filePath));
        }
        protected void ErrorDiagAndThrow(string errMsg) {
            ErrorDiagAndThrow(errMsg, GetToken());
        }
        protected void ErrorDiagAndThrow(DiagMsg diagMsg, TextSpan textSpan) {
            ErrorDiag(diagMsg, textSpan);
            Throw();
        }
        protected void ErrorDiagAndThrow(DiagMsg diagMsg) {
            ErrorDiagAndThrow(diagMsg, GetTextSpan());
        }
        //
        private static bool IsTrivalToken(TokenKind tokenKind) {
            return tokenKind == TokenKind.WhitespaceOrNewLine || tokenKind == TokenKind.SingleLineComment || tokenKind == TokenKind.MultiLineComment;
        }
        protected Token GetToken() {
            if (_token != null) {
                return _token.Value;
            }
            while (true) {
                var token = _lexer.GetToken();
                var tokenKind = token.TokenKind;
                if (!IsTrivalToken(tokenKind)) {
                    if (tokenKind == TokenKind.Error) {
                        ErrorDiagAndThrow(null, token);
                    }
                    else {
                        _token = token;
                        return token;
                    }
                }
            }
        }
        protected TextSpan GetTextSpan() {
            return GetToken().ToTextSpan(_filePath);
        }
        protected void ConsumeToken() {
            _token = null;
        }
        protected bool PeekToken(int kind) {
            return GetToken().Kind == kind;
        }
        protected bool PeekToken(int kind1, int kind2) {
            var kind = GetToken().Kind;
            return kind == kind1 || kind == kind2;
        }
        protected bool PeekToken(int kind1, int kind2, int kind3) {
            var kind = GetToken().Kind;
            return kind == kind1 || kind == kind2 || kind == kind3;
        }
        protected bool PeekToken(int kind1, int kind2, int kind3, int kind4) {
            var kind = GetToken().Kind;
            return kind == kind1 || kind == kind2 || kind == kind3 || kind == kind4;
        }
        protected bool Token(int kind, out TextSpan textSpan) {
            var token = GetToken();
            if (token.Kind == kind) {
                textSpan = token.ToTextSpan(_filePath);
                ConsumeToken();
                return true;
            }
            textSpan = default(TextSpan);
            return false;
        }
        protected bool Token(int kind) {
            if (GetToken().Kind == kind) {
                ConsumeToken();
                return true;
            }
            return false;
        }
        protected bool Token(TokenKind tokenKind) {
            return Token((int)tokenKind);
        }
        protected void TokenExpected(char ch) {
            if (!Token(ch)) {
                ErrorDiagAndThrow(ch.ToString() + " expected.");
            }
        }
        protected void TokenExpected(int kind, string errMsg) {
            if (!Token(kind)) {
                ErrorDiagAndThrow(errMsg);
            }
        }
        protected void TokenExpected(TokenKind tokenKind, string errMsg) {
            TokenExpected((int)tokenKind, errMsg);
        }
        public void TokenExpected(char ch, out TextSpan textSpan) {
            if (!Token(ch, out textSpan)) {
                ErrorDiagAndThrow(ch.ToString() + " expected.");
            }
        }
        protected void TokenExpected(int kind, string errMsg, out TextSpan textSpan) {
            if (!Token(kind, out textSpan)) {
                ErrorDiagAndThrow(errMsg);
            }
        }
        protected void EndOfFileExpected() {
            TokenExpected(char.MaxValue, "End of file expected.");
        }
        protected bool Name(out NameNode result) {
            var token = GetToken();
            var kind = token.TokenKind;
            if (kind == TokenKind.Name || kind == TokenKind.VerbatimName) {
                result = new NameNode(token.Value, token.ToTextSpan(_filePath));
                ConsumeToken();
                return true;
            }
            result = default(NameNode);
            return false;
        }
        protected NameNode NameExpected() {
            NameNode name;
            if (!Name(out name)) {
                ErrorDiagAndThrow("Name expected.");
            }
            return name;
        }
        protected bool Keyword(string keywordValue) {
            var token = GetToken();
            if (token.TokenKind == TokenKind.Name && token.Value == keywordValue) {
                ConsumeToken();
                return true;
            }
            return false;
        }
        protected void KeywordExpected(string keywordValue) {
            if (!Keyword(keywordValue)) {
                ErrorDiagAndThrow(keywordValue + " expetced.");
            }
        }
        protected bool Keyword(string keywordValue, out NameNode keyword) {
            var token = GetToken();
            if (token.TokenKind == TokenKind.Name && token.Value == keywordValue) {
                keyword = new NameNode(keywordValue, token.ToTextSpan(_filePath));
                ConsumeToken();
                return true;
            }
            keyword = default(NameNode);
            return false;
        }
        protected bool Keyword(string keywordValue, out TextSpan textSpan) {
            var token = GetToken();
            if (token.TokenKind == TokenKind.Name && token.Value == keywordValue) {
                textSpan = token.ToTextSpan(_filePath);
                ConsumeToken();
                return true;
            }
            textSpan = default(TextSpan);
            return false;
        }
        public bool AtomValue(out AtomValueNode result, AtomValueKind expectedKind = AtomValueKind.None) {
            var token = GetToken();
            var tokenValue = token.Value;
            var kind = AtomValueKind.None;
            switch (token.TokenKind) {
                case TokenKind.StringValue:
                case TokenKind.VerbatimStringValue:
                    kind = AtomValueKind.String;
                    break;
                case TokenKind.Name:
                    if (tokenValue == "null") {
                        kind = AtomValueKind.Null;
                    }
                    else if (tokenValue == "true" || tokenValue == "false") {
                        kind = AtomValueKind.Boolean;
                    }
                    break;
                case TokenKind.IntegerValue:
                    kind = AtomValueKind.Integer;
                    break;
                case TokenKind.DecimalValue:
                    kind = AtomValueKind.Decimal;
                    break;
                case TokenKind.RealValue:
                    kind = AtomValueKind.Real;
                    break;
            }
            if (kind != AtomValueKind.None && (expectedKind == AtomValueKind.None || kind == expectedKind)) {
                result = new AtomValueNode(kind, tokenValue, token.ToTextSpan(_filePath));
                ConsumeToken();
                return true;
            }
            result = default(AtomValueNode);
            return false;
        }
        protected AtomValueNode AtomValueExpected(AtomValueKind expectedKind = AtomValueKind.None) {
            AtomValueNode atomValue;
            if (!AtomValue(out atomValue, expectedKind)) {
                ErrorDiagAndThrow(expectedKind == AtomValueKind.None ? "Atom value expected." :
                    expectedKind.ToString() + " value expected.");
            }
            return atomValue;
        }
        protected bool StringValue(out AtomValueNode result) {
            return AtomValue(out result, AtomValueKind.String);
        }
        protected AtomValueNode StringValueExpected() {
            return AtomValueExpected(AtomValueKind.String);
        }
        protected bool IntegerValue(out AtomValueNode result) {
            return AtomValue(out result, AtomValueKind.Integer);
        }
        protected AtomValueNode IntegerValueExpected() {
            return AtomValueExpected(AtomValueKind.Integer);
        }

    }
    internal sealed class Parser : ParserBase {
        [ThreadStatic]
        private static Parser _instance;
        private static Parser Get(string filePath, TextReader reader, DiagContext context) {
            var instance = _instance ?? (_instance = new Parser());
            instance.Set(filePath, reader, context);
            return instance;
        }
        protected override void Set(string filePath, TextReader reader, DiagContext context) {
            base.Set(filePath, reader, context);
            _uriAliasingListStack.Clear();
        }
        protected override void Clear() {
            base.Clear();
            _uriAliasingListStack.Clear();
        }
        private Parser() {
            _uriAliasingListStack = new Stack<List<UriAliasingNode>>();
        }
        private readonly Stack<List<UriAliasingNode>> _uriAliasingListStack;
        private struct UriAliasingNode {
            public UriAliasingNode(string alias, string uri) {
                Alias = alias;
                Uri = uri;
            }
            public readonly string Alias;
            public readonly string Uri;
        }
        private bool UriAliasingList() {
            if (Token('<')) {
                List<UriAliasingNode> list = null;
                while (true) {
                    NameNode aliasNode;
                    if (Name(out aliasNode)) {
                        var alias = aliasNode.Value;
                        if (list == null) {
                            list = new List<UriAliasingNode>();
                        }
                        else {
                            foreach (var item in list) {
                                if (item.Alias == alias) {
                                    ErrorDiagAndThrow(new DiagMsg(DiagCode.DuplicateUriAlias, alias), aliasNode.TextSpan);
                                }
                            }
                        }
                        TokenExpected('=');
                        list.Add(new UriAliasingNode(alias, StringValueExpected().Value));
                    }
                    else {
                        TokenExpected('>');
                        if (list != null) {
                            _uriAliasingListStack.Push(list);
                            return true;
                        }
                        return false;
                    }
                }
            }
            return false;
        }
        private string GetUri(NameNode aliasNode) {
            var alias = aliasNode.Value;
            foreach (var uaList in _uriAliasingListStack) {
                foreach (var ua in uaList) {
                    if (ua.Alias == alias) {
                        return ua.Uri;
                    }
                }
            }
            ErrorDiagAndThrow(new DiagMsg(DiagCode.InvalidUriReference, alias), aliasNode.TextSpan);
            return null;
        }
        private bool ObjectValue(ObjectTypeMetadata declaredObjectTypeMd, out object result) {
            NameNode aliasNode;
            if (Name(out aliasNode)) {
                TokenExpected(':');
                var nameNode = NameExpected();
                var hasUriAliasingList = UriAliasingList();
                TokenExpected('{');
                var fullName = new FullName(GetUri(aliasNode), nameNode.Value);
                var objectTypeMd = declaredObjectTypeMd.Program.TryGetClassType(fullName);
                if (objectTypeMd == null) {
                    ErrorDiagAndThrow(new DiagMsg(DiagCode.InvalidClassReference, fullName.ToString()), nameNode.TextSpan);
                }
                if (!objectTypeMd.IsEqualToOrDeriveFrom(declaredObjectTypeMd)) {
                    ErrorDiagAndThrow(new DiagMsg(DiagCode.ClassNotEqualToOrDeriveFrom, objectTypeMd.DisplayName, declaredObjectTypeMd.DisplayName),
                        nameNode.TextSpan);
                }
                if (objectTypeMd.IsAbstract) {
                    ErrorDiagAndThrow(new DiagMsg(DiagCode.ClassIsAbstract, objectTypeMd.DisplayName), nameNode.TextSpan);
                }
                var obj = objectTypeMd.CreateInstance();
                if (!objectTypeMd.InvokeOnLoad(true, obj, _context)) {
                    Throw();
                }
                objectTypeMd.SetTextSpan(obj, nameNode.TextSpan);
                List<PropertyMetadata> propMdList = null;
                objectTypeMd.GetAllProperties(ref propMdList);
                while (true) {
                    NameNode propNameNode;
                    if (Name(out propNameNode)) {
                        var propName = propNameNode.Value;
                        PropertyMetadata propMd = null;
                        if (propMdList != null) {
                            for (var i = 0; i < propMdList.Count; ++i) {
                                if (propMdList[i].Name == propName) {
                                    propMd = propMdList[i];
                                    propMdList.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                        if (propMd == null) {
                            ErrorDiagAndThrow(new DiagMsg(DiagCode.InvalidPropertyName, propName), propNameNode.TextSpan);
                        }
                        TokenExpected('=');
                        propMd.SetValue(obj, ValueExpected(propMd.Type));
                    }
                    else {
                        TextSpan ts;
                        TokenExpected('}', out ts);
                        if (propMdList != null && propMdList.Count > 0) {
                            foreach (var propMd in propMdList) {
                                ErrorDiag(new DiagMsg(DiagCode.PropertyMissing, propMd.Name), ts);
                            }
                            Throw();
                        }
                        if (!objectTypeMd.InvokeOnLoad(false, obj, _context)) {
                            Throw();
                        }
                        if (hasUriAliasingList) {
                            _uriAliasingListStack.Pop();
                        }
                        result = obj;
                        return true;
                    }
                }
            }
            result = null;
            return false;
        }
        private object ValueExpected(TypeMetadata typeMd) {
            object value;
            if (Value(typeMd, out value)) {
                return value;
            }
            ErrorDiagAndThrow(new DiagMsg(DiagCode.ValueExpected));
            return null;
        }
        private bool Value(TypeMetadata typeMd, out object result) {
            var typeKind = typeMd.Kind;
            AtomValueNode avNode;
            if (AtomValue(out avNode)) {
                if (avNode.IsNull) {
                    if (!typeMd.IsNullable) {
                        ErrorDiagAndThrow(new DiagMsg(DiagCode.NullNotAllowed), avNode.TextSpan);
                    }
                    result = null;
                    return true;
                }
                if (!typeKind.IsAtom()) {
                    ErrorDiagAndThrow(new DiagMsg(DiagCode.SpecificValueExpected, typeKind.ToString()), avNode.TextSpan);
                }
                result = ParseAtomValue(typeKind, avNode);
                return true;
            }
            else {
                TextSpan ts;
                if (Token('[', out ts)) {
                    var isList = typeKind.IsList();
                    var isSet = typeKind.IsAtomSet() || typeKind.IsObjectSet();
                    if (!isList && !isSet) {
                        ErrorDiagAndThrow(new DiagMsg(DiagCode.SpecificValueExpected, typeKind.ToString()), ts);
                    }
                    var collTypeMd = (CollectionTypeMetadata)typeMd;
                    var collObj = collTypeMd.CreateInstance();
                    var itemTypeMd = collTypeMd.ItemType;
                    while (true) {
                        object itemObj;
                        if (isSet) {
                            ts = GetTextSpan();
                        }
                        if (Value(itemTypeMd, out itemObj)) {
                            if (isSet) {
                                if (!collTypeMd.InvokeBoolAdd(collObj, itemObj)) {
                                    ErrorDiagAndThrow(new DiagMsg(DiagCode.DuplicateSetItem), ts);
                                }
                            }
                            else {
                                collTypeMd.InvokeAdd(collObj, itemObj);
                            }
                        }
                        else {
                            TokenExpected(']');
                            result = collObj;
                            return true;
                        }
                    }
                }
                else if (Token((int)TokenKind.HashOpenBracket, out ts)) {
                    if (!typeKind.IsMap()) {
                        ErrorDiagAndThrow(new DiagMsg(DiagCode.SpecificValueExpected, typeKind.ToString()), ts);
                    }
                    var collTypeMd = (CollectionTypeMetadata)typeMd;
                    var collObj = collTypeMd.CreateInstance();
                    var keyTypeMd = collTypeMd.KeyType;
                    var itemTypeMd = collTypeMd.ItemType;
                    while (true) {
                        object keyObj;
                        ts = GetTextSpan();
                        if (Value(keyTypeMd, out keyObj)) {
                            if (collTypeMd.InvokeContainsKey(collObj, keyObj)) {
                                ErrorDiagAndThrow(new DiagMsg(DiagCode.DuplicateMapKey), ts);
                            }
                            TokenExpected('=');
                            collTypeMd.InvokeAdd(collObj, keyObj, ValueExpected(itemTypeMd));
                        }
                        else {
                            TokenExpected(']');
                            result = collObj;
                            return true;
                        }
                    }
                }
                else if (PeekToken((int)TokenKind.Name, (int)TokenKind.VerbatimName)) {
                    if (!typeKind.IsObject()) {
                        ErrorDiagAndThrow(new DiagMsg(DiagCode.SpecificValueExpected, typeKind.ToString()));
                    }
                    return ObjectValue((ObjectTypeMetadata)typeMd, out result);
                }
            }
            result = null;
            return false;
        }
        private object ParseAtomValue(TypeKind typeKind, AtomValueNode avNode) {
            var s = avNode.Value;
            switch (typeKind) {
                case TypeKind.String:
                    return s;
                case TypeKind.IgnoreCaseString:
                    return new IgnoreCaseString(s);
                case TypeKind.Decimal: {
                        decimal r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.Int64: {
                        long r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.Int32: {
                        int r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.Int16: {
                        short r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.SByte: {
                        sbyte r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.UInt64: {
                        ulong r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.UInt32: {
                        uint r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.UInt16: {
                        ushort r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.Byte: {
                        byte r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.Double: {
                        double r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.Single: {
                        float r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.Boolean: {
                        bool r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.Binary: {
                        byte[] r;
                        if (s.TryInvParse(out r)) {
                            return new BinaryValue(r);
                        }
                    }
                    break;
                case TypeKind.Guid: {
                        Guid r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.TimeSpan: {
                        TimeSpan r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;
                case TypeKind.DateTimeOffset: {
                        DateTimeOffset r;
                        if (s.TryInvParse(out r)) {
                            return r;
                        }
                    }
                    break;

                default:
                    throw new InvalidOperationException("Invalid type kind: " + typeKind.ToString());
            }
            ErrorDiagAndThrow(new DiagMsg(DiagCode.InvalidAtomValue, s, typeKind.ToString()), avNode.TextSpan);
            return null;
        }

    }
}
