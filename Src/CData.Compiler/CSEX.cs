using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CData.Compiler {

    internal static class CSEX {
        internal static TextSpan GetTextSpan(AttributeData attData) {
            if (attData != null) {
                return GetTextSpan(attData.ApplicationSyntaxReference);
            }
            return default(TextSpan);
        }
        internal static TextSpan GetTextSpan(SyntaxReference sr) {
            if (sr != null) {
                return GetTextSpan(sr.GetSyntax().GetLocation());
            }
            return default(TextSpan);
        }
        internal static TextSpan GetTextSpan(ISymbol symbol) {
            if (symbol != null) {
                var locations = symbol.Locations;
                if (locations.Length > 0) {
                    return GetTextSpan(locations[0]);
                }
            }
            return default(TextSpan);
        }
        internal static TextSpan GetTextSpan(Location location) {
            if (location != null && location.IsInSource) {
                var csLineSpan = location.GetLineSpan();
                if (csLineSpan.IsValid) {
                    var csTextSpan = location.SourceSpan;
                    return new TextSpan(csLineSpan.Path, csTextSpan.Start, csTextSpan.Length,
                        ToTextPosition(csLineSpan.StartLinePosition), ToTextPosition(csLineSpan.EndLinePosition));
                }
            }
            return default(TextSpan);
        }
        private static TextPosition ToTextPosition(this LinePosition csPosition) {
            return new TextPosition(csPosition.Line + 1, csPosition.Character + 1);
        }
        //
        //
        internal static readonly string[] IgnoreCaseStringNameParts = new string[] { "IgnoreCaseString", "CData" };
        internal static readonly string[] BinaryNameParts = new string[] { "Binary", "CData" };
        internal static readonly string[] IOjectSet2NameParts = new string[] { "IOjectSet`2", "CData" };
        internal static readonly string[] ContractNamespaceAttributeNameParts = new string[] { "ContractNamespaceAttribute", "CData" };
        internal static readonly string[] ContractClassAttributeNameParts = new string[] { "ContractClassAttribute", "CData" };
        internal static readonly string[] ContractPropertyAttributeNameParts = new string[] { "ContractPropertyAttribute", "CData" };

        internal static int MapNamespaces(LogicalNamespaceMap nsMap, IAssemblySymbol assSymbol, bool isInSource) {
            var count = 0;
            foreach (AttributeData attData in assSymbol.GetAttributes()) {
                if (attData.AttributeClass.FullNameEquals(ContractNamespaceAttributeNameParts)) {
                    var ctorArgs = attData.ConstructorArguments;
                    string uri = null, fullNameStr = null;
                    if (ctorArgs.Length == 2) {
                        uri = ctorArgs[0].Value as string;
                        if (uri != null) {
                            fullNameStr = ctorArgs[1].Value as string;
                        }
                    }
                    if (fullNameStr != null) {
                        LogicalNamespace logicalNs;
                        if (nsMap.TryGetValue(uri, out logicalNs)) {
                            DottedName fullName;
                            if (DottedName.TryParse(fullNameStr, out fullName)) {
                                if (logicalNs.DottedName == null) {
                                    logicalNs.DottedName = fullName;
                                    logicalNs.IsRef = !isInSource;
                                    ++count;
                                }
                                else {// if (isInSource) {
                                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.DuplicateContractNamespaceAttributeUri, uri),
                                        GetTextSpan(attData));
                                }
                            }
                            else {//if (isInSource) {
                                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractNamespaceAttributeNamespaceName, fullNameStr),
                                    GetTextSpan(attData));
                            }
                        }
                        else if (isInSource) {
                            DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractNamespaceAttributeUri, uri),
                                GetTextSpan(attData));
                        }
                    }
                    else if (isInSource) {
                        DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractNamespaceAttribute),
                            GetTextSpan(attData));
                    }
                }
            }
            return count;
        }
        internal static string GetFirstArgumentAsString(AttributeData attData) {
            var ctorArgs = attData.ConstructorArguments;
            if (ctorArgs.Length > 0) {
                return ctorArgs[0].Value as string;
            }
            return null;
        }
        internal static void MapClasses(LogicalNamespaceMap nsMap, INamespaceSymbol nsSymbol) {
            if (!nsSymbol.IsGlobalNamespace) {
                foreach (var logicalNs in nsMap.Values) {
                    var nsInfo = logicalNs.NamespaceInfo;
                    if (nsSymbol.FullNameEquals(nsInfo.DottedName.NameParts)) {
                        var typeSymbolList = nsSymbol.GetMembers().OfType<INamedTypeSymbol>().Where(i => i.TypeKind == Microsoft.CodeAnalysis.TypeKind.Class).ToList();
                        for (var i = 0; i < typeSymbolList.Count; ) {
                            var typeSymbol = typeSymbolList[i];
                            var clsAttData = typeSymbol.GetAttributeData(ContractClassAttributeNameParts);
                            if (clsAttData != null) {
                                if (typeSymbol.IsGenericType) {
                                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.ContractClassCannotBeGeneric), GetTextSpan(typeSymbol));
                                }
                                if (typeSymbol.IsStatic) {
                                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.ContractClassCannotBeStatic), GetTextSpan(typeSymbol));
                                }
                                var clsName = GetFirstArgumentAsString(clsAttData);
                                if (clsName == null) {
                                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractClassAttribute), GetTextSpan(clsAttData));
                                }
                                ClassInfo clsInfo = nsInfo.GetEntity<ClassInfo>(clsName);
                                if (clsInfo == null) {
                                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractClassAttributeName, clsName), GetTextSpan(clsAttData));
                                }
                                if (clsInfo.Symbol != null) {
                                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.DuplicateContractClassAttributeName, clsName), GetTextSpan(clsAttData));
                                }
                                if (!clsInfo.IsAbstract) {
                                    if (typeSymbol.IsAbstract || !typeSymbol.HasParameterlessConstructor()) {
                                        DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.NonAbstractParameterlessConstructorContractClassRequired), GetTextSpan(typeSymbol));
                                    }
                                }
                                clsInfo.Symbol = typeSymbol;
                                typeSymbolList.RemoveAt(i);
                                continue;
                            }
                            ++i;
                        }

                        foreach (var typeSymbol in typeSymbolList) {
                            if (!typeSymbol.IsGenericType) {
                                var clsName = typeSymbol.Name;
                                ClassInfo clsInfo = nsInfo.GetEntity<ClassInfo>(clsName);
                                if (clsInfo != null) {
                                    if (clsInfo.Symbol == null) {
                                        if (typeSymbol.IsStatic) {
                                            DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.ContractClassCannotBeStatic), GetTextSpan(typeSymbol));
                                        }
                                        if (!clsInfo.IsAbstract) {
                                            if (typeSymbol.IsAbstract || !typeSymbol.HasParameterlessConstructor()) {
                                                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.NonAbstractParameterlessConstructorContractClassRequired), GetTextSpan(typeSymbol));
                                            }
                                        }
                                        clsInfo.Symbol = typeSymbol;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            foreach (var subNsSymbol in nsSymbol.GetNamespaceMembers()) {
                MapClasses(nsMap, subNsSymbol);
            }
        }


        private static bool IsAtomType(TypeKind typeKind, ITypeSymbol typeSymbol) {
            switch (typeKind) {
                case TypeKind.String:
                    return typeSymbol.SpecialType == SpecialType.System_String;
                case TypeKind.IgnoreCaseString:
                    return typeSymbol.FullNameEquals(IgnoreCaseStringNameParts);
                case TypeKind.Int64:
                    return typeSymbol.SpecialType == SpecialType.System_Int64;
                case TypeKind.Int32:
                    return typeSymbol.SpecialType == SpecialType.System_Int32;
                case TypeKind.Int16:
                    return typeSymbol.SpecialType == SpecialType.System_Int16;
                case TypeKind.SByte:
                    return typeSymbol.SpecialType == SpecialType.System_SByte;
                case TypeKind.UInt64:
                    return typeSymbol.SpecialType == SpecialType.System_UInt64;
                case TypeKind.UInt32:
                    return typeSymbol.SpecialType == SpecialType.System_UInt32;
                case TypeKind.UInt16:
                    return typeSymbol.SpecialType == SpecialType.System_UInt16;
                case TypeKind.Byte:
                    return typeSymbol.SpecialType == SpecialType.System_Byte;
                case TypeKind.Double:
                    return typeSymbol.SpecialType == SpecialType.System_Double;
                case TypeKind.Single:
                    return typeSymbol.SpecialType == SpecialType.System_Single;
                case TypeKind.Boolean:
                    return typeSymbol.SpecialType == SpecialType.System_Boolean;
                case TypeKind.Binary:
                    return typeSymbol.FullNameEquals(BinaryNameParts);
                case TypeKind.Guid:
                    return typeSymbol.FullNameEquals(CS.GuidNameParts);
                case TypeKind.TimeSpan:
                    return typeSymbol.FullNameEquals(CS.TimeSpanNameParts);
                case TypeKind.DateTimeOffset:
                    return typeSymbol.FullNameEquals(CS.DateTimeOffsetNameParts);
                default:
                    throw new ArgumentException("Invalid type kind: " + typeKind.ToString());
            }
        }
        internal static bool IsAtomType(TypeKind typeKind, bool isNullable, ITypeSymbol typeSymbol) {
            if (!isNullable || typeKind == TypeKind.String || typeKind == TypeKind.IgnoreCaseString || typeKind == TypeKind.Binary) {
                return IsAtomType(typeKind, typeSymbol);
            }
            if (typeSymbol.SpecialType == SpecialType.System_Nullable_T) {
                return IsAtomType(typeKind, ((INamedTypeSymbol)typeSymbol).TypeArguments[0]);
            }
            return false;
        }
        //
        //
        //
        internal static AliasQualifiedNameSyntax CDataName {
            get { return CS.GlobalAliasQualifiedName("CData"); }
        }
        internal static QualifiedNameSyntax IgnoreCaseStringName {
            get { return CS.QualifiedName(CDataName, "IgnoreCaseString"); }
        }
        internal static QualifiedNameSyntax BinaryName {
            get { return CS.QualifiedName(CDataName, "Binary"); }
        }
        internal static QualifiedNameSyntax ObjectSetOf(TypeSyntax keyType, TypeSyntax objectType) {
            return SyntaxFactory.QualifiedName(CDataName, CS.GenericName("ObjectSet", keyType, objectType));
        }
        internal static QualifiedNameSyntax ClassMetadataName {
            get { return CS.QualifiedName(CDataName, "ClassMetadata"); }
        }
        internal static QualifiedNameSyntax ClassRefTypeMetadataName {
            get { return CS.QualifiedName(CDataName, "ClassRefTypeMetadata"); }
        }
        internal static QualifiedNameSyntax PropertyMetadataName {
            get { return CS.QualifiedName(CDataName, "PropertyMetadata"); }
        }
        internal static ArrayTypeSyntax PropertyMetadataArrayType {
            get { return CS.OneDimArrayType(PropertyMetadataName); }
        }
        internal static QualifiedNameSyntax CollectionTypeMetadataName {
            get { return CS.QualifiedName(CDataName, "CollectionTypeMetadata"); }
        }
        internal static QualifiedNameSyntax AtomRefTypeMetadataName {
            get { return CS.QualifiedName(CDataName, "AtomRefTypeMetadata"); }
        }
        internal static MemberAccessExpressionSyntax AtomRefTypeMetadataExpr {
            get { return CS.MemberAccessExpr(CDataName, "AtomRefTypeMetadata"); }
        }
        internal static ExpressionSyntax Literal(TypeKind value) {
            return CS.MemberAccessExpr(CS.MemberAccessExpr(CDataName, "TypeKind"), value.ToString());
        }
        internal static QualifiedNameSyntax FullNameName {
            get { return CS.QualifiedName(CDataName, "FullName"); }
        }
        internal static ExpressionSyntax Literal(FullName value) {
            return CS.NewObjExpr(FullNameName, CS.Literal(value.Uri), CS.Literal(value.Name));
        }
        internal const string ThisMetadataNameStr = "__ThisMetadata";
        internal const string MetadataNameStr = "__Metadata";
        internal const string TextSpanNameStr = "__TextSpan";
        internal static QualifiedNameSyntax DiagContextName {
            get { return CS.QualifiedName(CDataName, "DiagContext"); }
        }
        internal static QualifiedNameSyntax TextSpanName {
            get { return CS.QualifiedName(CDataName, "TextSpan"); }
        }
        internal static MemberAccessExpressionSyntax SerializerExpr {
            get { return CS.MemberAccessExpr(CDataName, "Serializer"); }
        }
        internal static QualifiedNameSyntax ContractTypesAttributeName {
            get { return CS.QualifiedName(CDataName, "ContractTypesAttribute"); }
        }




    }

}
