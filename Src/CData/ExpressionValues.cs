using System;
using System.Collections.Generic;

namespace CData {
    public enum ExpressionValueKind : byte {
        None = 0,
        Atom = 1,
        List = 2,
        Class = 3
    }
    public abstract class ExpressionValue {
        internal ExpressionValue(ExpressionValueKind kind, TextSpan textSpan) {
            Kind = kind;
            TextSpan = textSpan;
        }
        public readonly ExpressionValueKind Kind;
        public readonly TextSpan TextSpan;
    }
    public sealed class AtomExpressionValue : ExpressionValue {
        internal AtomExpressionValue(TextSpan textSpan, TypeKind typeKind, object value, bool isImplicit)
            : base(ExpressionValueKind.Atom, textSpan) {
            TypeKind = typeKind;
            Value = value;
            IsImplicit = isImplicit;
        }
        public AtomExpressionValue(TypeKind typeKind, object value)
            : this(default(TextSpan), typeKind, value, false) {
        }
        public readonly TypeKind TypeKind;
        public readonly object Value;
        internal readonly bool IsImplicit;
    }
    public sealed class ListExpressionValue : ExpressionValue {
        internal ListExpressionValue(TextSpan textSpan, List<ExpressionValue> items)
            : base(ExpressionValueKind.List, textSpan) {
            Items = items;
        }
        public readonly List<ExpressionValue> Items;//opt
    }
    public sealed class ClassExpressionValue : ExpressionValue {
        internal ClassExpressionValue(TextSpan textSpan, List<NamedExpressionValue> members)
            : base(ExpressionValueKind.Class, textSpan) {
            Members = members;
        }
        public readonly List<NamedExpressionValue> Members;//opt
    }
    public struct NamedExpressionValue {
        public NamedExpressionValue(string name, ExpressionValue value) {
            Name = name;
            Value = value;
        }
        public readonly string Name;
        public readonly ExpressionValue Value;
    }


}
