using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CData {
    internal enum DiagCode {
        None = 0,
        Parsing = -1000,
        UriReserved,
        UriAliasReserved,
        InvalidUriReference,
        DuplicateNamespaceAlias,
        InvalidNamespaceReference,
        InvalidNamespaceAliasReference,
        AmbiguousGlobalTypeReference,
        InvalidGlobalTypeReference,
        DuplicateParameterName,

        InvalidClassReference,
        ClassNotEqualToOrDeriveFromTheDeclared,
        ClassIsAbstract,
        InvalidPropertyName,
        PropertyMissing,
        NullNotAllowed,
        ValueExpected,
        SpecificValueExpected,
        InvalidAtomValue,
        InvalidEnumReference,
        EnumNotEqualToTheDeclared,
        InvalidEnumMemberName,
        DuplicateSetItem,
        DuplicateMapKey,

    }
    internal struct DiagMsg {
        public DiagMsg(DiagCode code) {
            Code = code;
            _msgArgs = null;
        }
        public DiagMsg(DiagCode code, params string[] msgArgs) {
            Code = code;
            _msgArgs = msgArgs;
        }
        public readonly DiagCode Code;
        private readonly string[] _msgArgs;
        public string GetMessage() {
            switch (Code) {
                case DiagCode.UriReserved:
                    return "Uri '" + Extensions.SystemUri + "' is reserved.";
                case DiagCode.UriAliasReserved:
                    return "Uri alias 'sys' or 'thisns' are reserved.";
                case DiagCode.InvalidUriReference:
                    return "Invalid uri reference '{0}'.".InvFormat(_msgArgs);
                case DiagCode.DuplicateNamespaceAlias:
                    return "Duplicate namespace alias '{0}'.".InvFormat(_msgArgs);
                case DiagCode.InvalidNamespaceReference:
                    return "Invalid namespace reference '{0}'.".InvFormat(_msgArgs);
                case DiagCode.InvalidNamespaceAliasReference:
                    return "Invalid namespace alias reference '{0}'.".InvFormat(_msgArgs);
                case DiagCode.AmbiguousGlobalTypeReference:
                    return "Ambiguous global type reference '{0}'.".InvFormat(_msgArgs);
                case DiagCode.InvalidGlobalTypeReference:
                    return "Invalid global type reference '{0}'.".InvFormat(_msgArgs);
                case DiagCode.DuplicateParameterName:
                    return "Duplicate parameter name '{0}'.".InvFormat(_msgArgs);


                case DiagCode.InvalidClassReference:
                    return "Invalid class reference '{0}'.".InvFormat(_msgArgs);
                case DiagCode.ClassNotEqualToOrDeriveFromTheDeclared:
                    return "Class '{0}' not equal to or derive from the declared class '{1}'.".InvFormat(_msgArgs);
                case DiagCode.ClassIsAbstract:
                    return "Class '{0}' is abstract.".InvFormat(_msgArgs);
                case DiagCode.InvalidPropertyName:
                    return "Invalid property name '{0}'.".InvFormat(_msgArgs);
                case DiagCode.PropertyMissing:
                    return "Property '{0}' missing.".InvFormat(_msgArgs);
                case DiagCode.NullNotAllowed:
                    return "Null not allowed.";
                case DiagCode.ValueExpected:
                    return "Value expetced.";
                case DiagCode.SpecificValueExpected:
                    return "{0} value expetced.".InvFormat(_msgArgs);
                case DiagCode.InvalidAtomValue:
                    return "Invalid atom '{0}' value '{1}'.".InvFormat(_msgArgs);
                case DiagCode.InvalidEnumReference:
                    return "Invalid enum reference '{0}'.".InvFormat(_msgArgs);
                case DiagCode.EnumNotEqualToTheDeclared:
                    return "Enum '{0}' not equal to the declared enum '{1}'.".InvFormat(_msgArgs);
                case DiagCode.InvalidEnumMemberName:
                    return "Invalid enum member name '{0}'.".InvFormat(_msgArgs);
                case DiagCode.DuplicateSetItem:
                    return "Duplicate set item.";
                case DiagCode.DuplicateMapKey:
                    return "Duplicate map key.";

                default:
                    throw new InvalidOperationException("Invalid code: " + Code.ToString());
            }
        }
    }
    public enum DiagSeverity : byte {
        None = 0,
        Error = 1,
        Warning = 2,
        Info = 3
    }
    [DataContract(Namespace = Extensions.SystemUri)]
    public struct Diag {
        public Diag(DiagSeverity severity, int code, string message, TextSpan textSpan) {
            Severity = severity;
            Code = code;
            Message = message;
            TextSpan = textSpan;
        }
        internal Diag(DiagSeverity severity, DiagMsg diagMsg, TextSpan textSpan)
            : this(severity, (int)diagMsg.Code, diagMsg.GetMessage(), textSpan) {
        }
        [DataMember]
        public readonly DiagSeverity Severity;
        [DataMember]
        public readonly int Code;
        [DataMember]
        public readonly string Message;
        [DataMember]
        public readonly TextSpan TextSpan;//opt
        public bool IsError {
            get {
                return Severity == DiagSeverity.Error;
            }
        }
        public bool IsWarning {
            get {
                return Severity == DiagSeverity.Warning;
            }
        }
        public bool IsInfo {
            get {
                return Severity == DiagSeverity.Info;
            }
        }
        internal DiagCode DiagCode {
            get {
                return (DiagCode)Code;
            }
        }
        public bool HasTextSpan {
            get {
                return TextSpan.IsValid;
            }
        }
        public bool IsValid {
            get {
                return Severity != DiagSeverity.None;
            }
        }
        public override string ToString() {
            if (IsValid) {
                var sb = StringBuilderBuffer.Acquire();
                sb.Append(Severity.ToString());
                sb.Append(' ');
                sb.Append(Code.ToInvString());
                sb.Append(": ");
                sb.Append(Message);
                if (HasTextSpan) {
                    sb.Append("\r\n    ");
                    sb.Append(TextSpan.ToString());
                }
                return sb.ToStringAndRelease();
            }
            return null;
        }
    }
    [CollectionDataContract(Namespace = Extensions.SystemUri)]
    public class DiagContext : List<Diag> {
        public void AddDiag(DiagSeverity severity, int code, string message, TextSpan textSpan) {
            Add(new Diag(severity, code, message, textSpan));
        }
        internal void AddDiag(DiagSeverity severity, DiagMsg diagMsg, TextSpan textSpan) {
            Add(new Diag(severity, diagMsg, textSpan));
        }
        public bool HasDiags {
            get {
                return Count > 0;
            }
        }
        public bool HasErrorDiags {
            get {
                return HasErrorDiagsCore(0);
            }
        }
        private bool HasErrorDiagsCore(int startIndex) {
            var count = Count;
            for (; startIndex < count; ++startIndex) {
                if (this[startIndex].IsError) {
                    return true;
                }
            }
            return false;
        }
        public struct Marker {
            internal Marker(DiagContext context) {
                Context = context;
                StartIndex = context.Count;
            }
            internal readonly DiagContext Context;
            public readonly int StartIndex;
            public int Count {
                get {
                    return Context.Count - StartIndex;
                }
            }
            public bool HasErrorDiags {
                get {
                    return Context.HasErrorDiagsCore(StartIndex);
                }
            }
            public void Restore() {
                Context.RemoveRange(StartIndex, Context.Count - StartIndex);
            }
        }
        public Marker Mark() {
            return new Marker(this);
        }
        public virtual void Reset() {
            Clear();
        }
    }

}
