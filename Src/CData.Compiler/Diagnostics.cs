using System;

namespace CData.Compiler {
    internal enum DiagCodeEx {
        None = 0,
        InternalCompilerError = -2000,
        //
        AliasSysReserved,
        UriSystemReserved,
        DuplicateImportAlias,
        InvalidNamespaceReference,
        DuplicateNamespaceMemberName,
        DuplicateEnumMemberName,
        InvalidAtomValue,
        DuplicatePropertyName,
        SpecificTypeExpected,
        InvalidImportAliasReference,
        AmbiguousNameReference,
        InvalidNameReference,
        InvalidClassNameReference,
        InvalidAtomNameReference,
        InvalidPropertyNameReference,
        CircularReferenceNotAllowed,

        KeySelectorRequiredForObjectSet,
        KeySelectorNotAllowedForAtomSet,
        BaseClassIsSealed,
        ObjectSetKeyCannotBeNullable,
        InvalidObjectSetKey,
        ObjectSetKeyMustBeAtom,

        //
        InvalidContractNamespaceAttribute,
        InvalidContractNamespaceAttributeUri,
        DuplicateContractNamespaceAttributeUri,
        InvalidContractNamespaceAttributeNamespaceName,
        ContractNamespaceAttributeRequired,
        InvalidContractClassAttribute,
        InvalidContractClassAttributeName,
        DuplicateContractClassAttributeName,
        ContractClassCannotBeGeneric,
        ContractClassCannotBeStatic,
        NonAbstractParameterlessConstructorContractClassRequired,
        InvalidContractPropertyAttribute,
        InvalidContractPropertyAttributeName,
        DuplicateContractPropertyAttributeName,
        ContractPropertyOrFieldCannotBeStatic,
        ContractPropertyMustHaveGetterAndSetter,
        ContractPropertyCannotBeIndexer,
        ContractFieldCannotBeConst,
        InvalidContractPropertyType,



    }
    internal struct DiagMsgEx {
        public DiagMsgEx(DiagCodeEx code) {
            Code = code;
            _msgArgs = null;
        }
        public DiagMsgEx(DiagCodeEx code, params string[] msgArgs) {
            Code = code;
            _msgArgs = msgArgs;
        }
        public readonly DiagCodeEx Code;
        private readonly string[] _msgArgs;
        public string GetMessage() {
            switch (Code) {
                //
                case DiagCodeEx.AliasSysReserved:
                    return "Alias 'sys' is reserved.";
                case DiagCodeEx.UriSystemReserved:
                    return "Uri '" + Extensions.SystemUri + "' is reserved.";
                case DiagCodeEx.DuplicateImportAlias:
                    return "Duplicate import alias '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.InvalidNamespaceReference:
                    return "Invalid namespace reference '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.DuplicateNamespaceMemberName:
                    return "Duplicate namespace member name '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.DuplicateEnumMemberName:
                    return "Duplicate enum member name '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.InvalidAtomValue:
                    return "Invalid atom value '{0}' of type '{1}'.".InvFormat(_msgArgs);

                case DiagCodeEx.DuplicatePropertyName:
                    return "Duplicate property name '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.SpecificTypeExpected:
                    return "'{0}' type expected.".InvFormat(_msgArgs);
                case DiagCodeEx.InvalidImportAliasReference:
                    return "Invalid import alias reference '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.AmbiguousNameReference:
                    return "Ambiguous name reference '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.InvalidNameReference:
                    return "Invalid name reference '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.InvalidClassNameReference:
                    return "Invalid class name reference '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.InvalidAtomNameReference:
                    return "Invalid atom name reference '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.InvalidPropertyNameReference:
                    return "Invalid property name reference '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.CircularReferenceNotAllowed:
                    return "Circular reference not allowed.";



                //
                case DiagCodeEx.InvalidContractNamespaceAttribute:
                    return "Invalid ContractNamespaceAttribute.";
                case DiagCodeEx.InvalidContractNamespaceAttributeUri:
                    return "Invalid ContractNamespaceAttribute uri '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.DuplicateContractNamespaceAttributeUri:
                    return "Duplicate ContractNamespaceAttribute uri '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.InvalidContractNamespaceAttributeNamespaceName:
                    return "Invalid ContractNamespaceAttribute namespaceName '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.ContractNamespaceAttributeRequired:
                    return "ContractNamespaceAttribute required for uri '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.InvalidContractClassAttribute:
                    return "Invalid ContractClassAttribute.";
                case DiagCodeEx.InvalidContractClassAttributeName:
                    return "Invalid ContractClassAttribute name '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.DuplicateContractClassAttributeName:
                    return "Duplicate ContractClassAttribute name '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.ContractClassCannotBeGeneric:
                    return "Contract class cannot be generic.";
                case DiagCodeEx.ContractClassCannotBeStatic:
                    return "Contract class cannot be static.";
                case DiagCodeEx.NonAbstractParameterlessConstructorContractClassRequired:
                    return "Non-abstract parameterless-constructor contract class required.";
                case DiagCodeEx.InvalidContractPropertyAttribute:
                    return "Invalid ContractPropertyAttribute.";
                case DiagCodeEx.InvalidContractPropertyAttributeName:
                    return "Invalid ContractPropertyAttribute name '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.DuplicateContractPropertyAttributeName:
                    return "Duplicate ContractPropertyAttribute name '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.ContractPropertyOrFieldCannotBeStatic:
                    return "Contract property or field cannot be static.";
                case DiagCodeEx.ContractPropertyMustHaveGetterAndSetter:
                    return "Contract property must have getter and setter.";
                case DiagCodeEx.ContractPropertyCannotBeIndexer:
                    return "Contract property cannot be indexer";
                case DiagCodeEx.ContractFieldCannotBeConst:
                    return "Contract field cannot be const.";
                case DiagCodeEx.InvalidContractPropertyType:
                    return "Invalid contract property/field type {0}. {1} expected.".InvFormat(_msgArgs);



                default:
                    throw new InvalidOperationException("Invalid code: " + Code.ToString());
            }
        }
    }

    internal sealed class DiagContextEx : DiagContext {
        [ThreadStatic]
        public static DiagContextEx Current;
        public sealed class ContextException : Exception { }
        private static readonly ContextException _contextException = new ContextException();
        public static void ErrorDiagAndThrow(DiagMsgEx diagMsg, TextSpan textSpan) {
            ErrorDiag(diagMsg, textSpan);
            throw _contextException;
        }
        private static void ErrorDiag(DiagMsgEx diagMsg, TextSpan textSpan) {
            Current.AddDiag(DiagSeverity.Error, (int)diagMsg.Code, diagMsg.GetMessage(), textSpan);
        }
        //private static void ThrowIfHasErrors() {
        //    if (Current.HasErrorDiags) {
        //        throw _contextException;
        //    }
        //}
        //private static void WarningDiag(DiagMsgEx diagMsg, TextSpan textSpan) {
        //    Current.AddDiag(DiagSeverity.Warning, (int)diagMsg.Code, diagMsg.GetMessage(), textSpan);
        //}

    }
}
