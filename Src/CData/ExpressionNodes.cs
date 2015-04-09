using System;
using System.Linq.Expressions;

namespace CData {
    internal abstract class ExpressionNode {
        protected ExpressionNode(ExpressionType type) {
            Type = type;
        }
        public readonly ExpressionType Type;
    }

    internal sealed class ConditionalExpressionNode : ExpressionNode {
        public ConditionalExpressionNode(ExpressionNode condition, ExpressionNode whenTrue, ExpressionNode whenFalse)
            : base(ExpressionType.Conditional) {
            Condition = condition;
            WhenTrue = whenTrue;
            WhenFalse = whenFalse;
        }
        public readonly ExpressionNode Condition;
        public readonly ExpressionNode WhenTrue;
        public readonly ExpressionNode WhenFalse;
    }
    internal sealed class BinaryExpressionNode : ExpressionNode {
        public BinaryExpressionNode(ExpressionType type, ExpressionNode left, ExpressionNode right) :
            base(type) {
            Left = left;
            Right = right;
        }
        public readonly ExpressionNode Left;
        public readonly ExpressionNode Right;

    }





}
