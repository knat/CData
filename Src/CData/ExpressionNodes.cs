using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace CData {
    internal sealed class QueryNode {
        internal QueryNode(List<AliasUriNode> aliasUriList, ExpressionNode expression) {
            AliasUriList = aliasUriList;
            Expression = expression;
        }
        public readonly List<AliasUriNode> AliasUriList;//opt
        public readonly ExpressionNode Expression;
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
    }
    internal sealed class LambdaExpressionNode : ExpressionNode {
        public LambdaExpressionNode(List<NameNode> parameterNameList, ExpressionNode body)
            : base(ExpressionKind.Lambda) {
            ParameterNameList = parameterNameList;
            Body = body;
        }
        public readonly List<NameNode> ParameterNameList;
        public readonly ExpressionNode Body;
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

    }
    //Not, Negate, UnaryPlus, OnesComplement, Parenthesized
    internal sealed class UnaryExpressionNode : ExpressionNode {
        public UnaryExpressionNode(ExpressionKind kind, ExpressionNode expression) :
            base(kind) {
            Expression = expression;
        }
        public readonly ExpressionNode Expression;
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
    }

    internal sealed class QualifiableNameExpressionNode : ExpressionNode {
        internal QualifiableNameExpressionNode(QualifiableNameNode qName)
            : base(ExpressionKind.QualifiableName) {
            QName = qName;
        }
        public readonly QualifiableNameNode QName;
    }
    internal sealed class LiteralExpressionNode : ExpressionNode {
        internal LiteralExpressionNode(AtomValueNode value)
            : base(ExpressionKind.Literal) {
            Value = value;
        }
        public readonly AtomValueNode Value;
    }
    internal sealed class AnonymousObjectCreationExpressionNode : ExpressionNode {
        internal AnonymousObjectCreationExpressionNode(List<AnonymousObjectMemberNode> memberList)
            : base(ExpressionKind.AnonymousObjectCreation) {
            MemberList = memberList;
        }
        public readonly List<AnonymousObjectMemberNode> MemberList;
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
    }
    internal sealed class CallOrIndexExpressionNode : ExpressionNode {
        internal CallOrIndexExpressionNode(bool isCall, ExpressionNode expression, List<ExpressionNode> argumentList)
            : base(isCall ? ExpressionKind.Call : ExpressionKind.Index) {
            Expression = expression;
            ArgumentList = argumentList;
        }
        public readonly ExpressionNode Expression;
        public readonly List<ExpressionNode> ArgumentList;
    }



}
