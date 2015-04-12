using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace CData {
    public static class QueryParser {
        public static bool TryParse(ClassMd classMd, string arguments, string query, DiagContext context, out QueryNode result) {
            if (classMd == null) throw new ArgumentNullException("classMd");
            if (query == null) throw new ArgumentNullException("query");
            result = null;
            var sr = _simpleStringReader;
            List<QueryArgumentNode> argList = null;
            if (!string.IsNullOrEmpty(arguments)) {
                sr.SetString(arguments);
                if (!Parser.ParseQueryArguments("QueryArguments", sr, context, out argList)) {
                    return false;
                }
            }
            //Queue<int>
            sr.SetString(query);



            return false;
        }
        [ThreadStatic]
        private static readonly SimpleStringReader _simpleStringReader = new SimpleStringReader();
        private const int _queryChacheLength = 16;
        [ThreadStatic]
        private static readonly QueryCache[] _queryCacheBuf = new QueryCache[_queryChacheLength];
        private struct QueryCache {
            public readonly ClassMd ClassMd;
            public readonly string Query;
            public readonly Expression Expression;
        }


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
        public readonly ClassMd ClassMd;
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

    internal sealed class QueryDiagContext : DiagContext {
        public static QueryDiagContext Get(ClassMd classMd) {
            var obj = _instance;
            obj.Reset();
            obj._classMd = classMd;
            //obj._uriAliasList = uriAliasList;
            return obj;
        }
        [ThreadStatic]
        private static readonly QueryDiagContext _instance = new QueryDiagContext();
        private QueryDiagContext() {
            UriAliasList = new List<UriAliasNode>();
            _lambdaParameterStack = new Stack<LambdaParameterNodeList>();
        }
        private ClassMd _classMd;
        internal readonly List<UriAliasNode> UriAliasList;
        private readonly Stack<LambdaParameterNodeList> _lambdaParameterStack;
        public override void Reset() {
            base.Reset();
            UriAliasList.Clear();
            _lambdaParameterStack.Clear();
        }
        internal void PushParameterList(LambdaParameterNodeList list) {
            var stack = _lambdaParameterStack;
            if (stack.Count > 0) {
                foreach (var itemlist in stack) {
                    foreach (var ip in itemlist) {
                        foreach (var p in list) {
                            if (ip.Name == p.Name) {
                                ErrorAndThrow(new DiagMsg(DiagCode.DuplicateLambdaParameterName, p.Name.Value), p.Name.TextSpan);
                            }
                        }
                    }
                }
            }
            stack.Push(list);
        }
        internal void PopParameterList() {
            _lambdaParameterStack.Pop();
        }

        internal void ErrorAndThrow(DiagMsg diagMsg, TextSpan textSpan) {
            base.AddDiag(DiagSeverity.Error, diagMsg, textSpan);
            throw DiagExceptionObject;
        }
        internal object Resolve(QualifiableNameNode qName) {
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
                foreach (var itemlist in stack) {
                    foreach (var ip in itemlist) {
                        if (ip.Name.Value == name) {
                            return ip;
                        }
                    }
                }
            }
            object result = _classMd.GetPropertyInHierarchy(name);
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
                ErrorAndThrow(new DiagMsg(DiagCode.InvalidGlobalTypeReference, qName.Name.Value), qName.Name.TextSpan);
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
                    result = AtomMd.Get(name);
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
                            ErrorAndThrow(new DiagMsg(DiagCode.InvalidAliasReference, alias), aliasNode.TextSpan);
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
                                    ErrorAndThrow(new DiagMsg(DiagCode.AmbiguousGlobalTypeReference, name), nameNode.TextSpan);
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
        Literal,
        AnonymousObjectCreation,
        Parenthesized,

    }
    internal abstract class ExpressionNode {
        protected ExpressionNode(ExpressionKind kind) {
            Kind = kind;
        }
        public readonly ExpressionKind Kind;
        //internal abstract void Resolve(QueryDiagContext ctx);
    }
    internal sealed class LambdaExpressionNode : ExpressionNode {
        public LambdaExpressionNode(LambdaParameterNodeList parameterList, ExpressionNode body)
            : base(ExpressionKind.Lambda) {
            ParameterList = parameterList;
            Body = body;
        }
        public readonly LambdaParameterNodeList ParameterList;//opt
        public readonly ExpressionNode Body;
        //internal override void Resolve(QueryDiagContext ctx) {
        //    Body.Resolve(ctx);
        //}
    }
    internal sealed class LambdaParameterNodeList : List<LambdaParameterNode> {

    }
    internal struct LambdaParameterNode {
        public LambdaParameterNode(NameNode name) {
            Name = name;
        }
        public readonly NameNode Name;
    }

    internal sealed class ConditionalExpressionNode : ExpressionNode {
        public ConditionalExpressionNode(ExpressionNode condition, ExpressionNode whenTrue, ExpressionNode whenFalse)
            : base(ExpressionKind.Conditional) {
            Condition = condition;
            WhenTrue = whenTrue;
            WhenFalse = whenFalse;
        }
        public readonly ExpressionNode Condition;
        public readonly ExpressionNode WhenTrue;
        public readonly ExpressionNode WhenFalse;
        //internal override void Resolve(QueryDiagContext ctx) {
        //    Condition.Resolve(ctx);
        //    WhenTrue.Resolve(ctx);
        //    WhenFalse.Resolve(ctx);
        //}
    }
    //Coalesce, OrElse, AndAlso, Or, ExclusiveOr, And, Equal, NotEqual, LessThan, LessThanOrEqual, GreaterThan, GreaterThanOrEqual
    //LeftShift, RightShift, Add, Subtract, Multiply, Divide, Modulo
    internal sealed class BinaryExpressionNode : ExpressionNode {
        public BinaryExpressionNode(ExpressionKind kind, ExpressionNode left, ExpressionNode right) :
            base(kind) {
            Left = left;
            Right = right;
        }
        public readonly ExpressionNode Left;
        public readonly ExpressionNode Right;
        //internal override void Resolve(QueryDiagContext ctx) {
        //    Left.Resolve(ctx);
        //    Right.Resolve(ctx);
        //}

    }
    //Not, Negate, UnaryPlus, OnesComplement, Parenthesized
    internal sealed class UnaryExpressionNode : ExpressionNode {
        public UnaryExpressionNode(ExpressionKind kind, ExpressionNode expression) :
            base(kind) {
            Expression = expression;
        }
        public readonly ExpressionNode Expression;
        //internal override void Resolve(QueryDiagContext ctx) {
        //    Expression.Resolve(ctx);
        //}
    }
    //Convert, TypeIs, TypeAs
    internal sealed class TypedExpressionNode : ExpressionNode {
        public TypedExpressionNode(ExpressionKind kind, QualifiableNameNode typeQName, GlobalTypeMd type, ExpressionNode expression) :
            base(kind) {
            TypeQName = typeQName;
            Type = type;
            Expression = expression;
        }
        public readonly QualifiableNameNode TypeQName;
        public readonly GlobalTypeMd Type;
        public readonly ExpressionNode Expression;
        //internal override void Resolve(QueryDiagContext ctx) {
        //    _globalType = ctx.ResolveAsGlobalType(TypeQName);
        //    Expression.Resolve(ctx);
        //}
    }

    internal sealed class QualifiableNameExpressionNode : ExpressionNode {
        internal QualifiableNameExpressionNode(QueryDiagContext ctx, QualifiableNameNode qName)
            : base(ExpressionKind.QualifiableName) {
            QName = qName;
            _target = ctx.Resolve(qName);
        }
        public readonly QualifiableNameNode QName;
        private object _target;
        //internal override void Resolve(QueryDiagContext ctx) {
        //    throw new NotImplementedException();
        //}
    }
    internal sealed class LiteralExpressionNode : ExpressionNode {
        internal LiteralExpressionNode(AtomValueNode value)
            : base(ExpressionKind.Literal) {
            Value = value;
        }
        public readonly AtomValueNode Value;
        //internal override void Resolve(QueryDiagContext ctx) {
        //    throw new NotImplementedException();
        //}
    }
    internal sealed class AnonymousObjectCreationExpressionNode : ExpressionNode {
        internal AnonymousObjectCreationExpressionNode(List<AnonymousObjectMemberNode> memberList)
            : base(ExpressionKind.AnonymousObjectCreation) {
            MemberList = memberList;
        }
        public readonly List<AnonymousObjectMemberNode> MemberList;
        //internal override void Resolve(QueryDiagContext ctx) {
        //    throw new NotImplementedException();
        //}
    }
    internal sealed class AnonymousObjectMemberNode {
        internal AnonymousObjectMemberNode(NameNode name, ExpressionNode value) {
            Name = name;
            Value = value;
        }
        public readonly NameNode Name;
        public readonly ExpressionNode Value;
    }
    internal sealed class MemberAccessExpressionNode : ExpressionNode {
        internal MemberAccessExpressionNode(ExpressionNode expression, NameNode name)
            : base(ExpressionKind.MemberAccess) {
            Expression = expression;
            Name = name;
        }
        public readonly ExpressionNode Expression;
        public readonly NameNode Name;
        //internal override void Resolve(QueryDiagContext ctx) {
        //    throw new NotImplementedException();
        //}
    }
    internal sealed class CallOrIndexExpressionNode : ExpressionNode {
        internal CallOrIndexExpressionNode(bool isCall, ExpressionNode expression, List<ExpressionNode> argumentList)
            : base(isCall ? ExpressionKind.Call : ExpressionKind.Index) {
            Expression = expression;
            ArgumentList = argumentList;
        }
        public readonly ExpressionNode Expression;
        public readonly List<ExpressionNode> ArgumentList;
        //internal override void Resolve(QueryDiagContext ctx) {
        //    throw new NotImplementedException();
        //}
    }



}
