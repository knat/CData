using System;

namespace CData.Compiler {
    internal enum DiagCodeEx {
        None = 0,
        InternalCompilerError = -2000,
        //common
        AliasSysReserved,
        UriSystemReserved,
        DuplicateUriAlias,
        InvalidUriReference,
        DuplicateImportAlias,
        InvalidNamespaceReference,
        DuplicateClassName,
        DuplicatePropertyName,
        TypeExpected,


        DuplicateIndicator,
        IndicatorRequiredForNamespace,
        InvalidImportAliasReference,
        AmbiguousNameReference,
        InvalidNameReference,
        InvalidClassNameReference,
        InvalidAtomNameReference,
        InvalidPropertyNameReference,
        KeySelectorRequiredForObjectSet,
        KeySelectorNotAllowedForAtomSet,
        CircularReferenceNotAllowed,
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



        InvalidContractClassAttributeName,
        DuplicateContractClassAttributeName,
        InvalidContractPropertyAttributeName,

        





        //
        //
        InvalidElementNameReference,

        //facets
        UInt64ValueRequired,
        ByteValueRequired,
        FacetNotAllowedForType,
        MaxLengthNotGreaterThanOrEqualToMinLength,
        MinLengthNotGreaterThanOrEqualToBaseMinLength,
        MaxLengthNotLessThanOrEqualToBaseMaxLength,
        PrecisionCannotBeZero,
        ScaleNotLessThanOrEqualToPrecision,
        PrecisionNotLessThanOrEqualToBasePrecision,
        ScaleNotLessThanOrEqualToBaseScale,
        InvalidPattern,
        InvalidLiteralForType,
        DuplicateEnumItemName,
        EnumItemNameNotAllowedInRestriction,
        EnumItemNotInBaseEnum,
        MinValueNotGreaterThanOrEqualToBaseMinValue,
        MinValueNotGreaterThanBaseMinValue,
        MaxValueNotLessThanOrEqualToBaseMaxValue,
        MaxValueNotLessThanBaseMaxValue,
        MaxValueNotGreaterThanOrEqualToMinValue,
        MaxValueNotGreaterThanMinValue,

        //type
        SimpleTypeRequired,
        ComplexTypeRequired,
        BaseTypeSealed,
        CannotExtendOrRestrictSysComplexType,
        CannotRestrictSysSimpleAtomListType,
        CannotExtendSimpleChildWithComplexChildren,
        CannotExtendChildrenWithSimpleChild,
        CannotRestrictSimpleChildWithComplexChildren,
        CannotRestrictComplexChildrenWithSimpleChild,
        CannotRestrictNullSimpleChild,
        AttributesChildrenNotAllowedInSimpleTypeRestriction,
        FacetsNotAllowedInComplexTypeRestriction,
        TypeNotEqualToOrDeriveFrom,
        TypeNotEqualToOrDeriveFromRestricted,
        TypeNotEqualToOrDeriveFromSubstituted,

        //
        //attribute
        DuplicateAttributeName,
        CannotFindRestrictedAttribute,
        CannotDeleteRequiredAttribute,
        CannotChangeRequiredToOptional,
        //
        DeletionNotAllowedInExtension,
        CannotChangeNonNullableToNullable,

        //child
        SubstitutedElementSealed,
        DuplicateMemberName,
        MaxOccurrenceNotEqualToOrGreaterThanMinOccurrence,
        MaxOccurrenceCannotBeZero,
        MaxOccurrenceCannotGreaterThanOneInChildSet,
        ComplexChildrenKindNotEqualToBase,
        DuplicateElementFullName,
        CannotFindRestrictedMember,
        CannotDeleteRequiredMember,
        MemberKindNotEqualToRestricted,
        MinOccurrenceNotEqualToOrGreaterThanRestricted,
        MaxOccurrenceNotEqualToOrLessThanRestricted,
        ElementNameNotEqualToRestricted,
        ElementNotEqualToOrSubstituteForRestricted,
        //AmbiguousElementFullName,

        //
        //

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




                //common
                case DiagCodeEx.AliasSysReserved:
                    return "Alias 'sys' reserved.";
                case DiagCodeEx.UriSystemReserved:
                    return "Uri '" + Extensions.SystemUri + "' reserved.";
                case DiagCodeEx.DuplicateUriAlias:
                    return "Duplicate uri alias '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.InvalidUriReference:
                    return "Invalid uri reference '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.DuplicateImportAlias:
                    return "Duplicate import alias '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.InvalidNamespaceReference:
                    return "Invalid namespace reference '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.DuplicateClassName:
                    return "Duplicate class name '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.DuplicatePropertyName:
                    return "Duplicate property name '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.TypeExpected:
                    return "'{0}' type expected.".InvFormat(_msgArgs);


