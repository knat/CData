using System;
using System.Collections.Generic;
using System.IO;

namespace CData {
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
            ErrorDiagAndThrow(diagMsg, GetToken().ToTextSpan(_filePath));
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
            var kind = AtomValueKind.None;
            var token = GetToken();
            var tokenValue = token.Value;
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
        private bool ClassValue(ClassTypeMetadata declaredClassTypeMd, out object result) {
            NameNode aliasNode;
            if (Name(out aliasNode)) {
                TokenExpected(':');
                var nameNode = NameExpected();
                var hasUriAliasingList = UriAliasingList();
                TokenExpected('{');
                var fullName = new FullName(GetUri(aliasNode), nameNode.Value);
                var classTypeMd = declaredClassTypeMd.Program.TryGetClassType(fullName);
                if (classTypeMd == null) {
                    ErrorDiagAndThrow(new DiagMsg(DiagCode.InvalidClassReference, fullName.ToString()), nameNode.TextSpan);
                }
                if (!classTypeMd.IsEqualToOrDeriveFrom(declaredClassTypeMd)) {
                    ErrorDiagAndThrow(new DiagMsg(DiagCode.ClassNotEqualToOrDeriveFrom, classTypeMd.DisplayName, declaredClassTypeMd.DisplayName),
                        nameNode.TextSpan);
                }
                if (classTypeMd.IsAbstract) {
                    ErrorDiagAndThrow(new DiagMsg(DiagCode.ClassIsAbstract, classTypeMd.DisplayName), nameNode.TextSpan);
                }
                var obj = classTypeMd.CreateInstance();
                if (!classTypeMd.InvokeOnLoadMethod(true, obj, _context)) {
                    Throw();
                }
                List<PropertyMetadata> propMdList = null;
                classTypeMd.GetAllProperties(ref propMdList);
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
                        if (!classTypeMd.InvokeOnLoadMethod(false, obj, _context)) {
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
            result = null;
            var typeKind = typeMd.Kind;
            AtomValueNode atomValueNode;
            if (AtomValue(out atomValueNode)) {
                if (atomValueNode.IsNull) {
                    if (!typeMd.IsNullable) {
                        ErrorDiagAndThrow(new DiagMsg(DiagCode.TypeIsNotNullable, typeMd.DisplayName), atomValueNode.TextSpan);
                    }
                    return true;
                }
                if (!typeKind.IsAtom()) {
                    ErrorDiagAndThrow(new DiagMsg(DiagCode.SpecificValueExpected, typeKind.ToString()), atomValueNode.TextSpan);
                }
                result = ParseAtomValue(typeKind, atomValueNode);
                return true;
            }
            else {
                TextSpan ts;
                if (Token('[', out ts)) {
                    var isList = typeKind.IsList();
                    var isSet = typeKind.IsSet();
                    if (!isList && !isSet) {
                        ErrorDiagAndThrow(new DiagMsg(DiagCode.SpecificValueExpected, typeKind.ToString()), ts);
                    }
                    var itemTypeMd = ((ItemCollectionTypeMetadata)typeMd).ItemType;
                    var setTypeMd = isSet ? (SetTypeMetadata)typeMd : null;
                    object collValue = null;
                    while (true) {
                        object itemValue;
                        if (Value(itemTypeMd, out itemValue)) {
                            if (collValue == null) {

                            }
                            if (isSet) {

                            }
                            else {

                            }

                        }
                        else {
                            TokenExpected(']');

                            return true;
                        }
                    }


                }
                else if (Token((int)TokenKind.DollarOpenBracket, out ts)) {
                    if (!typeKind.IsMap()) {
                        ErrorDiagAndThrow(new DiagMsg(DiagCode.SpecificValueExpected, typeKind.ToString()), ts);
                    }

                    TokenExpected(']');
                }
                else if (PeekToken((int)TokenKind.Name, (int)TokenKind.VerbatimName)) {
                    if (!typeKind.IsClass()) {
                        ErrorDiagAndThrow(new DiagMsg(DiagCode.SpecificValueExpected, typeKind.ToString()));
                    }
                    return ClassValue((ClassTypeMetadata)typeMd, out result);
                }
            }
            return false;
        }
        private object ParseAtomValue(TypeKind typeKind, AtomValueNode atomValueNode) {
            var s = atomValueNode.Value;
            switch (typeKind) {
                case TypeKind.String:
                case TypeKind.IgnoreCaseString:
                    return s;
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
            ErrorDiagAndThrow(new DiagMsg(DiagCode.InvalidAtomValue, s, typeKind.ToString()), atomValueNode.TextSpan);
            return null;
        }

        //public TextSpan ClassEndExpected(bool hasUriAliasingList) {
        //    TextSpan endTs;
        //    TokenExpected('}', out endTs);
        //    if (hasUriAliasingList) {
        //        _uriAliasingListStack.Pop();
        //    }
        //    return endTs;
        //}

        //public bool PropertyName(out NameNode name) {
        //    if (Name(out name)) {
        //        TokenExpected('=');
        //        return true;
        //    }
        //    return false;
        //}
        //public bool ListOrSetValueBegin(out TextSpan textSpan) {
        //    return Token('[', out textSpan);
        //}



        //private bool ParsingUnit(string filePath, TextReader reader, DiagContext context, out ElementNode result) {
        //    Set(filePath, reader, context);
        //    _uriAliasingListStack.Clear();
        //    try {
        //        if (Element(out result)) {
        //            EndOfFileExpected();
        //            return true;
        //        }
        //        else {
        //            ErrorDiagAndThrow("Element expected.");
        //        }
        //    }
        //    catch (ParsingException) {
        //    }
        //    finally {
        //        Clear();
        //    }
        //    result = default(ElementNode);
        //    return false;
        //}
        //private bool UriAliasing(List<UriAliasingNode> list, out UriAliasingNode result) {
        //    NameNode alias;
        //    if (Name(out alias)) {
        //        if (alias.Value == "sys") {
        //            ErrorDiagAndThrow(new DiagMsg(DiagCode.AliasSysIsReserved), alias.TextSpan);
        //        }
        //        if (list.CountOrZero() > 0) {
        //            foreach (var item in list) {
        //                if (item.Alias == alias) {
        //                    ErrorDiagAndThrow(new DiagMsg(DiagCode.DuplicateUriAlias, alias.ToString()), alias.TextSpan);
        //                }
        //            }
        //        }
        //        TokenExpected('=');
        //        result = new UriAliasingNode(alias, StringValueExpected());
        //        return true;
        //    }
        //    result = default(UriAliasingNode);
        //    return false;
        //}
        //protected override bool QualifiableName(out QualifiableNameNode result) {
        //    NameNode name;
        //    if (Name(out name)) {
        //        if (Token(':')) {
        //            result = new QualifiableNameNode(name, NameExpected());
        //        }
        //        else {
        //            result = new QualifiableNameNode(default(NameNode), name);
        //        }
        //        if (_getFullName) {
        //            GetFullName(ref result);
        //        }
        //        return true;
        //    }
        //    result = default(QualifiableNameNode);
        //    return false;
        //}
        //private void GetFullName(ref QualifiableNameNode qName) {
        //    string uri = null;
        //    if (qName.Alias.IsValid) {
        //        uri = GetUri(qName.Alias);
        //    }
        //    qName.FullName = new FullName(uri, qName.Name.Value);
        //}
        //private string GetUri(NameNode alias) {
        //    if (alias.Value == "sys") {
        //        return Extensions.SystemUri;
        //    }
        //    foreach (var uaList in _uriAliasingListStack) {
        //        foreach (var ua in uaList) {
        //            if (ua.Alias == alias) {
        //                return ua.Uri.Value;
        //            }
        //        }
        //    }
        //    ErrorDiagAndThrow(new DiagMsg(DiagCode.InvalidUriReference, alias.ToString()), alias.TextSpan);
        //    return null;
        //}
        //private bool Element(out ElementNode result) {
        //    QualifiableNameNode qName;
        //    _getFullName = false;
        //    var hasQName = QualifiableName(out qName);
        //    _getFullName = true;
        //    if (hasQName) {
        //        var hasUriAliasingList = UriAliasingList();
        //        GetFullName(ref qName);
        //        var elementValue = default(ElementValueNode);
        //        TextSpan equalsTextSpan;
        //        if (Token('=', out equalsTextSpan)) {
        //            if (!ElementValue(equalsTextSpan, out elementValue)) {
        //                ErrorDiagAndThrow("Element value expected.");
        //            }
        //        }
        //        if (hasUriAliasingList) {
        //            _uriAliasingListStack.Pop();
        //        }
        //        result = new ElementNode(qName, elementValue);
        //        return true;
        //    }
        //    result = default(ElementNode);
        //    return false;
        //}
        //private bool ElementValue(TextSpan equalsTextSpan, out ElementValueNode result) {
        //    QualifiableNameNode typeQName;
        //    var hasTypeQName = TypeIndicator(out typeQName);
        //    ComplexValueNode complexValue;
        //    var simpleValue = default(SimpleValueNode);
        //    if (!ComplexValue(equalsTextSpan, typeQName, out complexValue)) {
        //        if (!SimpleValue(typeQName, out simpleValue)) {
        //            if (hasTypeQName) {
        //                ErrorDiagAndThrow("Complex value or simple value expetced.");
        //            }
        //            result = default(ElementValueNode);
        //            return false;
        //        }
        //    }
        //    result = new ElementValueNode(complexValue, simpleValue);
        //    return true;
        //}
        //private bool ComplexValue(TextSpan equalsTextSpan, QualifiableNameNode typeQName, out ComplexValueNode result) {
        //    NodeList<AttributeNode> attributeList;
        //    List('[', ']', _attributeGetter, "Attribute or ] expected.", out attributeList);
        //    NodeList<ElementNode> elementList = null;
        //    var simpleChild = default(SimpleValueNode);
        //    if (Token('$')) {
        //        simpleChild = SimpleValueExpected();
        //    }
        //    else {
        //        List('{', '}', _elementGetter, "Element or } expected.", out elementList);
        //    }
        //    var semicolonTextSpan = default(TextSpan);
        //    if (attributeList == null && elementList == null && !simpleChild.IsValid) {
        //        if (!Token(';', out semicolonTextSpan)) {
        //            result = default(ComplexValueNode);
        //            return false;
        //        }
        //    }
        //    result = new ComplexValueNode(equalsTextSpan, typeQName, attributeList, elementList, simpleChild, semicolonTextSpan);
        //    return true;
        //}
        //private bool Attribute(List<AttributeNode> list, out AttributeNode result) {
        //    NameNode name;
        //    if (Name(out name)) {
        //        if (list.CountOrZero() > 0) {
        //            foreach (var item in list) {
        //                if (item.NameNode == name) {
        //                    ErrorDiagAndThrow(new DiagMsg(DiagCode.DuplicateAttributeName, name.ToString()), name.TextSpan);
        //                }
        //            }
        //        }
        //        var value = default(SimpleValueNode);
        //        if (Token('=')) {
        //            value = SimpleValueExpected();
        //        }
        //        result = new AttributeNode(name, value);
        //        return true;
        //    }
        //    result = default(AttributeNode);
        //    return false;
        //}

    }
}
