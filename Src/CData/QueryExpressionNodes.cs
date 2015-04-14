using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace CData {
    public static class QueryParser {
        public static bool TryParse(ClassTypeMd classMd, string arguments, string query, DiagContext diagCtx, out QueryNode result) {
            if (classMd == null) throw new ArgumentNullException("classMd");
            if (query == null) throw new ArgumentNullException("query");
            result = null;
            var sr = SimpleStringReader;
            List<QueryArgumentNode> argList = null;
            if (!string.IsNullOrEmpty(arguments)) {
                sr.SetString(arguments);
                if (!Parser.ParseQueryArguments("QueryArguments", sr, diagCtx, out argList)) {
                    return false;
                }
            }


            _currentContext = diagCtx;
            sr.SetString(query);
            ExpressionNode expr;
            if (!Parser.ParseQuery("Query", sr, diagCtx, QueryContext.Get(classMd, argList), out expr)) {
                return false;
            }



            return false;
        }
        [ThreadStatic]
        private static DiagContext _currentContext;
        internal static void ErrorAndThrow(DiagMsg diagMsg, TextSpan textSpan) {
            Error(diagMsg, textSpan);
            throw DiagContext.DiagExceptionObject;
        }
        private static void Error(DiagMsg diagMsg, TextSpan textSpan) {
            _currentContext.AddDiag(DiagSeverity.Error, (int)diagMsg.Code, diagMsg.GetMessage(), textSpan);
        }


        [ThreadStatic]
        private static SimpleStringReader _simpleStringReader;
        private static SimpleStringReader SimpleStringReader {
            get { return _simpleStringReader ?? (_simpleStringReader = new SimpleStringReader()); }
        }
        private const int _queryChacheLength = 16;
        //[ThreadStatic]
        //private static readonly QueryCache[] _queryCacheBuf = new QueryCache[_queryChacheLength];
        //private struct QueryCache {
        //    public readonly ClassTypeMd ClassMd;
        //    public readonly string Query;
        //    public readonly Expression Expression;
        //}


    }

    //internal sealed class QueryNode {
    //    internal QueryNode(List<UriAliasNode> uriAliasList, ExpressionNode expression) {
    //        UriAliasList = uriAliasList;
    //        Expression = expression;
    //    }
    //    internal readonly List<UriAliasNode> UriAliasList;//opt
    //    public readonly ExpressionNode Expression;
    //    internal bool Resolve(ClassMd classMd, DiagContext context) {
    //        //#arg p1 = 123
    //        return false;
    //    }
    //}
    public sealed class QueryNode {
        public readonly ClassTypeMd ClassMd;
        public readonly string QueryString;
        internal readonly List<QueryArgumentNode> ArgumentList;//opt

    }
    internal struct QueryArgumentNode {
        public QueryArgumentNode(string name, AtomValueNode value) {
            Name = name;
            Value = value;
        }
        public readonly string Name;
        public readonly AtomValueNode Value;
    }

    internal sealed class QueryContext {
        public static QueryContext Get(ClassTypeMd classMd, List<QueryArgumentNode> argList) {
            var obj = Instance;
            obj.Set(classMd, argList);
            return obj;
        }
        [ThreadStatic]
        private static QueryContext _instance;
        private static QueryContext Instance {
            get { return _instance ?? (_instance = new QueryContext()); }
        }
        private QueryContext() {
            UriAliasList = new List<UriAliasNode>();
            _lambdaParameterStack = new Stack<object>();
        }
        private ClassTypeMd _classMd;
        private List<QueryArgumentNode> _argumentList;//opt
        public readonly List<UriAliasNode> UriAliasList;
        private readonly Stack<object> _lambdaParameterStack;
        public void Set(ClassTypeMd classMd, List<QueryArgumentNode> argList) {
            _classMd = classMd;
            _argumentList = argList;
            UriAliasList.Clear();
            _lambdaParameterStack.Clear();
        }
        internal void CheckLambdaParameterName(NameNode nameNode) {
            var stack = _lambdaParameterStack;
            if (stack.Count > 0) {
                var name = nameNode.Value;
                foreach (var item in stack) {
                    var para = item as LambdaParameterNode;
                    if (para != null) {
                        if (para.Name == name) {
                            QueryParser.ErrorAndThrow(new DiagMsg(DiagCode.DuplicateLambdaParameterName, name), nameNode.TextSpan);
                        }
                    }
                    else {
                        foreach (var p in ((List<LambdaParameterNode>)item)) {
                            if (p.Name == name) {
                                QueryParser.ErrorAndThrow(new DiagMsg(DiagCode.DuplicateLambdaParameterName, name), nameNode.TextSpan);
                            }
                        }
                    }
                }
            }
        }
        internal void PushLambdaParameter(object paraOrList) {
            _lambdaParameterStack.Push(paraOrList);
        }
        internal void PopLambdaParameter() {
            _lambdaParameterStack.Pop();
        }
        internal ITypeProviderMd Resolve(QualifiableNameNode qName) {
            if (qName.IsQualified) {
                return ResolveAsGlobalType(qName);
            }
            var nameNode = qName.Name;
            var name = nameNode.Value;
            if (name == "this" && nameNode.TextSpan.Length == 4) {
                return _classMd;
            }
            var stack = _lambdaParameterStack;
            if (stack.Count > 0) {
                foreach (var item in stack) {
                    var para = item as LambdaParameterNode;
                    if (para != null) {
                        if (para.Name == name) {
                            return para;
                        }
                    }
                    else {
                        foreach (var p in ((List<LambdaParameterNode>)item)) {
                            if (p.Name == name) {
                                return p;
                            }
                        }
                    }
                }
            }
            ITypeProviderMd result = _classMd.GetPropertyInHierarchy(name);
            if (result != null) {
                return result;
            }
            result = TryResolveAsGlobalType(qName);
            if (result == null) {

            }
            return result;
        }
        internal GlobalTypeMd ResolveAsGlobalType(QualifiableNameNode qName) {
            var result = TryResolveAsGlobalType(qName);
            if (result == null) {
                QueryParser.ErrorAndThrow(new DiagMsg(DiagCode.InvalidGlobalTypeReference, qName.Name.Value), qName.Name.TextSpan);
            }
            return result;
        }
        private GlobalTypeMd TryResolveAsGlobalType(QualifiableNameNode qName) {
            GlobalTypeMd result = null;
            var nameNode = qName.Name;
            var name = nameNode.Value;
            if (qName.IsQualified) {
                var aliasNode = qName.Alias;
                var alias = aliasNode.Value;
                if (alias == "sys") {
                    result = AtomTypeMd.Get(name);
                }
                else {
                    string uri = null;
                    if (alias == "thisns") {
                        uri = _classMd.FullName.Uri;
                    }
                    else {
                        if (UriAliasList != null) {
                            foreach (var item in UriAliasList) {
                                if (item.Alias == alias) {
                                    uri = item.Uri;
                                    break;
                                }
                            }
                        }
                        if (uri == null) {
                            QueryParser.ErrorAndThrow(new DiagMsg(DiagCode.InvalidAliasReference, alias), aliasNode.TextSpan);
                        }
                    }
                    result = ProgramMd.GetGlobalType<GlobalTypeMd>(new FullName(uri, name));
                }
            }
            else {
                result = ProgramMd.GetGlobalType<GlobalTypeMd>(new FullName(_classMd.FullName.Uri, name));
                if (result == null) {
                    if (UriAliasList != null) {
                        foreach (var item in UriAliasList) {
                            var globalType = ProgramMd.GetGlobalType<GlobalTypeMd>(new FullName(item.Uri, name));
                            if (globalType != null) {
                                if (result != null) {
                                    QueryParser.ErrorAndThrow(new DiagMsg(DiagCode.AmbiguousGlobalTypeReference, name), nameNode.TextSpan);
                                }
                                result = globalType;
                            }
                        }
                    }
                }
            }
            return result;
        }
    }
    public enum ExpressionKind {
        None = -1,
        Lambda = ExpressionType.Lambda,
        Conditional = ExpressionType.Conditional,
        Coalesce = ExpressionType.Coalesce,
        OrElse = ExpressionType.OrElse,
        AndAlso = ExpressionType.AndAlso,
        Or = ExpressionType.Or,
        ExclusiveOr = ExpressionType.ExclusiveOr,
        And = ExpressionType.And,
        Equal = ExpressionType.Equal,
        NotEqual = ExpressionType.NotEqual,
        LessThan = ExpressionType.LessThan,
        LessThanOrEqual = ExpressionType.LessThanOrEqual,
        GreaterThan = ExpressionType.GreaterThan,
        GreaterThanOrEqual = ExpressionType.GreaterThanOrEqual,
        TypeIs = ExpressionType.TypeIs,
        TypeAs = ExpressionType.TypeAs,
        LeftShift = ExpressionType.LeftShift,
        RightShift = ExpressionType.RightShift,
        Add = ExpressionType.Add,
        Subtract = ExpressionType.Subtract,
        Multiply = ExpressionType.Multiply,
        Divide = ExpressionType.Divide,
        Modulo = ExpressionType.Modulo,
        Not = ExpressionType.Not,
        Negate = ExpressionType.Negate,
        UnaryPlus = ExpressionType.UnaryPlus,
        OnesComplement = ExpressionType.OnesComplement,
        Convert = ExpressionType.Convert,
        MemberAccess = ExpressionType.MemberAccess,
        Call = ExpressionType.Call,
        Index = ExpressionType.Index,

        QualifiableName = 1000,
        ArgumentRef,
        Null,
        Literal,
        AnonymousObjectCreation,
        //Parenthesized,

    }
    public abstract class ExpressionNode {
        internal ExpressionNode(ExpressionKind kind, TypeMd typeMd) {
            Kind = kind;
            TypeMd = typeMd;
        }
        public readonly ExpressionKind Kind;
        public TypeMd TypeMd { get; internal set; }
    }
    public sealed class LambdaExpressionNode : ExpressionNode {
        internal LambdaExpressionNode(TypeMd typeMd, object parameterOrList, ExpressionNode body)
            : base(ExpressionKind.Lambda, typeMd) {
            ParameterOrList = parameterOrList;
            Body = body;
        }
        public readonly object ParameterOrList;//opt, LambdaParameterNode or List<LambdaParameterNode>
        public readonly ExpressionNode Body;
    }
    public sealed class LambdaParameterNode : ITypeProviderMd {
        internal LambdaParameterNode(string name, TypeMd typeMd) {
            Name = name;
            TypeMd = typeMd;
        }
        public readonly string Name;
        public readonly TypeMd TypeMd;
        TypeMd ITypeProviderMd.Type {
            get { return TypeMd; }
        }
    }

    public sealed class ConditionalExpressionNode : ExpressionNode {
        internal ConditionalExpressionNode(TypeMd typeMd, ExpressionNode condition, ExpressionNode whenTrue, ExpressionNode whenFalse)
            : base(ExpressionKind.Conditional, typeMd) {
            Condition = condition;
            WhenTrue = whenTrue;
            WhenFalse = whenFalse;
        }
        public readonly ExpressionNode Condition;
        public readonly ExpressionNode WhenTrue;
        public readonly ExpressionNode WhenFalse;
    }
    //Coalesce, OrElse, AndAlso, Or, ExclusiveOr, And, Equal, NotEqual, LessThan, LessThanOrEqual, GreaterThan, GreaterThanOrEqual
    //LeftShift, RightShift, Add, Subtract, Multiply, Divide, Modulo
    public sealed class BinaryExpressionNode : ExpressionNode {
        internal BinaryExpressionNode(ExpressionKind kind, TypeMd typeMd, ExpressionNode left, ExpressionNode right) :
            base(kind, typeMd) {
            Left = left;
            Right = right;
        }
        public readonly ExpressionNode Left;
        public readonly ExpressionNode Right;
    }
    //Not, Negate, UnaryPlus, OnesComplement 
    public sealed class UnaryExpressionNode : ExpressionNode {
        internal UnaryExpressionNode(ExpressionKind kind, TypeMd typeMd, ExpressionNode expression) :
            base(kind, typeMd) {
            Expression = expression;
        }
        public readonly ExpressionNode Expression;
    }
    //Convert, TypeIs, TypeAs
    public sealed class TypedExpressionNode : ExpressionNode {
        internal TypedExpressionNode(ExpressionKind kind, TypeMd typeMd, QualifiableNameNode typeQName, GlobalTypeMd type, ExpressionNode expression) :
            base(kind, typeMd) {
            TypeQName = typeQName;
            Type = type;
            Expression = expression;
        }
        internal readonly QualifiableNameNode TypeQName;
        public readonly GlobalTypeMd Type;
        public readonly ExpressionNode Expression;
    }

    public sealed class QualifiableNameExpressionNode : ExpressionNode {
        internal QualifiableNameExpressionNode(ITypeProviderMd typeProviderMd)
            : base(ExpressionKind.QualifiableName, typeProviderMd.Type) {
            _typeProviderMd = typeProviderMd;
        }
        private ITypeProviderMd _typeProviderMd;

    }
    public sealed class LiteralExpressionNode : ExpressionNode {
        internal LiteralExpressionNode(TypeMd typeMd, object value)
            : base(ExpressionKind.Literal, typeMd) {
            Value = value;
        }
        public readonly object Value;
    }
    public sealed class AnonymousObjectCreationExpressionNode : ExpressionNode {
        internal AnonymousObjectCreationExpressionNode(TypeMd typeMd, List<AnonymousObjectMemberNode> memberList)
            : base(ExpressionKind.AnonymousObjectCreation, typeMd) {
            MemberList = memberList;
        }
        internal readonly List<AnonymousObjectMemberNode> MemberList;
    }
    internal sealed class AnonymousObjectMemberNode {
        internal AnonymousObjectMemberNode(NameNode name, ExpressionNode value) {
            Name = name;
            Value = value;
        }
        public readonly NameNode Name;
        public readonly ExpressionNode Value;
    }
    public sealed class MemberAccessExpressionNode : ExpressionNode {
        internal MemberAccessExpressionNode(TypeMd typeMd, ExpressionNode expression, NameNode name)
            : base(ExpressionKind.MemberAccess, typeMd) {
            Expression = expression;
            Name = name;
        }
        public readonly ExpressionNode Expression;
        internal readonly NameNode Name;
        //internal override void Resolve(QueryDiagContext ctx) {
        //    throw new NotImplementedException();
        //}
    }
    public sealed class CallOrIndexExpressionNode : ExpressionNode {
        internal CallOrIndexExpressionNode(TypeMd typeMd, bool isCall, ExpressionNode expression, List<ExpressionNode> argumentList)
            : base(isCall ? ExpressionKind.Call : ExpressionKind.Index, typeMd) {
            Expression = expression;
            ArgumentList = argumentList;
        }
        public readonly ExpressionNode Expression;
        internal readonly List<ExpressionNode> ArgumentList;
        //internal override void Resolve(QueryDiagContext ctx) {
        //    throw new NotImplementedException();
        //}
    }



}
