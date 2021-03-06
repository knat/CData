﻿using System;
using System.Linq;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CData.Compiler {
    internal static class EX {
        internal static readonly string[] IgnoreCaseStringNameParts = new string[] { "IgnoreCaseString", "CData" };
        internal static readonly string[] BinaryNameParts = new string[] { "Binary", "CData" };
        internal static readonly string[] IObjectSet2NameParts = new string[] { "IObjectSet`2", "CData" };
        internal static readonly string[] ContractNamespaceAttributeNameParts = new string[] { "ContractNamespaceAttribute", "CData" };
        internal static readonly string[] __CompilerContractNamespaceAttributeNameParts = new string[] { "__CompilerContractNamespaceAttribute", "CData" };
        internal static readonly string[] ContractClassAttributeNameParts = new string[] { "ContractClassAttribute", "CData" };
        internal static readonly string[] ContractPropertyAttributeNameParts = new string[] { "ContractPropertyAttribute", "CData" };

        internal static int MapNamespaces(LogicalNamespaceMap nsMap, IAssemblySymbol assSymbol, bool isRef) {
            var count = 0;
            foreach (AttributeData attData in assSymbol.GetAttributes()) {
                if (attData.AttributeClass.FullNameEquals(isRef ? __CompilerContractNamespaceAttributeNameParts : ContractNamespaceAttributeNameParts)) {
                    var ctorArgs = attData.ConstructorArguments;
                    string uri = null, dottedNameStr = null;
                    var ctorArgsLength = ctorArgs.Length;
                    if (ctorArgsLength >= 2) {
                        uri = ctorArgs[0].Value as string;
                        if (uri != null) {
                            dottedNameStr = ctorArgs[1].Value as string;
                        }
                    }
                    var success = false;
                    if (dottedNameStr != null) {
                        LogicalNamespace logicalNs;
                        if (nsMap.TryGetValue(uri, out logicalNs)) {
                            DottedName dottedName;
                            if (DottedName.TryParse(dottedNameStr, out dottedName)) {
                                if (logicalNs.DottedName == null) {
                                    logicalNs.DottedName = dottedName;
                                    logicalNs.IsRef = isRef;
                                    ++count;
                                    if (isRef) {
                                        string mdData = null;
                                        if (ctorArgsLength >= 3) {
                                            mdData = ctorArgs[2].Value as string;
                                        }
                                        if (mdData != null) {
                                            using (var sr = new StringReader(mdData)) {
                                                var diagCtx = new DiagContext();
                                                MdNamespace mdNs;
                                                if (MdNamespace.TryLoad("__CompilerContractNamespaceAttribute", sr, diagCtx, out mdNs)) {
                                                    if (logicalNs.NamespaceInfo.Set(mdNs)) {
                                                        success = true;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else if (!isRef) {
                                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.DuplicateContractNamespaceAttributeUri, uri),
                                        GetTextSpan(attData));
                                }
                            }
                            else if (!isRef) {
                                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractNamespaceAttributeNamespaceName, dottedNameStr),
                                    GetTextSpan(attData));
                            }
                        }
                        else {
                            if (!isRef) {
                                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractNamespaceAttributeUri, uri),
                                    GetTextSpan(attData));
                            }
                            success = true;
                        }
                    }
                    else if (!isRef) {
                        DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractNamespaceAttribute),
                            GetTextSpan(attData));
                    }
                    if (isRef && !success) {
                        DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.Invalid__CompilerContractNamespaceAttribute,
                            uri, dottedNameStr, assSymbol.Identity.Name), default(TextSpan));
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
                                ClassInfo clsInfo = nsInfo.GetGlobalType<ClassInfo>(clsName);
                                if (clsInfo == null) {
                                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractClassAttributeName, clsName), GetTextSpan(clsAttData));
                                }
                                if (clsInfo.Symbol != null) {
                                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.DuplicateContractClassAttributeName, clsName), GetTextSpan(clsAttData));
                                }
                                if (!clsInfo.IsAbstract) {
                                    if (typeSymbol.IsAbstract) {
                                        DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.NonAbstractContractClassRequired),
                                            GetTextSpan(typeSymbol));
                                    }
                                    if (!typeSymbol.HasParameterlessConstructor()) {
                                        DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.ParameterlessConstructorRequired),
                                            GetTextSpan(typeSymbol));
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
                                ClassInfo clsInfo = nsInfo.GetGlobalType<ClassInfo>(clsName);
                                if (clsInfo != null) {
                                    if (clsInfo.Symbol == null) {
                                        if (typeSymbol.IsStatic) {
                                            DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.ContractClassCannotBeStatic), GetTextSpan(typeSymbol));
                                        }
                                        if (!clsInfo.IsAbstract) {
                                            if (typeSymbol.IsAbstract) {
                                                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.NonAbstractContractClassRequired),
                                                    GetTextSpan(typeSymbol));
                                            }
                                            if (!typeSymbol.HasParameterlessConstructor()) {
                                                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.ParameterlessConstructorRequired),
                                                    GetTextSpan(typeSymbol));
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
        #region
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
        #endregion
        private static bool IsAtomType(TypeKind typeKind, ITypeSymbol typeSymbol) {
            switch (typeKind) {
                case TypeKind.String:
                    return typeSymbol.SpecialType == SpecialType.System_String;
                case TypeKind.IgnoreCaseString:
                    return typeSymbol.FullNameEquals(IgnoreCaseStringNameParts);
                case TypeKind.Char:
                    return typeSymbol.SpecialType == SpecialType.System_Char;
                case TypeKind.Decimal:
                    return typeSymbol.SpecialType == SpecialType.System_Decimal;
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
            if (!isNullable || typeKind.IsClrRefAtom()) {
                return IsAtomType(typeKind, typeSymbol);
            }
            if (typeSymbol.SpecialType == SpecialType.System_Nullable_T) {
                return IsAtomType(typeKind, ((INamedTypeSymbol)typeSymbol).TypeArguments[0]);
            }
            return false;
        }
        internal static ExpressionSyntax AtomValueLiteral(TypeKind typeKind, object value) {
            switch (typeKind) {
                case TypeKind.String:
                    return CS.Literal((string)value);
                case TypeKind.IgnoreCaseString:
                    return Literal((IgnoreCaseString)value);
                case TypeKind.Char:
                    return CS.Literal((char)value);
                case TypeKind.Decimal:
                    return CS.Literal((decimal)value);
                case TypeKind.Int64:
                    return CS.Literal((long)value);
                case TypeKind.Int32:
                    return CS.Literal((int)value);
                case TypeKind.Int16:
                    return CS.Literal((short)value);
                case TypeKind.SByte:
                    return CS.Literal((sbyte)value);
                case TypeKind.UInt64:
                    return CS.Literal((ulong)value);
                case TypeKind.UInt32:
                    return CS.Literal((uint)value);
                case TypeKind.UInt16:
                    return CS.Literal((ushort)value);
                case TypeKind.Byte:
                    return CS.Literal((byte)value);
                case TypeKind.Double:
                    return CS.Literal((double)value);
                case TypeKind.Single:
                    return CS.Literal((float)value);
                case TypeKind.Boolean:
                    return CS.Literal((bool)value);
                case TypeKind.Binary:
                    return Literal((Binary)value);
                case TypeKind.Guid:
                    return CS.Literal((Guid)value); ;
                case TypeKind.TimeSpan:
                    return CS.Literal((TimeSpan)value); ;
                case TypeKind.DateTimeOffset:
                    return CS.Literal((DateTimeOffset)value); ;
                default:
                    throw new ArgumentException("Invalid type kind: " + typeKind.ToString());
            }
        }
        //internal static ExpressionSyntax NameValuePairLiteral(NameValuePair value, TypeKind kind) {
        //    return CS.NewObjExpr(NameValuePairName, CS.Literal(value.Name), AtomValueLiteral(kind, value.Value));
        //}

        internal static string ToIdString(string s) {
            if (s == null || s.Length == 0) return s;
            var sb = StringBuilderBuffer.Acquire();
            foreach (var ch in s) {
                if (SyntaxFacts.IsIdentifierPartCharacter(ch)) {
                    sb.Append(ch);
                }
                else {
                    sb.Append('_');
                }
            }
            return sb.ToStringAndRelease();
        }
        internal static AliasQualifiedNameSyntax CDataName {
            get { return CS.GlobalAliasQualifiedName("CData"); }
        }
        internal static string UserAssemblyMetadataName(string assName) {
            return "AssemblyMetadata_" + ToIdString(assName);
        }
        internal static QualifiedNameSyntax __CompilerContractNamespaceAttributeName {
            get { return CS.QualifiedName(CDataName, "__CompilerContractNamespaceAttribute"); }
        }
        internal static QualifiedNameSyntax AssemblyMetadataName {
            get { return CS.QualifiedName(CDataName, "AssemblyMetadata"); }
        }
        internal static QualifiedNameSyntax GlobalTypeMetadataName {
            get { return CS.QualifiedName(CDataName, "GlobalTypeMetadata"); }
        }
        internal static ArrayTypeSyntax GlobalTypeMetadataArrayType {
            get { return CS.OneDimArrayType(GlobalTypeMetadataName); }
        }
        internal static QualifiedNameSyntax EnumMetadataName {
            get { return CS.QualifiedName(CDataName, "EnumMetadata"); }
        }
        internal static QualifiedNameSyntax NameValuePairName {
            get { return CS.QualifiedName(CDataName, "NameValuePair"); }
        }
        internal static ArrayTypeSyntax NameValuePairArrayType {
            get { return CS.OneDimArrayType(NameValuePairName); }
        }
        internal static QualifiedNameSyntax ClassMetadataName {
            get { return CS.QualifiedName(CDataName, "ClassMetadata"); }
        }
        internal static QualifiedNameSyntax PropertyMetadataName {
            get { return CS.QualifiedName(CDataName, "PropertyMetadata"); }
        }
        internal static ArrayTypeSyntax PropertyMetadataArrayType {
            get { return CS.OneDimArrayType(PropertyMetadataName); }
        }
        internal static QualifiedNameSyntax GlobalTypeRefMetadataName {
            get { return CS.QualifiedName(CDataName, "GlobalTypeRefMetadata"); }
        }
        internal static MemberAccessExpressionSyntax GlobalTypeRefMetadataExpr {
            get { return CS.MemberAccessExpr(CDataName, "GlobalTypeRefMetadata"); }
        }
        internal static QualifiedNameSyntax CollectionMetadataName {
            get { return CS.QualifiedName(CDataName, "CollectionMetadata"); }
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
        internal static QualifiedNameSyntax IgnoreCaseStringName {
            get { return CS.QualifiedName(CDataName, "IgnoreCaseString"); }
        }
        internal static ExpressionSyntax Literal(IgnoreCaseString value) {
            return CS.NewObjExpr(IgnoreCaseStringName, CS.Literal(value.Value), CS.Literal(value.IsReadOnly));
        }
        internal static QualifiedNameSyntax BinaryName {
            get { return CS.QualifiedName(CDataName, "Binary"); }
        }
        internal static ExpressionSyntax Literal(Binary value) {
            return CS.NewObjExpr(BinaryName, CS.Literal(value.ToBytes()), CS.Literal(value.IsReadOnly));
        }
        internal static QualifiedNameSyntax ObjectSetOf(TypeSyntax keyType, TypeSyntax objectType) {
            return SyntaxFactory.QualifiedName(CDataName, CS.GenericName("ObjectSet", keyType, objectType));
        }

        internal static QualifiedNameSyntax DiagContextName {
            get { return CS.QualifiedName(CDataName, "DiagContext"); }
        }
        internal static QualifiedNameSyntax TextSpanName {
            get { return CS.QualifiedName(CDataName, "TextSpan"); }
        }
        internal static MemberAccessExpressionSyntax SerializerExpr {
            get { return CS.MemberAccessExpr(CDataName, "Serializer"); }
        }



    }

}
