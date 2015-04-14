using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace CData {
    [Flags]
    public enum ExpressionActionFlags {
        None = 0,
        AllowIdempotent = 1,
        AllowUnsafe = AllowIdempotent | 2,
    }

    public static class ExpressionParser {
        public static bool TryParse(ClassTypeMd classTypeMd, ExpressionActionFlags flags, string argsText, string exprText,
            DiagContext diagCtx, out object result) {
            if (classTypeMd == null) throw new ArgumentNullException("classTypeMd");
            if (exprText == null) throw new ArgumentNullException("exprText");
            result = null;
            var sr = SimpleStringReader;
            List<NamedExpressionValue> argList = null;
            if (!string.IsNullOrEmpty(argsText)) {
                sr.SetString(argsText);
                if (!Parser.ParseExpressionArguments("ExpressionArguments", sr, diagCtx, out argList)) {
                    return false;
                }
            }


            _currentContext = diagCtx;
            sr.SetString(exprText);
            ExpressionNode expr;
            if (!Parser.ParseExpression("Expression", sr, diagCtx, ExpressionContext.Get(classTypeMd, flags, argList), out expr)) {
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

    //public sealed class QueryNode {
    //    public readonly ClassTypeMd ClassMd;
    //    public readonly string QueryString;
    //    internal readonly List<QueryArgumentNode> ArgumentList;//opt

    //}




    internal sealed class ExpressionContext {
        public static ExpressionContext Get(ClassTypeMd classTypeMd, ExpressionActionFlags flags, List<NamedExpressionValue> argList) {
            var obj = Instance;
            obj.Set(classTypeMd, flags, argList);
            return obj;
        }
        [ThreadStatic]
        private static ExpressionContext _instance;
        private static ExpressionContext Instance {
            get { return _instance ?? (_instance = new ExpressionContext()); }
        }
        private ExpressionContext() {
            UriAliasList = new List<UriAliasNode>();
            _lambdaParameterStack = new Stack<object>();
        }
        private ClassTypeMd _classTypeMd;
        private ExpressionActionFlags _flags;
        private List<NamedExpressionValue> _argumentList;//opt
        internal readonly List<UriAliasNode> UriAliasList;
        private readonly Stack<object> _lambdaParameterStack;
        public void Set(ClassTypeMd classTypeMd, ExpressionActionFlags flags, List<NamedExpressionValue> argList) {
            _classTypeMd = classTypeMd;
            _flags = flags;
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
                            ExpressionParser.ErrorAndThrow(new DiagMsg(DiagCode.DuplicateLambdaParameterName, name), nameNode.TextSpan);
                        }
                    }
                    else {
                        foreach (var p in ((List<LambdaParameterNode>)item)) {
                            if (p.Name == name) {
                                ExpressionParser.ErrorAndThrow(new DiagMsg(DiagCode.DuplicateLambdaParameterName, name), nameNode.TextSpan);
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
        internal ITypeProviderMd Resolve(QNameNode qName) {
            if (qName.IsQualified) {
                return ResolveAsGlobalType(qName);
            }
            var nameNode = qName.Name;
            var name = nameNode.Value;
            if (name == "this" && nameNode.TextSpan.Length == 4) {
                return _classTypeMd;
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
            ITypeProviderMd result = _classTypeMd.GetPropertyInHierarchy(name);
            if (result != null) {
                return result;
            }
            result = TryResolveAsGlobalType(qName);
            if (result == null) {

            }
            return result;
        }
        internal GlobalTypeMd ResolveAsGlobalType(QNameNode qName) {
            var result = TryResolveAsGlobalType(qName);
            if (result == null) {
                ExpressionParser.ErrorAndThrow(new DiagMsg(DiagCode.InvalidGlobalTypeReference, qName.Name.Value), qName.Name.TextSpan);
            }
            return result;
        }
        private GlobalTypeMd TryResolveAsGlobalType(QNameNode qName) {
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
                        uri = _classTypeMd.FullName.Uri;
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
                            ExpressionParser.ErrorAndThrow(new DiagMsg(DiagCode.InvalidAliasReference, alias), aliasNode.TextSpan);
                        }
                    }
                    result = ProgramMd.GetGlobalType<GlobalTypeMd>(new FullName(uri, name));
                }
            }
            else {
                result = ProgramMd.GetGlobalType<GlobalTypeMd>(new FullName(_classTypeMd.FullName.Uri, name));
                if (result == null) {
                    if (UriAliasList != null) {
                        foreach (var item in UriAliasList) {
                            var globalType = ProgramMd.GetGlobalType<GlobalTypeMd>(new FullName(item.Uri, name));
                            if (globalType != null) {
                                if (result != null) {
                                    ExpressionParser.ErrorAndThrow(new DiagMsg(DiagCode.AmbiguousGlobalTypeReference, name), nameNode.TextSpan);
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
        Literal,
        AnonymousObjectCreation,
        //Parenthesized,

    }
    public abstract class ExpressionNode {
        internal ExpressionNode(TextSpan textSpan, ExpressionKind kind, TypeMd typeMd) {
            TextSpan = textSpan;
            Kind = kind;
            TypeMd = typeMd;
        }
        public readonly TextSpan TextSpan;
        public readonly ExpressionKind Kind;
        public readonly TypeMd TypeMd;// { get; internal set; }
    }
    public sealed class LambdaExpressionNode : ExpressionNode {
        internal LambdaExpressionNode(TextSpan textSpan, TypeMd typeMd, object parameterOrList, ExpressionNode body)
            : base(textSpan, ExpressionKind.Lambda, typeMd) {
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
        internal ConditionalExpressionNode(TextSpan textSpan, TypeMd typeMd, ExpressionNode condition, ExpressionNode whenTrue, ExpressionNode whenFalse)
            : base(textSpan, ExpressionKind.Conditional, typeMd) {
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
        internal BinaryExpressionNode(TextSpan textSpan, ExpressionKind kind, TypeMd typeMd, ExpressionNode left, ExpressionNode right) :
            base(textSpan, kind, typeMd) {
            Left = left;
            Right = right;
        }
        public readonly ExpressionNode Left;
        public readonly ExpressionNode Right;
    }
    //Not, Negate, UnaryPlus, OnesComplement 
    public sealed class UnaryExpressionNode : ExpressionNode {
        internal UnaryExpressionNode(TextSpan textSpan, ExpressionKind kind, TypeMd typeMd, ExpressionNode expression) :
            base(textSpan, kind, typeMd) {
            Expression = expression;
        }
        public readonly ExpressionNode Expression;
    }
    //Convert, TypeIs, TypeAs
    public sealed class TypedExpressionNode : ExpressionNode {
        internal TypedExpressionNode(TextSpan textSpan, ExpressionKind kind, TypeMd typeMd, GlobalTypeMd type, ExpressionNode expression) :
            base(textSpan, kind, typeMd) {
            //TypeQName = typeQName;
            Type = type;
            Expression = expression;
        }
        //internal readonly QualifiableNameNode TypeQName;
        public readonly GlobalTypeMd Type;
        public readonly ExpressionNode Expression;
    }

    public sealed class QualifiableNameExpressionNode : ExpressionNode {
        internal QualifiableNameExpressionNode(TextSpan textSpan, ITypeProviderMd typeProviderMd)
            : base(textSpan, ExpressionKind.QualifiableName, typeProviderMd.Type) {
            _typeProviderMd = typeProviderMd;
        }
        private ITypeProviderMd _typeProviderMd;

    }
    public sealed class LiteralExpressionNode : ExpressionNode {
        internal LiteralExpressionNode(TextSpan textSpan, TypeMd typeMd, object value)
            : base(textSpan, ExpressionKind.Literal, typeMd) {
            Value = value;
        }
        public readonly object Value;
    }
    public sealed class AnonymousObjectCreationExpressionNode : ExpressionNode {
        internal AnonymousObjectCreationExpressionNode(TextSpan textSpan, TypeMd typeMd, List<AnonymousObjectMemberNode> memberList)
            : base(textSpan, ExpressionKind.AnonymousObjectCreation, typeMd) {
            MemberList = memberList;
        }
        internal readonly List<AnonymousObjectMemberNode> MemberList;//opt
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
        internal MemberAccessExpressionNode(TextSpan textSpan, TypeMd typeMd, ExpressionNode expression, string name)
            : base(textSpan, ExpressionKind.MemberAccess, typeMd) {
            Expression = expression;
            Name = name;
        }
        public readonly ExpressionNode Expression;
        public readonly string Name;
    }
    public sealed class CallOrIndexExpressionNode : ExpressionNode {
        internal CallOrIndexExpressionNode(TextSpan textSpan, TypeMd typeMd, bool isCall, ExpressionNode expression, List<ExpressionNode> argumentList)
            : base(textSpan, isCall ? ExpressionKind.Call : ExpressionKind.Index, typeMd) {
            Expression = expression;
            ArgumentList = argumentList;
        }
        public readonly ExpressionNode Expression;
        internal readonly List<ExpressionNode> ArgumentList;
    }



}