                //
                //
                //


                case DiagCodeEx.DuplicateIndicator:
                    return "Duplicate indicator '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.IndicatorRequiredForNamespace:
                    return "Indicator required for namespace '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.InvalidImportAliasReference:
                    return "Invalid import alias reference '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.AmbiguousNameReference:
                    return "Ambiguous name reference '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.InvalidNameReference:
                    return "Invalid name reference '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.InvalidClassNameReference:
                    return "Invalid type name reference '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.InvalidElementNameReference:
                    return "Invalid element name reference '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.CircularReferenceNotAllowed:
                    return "Circular reference not allowed.";

                //facets
                case DiagCodeEx.UInt64ValueRequired:
                    return "UInt64 value required.";
                case DiagCodeEx.ByteValueRequired:
                    return "Byte value required.";
                case DiagCodeEx.FacetNotAllowedForType:
                    return "Facet not allowed for type '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.MaxLengthNotGreaterThanOrEqualToMinLength:
                    return "Max length '{0}' not greater than or equal to min length '{1}'.".InvFormat(_msgArgs);
                case DiagCodeEx.MinLengthNotGreaterThanOrEqualToBaseMinLength:
                    return "Min length '{0}' not greater than or equal to base min length '{1}'.".InvFormat(_msgArgs);
                case DiagCodeEx.MaxLengthNotLessThanOrEqualToBaseMaxLength:
                    return "Max length '{0}' not less than or equal to base max length '{1}'.".InvFormat(_msgArgs);
                case DiagCodeEx.PrecisionCannotBeZero:
                    return "Precision cannot ze zero.";
                case DiagCodeEx.ScaleNotLessThanOrEqualToPrecision:
                    return "Scale '{0}' not less than or equal to precision '{1}'.".InvFormat(_msgArgs);
                case DiagCodeEx.PrecisionNotLessThanOrEqualToBasePrecision:
                    return "Precision '{0}' not less than or equal to base precision '{1}'.".InvFormat(_msgArgs);
                case DiagCodeEx.ScaleNotLessThanOrEqualToBaseScale:
                    return "Scale '{0}' not less than or equal to base scale '{1}'.".InvFormat(_msgArgs);
                case DiagCodeEx.InvalidPattern:
                    return "Invalid pattern '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.InvalidLiteralForType:
                    return "Invalid literal '{0}' for type '{1}'.".InvFormat(_msgArgs);
                case DiagCodeEx.DuplicateEnumItemName:
                    return "Duplicate enum item name '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.EnumItemNameNotAllowedInRestriction:
                    return "Enum item name not allowed in restriction.";
                case DiagCodeEx.EnumItemNotInBaseEnum:
                    return "Enum item '{0}' not in base enum.".InvFormat(_msgArgs);
                case DiagCodeEx.MinValueNotGreaterThanOrEqualToBaseMinValue:
                    return "Min value '{0}' not greater than or equal to base min value '{1}'.".InvFormat(_msgArgs);
                case DiagCodeEx.MinValueNotGreaterThanBaseMinValue:
                    return "Min value '{0}' not greater than base min value '{1}'.".InvFormat(_msgArgs);
                case DiagCodeEx.MaxValueNotLessThanOrEqualToBaseMaxValue:
                    return "Max value '{0}' not less than or equal to base max value '{1}'.".InvFormat(_msgArgs);
                case DiagCodeEx.MaxValueNotLessThanBaseMaxValue:
                    return "Max value '{0}' not less than base max value '{1}'.".InvFormat(_msgArgs);
                case DiagCodeEx.MaxValueNotGreaterThanOrEqualToMinValue:
                    return "Max value '{0}' not greater than or equal to min value '{1}'.".InvFormat(_msgArgs);
                case DiagCodeEx.MaxValueNotGreaterThanMinValue:
                    return "Max value '{0}' not greater than min value '{1}'.".InvFormat(_msgArgs);

