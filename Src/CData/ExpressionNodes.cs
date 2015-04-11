using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace CData {
    internal sealed class QueryNode {
        internal QueryNode(List<AliasUriNode> aliasUriList, ExpressionNode expression) {
            AliasUriList = aliasUriList;
            Expression = expression;
        }
        internal readonly List<AliasUriNode> AliasUriList;//opt
        public readonly ExpressionNode Expression;
        internal bool Resolve(ClassMd classMd, DiagContext context) {

            return false;
        }
    }
    internal sealed class QueryDiagContext : DiagContext {
        public static QueryDiagContext Get(ClassMd classMd, List<AliasUriNode> aliasUriList) {
            var obj = _instance;
            obj.Reset();
            obj._classMd = classMd;
            obj._aliasUriList = aliasUriList;
            return obj;
        }
        [ThreadStatic]
        private static readonly QueryDiagContext _instance = new QueryDiagContext();
        private QueryDiagContext() {
            _parameterListStack = new Stack<ParameterNodeList>();
        }
        private ClassMd _classMd;
        private List<AliasUriNode> _aliasUriList;//opt
        private readonly Stack<ParameterNodeList> _parameterListStack;
        public override void Reset() {
            base.Reset();
            _parameterListStack.Clear();
        }
        internal void PushParameterList(ParameterNodeList list) {
            var stack = _parameterListStack;
            if (stack.Count > 0) {
                foreach (var itemlist in stack) {
                    foreach (var ip in itemlist) {
                        foreach (var p in list) {
                            if (ip.Name == p.Name) {
                                ErrorAndThrow(new DiagMsg(DiagCode.DuplicateParameterName, p.Name.Value), p.Name.TextSpan);
                            }
                        }
                    }
                }
            }
            stack.Push(list);
        }
        internal void PopParameterList() {
            _parameterListStack.Pop();
        }

        internal sealed class ContextException : Exception { }
        private static readonly ContextException _contextException = new ContextException();
        internal void ErrorAndThrow(DiagMsg diagMsg, TextSpan textSpan) {
            base.AddDiag(DiagSeverity.Error, diagMsg, textSpan);
            throw _contextException;
        }
        internal GlobalTypeMd ResolveAsGlobalType(QualifiableNameNode qName) {
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
                        if (_aliasUriList != null) {
                            foreach (var item in _aliasUriList) {
                                if (item.Alias == aliasNode) {
                                    uri = item.Uri.Value;
                                    break;
                                }
                            }
                        }
                        if (uri == null) {
                            ErrorAndThrow(new DiagMsg(DiagCode.InvalidNamespaceAliasReference, alias), aliasNode.TextSpan);
                        }
                    }
                    result = ProgramMd.GetGlobalType<GlobalTypeMd>(new FullName(uri, name));
                }
            }
            else {
                result = ProgramMd.GetGlobalType<GlobalTypeMd>(new FullName(_classMd.FullName.Uri, name));
                if (result == null) {
                    if (_aliasUriList != null) {
                        foreach (var item in _aliasUriList) {
                            var globalType = ProgramMd.GetGlobalType<GlobalTypeMd>(new FullName(item.Uri.Value, name));
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
            if (result == null) {
                ErrorAndThrow(new DiagMsg(DiagCode.InvalidGlobalTypeReference, name), nameNode.TextSpan);
            }
            return result;
        }
    }
    internal sealed class AliasUriNode {
        public AliasUriNode(NameNode alias, AtomValueNode uri) {
            Alias = alias;
            Uri = uri;
        }
        public readonly NameNode Alias;
        public readonly AtomValueNode Uri;
    }
    internal enum ExpressionKind {
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
        public LambdaExpressionNode(ParameterNodeList parameterList, ExpressionNode body)
            : base(ExpressionKind.Lambda) {
            ParameterList = parameterList;
            Body = body;
        }
        public readonly ParameterNodeList ParameterList;//opt
        public readonly ExpressionNode Body;
        //internal override void Resolve(QueryDiagContext ctx) {
        //    Body.Resolve(ctx);
        //}
    }
    internal sealed class ParameterNodeList : List<ParameterNode> {

    }
    internal sealed class ParameterNode {
        public ParameterNode(NameNode name) {
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
        public TypedExpressionNode(ExpressionKind kind, QualifiableNameNode typeQName, ExpressionNode expression) :
            base(kind) {
            TypeQName = typeQName;
            Expression = expression;
        }
        public readonly QualifiableNameNode TypeQName;
        public readonly ExpressionNode Expression;
        private GlobalTypeMd _globalType;
        //internal override void Resolve(QueryDiagContext ctx) {
        //    _globalType = ctx.ResolveAsGlobalType(TypeQName);
        //    Expression.Resolve(ctx);
        //}
    }

    internal sealed class QualifiableNameExpressionNode : ExpressionNode {
        internal QualifiableNameExpressionNode(QualifiableNameNode qName)
            : base(ExpressionKind.QualifiableName) {
            QName = qName;
        }
        public readonly QualifiableNameNode QName;
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
