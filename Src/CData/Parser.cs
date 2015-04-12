using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace CData {
    internal static class ParserKeywords {
        public const string ArgKeyword = "arg";
        public const string AsKeyword = "as";
        public const string ImportKeyword = "import";
        public const string IsKeyword = "is";
        public const string NewKeyword = "new";
        public const string ThisKeyword = "this";
    }
    internal abstract class ParserBase {
        protected ParserBase() {
            _tokens = new Token[_tokenBufLength];
        }
        protected void Set(string filePath, TextReader reader, DiagContext context) {
            if (filePath == null) throw new ArgumentNullException("filePath");
            if (context == null) throw new ArgumentNullException("context");
            _lexer = Lexer.Get(reader);
            _tokenIndex = -1;
            _filePath = filePath;
            _context = context;
        }
        protected void Clear() {
            if (_lexer != null) {
                _lexer.Clear();
            }
            _tokenIndex = -1;
            _filePath = null;
            _context = null;
        }
        private Lexer _lexer;
        private const int _tokenBufLength = 8;
        private readonly Token[] _tokens;
        private int _tokenIndex;
        private string _filePath;
        protected DiagContext _context;
        //
        protected void Error(int code, string errMsg, TextSpan textSpan) {
            _context.AddDiag(DiagSeverity.Error, code, errMsg, textSpan);
        }
        protected void Error(DiagMsg diagMsg, TextSpan textSpan) {
            _context.AddDiag(DiagSeverity.Error, diagMsg, textSpan);
        }
        protected void Throw() {
            throw DiagContext.DiagExceptionObject;
        }
        protected void ErrorAndThrow(string errMsg, TextSpan textSpan) {
            Error((int)DiagCode.Parsing, errMsg, textSpan);
            Throw();
        }
        protected void ErrorAndThrow(string errMsg, Token token) {
            ErrorAndThrow(errMsg ?? token.Value, GetTextSpan(token));
        }
        protected void ErrorAndThrow(string errMsg) {
            ErrorAndThrow(errMsg, GetToken());
        }
        protected void ErrorAndThrow(DiagMsg diagMsg, TextSpan textSpan) {
            Error(diagMsg, textSpan);
            Throw();
        }
        protected void ErrorAndThrow(DiagMsg diagMsg) {
            ErrorAndThrow(diagMsg, GetTextSpan());
        }
        //
        private static bool IsTrivalToken(TokenKind kind) {
            return kind == TokenKind.WhitespaceOrNewLine || kind == TokenKind.SingleLineComment || kind == TokenKind.MultiLineComment;
        }
        protected static bool IsNameToken(TokenKind kind) {
            return kind == TokenKind.Name || kind == TokenKind.VerbatimName;
        }
        protected static bool IsNameToken(int kind) {
            return kind == (int)TokenKind.Name || kind == (int)TokenKind.VerbatimName;
        }
        protected static bool IsStringToken(TokenKind kind) {
            return kind == TokenKind.StringValue || kind == TokenKind.VerbatimStringValue;
        }
        protected static bool IsStringToken(int kind) {
            return kind == (int)TokenKind.StringValue || kind == (int)TokenKind.VerbatimStringValue;
        }
        protected TextSpan GetTextSpan(Token token) {
            return token.ToTextSpan(_filePath);
        }
        protected TextSpan GetTextSpan() {
            return GetTextSpan(GetToken());
        }
        protected Token GetToken(int index = 0) {
            Debug.Assert(index >= 0 && index < _tokenBufLength);
            var tokens = _tokens;
            while (_tokenIndex < index) {
                var token = _lexer.GetToken();
                var tokenKind = token.TokenKind;
                if (!IsTrivalToken(tokenKind)) {
                    if (tokenKind == TokenKind.Error) {
                        ErrorAndThrow(null, token);
                    }
                    else {
                        tokens[++_tokenIndex] = token;
                    }
                }
            }
            return tokens[index];
        }
        protected void ConsumeToken() {
            Debug.Assert(_tokenIndex >= 0);
            if (--_tokenIndex >= 0) {
                Array.Copy(_tokens, 1, _tokens, 0, _tokenIndex + 1);
            }
        }
        protected bool PeekToken(int kind, int tkIdx = 0) {
            return GetToken(tkIdx).Kind == kind;
        }
        protected bool PeekToken(int kind1, int kind2, int tkIdx = 0) {
            var kind = GetToken(tkIdx).Kind;
            return kind == kind1 || kind == kind2;
        }
        protected bool PeekToken(int kind1, int kind2, int kind3, int tkIdx = 0) {
            var kind = GetToken(tkIdx).Kind;
            return kind == kind1 || kind == kind2 || kind == kind3;
        }
        protected bool PeekToken(int kind1, int kind2, int kind3, int kind4, int tkIdx = 0) {
            var kind = GetToken(tkIdx).Kind;
            return kind == kind1 || kind == kind2 || kind == kind3 || kind == kind4;
        }
        protected bool Token(int kind, out TextSpan textSpan) {
            var token = GetToken();
            if (token.Kind == kind) {
                textSpan = GetTextSpan(token);
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
                ErrorAndThrow(ch.ToString() + " expected.");
            }
        }
        protected void TokenExpected(int kind, string errMsg) {
            if (!Token(kind)) {
                ErrorAndThrow(errMsg);
            }
        }
        protected void TokenExpected(TokenKind tokenKind, string errMsg) {
            TokenExpected((int)tokenKind, errMsg);
        }
        public void TokenExpected(char ch, out TextSpan textSpan) {
            if (!Token(ch, out textSpan)) {
                ErrorAndThrow(ch.ToString() + " expected.");
            }
        }
        protected void TokenExpected(int kind, string errMsg, out TextSpan textSpan) {
            if (!Token(kind, out textSpan)) {
                ErrorAndThrow(errMsg);
            }
        }
        protected void EndOfFileExpected() {
            TokenExpected(char.MaxValue, "End of file expected.");
        }
        protected bool Name(out NameNode result) {
            var token = GetToken();
            if (IsNameToken(token.TokenKind)) {
                result = new NameNode(token.Value, GetTextSpan(token));
                ConsumeToken();
                return true;
            }
            result = default(NameNode);
            return false;
        }
        protected NameNode NameExpected() {
            NameNode name;
            if (!Name(out name)) {
                ErrorAndThrow("Name expected.");
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
                ErrorAndThrow(keywordValue + " expetced.");
            }
        }
        protected bool Keyword(string keywordValue, out NameNode keyword) {
            var token = GetToken();
            if (token.TokenKind == TokenKind.Name && token.Value == keywordValue) {
                keyword = new NameNode(keywordValue, GetTextSpan(token));
                ConsumeToken();
                return true;
            }
            keyword = default(NameNode);
            return false;
        }
        protected bool Keyword(string keywordValue, out TextSpan textSpan) {
            var token = GetToken();
            if (token.TokenKind == TokenKind.Name && token.Value == keywordValue) {
                textSpan = GetTextSpan(token);
                ConsumeToken();
                return true;
            }
            textSpan = default(TextSpan);
            return false;
        }
        protected bool QualifiableName(out QualifiableNameNode result) {
            NameNode name;
            if (Name(out name)) {
                if (Token(TokenKind.ColonColon)) {
                    result = new QualifiableNameNode(name, NameExpected());
                }
                else {
                    result = new QualifiableNameNode(default(NameNode), name);
                }
                return true;
            }
            result = default(QualifiableNameNode);
            return false;
        }
        protected QualifiableNameNode QualifiableNameExpected() {
            QualifiableNameNode qName;
            if (!QualifiableName(out qName)) {
                ErrorAndThrow("Qualifiable name expected.");
            }
            return qName;
        }

        protected bool AtomValue(out AtomValueNode result, bool takeNumberSign) {
            var token = GetToken();
            var tokenValue = token.Value;
            var valueKind = AtomValueKind.None;
            switch (token.TokenKind) {
                case TokenKind.StringValue:
                case TokenKind.VerbatimStringValue:
                    valueKind = AtomValueKind.String;
                    break;
                case TokenKind.CharValue:
                    valueKind = AtomValueKind.Char;
                    break;
                case TokenKind.Name:
                    if (tokenValue == "null") {
                        valueKind = AtomValueKind.Null;
                    }
                    else if (tokenValue == "true" || tokenValue == "false") {
                        valueKind = AtomValueKind.Boolean;
                    }
                    break;
                case TokenKind.IntegerValue:
                    valueKind = AtomValueKind.Integer;
                    break;
                case TokenKind.DecimalValue:
                    valueKind = AtomValueKind.Decimal;
                    break;
                case TokenKind.RealValue:
                    valueKind = AtomValueKind.Real;
                    break;
            }
            if (valueKind != AtomValueKind.None) {
                result = new AtomValueNode(valueKind, tokenValue, GetTextSpan(token));
                ConsumeToken();
                return true;
            }
            if (takeNumberSign && (token.Kind == '-' || token.Kind == '+')) {
                var nextToken = GetToken(1);
                switch (nextToken.TokenKind) {
                    case TokenKind.IntegerValue:
                        valueKind = AtomValueKind.Integer;
                        break;
                    case TokenKind.DecimalValue:
                        valueKind = AtomValueKind.Decimal;
                        break;
                    case TokenKind.RealValue:
                        valueKind = AtomValueKind.Real;
                        break;
                }
                if (valueKind != AtomValueKind.None) {
                    result = new AtomValueNode(valueKind, (token.Kind == '-' ? "-" : "+") + nextToken.Value, GetTextSpan(nextToken));
                    ConsumeToken();
                    ConsumeToken();
                    return true;
                }
            }
            result = default(AtomValueNode);
            return false;
        }
        protected AtomValueNode AtomValueExpected(bool takeNumberSign) {
            AtomValueNode av;
            if (!AtomValue(out av, takeNumberSign)) {
                ErrorAndThrow("Atom value expected.");
            }
            return av;
        }
        protected AtomValueNode NonNullAtomValueExpected(bool takeNumberSign) {
            var av = AtomValueExpected(takeNumberSign);
            if (av.IsNull) {
                ErrorAndThrow("Non-null atom value expected.", av.TextSpan);
            }
            return av;
        }
        protected bool StringValue(out AtomValueNode result) {
            var token = GetToken();
            if (IsStringToken(token.TokenKind)) {
                result = new AtomValueNode(AtomValueKind.String, token.Value, GetTextSpan(token));
                ConsumeToken();
                return true;
            }
            result = default(AtomValueNode);
            return false;
        }
        protected AtomValueNode StringValueExpected() {
            AtomValueNode av;
            if (!StringValue(out av)) {
                ErrorAndThrow("String value expected.");
            }
            return av;
        }
        protected AtomValueNode UriExpected() {
            var uri = StringValueExpected();
            if (uri.Value == Extensions.SystemUri) {
                ErrorAndThrow(new DiagMsg(DiagCode.UriReserved), uri.TextSpan);
            }
            return uri;
        }
        protected NameNode AliasExpected() {
            var aliasNode = NameExpected();
            var alias = aliasNode.Value;
            if (alias == "sys" || alias == "thisns") {
                ErrorAndThrow(new DiagMsg(DiagCode.AliasReserved), aliasNode.TextSpan);
            }
            return aliasNode;
        }

        //
        //
        //query & expressions
        //
        //
        //private bool Query(out QueryNode result) {
        //    List<UriAliasNode> uriAliasList = null;
        //    while (Token('#')) {
        //        KeywordExpected(ParserKeywords.ImportKeyword);
        //        var uriNode = UriExpected();
        //        var uri = uriNode.Value;
        //        if (!ProgramMd.IsUriDefined(uri)) {
        //            ErrorAndThrow(new DiagMsg(DiagCode.InvalidUriReference, uri), uriNode.TextSpan);
        //        }
        //        string alias = null;
        //        if (Keyword(ParserKeywords.AsKeyword)) {
        //            var aliasNode = AliasExpected();
        //            alias = aliasNode.Value;
        //            if (uriAliasList != null) {
        //                foreach (var item in uriAliasList) {
        //                    if (item.Alias == alias) {
        //                        ErrorAndThrow(new DiagMsg(DiagCode.DuplicateAlias, alias), aliasNode.TextSpan);
        //                    }
        //                }
        //            }
        //        }
        //        if (uriAliasList == null) {
        //            uriAliasList = new List<UriAliasNode>();
        //        }
        //        uriAliasList.Add(new UriAliasNode(uri, alias));
        //    }
        //    ExpressionNode expr;
        //    if (uriAliasList == null) {
        //        Expression(null, out expr);
        //    }
        //    else {
        //        expr = ExpressionExpected(null);
        //    }
        //    if (expr != null) {
        //        result = new QueryNode(uriAliasList, expr);
        //        return true;
        //    }
        //    result = null;
        //    return false;
        //}
        protected List<QueryArgumentNode> QueryArguments() {
            List<QueryArgumentNode> list = null;
            NameNode nameNode;
            while (Name(out nameNode)) {
                var name = nameNode.Value;
                if (list == null) {
                    list = new List<QueryArgumentNode>();
                }
                else {
                    foreach (var item in list) {
                        if (item.Name == name) {
                            ErrorAndThrow(new DiagMsg(DiagCode.DuplicateQueryArgumentName, name), nameNode.TextSpan);
                        }
                    }
                }
                TokenExpected('=');
                list.Add(new QueryArgumentNode(name, AtomValueExpected(true)));
            }
            return list;
        }

        private void ExpressionExpectedErrorAndThrow() {
            ErrorAndThrow("Expression expected.");
        }
        private ExpressionNode ExpressionExpected(QueryDiagContext ctx) {
            ExpressionNode r;
            if (!Expression(ctx, out r)) {
                ExpressionExpectedErrorAndThrow();
            }
            return r;
        }
        private bool Expression(QueryDiagContext ctx, out ExpressionNode result) {
            LambdaParameterNodeList pList = null;
            var tk0Kind = GetToken().Kind;
            if (IsNameToken(tk0Kind)) {
                if (GetToken(1).TokenKind == TokenKind.EqualsGreaterThan) {
                    pList = new LambdaParameterNodeList { new LambdaParameterNode(NameExpected()) };
                }
            }
            else if (tk0Kind == '(') {
                var tk1Kind = GetToken(1).Kind;
                if (IsNameToken(tk1Kind)) {
                    var tk2Kind = GetToken(2).Kind;
                    if (tk2Kind == ')') {
                        if (GetToken(3).TokenKind == TokenKind.EqualsGreaterThan) {
                            ConsumeToken();// (
                            pList = new LambdaParameterNodeList { new LambdaParameterNode(NameExpected()) };
                            ConsumeToken();// )
                        }
                    }
                    else if (tk2Kind == ',') {
                        ConsumeToken();// (
                        pList = new LambdaParameterNodeList { new LambdaParameterNode(NameExpected()) };
                        while (Token(',')) {
                            var name = NameExpected();
                            foreach (var item in pList) {
                                if (item.Name == name) {
                                    ErrorAndThrow(new DiagMsg(DiagCode.DuplicateLambdaParameterName, name.Value), name.TextSpan);
                                }
                            }
                            pList.Add(new LambdaParameterNode(name));
                        }
                        TokenExpected(')');
                    }
                }
                else if (tk1Kind == ')') {
                    ConsumeToken();// (
                    ConsumeToken();// )
                    pList = new LambdaParameterNodeList();
                }
            }
            if (pList != null) {
                TokenExpected(TokenKind.EqualsGreaterThan, "=> expected.");
                if (pList.Count > 0) {
                    ctx.PushParameterList(pList);
                }
                var body = ExpressionExpected(ctx);
                if (pList.Count > 0) {
                    ctx.PopParameterList();
                }
                result = new LambdaExpressionNode(pList, body);
                return true;
            }
            return ConditionalExpression(ctx, out result);
        }
        private bool ConditionalExpression(QueryDiagContext ctx, out ExpressionNode result) {
            ExpressionNode condition;
            if (CoalesceExpression(ctx, out condition)) {
                ExpressionNode whenTrue = null, whenFalse = null;
                if (Token('?')) {
                    whenTrue = ExpressionExpected(ctx);
                    TokenExpected(':');
                    whenFalse = ExpressionExpected(ctx);
                }
                if (whenTrue == null) {
                    result = condition;
                }
                else {
                    result = new ConditionalExpressionNode(condition, whenTrue, whenFalse);
                }
                return true;
            }
            result = null;
            return false;
        }
        private bool CoalesceExpression(QueryDiagContext ctx, out ExpressionNode result) {
            ExpressionNode left;
            if (OrElseExpression(ctx, out left)) {
                ExpressionNode right = null;
                if (Token(TokenKind.QuestionQuestion)) {
                    if (!CoalesceExpression(ctx, out right)) {
                        ExpressionExpectedErrorAndThrow();
                    }
                }
                if (right == null) {
                    result = left;
                }
                else {
                    result = new BinaryExpressionNode(ExpressionKind.Coalesce, left, right);
                }
                return true;
            }
            result = null;
            return false;
        }
        private bool OrElseExpression(QueryDiagContext ctx, out ExpressionNode result) {
            if (AndAlsoExpression(ctx, out result)) {
                while (Token(TokenKind.BarBar)) {
                    ExpressionNode right;
                    if (!AndAlsoExpression(ctx, out right)) {
                        ExpressionExpectedErrorAndThrow();
                    }
                    result = new BinaryExpressionNode(ExpressionKind.OrElse, result, right);
                }
            }
            return result != null;
        }
        private bool AndAlsoExpression(QueryDiagContext ctx, out ExpressionNode result) {
            if (OrExpression(ctx, out result)) {
                while (Token(TokenKind.AmpersandAmpersand)) {
                    ExpressionNode right;
                    if (!OrExpression(ctx, out right)) {
                        ExpressionExpectedErrorAndThrow();
                    }
                    result = new BinaryExpressionNode(ExpressionKind.AndAlso, result, right);
                }
            }
            return result != null;
        }
        private bool OrExpression(QueryDiagContext ctx, out ExpressionNode result) {
            if (ExclusiveOrExpression(ctx, out result)) {
                while (Token('|')) {
                    ExpressionNode right;
                    if (!ExclusiveOrExpression(ctx, out right)) {
                        ExpressionExpectedErrorAndThrow();
                    }
                    result = new BinaryExpressionNode(ExpressionKind.Or, result, right);
                }
            }
            return result != null;
        }
        private bool ExclusiveOrExpression(QueryDiagContext ctx, out ExpressionNode result) {
            if (AndExpression(ctx, out result)) {
                while (Token('^')) {
                    ExpressionNode right;
                    if (!AndExpression(ctx, out right)) {
                        ExpressionExpectedErrorAndThrow();
                    }
                    result = new BinaryExpressionNode(ExpressionKind.ExclusiveOr, result, right);
                }
            }
            return result != null;
        }
        private bool AndExpression(QueryDiagContext ctx, out ExpressionNode result) {
            if (EqualityExpression(ctx, out result)) {
                while (Token('&')) {
                    ExpressionNode right;
                    if (!EqualityExpression(ctx, out right)) {
                        ExpressionExpectedErrorAndThrow();
                    }
                    result = new BinaryExpressionNode(ExpressionKind.And, result, right);
                }
            }
            return result != null;
        }
        private bool EqualityExpression(QueryDiagContext ctx, out ExpressionNode result) {
            if (RelationalExpression(ctx, out result)) {
                while (true) {
                    ExpressionKind kind;
                    if (Token(TokenKind.EqualsEquals)) {
                        kind = ExpressionKind.Equal;
                    }
                    else if (Token(TokenKind.ExclamationEquals)) {
                        kind = ExpressionKind.NotEqual;
                    }
                    else {
                        break;
                    }
                    ExpressionNode right;
                    if (!RelationalExpression(ctx, out right)) {
                        ExpressionExpectedErrorAndThrow();
                    }
                    result = new BinaryExpressionNode(kind, result, right);
                }
            }
            return result != null;
        }
        private bool RelationalExpression(QueryDiagContext ctx, out ExpressionNode result) {
            if (ShiftExpression(ctx, out result)) {
                while (true) {
                    ExpressionKind kind;
                    if (Token('<')) {
                        kind = ExpressionKind.LessThan;
                    }
                    else if (Token(TokenKind.LessThanEquals)) {
                        kind = ExpressionKind.LessThanOrEqual;
                    }
                    else if (Token('>')) {
                        kind = ExpressionKind.GreaterThan;
                    }
                    else if (Token(TokenKind.GreaterThanEquals)) {
                        kind = ExpressionKind.GreaterThanOrEqual;
                    }
                    else if (Keyword(ParserKeywords.IsKeyword)) {
                        kind = ExpressionKind.TypeIs;
                    }
                    else if (Keyword(ParserKeywords.AsKeyword)) {
                        kind = ExpressionKind.TypeAs;
                    }
                    else {
                        break;
                    }
                    if (kind == ExpressionKind.TypeIs || kind == ExpressionKind.TypeAs) {
                        var qName = QualifiableNameExpected();
                        result = new TypedExpressionNode(kind, qName, ctx.ResolveAsGlobalType(qName), result);
                    }
                    else {
                        ExpressionNode right;
                        if (!ShiftExpression(ctx, out right)) {
                            ExpressionExpectedErrorAndThrow();
                        }
                        result = new BinaryExpressionNode(kind, result, right);
                    }
                }
            }
            return result != null;
        }
        private bool ShiftExpression(QueryDiagContext ctx, out ExpressionNode result) {
            if (AdditiveExpression(ctx, out result)) {
                while (true) {
                    ExpressionKind kind = ExpressionKind.None;
                    if (Token(TokenKind.LessThanLessThan)) {
                        kind = ExpressionKind.LeftShift;
                    }
                    else {
                        var tk1 = GetToken();
                        if (tk1.Kind == '>') {
                            var tk2 = GetToken(1);
                            if (tk2.Kind == '>') {
                                if (tk1.StartIndex + 1 == tk2.StartIndex) {
                                    ConsumeToken();
                                    ConsumeToken();
                                    kind = ExpressionKind.RightShift;
                                }
                            }
                        }
                    }
                    if (kind == ExpressionKind.None) {
                        break;
                    }
                    ExpressionNode right;
                    if (!AdditiveExpression(ctx, out right)) {
                        ExpressionExpectedErrorAndThrow();
                    }
                    result = new BinaryExpressionNode(kind, result, right);
                }
            }
            return result != null;
        }
        private bool AdditiveExpression(QueryDiagContext ctx, out ExpressionNode result) {
            if (MultiplicativeExpression(ctx, out result)) {
                while (true) {
                    ExpressionKind kind;
                    if (Token('+')) {
                        kind = ExpressionKind.Add;
                    }
                    else if (Token('-')) {
                        kind = ExpressionKind.Subtract;
                    }
                    else {
                        break;
                    }
                    ExpressionNode right;
                    if (!MultiplicativeExpression(ctx, out right)) {
                        ExpressionExpectedErrorAndThrow();
                    }
                    result = new BinaryExpressionNode(kind, result, right);
                }
            }
            return result != null;
        }
        private bool MultiplicativeExpression(QueryDiagContext ctx, out ExpressionNode result) {
            if (PrefixUnaryExpression(ctx, out result)) {
                while (true) {
                    ExpressionKind kind;
                    if (Token('*')) {
                        kind = ExpressionKind.Multiply;
                    }
                    else if (Token('/')) {
                        kind = ExpressionKind.Divide;
                    }
                    else if (Token('%')) {
                        kind = ExpressionKind.Modulo;
                    }
                    else {
                        break;
                    }
                    ExpressionNode right;
                    if (!PrefixUnaryExpression(ctx, out right)) {
                        ExpressionExpectedErrorAndThrow();
                    }
                    result = new BinaryExpressionNode(kind, result, right);
                }
            }
            return result != null;
        }
        private bool PrefixUnaryExpression(QueryDiagContext ctx, out ExpressionNode result) {
            var kind = ExpressionKind.None;
            var tk0Kind = GetToken().Kind;
            if (tk0Kind == '!') {
                kind = ExpressionKind.Not;
            }
            else if (tk0Kind == '-') {
                kind = ExpressionKind.Negate;
            }
            else if (tk0Kind == '+') {
                kind = ExpressionKind.UnaryPlus;
            }
            else if (tk0Kind == '~') {
                kind = ExpressionKind.OnesComplement;
            }
            if (kind != ExpressionKind.None) {
                ConsumeToken();
                ExpressionNode expr;
                if (!PrefixUnaryExpression(ctx, out expr)) {
                    ExpressionExpectedErrorAndThrow();
                }
                result = new UnaryExpressionNode(kind, expr);
                return true;
            }
            if (tk0Kind == '(') {
                var tk1Kind = GetToken(1).Kind;
                if (IsNameToken(tk1Kind)) {
                    var isOk = false;
                    var tk2Kind = GetToken(2).Kind;
                    if (tk2Kind == ')') {
                        var tk3Kind = GetToken(3).Kind;
                        isOk = tk3Kind != '-' && tk3Kind != '+';
                    }
                    else if (tk2Kind == (int)TokenKind.ColonColon) {
                        var tk3Kind = GetToken(3).Kind;
                        if (IsNameToken(tk3Kind)) {
                            var tk4Kind = GetToken(4).Kind;
                            if (tk4Kind == ')') {
                                var tk5Kind = GetToken(5).Kind;
                                isOk = tk5Kind != '-' && tk5Kind != '+';
                            }
                        }
                    }
                    if (isOk) {
                        ConsumeToken();// (
                        var qName = QualifiableNameExpected();
                        ConsumeToken();// )
                        ExpressionNode expr;
                        if (PrefixUnaryExpression(ctx, out expr)) {
                            result = new TypedExpressionNode(ExpressionKind.Convert, qName, ctx.ResolveAsGlobalType(qName), expr);
                            return true;
                        }
                        result = new UnaryExpressionNode(ExpressionKind.Parenthesized, new QualifiableNameExpressionNode(ctx, qName));
                        return true;
                    }
                }
            }
            return PrimaryExpression(ctx, out result);
        }
        private bool PrimaryExpression(QueryDiagContext ctx, out ExpressionNode result) {
            ExpressionNode expr = null;
            QualifiableNameNode qName;
            if (QualifiableName(out qName)) {
                expr = new QualifiableNameExpressionNode(ctx, qName);
            }
            else {
                AtomValueNode av;
                if (AtomValue(out av, false)) {
                    expr = new LiteralExpressionNode(av);
                }
                else {
                    if (Keyword(ParserKeywords.NewKeyword)) {
                        TokenExpected('{');
                        var memberList = new List<AnonymousObjectMemberNode>();
                        while (true) {
                            if (memberList.Count > 0) {
                                if (!Token(',')) {
                                    break;
                                }
                            }
                            NameNode name;
                            if (Name(out name)) {
                                if (memberList.Count > 0) {
                                    foreach (var item in memberList) {
                                        if (item.Name == name) {
                                            //..
                                        }
                                    }
                                }
                                TokenExpected('=');
                                memberList.Add(new AnonymousObjectMemberNode(name, ExpressionExpected(ctx)));
                            }
                            else {
                                break;
                            }
                        }
                        TokenExpected('}');
                        expr = new AnonymousObjectCreationExpressionNode(memberList);
                    }
                    else {
                        if (Token('(')) {
                            var op = ExpressionExpected(ctx);
                            TokenExpected(')');
                            expr = new UnaryExpressionNode(ExpressionKind.Parenthesized, op);
                        }
                    }
                }
            }
            if (expr != null) {
                while (true) {
                    if (Token('.')) {
                        expr = new MemberAccessExpressionNode(expr, NameExpected());
                    }
                    else if (Token('(')) {
                        var argList = new List<ExpressionNode>();
                        ExpressionNode arg;
                        if (Expression(ctx, out arg)) {
                            argList.Add(arg);
                            while (Token(',')) {
                                argList.Add(ExpressionExpected(ctx));
                            }
                        }
                        TokenExpected(')');
                        expr = new CallOrIndexExpressionNode(true, expr, argList);
                    }
                    else if (Token('[')) {
                        var argList = new List<ExpressionNode>();
                        argList.Add(ExpressionExpected(ctx));
                        while (Token(',')) {
                            argList.Add(ExpressionExpected(ctx));
                        }
                        TokenExpected(']');
                        expr = new CallOrIndexExpressionNode(false, expr, argList);
                    }
                    else {
                        break;
                    }
                }
            }
            result = expr;
            return expr != null;
        }


    }
    internal sealed class Parser : ParserBase {
        internal static bool ParseData(string filePath, TextReader reader, DiagContext context, ClassMd classMd, out object result) {
            if (classMd == null) throw new ArgumentNullException("classMd");
            return _instance.Data(filePath, reader, context, classMd, out result);
        }
        internal static bool ParseQueryArguments(string filePath, TextReader reader, DiagContext context, out List<QueryArgumentNode> result) {
            return _instance.QueryArguments(filePath, reader, context, out result);
        }
        [ThreadStatic]
        private static readonly Parser _instance = new Parser();
        private Stack<List<UriAliasNode>> _uriAliasListStack;
        private bool Data(string filePath, TextReader reader, DiagContext context, ClassMd clsMd, out object result) {
            try {
                Set(filePath, reader, context);
                if (_uriAliasListStack == null) {
                    _uriAliasListStack = new Stack<List<UriAliasNode>>();
                }
                else {
                    _uriAliasListStack.Clear();
                }
                object obj;
                if (ClassValue(clsMd, out obj)) {
                    EndOfFileExpected();
                    result = obj;
                    return true;
                }
                else {
                    ErrorAndThrow("Class value expected.");
                }
            }
            catch (DiagContext.DiagException) { }
            finally {
                Clear();
            }
            result = null;
            return false;
        }
        private bool QueryArguments(string filePath, TextReader reader, DiagContext context, out List<QueryArgumentNode> result) {
            try {
                Set(filePath, reader, context);
                var list = QueryArguments();
                EndOfFileExpected();
                result = list;
                return true;
            }
            catch (DiagContext.DiagException) { }
            finally {
                Clear();
            }
            result = null;
            return false;
        }

        private bool UriAliasList() {
            if (Token('<')) {
                List<UriAliasNode> list = null;
                while (true) {
                    NameNode aliasNode;
                    if (Name(out aliasNode)) {
                        var alias = aliasNode.Value;
                        if (list == null) {
                            list = new List<UriAliasNode>();
                        }
                        else {
                            foreach (var item in list) {
                                if (item.Alias == alias) {
                                    ErrorAndThrow(new DiagMsg(DiagCode.DuplicateAlias, alias), aliasNode.TextSpan);
                                }
                            }
                        }
                        TokenExpected('=');
                        list.Add(new UriAliasNode(StringValueExpected().Value, alias));
                    }
                    else {
                        TokenExpected('>');
                        if (list != null) {
                            _uriAliasListStack.Push(list);
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
            foreach (var uaList in _uriAliasListStack) {
                foreach (var ua in uaList) {
                    if (ua.Alias == alias) {
                        return ua.Uri;
                    }
                }
            }
            ErrorAndThrow(new DiagMsg(DiagCode.InvalidUriReference, alias), aliasNode.TextSpan);
            return null;
        }
        private bool ClassValue(ClassMd declaredClsMd, out object result) {
            NameNode aliasNode;
            if (Name(out aliasNode)) {
                TokenExpected(':');
                var nameNode = NameExpected();
                var hasUriAliasingList = UriAliasList();
                TokenExpected('{');
                var fullName = new FullName(GetUri(aliasNode), nameNode.Value);
                var clsMd = ProgramMd.GetGlobalType<ClassMd>(fullName);
                if (clsMd == null) {
                    ErrorAndThrow(new DiagMsg(DiagCode.InvalidClassReference, fullName.ToString()), nameNode.TextSpan);
                }
                if (!clsMd.IsEqualToOrDeriveFrom(declaredClsMd)) {
                    ErrorAndThrow(new DiagMsg(DiagCode.ClassNotEqualToOrDeriveFromTheDeclared, fullName.ToString(), declaredClsMd.FullName.ToString()),
                        nameNode.TextSpan);
                }
                if (clsMd.IsAbstract) {
                    ErrorAndThrow(new DiagMsg(DiagCode.ClassIsAbstract, fullName.ToString()), nameNode.TextSpan);
                }
                var obj = clsMd.CreateInstance();
                clsMd.SetTextSpan(obj, nameNode.TextSpan);
                if (!clsMd.InvokeOnLoad(true, obj, _context)) {
                    Throw();
                }
                List<ClassPropertyMd> propMdList = null;
                clsMd.GetPropertiesInHierarchy(ref propMdList);
                while (true) {
                    NameNode propNameNode;
                    if (Name(out propNameNode)) {
                        var propName = propNameNode.Value;
                        ClassPropertyMd propMd = null;
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
                            ErrorAndThrow(new DiagMsg(DiagCode.InvalidPropertyName, propName), propNameNode.TextSpan);
                        }
                        TokenExpected('=');
                        propMd.SetValue(obj, LocalValueExpected(propMd.Type));
                    }
                    else {
                        TextSpan ts;
                        TokenExpected('}', out ts);
                        if (propMdList != null && propMdList.Count > 0) {
                            foreach (var propMd in propMdList) {
                                Error(new DiagMsg(DiagCode.PropertyMissing, propMd.Name), ts);
                            }
                            Throw();
                        }
                        if (!clsMd.InvokeOnLoad(false, obj, _context)) {
                            Throw();
                        }
                        if (hasUriAliasingList) {
                            _uriAliasListStack.Pop();
                        }
                        result = obj;
                        return true;
                    }
                }
            }
            result = null;
            return false;
        }
        private object LocalValueExpected(LocalTypeMd typeMd) {
            object value;
            if (LocalValue(typeMd, out value)) {
                return value;
            }
            ErrorAndThrow(new DiagMsg(DiagCode.ValueExpected));
            return null;
        }
        private bool LocalValue(LocalTypeMd typeMd, out object result) {
            var typeKind = typeMd.Kind;
            AtomValueNode avNode;
            if (AtomValue(out avNode, true)) {
                if (avNode.IsNull) {
                    if (!typeMd.IsNullable) {
                        ErrorAndThrow(new DiagMsg(DiagCode.NullNotAllowed), avNode.TextSpan);
                    }
                    result = null;
                    return true;
                }
                if (!typeKind.IsAtom()) {
                    ErrorAndThrow(new DiagMsg(DiagCode.SpecificValueExpected, typeKind.ToString()), avNode.TextSpan);
                }
                result = AtomExtensions.TryParse(typeKind, avNode.Value);
                if (result == null) {
                    ErrorAndThrow(new DiagMsg(DiagCode.InvalidAtomValue, typeKind.ToString(), avNode.Value), avNode.TextSpan);
                }
                return true;
            }
            else {
                TextSpan ts;
                if (Token('$', out ts)) {
                    if (typeKind != TypeKind.Enum) {
                        ErrorAndThrow(new DiagMsg(DiagCode.SpecificValueExpected, typeKind.ToString()), ts);
                    }
                    var uri = GetUri(NameExpected());
                    TokenExpected(':');
                    var nameNode = NameExpected();
                    var fullName = new FullName(uri, nameNode.Value);
                    var enumMd = ProgramMd.GetGlobalType<EnumMd>(fullName);
                    if (enumMd == null) {
                        ErrorAndThrow(new DiagMsg(DiagCode.InvalidEnumReference, fullName.ToString()), nameNode.TextSpan);
                    }
                    var declaredEnumMd = ((GlobalTypeRefMd)typeMd).GlobalType as EnumMd;
                    if (enumMd != declaredEnumMd) {
                        ErrorAndThrow(new DiagMsg(DiagCode.EnumNotEqualToTheDeclared, fullName.ToString(), declaredEnumMd.FullName.ToString()),
                            nameNode.TextSpan);
                    }
                    TokenExpected('.');
                    var memberNameNode = NameExpected();
                    result = enumMd.GetPropertyValue(memberNameNode.Value);
                    if (result == null) {
                        ErrorAndThrow(new DiagMsg(DiagCode.InvalidEnumMemberName, memberNameNode.Value), memberNameNode.TextSpan);
                    }
                    return true;
                }
                else if (Token('[', out ts)) {
                    var isList = typeKind == TypeKind.List;
                    var isSet = typeKind == TypeKind.SimpleSet || typeKind == TypeKind.ObjectSet;
                    if (!(isList || isSet)) {
                        ErrorAndThrow(new DiagMsg(DiagCode.SpecificValueExpected, typeKind.ToString()), ts);
                    }
                    var collMd = (CollectionMd)typeMd;
                    var collObj = collMd.CreateInstance();
                    var itemMd = collMd.ItemOrValueType;
                    while (true) {
                        object itemObj;
                        if (isSet) {
                            ts = GetTextSpan();
                        }
                        if (LocalValue(itemMd, out itemObj)) {
                            if (isSet) {
                                if (!collMd.InvokeBoolAdd(collObj, itemObj)) {
                                    ErrorAndThrow(new DiagMsg(DiagCode.DuplicateSetItem), ts);
                                }
                            }
                            else {
                                collMd.InvokeAdd(collObj, itemObj);
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
                    if (typeKind != TypeKind.Map) {
                        ErrorAndThrow(new DiagMsg(DiagCode.SpecificValueExpected, typeKind.ToString()), ts);
                    }
                    var collMd = (CollectionMd)typeMd;
                    var collObj = collMd.CreateInstance();
                    var keyMd = collMd.MapKeyType;
                    var valueMd = collMd.ItemOrValueType;
                    while (true) {
                        object keyObj;
                        ts = GetTextSpan();
                        if (LocalValue(keyMd, out keyObj)) {
                            if (collMd.InvokeContainsKey(collObj, keyObj)) {
                                ErrorAndThrow(new DiagMsg(DiagCode.DuplicateMapKey), ts);
                            }
                            TokenExpected('=');
                            collMd.InvokeAdd(collObj, keyObj, LocalValueExpected(valueMd));
                        }
                        else {
                            TokenExpected(']');
                            result = collObj;
                            return true;
                        }
                    }
                }
                else if (PeekToken((int)TokenKind.Name, (int)TokenKind.VerbatimName)) {
                    if (typeKind != TypeKind.Class) {
                        ErrorAndThrow(new DiagMsg(DiagCode.SpecificValueExpected, typeKind.ToString()));
                    }
                    return ClassValue(((GlobalTypeRefMd)typeMd).GlobalType as ClassMd, out result);
                }
            }
            result = null;
            return false;
        }


    }

}
