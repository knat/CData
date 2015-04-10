using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace CData {
    internal static class ParserKeywords {
        public const string AsKeyword = "as";
        public const string IsKeyword = "is";
        public const string NewKeyword = "new";
    }
    internal abstract class ParserBase {
        protected ParserBase() {
            _tokens = new Token[_tokenBufLength];
        }
        protected virtual void Set(string filePath, TextReader reader, DiagContext context) {
            if (filePath == null) throw new ArgumentNullException("filePath");
            if (context == null) throw new ArgumentNullException("context");
            _lexer = Lexer.Get(reader);
            _tokenIndex = -1;
            _filePath = filePath;
            _context = context;
        }
        protected virtual void Clear() {
            if (_lexer != null) {
                _lexer.Clear();
            }
            _tokenIndex = -1;
            _filePath = null;
            _context = null;
        }
        private Lexer _lexer;
        private const int _tokenBufLength = 4;
        private readonly Token[] _tokens;
        private int _tokenIndex;
        private string _filePath;
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
            ErrorDiagAndThrow(errMsg ?? token.Value, GetTextSpan(token));
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
                        ErrorDiagAndThrow(null, token);
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
                ErrorDiagAndThrow("Qualifiable name expected.");
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
                ErrorDiagAndThrow("Atom value expected.");
            }
            return av;
        }
        protected AtomValueNode NonNullAtomValueExpected(bool takeNumberSign) {
            var av = AtomValueExpected(takeNumberSign);
            if (av.IsNull) {
                ErrorDiagAndThrow("Non-null atom value expected.", av.TextSpan);
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
                ErrorDiagAndThrow("String value expected.");
            }
            return av;
        }
        protected AtomValueNode UriExpected() {
            var uri = StringValueExpected();
            if (uri.Value == Extensions.SystemUri) {
                ErrorDiagAndThrow(new DiagMsg(DiagCode.UriReserved), uri.TextSpan);
            }
            return uri;
        }
        protected NameNode UriAliasExpected() {
            var alias = NameExpected();
            if (alias.Value == "sys" || alias.Value == "thisns") {
                ErrorDiagAndThrow(new DiagMsg(DiagCode.UriAliasReserved), alias.TextSpan);
            }
            return alias;
        }

        //
        //
        //query & expressions
        //
        //
        private bool Query(out QueryNode result) {
            List<AliasUriNode> aliasUriList = null;
            while (Token('#')) {
                var alias = UriAliasExpected();
                if (aliasUriList != null) {
                    foreach (var item in aliasUriList) {
                        if (item.Alias == alias) {
                            ErrorDiagAndThrow(new DiagMsg(DiagCode.DuplicateNamespaceAlias, alias.Value), alias.TextSpan);
                        }
                    }
                }
                TokenExpected('=');
                var uri = UriExpected();
                if (!ProgramMetadata.IsUriDefined(uri.Value)) {
                    ErrorDiagAndThrow(new DiagMsg(DiagCode.InvalidNamespaceReference, uri.Value), uri.TextSpan);
                }
                if (aliasUriList == null) {
                    aliasUriList = new List<AliasUriNode>();
                }
                aliasUriList.Add(new AliasUriNode(alias, uri));
            }
            ExpressionNode expr;
            if (aliasUriList == null) {
                Expression(out expr);
            }
            else {
                expr = ExpressionExpected();
            }
            if (expr != null) {
                result = new QueryNode(aliasUriList, expr);
                return true;
            }
            result = null;
            return false;
        }

        private void ExpressionExpectedError() {
            ErrorDiagAndThrow("Expression expected.");
        }
        private ExpressionNode ExpressionExpected() {
            ExpressionNode r;
            if (!Expression(out r)) {
                ExpressionExpectedError();
            }
            return r;
        }
        private bool Expression(out ExpressionNode result) {
            var tk0Kind = GetToken().Kind;
            if (IsNameToken(tk0Kind)) {
                if (GetToken(1).TokenKind == TokenKind.EqualsGreaterThan) {
                    var pName = NameExpected();
                    ConsumeToken();// =>
                    result = new LambdaExpressionNode(new List<NameNode> { pName }, ExpressionExpected());
                    return true;
                }
            }
            else if (tk0Kind == '(') {
                if (IsNameToken(GetToken(1).Kind)) {
                    var tk2Kind = GetToken(2).Kind;
                    if (tk2Kind == ')') {
                        if (GetToken(3).TokenKind == TokenKind.EqualsGreaterThan) {
                            ConsumeToken();// (
                            var pName = NameExpected();
                            ConsumeToken();// )
                            ConsumeToken();// =>
                            result = new LambdaExpressionNode(new List<NameNode> { pName }, ExpressionExpected());
                            return true;
                        }
                    }
                    else if (tk2Kind == ',') {
                        ConsumeToken();// (
                        var pNameList = new List<NameNode> { NameExpected() };
                        while (Token(',')) {
                            var name = NameExpected();
                            foreach (var item in pNameList) {
                                if (item == name) {

                                }
                            }
                            pNameList.Add(name);
                        }
                        TokenExpected(')');
                        TokenExpected(TokenKind.EqualsGreaterThan, "=> expected.");
                        result = new LambdaExpressionNode(pNameList, ExpressionExpected());
                        return true;
                    }
                }
            }
            return ConditionalExpression(out result);
        }
        private bool ConditionalExpression(out ExpressionNode result) {
            ExpressionNode condition;
            if (CoalesceExpression(out condition)) {
                ExpressionNode whenTrue = null, whenFalse = null;
                if (Token('?')) {
                    whenTrue = ExpressionExpected();
                    TokenExpected(':');
                    whenFalse = ExpressionExpected();
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
        private bool CoalesceExpression(out ExpressionNode result) {
            ExpressionNode left;
            if (OrElseExpression(out left)) {
                ExpressionNode right = null;
                if (Token(TokenKind.QuestionQuestion)) {
                    if (!CoalesceExpression(out right)) {
                        ExpressionExpectedError();
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
        private bool OrElseExpression(out ExpressionNode result) {
            if (AndAlsoExpression(out result)) {
                while (Token(TokenKind.BarBar)) {
                    ExpressionNode right;
                    if (!AndAlsoExpression(out right)) {
                        ExpressionExpectedError();
                    }
                    result = new BinaryExpressionNode(ExpressionKind.OrElse, result, right);
                }
            }
            return result != null;
        }
        private bool AndAlsoExpression(out ExpressionNode result) {
            if (OrExpression(out result)) {
                while (Token(TokenKind.AmpersandAmpersand)) {
                    ExpressionNode right;
                    if (!OrExpression(out right)) {
                        ExpressionExpectedError();
                    }
                    result = new BinaryExpressionNode(ExpressionKind.AndAlso, result, right);
                }
            }
            return result != null;
        }
        private bool OrExpression(out ExpressionNode result) {
            if (ExclusiveOrExpression(out result)) {
                while (Token('|')) {
                    ExpressionNode right;
                    if (!ExclusiveOrExpression(out right)) {
                        ExpressionExpectedError();
                    }
                    result = new BinaryExpressionNode(ExpressionKind.Or, result, right);
                }
            }
            return result != null;
        }
        private bool ExclusiveOrExpression(out ExpressionNode result) {
            if (AndExpression(out result)) {
                while (Token('^')) {
                    ExpressionNode right;
                    if (!AndExpression(out right)) {
                        ExpressionExpectedError();
                    }
                    result = new BinaryExpressionNode(ExpressionKind.ExclusiveOr, result, right);
                }
            }
            return result != null;
        }
        private bool AndExpression(out ExpressionNode result) {
            if (EqualityExpression(out result)) {
                while (Token('&')) {
                    ExpressionNode right;
                    if (!EqualityExpression(out right)) {
                        ExpressionExpectedError();
                    }
                    result = new BinaryExpressionNode(ExpressionKind.And, result, right);
                }
            }
            return result != null;
        }
        private bool EqualityExpression(out ExpressionNode result) {
            if (RelationalExpression(out result)) {
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
                    if (!RelationalExpression(out right)) {
                        ExpressionExpectedError();
                    }
                    result = new BinaryExpressionNode(kind, result, right);
                }
            }
            return result != null;
        }
        private bool RelationalExpression(out ExpressionNode result) {
            if (ShiftExpression(out result)) {
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
                        result = new TypedExpressionNode(kind, QualifiableNameExpected(), result);
                    }
                    else {
                        ExpressionNode right;
                        if (!ShiftExpression(out right)) {
                            ExpressionExpectedError();
                        }
                        result = new BinaryExpressionNode(kind, result, right);
                    }
                }
            }
            return result != null;
        }
        private bool ShiftExpression(out ExpressionNode result) {
            if (AdditiveExpression(out result)) {
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
                    if (!AdditiveExpression(out right)) {
                        ExpressionExpectedError();
                    }
                    result = new BinaryExpressionNode(kind, result, right);
                }
            }
            return result != null;
        }
        private bool AdditiveExpression(out ExpressionNode result) {
            if (MultiplicativeExpression(out result)) {
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
                    if (!MultiplicativeExpression(out right)) {
                        ExpressionExpectedError();
                    }
                    result = new BinaryExpressionNode(kind, result, right);
                }
            }
            return result != null;
        }
        private bool MultiplicativeExpression(out ExpressionNode result) {
            if (PrefixUnaryExpression(out result)) {
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
                    if (!PrefixUnaryExpression(out right)) {
                        ExpressionExpectedError();
                    }
                    result = new BinaryExpressionNode(kind, result, right);
                }
            }
            return result != null;
        }
        private bool PrefixUnaryExpression(out ExpressionNode result) {
            ExpressionKind kind = ExpressionKind.None;
            var tk = GetToken();
            var tkKind = tk.Kind;
            if (tkKind == '!') {
                kind = ExpressionKind.Not;
            }
            else if (tkKind == '-') {
                kind = ExpressionKind.Negate;
            }
            else if (tkKind == '+') {
                kind = ExpressionKind.UnaryPlus;
            }
            else if (tkKind == '~') {
                kind = ExpressionKind.OnesComplement;
            }
            if (kind != ExpressionKind.None) {
                ConsumeToken();
                ExpressionNode expr;
                if (!PrefixUnaryExpression(out expr)) {
                    ExpressionExpectedError();
                }
                result = new UnaryExpressionNode(kind, expr);
                return true;
            }
            if (PrimaryExpression(out result)) {
                if (result.Kind == ExpressionKind.Parenthesized) {
                    var qNamExpr = ((UnaryExpressionNode)result).Expression as QualifiableNameExpressionNode;
                    if (qNamExpr != null) {
                        tk = GetToken();
                        tkKind = tk.Kind;
                        if (tkKind != '-' && tkKind != '+') {
                            ExpressionNode expr;
                            if (PrefixUnaryExpression(out expr)) {
                                result = new TypedExpressionNode(ExpressionKind.Convert, qNamExpr.QName, expr);
                                return true;
                            }
                        }
                    }
                }
            }
            return result != null;
        }
        private bool PrimaryExpression(out ExpressionNode result) {
            ExpressionNode expr = null;
            QualifiableNameNode qName;
            if (QualifiableName(out qName)) {
                expr = new QualifiableNameExpressionNode(qName);
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
                                memberList.Add(new AnonymousObjectMemberNode(name, ExpressionExpected()));
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
                            var op = ExpressionExpected();
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
                        if (Expression(out arg)) {
                            argList.Add(arg);
                            while (Token(',')) {
                                argList.Add(ExpressionExpected());
                            }
                        }
                        TokenExpected(')');
                        expr = new CallOrIndexExpressionNode(true, expr, argList);
                    }
                    else if (Token('[')) {
                        var argList = new List<ExpressionNode>();
                        argList.Add(ExpressionExpected());
                        while (Token(',')) {
                            argList.Add(ExpressionExpected());
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
        internal static bool Parse(string filePath, TextReader reader, DiagContext context, ClassMetadata classMetadata, out object result) {
            if (classMetadata == null) throw new ArgumentNullException("classMetadata");
            return (_instance ?? (_instance = new Parser())).ParsingUnit(filePath, reader, context, classMetadata, out result);
        }
        [ThreadStatic]
        private static Parser _instance;
        private Parser() {
            _uriAliasingListStack = new Stack<List<UriAliasingNode>>();
        }
        private readonly Stack<List<UriAliasingNode>> _uriAliasingListStack;
        protected override void Set(string filePath, TextReader reader, DiagContext context) {
            base.Set(filePath, reader, context);
            _uriAliasingListStack.Clear();
        }
        protected override void Clear() {
            base.Clear();
            _uriAliasingListStack.Clear();
        }
        private bool ParsingUnit(string filePath, TextReader reader, DiagContext context, ClassMetadata clsMd, out object result) {
            try {
                Set(filePath, reader, context);
                object obj;
                if (ClassValue(clsMd, out obj)) {
                    EndOfFileExpected();
                    result = obj;
                    return true;
                }
                else {
                    ErrorDiagAndThrow("Class value expected.");
                }
            }
            catch (ParsingException) { }
            finally {
                Clear();
            }
            result = null;
            return false;
        }
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
                                    ErrorDiagAndThrow(new DiagMsg(DiagCode.DuplicateNamespaceAlias, alias), aliasNode.TextSpan);
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
        private bool ClassValue(ClassMetadata declaredClsMd, out object result) {
            NameNode aliasNode;
            if (Name(out aliasNode)) {
                TokenExpected(':');
                var nameNode = NameExpected();
                var hasUriAliasingList = UriAliasingList();
                TokenExpected('{');
                var fullName = new FullName(GetUri(aliasNode), nameNode.Value);
                var clsMd = ProgramMetadata.GetGlobalType<ClassMetadata>(fullName);
                if (clsMd == null) {
                    ErrorDiagAndThrow(new DiagMsg(DiagCode.InvalidClassReference, fullName.ToString()), nameNode.TextSpan);
                }
                if (!clsMd.IsEqualToOrDeriveFrom(declaredClsMd)) {
                    ErrorDiagAndThrow(new DiagMsg(DiagCode.ClassNotEqualToOrDeriveFromTheDeclared, fullName.ToString(), declaredClsMd.FullName.ToString()),
                        nameNode.TextSpan);
                }
                if (clsMd.IsAbstract) {
                    ErrorDiagAndThrow(new DiagMsg(DiagCode.ClassIsAbstract, fullName.ToString()), nameNode.TextSpan);
                }
                var obj = clsMd.CreateInstance();
                clsMd.SetTextSpan(obj, nameNode.TextSpan);
                if (!clsMd.InvokeOnLoad(true, obj, _context)) {
                    Throw();
                }
                List<PropertyMetadata> propMdList = null;
                clsMd.GetPropertiesInHierarchy(ref propMdList);
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
                        propMd.SetValue(obj, LocalValueExpected(propMd.Type));
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
                        if (!clsMd.InvokeOnLoad(false, obj, _context)) {
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
        private object LocalValueExpected(LocalTypeMetadata typeMd) {
            object value;
            if (LocalValue(typeMd, out value)) {
                return value;
            }
            ErrorDiagAndThrow(new DiagMsg(DiagCode.ValueExpected));
            return null;
        }
        private bool LocalValue(LocalTypeMetadata typeMd, out object result) {
            var typeKind = typeMd.Kind;
            AtomValueNode avNode;
            if (AtomValue(out avNode, true)) {
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
                result = AtomExtensions.TryParse(typeKind, avNode.Value);
                if (result == null) {
                    ErrorDiagAndThrow(new DiagMsg(DiagCode.InvalidAtomValue, typeKind.ToString(), avNode.Value), avNode.TextSpan);
                }
                return true;
            }
            else {
                TextSpan ts;
                if (Token('$', out ts)) {
                    if (typeKind != TypeKind.Enum) {
                        ErrorDiagAndThrow(new DiagMsg(DiagCode.SpecificValueExpected, typeKind.ToString()), ts);
                    }
                    var uri = GetUri(NameExpected());
                    TokenExpected(':');
                    var nameNode = NameExpected();
                    var fullName = new FullName(uri, nameNode.Value);
                    var enumMd = ProgramMetadata.GetGlobalType<EnumMetadata>(fullName);
                    if (enumMd == null) {
                        ErrorDiagAndThrow(new DiagMsg(DiagCode.InvalidEnumReference, fullName.ToString()), nameNode.TextSpan);
                    }
                    var declaredEnumMd = ((GlobalTypeRefMetadata)typeMd).GlobalType as EnumMetadata;
                    if (enumMd != declaredEnumMd) {
                        ErrorDiagAndThrow(new DiagMsg(DiagCode.EnumNotEqualToTheDeclared, fullName.ToString(), declaredEnumMd.FullName.ToString()),
                            nameNode.TextSpan);
                    }
                    TokenExpected('.');
                    var memberNameNode = NameExpected();
                    result = enumMd.GetMemberValue(memberNameNode.Value);
                    if (result == null) {
                        ErrorDiagAndThrow(new DiagMsg(DiagCode.InvalidEnumMemberName, memberNameNode.Value), memberNameNode.TextSpan);
                    }
                    return true;
                }
                else if (Token('[', out ts)) {
                    var isList = typeKind == TypeKind.List;
                    var isSet = typeKind == TypeKind.SimpleSet || typeKind == TypeKind.ObjectSet;
                    if (!(isList || isSet)) {
                        ErrorDiagAndThrow(new DiagMsg(DiagCode.SpecificValueExpected, typeKind.ToString()), ts);
                    }
                    var collMd = (CollectionMetadata)typeMd;
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
                                    ErrorDiagAndThrow(new DiagMsg(DiagCode.DuplicateSetItem), ts);
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
                        ErrorDiagAndThrow(new DiagMsg(DiagCode.SpecificValueExpected, typeKind.ToString()), ts);
                    }
                    var collMd = (CollectionMetadata)typeMd;
                    var collObj = collMd.CreateInstance();
                    var keyMd = collMd.MapKeyType;
                    var valueMd = collMd.ItemOrValueType;
                    while (true) {
                        object keyObj;
                        ts = GetTextSpan();
                        if (LocalValue(keyMd, out keyObj)) {
                            if (collMd.InvokeContainsKey(collObj, keyObj)) {
                                ErrorDiagAndThrow(new DiagMsg(DiagCode.DuplicateMapKey), ts);
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
                        ErrorDiagAndThrow(new DiagMsg(DiagCode.SpecificValueExpected, typeKind.ToString()));
                    }
                    return ClassValue(((GlobalTypeRefMetadata)typeMd).GlobalType as ClassMetadata, out result);
                }
            }
            result = null;
            return false;
        }


    }

}