                //type
                case DiagCodeEx.SimpleTypeRequired:
                    return "Simple type required.";
                case DiagCodeEx.ComplexTypeRequired:
                    return "Complex type required.";
                case DiagCodeEx.BaseTypeSealed:
                    return "Base type '{0}' sealed.".InvFormat(_msgArgs);
                case DiagCodeEx.CannotExtendOrRestrictSysComplexType:
                    return "Cannot extend or restrict 'sys:ComplexType'.";
                case DiagCodeEx.CannotRestrictSysSimpleAtomListType:
                    return "Cannot restrict 'sys:SimpleType', 'sys:AtomType' or 'sys:ListType'.";
                case DiagCodeEx.CannotExtendSimpleChildWithComplexChildren:
                    return "Cannot extend simple child with complex children.";
                case DiagCodeEx.CannotExtendChildrenWithSimpleChild:
                    return "Cannot extend children with simple child.";
                case DiagCodeEx.CannotRestrictSimpleChildWithComplexChildren:
                    return "Cannot restrict simple child with complex children.";
                case DiagCodeEx.CannotRestrictComplexChildrenWithSimpleChild:
                    return "Cannot restrict complex children with simple child.";
                case DiagCodeEx.CannotRestrictNullSimpleChild:
                    return "Cannot restrict null simple child.";
                case DiagCodeEx.AttributesChildrenNotAllowedInSimpleTypeRestriction:
                    return "Attributes/children not allowed in simple type restriction.";
                case DiagCodeEx.FacetsNotAllowedInComplexTypeRestriction:
                    return "Facets not allowed in complex type restriction.";
                case DiagCodeEx.TypeNotEqualToOrDeriveFrom:
                    return "Type '{0}' not equal to or derive from '{1}'.".InvFormat(_msgArgs);
                case DiagCodeEx.TypeNotEqualToOrDeriveFromRestricted:
                    return "Type '{0}' not equal to or derive from the restricted '{1}'.".InvFormat(_msgArgs);
                case DiagCodeEx.TypeNotEqualToOrDeriveFromSubstituted:
                    return "Type '{0}' not equal to or derive from the substituted '{1}'.".InvFormat(_msgArgs);

                //
                //attribute
                case DiagCodeEx.DuplicateAttributeName:
                    return "Duplicate attribute name '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.CannotFindRestrictedAttribute:
                    return "Cannot find restricted attribute '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.CannotDeleteRequiredAttribute:
                    return "Cannot delete required attribute '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.CannotChangeRequiredToOptional:
                    return "Cannot change required to optional.";
                //
                case DiagCodeEx.DeletionNotAllowedInExtension:
                    return "Deletion not allowed in extension.";
                case DiagCodeEx.CannotChangeNonNullableToNullable:
                    return "Cannot change non-nullable to nullable.";

                //child
                case DiagCodeEx.SubstitutedElementSealed:
                    return "Substituted element '{0}' sealed.".InvFormat(_msgArgs);
                case DiagCodeEx.DuplicateMemberName:
                    return "Duplicate member name '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.MaxOccurrenceNotEqualToOrGreaterThanMinOccurrence:
                    return "Max occurrence '{0}' not equal to or greater than min occurrence '{1}'.".InvFormat(_msgArgs);
                case DiagCodeEx.MaxOccurrenceCannotBeZero:
                    return "Max occurrence cannot be zero.";
                case DiagCodeEx.MaxOccurrenceCannotGreaterThanOneInChildSet:
                    return "Max occurrence cannot greater than one in child set";
                case DiagCodeEx.ComplexChildrenKindNotEqualToBase:
                    return "Complex children kind '{0}' not equal to the base '{1}'.".InvFormat(_msgArgs);
                case DiagCodeEx.DuplicateElementFullName:
                    return "Duplicate element full name '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.CannotFindRestrictedMember:
                    return "Cannot find restricted member '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.CannotDeleteRequiredMember:
                    return "Cannot delete required member '{0}'.".InvFormat(_msgArgs);
                case DiagCodeEx.MemberKindNotEqualToRestricted:
                    return "Member kind '{0}' not equal to the restricted '{1}'.".InvFormat(_msgArgs);
                case DiagCodeEx.MinOccurrenceNotEqualToOrGreaterThanRestricted:
                    return "Min occurrence '{0}' not equal to or greater than the restricted '{1}'.".InvFormat(_msgArgs);
                case DiagCodeEx.MaxOccurrenceNotEqualToOrLessThanRestricted:
                    return "Max occurrence '{0}' not equal to or less than the restricted '{1}'.".InvFormat(_msgArgs);
                case DiagCodeEx.ElementNameNotEqualToRestricted:
                    return "Element name '{0}' not equal to the restricted '{1}'.".InvFormat(_msgArgs);
                case DiagCodeEx.ElementNotEqualToOrSubstituteForRestricted:
                    return "Element '{0}' not equal to or substitute for the restricted '{1}'.".InvFormat(_msgArgs);
                //case DiagCodeEx.AmbiguousElementFullName:
                //    return "Ambiguous element full name '{0}'.".InvFormat(_msgArgs);


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
