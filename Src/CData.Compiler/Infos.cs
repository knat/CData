﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CData.Compiler {
    internal sealed class NamespaceInfo {
        public NamespaceInfo(string uri) {
            Uri = uri;
            GlobalTypeList = new List<GlobalTypeInfo>();
        }
        public readonly string Uri;
        public readonly List<GlobalTypeInfo> GlobalTypeList;
        public DottedName DottedName;
        public bool IsRef;
        public T GetGlobalType<T>(string name) where T : GlobalTypeInfo {
            foreach (var globalType in GlobalTypeList) {
                if (globalType.Name == name) {
                    return globalType as T;
                }
            }
            return null;
        }
        public void SetGlobalTypeDottedNames() {
            var nsDottedName = DottedName;
            foreach (var globalType in GlobalTypeList) {
                string name;
                if (globalType.Symbol != null) {
                    name = globalType.Symbol.Name;
                }
                else {
                    name = globalType.Name;
                }
                globalType.DottedName = new DottedName(nsDottedName, name);
            }
        }
        public void MapAndCheckClassProperties() {
            foreach (var globalType in GlobalTypeList) {
                var cls = globalType as ClassInfo;
                if (cls != null) {
                    cls.MapAndCheckProperties(IsRef);
                }
            }
        }
        public void GetSyntax(List<MemberDeclarationSyntax> list, ExpressionSyntax assMdExpr, List<ExpressionSyntax> globalTypeMdList) {
            if (!IsRef) {
                var memberList = new List<MemberDeclarationSyntax>();
                foreach (var globalType in GlobalTypeList) {
                    globalType.GetSyntax(memberList, assMdExpr, globalTypeMdList);
                }
                list.Add(SyntaxFactory.NamespaceDeclaration(DottedName.FullNameSyntax,
                    default(SyntaxList<ExternAliasDirectiveSyntax>), default(SyntaxList<UsingDirectiveSyntax>),
                    SyntaxFactory.List(memberList)));
            }
        }

    }

    internal abstract class TypeInfo {
        protected TypeInfo(TypeKind kind) {
            Kind = kind;
        }
        public readonly TypeKind Kind;
        public bool IsAtom {
            get { return Kind.IsAtom(); }
        }
        public bool IsEnum {
            get { return Kind == TypeKind.Enum; }
        }
        public bool IsClass {
            get { return Kind == TypeKind.Class; }
        }

    }

    internal abstract class GlobalTypeInfo : TypeInfo {
        protected GlobalTypeInfo(TypeKind kind, NamespaceInfo ns, string name)
            : base(kind) {
            Namespace = ns;
            Name = name;
        }
        public readonly NamespaceInfo Namespace;
        public readonly string Name;
        public FullName FullName {
            get {
                return new FullName(Namespace.Uri, Name);
            }
        }
        public DottedName DottedName;
        public NameSyntax FullNameSyntax {
            get {
                return DottedName.FullNameSyntax;
            }
        }
        public ExpressionSyntax FullExprSyntax {
            get {
                return DottedName.FullExprSyntax;
            }
        }
        public INamedTypeSymbol Symbol;//opt
        public GlobalTypeRefInfo CreateGlobalTypeRef() {
            return new GlobalTypeRefInfo(this);
        }
        public abstract void GetSyntax(List<MemberDeclarationSyntax> list, ExpressionSyntax assMdExpr, List<ExpressionSyntax> globalTypeMdList);
    }
    internal abstract class SimpleGlobalTypeInfo : GlobalTypeInfo {
        protected SimpleGlobalTypeInfo(TypeKind kind, NamespaceInfo ns, string name)
            : base(kind, ns, name) {
        }
    }
    internal sealed class AtomInfo : SimpleGlobalTypeInfo {
        public static AtomInfo Get(TypeKind kind) {
            return _map[kind];
        }
        private static readonly Dictionary<TypeKind, AtomInfo> _map = new Dictionary<TypeKind, AtomInfo> {
            { TypeKind.String, new AtomInfo(TypeKind.String, CS.StringType, CS.StringType) },
            { TypeKind.IgnoreCaseString, new AtomInfo(TypeKind.IgnoreCaseString, CSEX.IgnoreCaseStringName, CSEX.IgnoreCaseStringName) },
            { TypeKind.Decimal, new AtomInfo(TypeKind.Decimal, CS.DecimalType, CS.DecimalNullableType) },
            { TypeKind.Int64, new AtomInfo(TypeKind.Int64, CS.LongType, CS.ULongNullableType) },
            { TypeKind.Int32, new AtomInfo(TypeKind.Int32, CS.IntType, CS.IntNullableType) },
            { TypeKind.Int16, new AtomInfo(TypeKind.Int16, CS.ShortType, CS.ShortNullableType) },
            { TypeKind.SByte, new AtomInfo(TypeKind.SByte, CS.SByteType, CS.SByteNullableType) },
            { TypeKind.UInt64, new AtomInfo(TypeKind.UInt64, CS.ULongType, CS.ULongNullableType) },
            { TypeKind.UInt32, new AtomInfo(TypeKind.UInt32, CS.UIntType, CS.UIntNullableType) },
            { TypeKind.UInt16, new AtomInfo(TypeKind.UInt16, CS.UShortType, CS.UShortNullableType) },
            { TypeKind.Byte, new AtomInfo(TypeKind.Byte, CS.ByteType, CS.ByteNullableType) },
            { TypeKind.Double, new AtomInfo(TypeKind.Double, CS.DoubleType, CS.DoubleNullableType) },
            { TypeKind.Single, new AtomInfo(TypeKind.Single, CS.FloatType, CS.FloatNullableType) },
            { TypeKind.Boolean, new AtomInfo(TypeKind.Boolean, CS.BoolType, CS.BoolNullableType) },
            { TypeKind.Binary, new AtomInfo(TypeKind.Binary, CSEX.BinaryName, CSEX.BinaryName) },
            { TypeKind.Guid, new AtomInfo(TypeKind.Guid, CS.GuidName, CS.GuidNullableType) },
            { TypeKind.TimeSpan, new AtomInfo(TypeKind.TimeSpan, CS.TimeSpanName, CS.TimeSpanNullableType) },
            { TypeKind.DateTimeOffset, new AtomInfo(TypeKind.DateTimeOffset, CS.DateTimeOffsetName, CS.DateTimeOffsetNullableType) },
        };
        private AtomInfo(TypeKind kind, TypeSyntax typeSyntax, TypeSyntax nullableTypeSyntax)
            : base(kind, null, null) {
            TypeSyntax = typeSyntax;
            NullableTypeSyntax = nullableTypeSyntax;
            TypeDisplayName = typeSyntax.ToString();
            NullableTypeDisplayName = nullableTypeSyntax.ToString();
        }
        public readonly TypeSyntax TypeSyntax;
        public readonly TypeSyntax NullableTypeSyntax;
        public readonly string TypeDisplayName;
        public readonly string NullableTypeDisplayName;
        public override void GetSyntax(List<MemberDeclarationSyntax> list, ExpressionSyntax assMdExpr, List<ExpressionSyntax> globalTypeMdList) {
            throw new NotImplementedException();
        }
    }
    internal sealed class EnumInfo : SimpleGlobalTypeInfo {
        public EnumInfo(NamespaceInfo ns, string name, AtomInfo atom, List<NameValuePair> memberList)
            : base(TypeKind.Enum, ns, name) {
            Atom = atom;
            MemberList = memberList;
        }
        public readonly AtomInfo Atom;
        public readonly List<NameValuePair> MemberList;//opt
        public override void GetSyntax(List<MemberDeclarationSyntax> list, ExpressionSyntax assMdExpr, List<ExpressionSyntax> globalTypeMdList) {
            var id = CS.UnescapedId(DottedName.LastName);
            var atom = Atom;
            var atomKind = atom.Kind;
            var isClrEnum = atomKind.IsClrEnum();
            if (isClrEnum) {
                var memberSyntaxList = new List<EnumMemberDeclarationSyntax>();
                if (MemberList != null) {
                    foreach (var member in MemberList) {
                        memberSyntaxList.Add(SyntaxFactory.EnumMemberDeclaration(default(SyntaxList<AttributeListSyntax>),
                            CS.UnescapedId(member.Name), SyntaxFactory.EqualsValueClause(CSEX.Literal(atomKind, member.Value))));
                    }
                }
                list.Add(SyntaxFactory.EnumDeclaration(default(SyntaxList<AttributeListSyntax>), CS.PublicTokenList, id,
                    CS.BaseList(atom.TypeSyntax), SyntaxFactory.SeparatedList(memberSyntaxList)));
            }
            else {
                var memberSyntaxList = new List<MemberDeclarationSyntax>();
                var canBeConst = atomKind == TypeKind.String || atomKind == TypeKind.Decimal || atomKind == TypeKind.Double || atomKind == TypeKind.Single
                    || atomKind == TypeKind.Boolean;
                if (MemberList != null) {
                    var atomTypeSyntax = atom.TypeSyntax;
                    foreach (var member in MemberList) {
                        memberSyntaxList.Add(CS.Field(canBeConst ? CS.PublicConstTokenList : CS.PublicStaticReadOnlyTokenList, atomTypeSyntax,
                           CS.UnescapedId(member.Name), CSEX.Literal(atomKind, member.Value)));
                    }
                }
                list.Add(CS.Class(null, CS.PublicStaticTokenList, id, null, memberSyntaxList));
            }
            //>new EnumMetadata(FullName fullName, Type clrType, bool isClrEnum)

        }
    }
    internal sealed class ClassInfo : GlobalTypeInfo {
        public ClassInfo(NamespaceInfo ns, string name, bool isAbstract, bool isSealed, ClassInfo baseClass,
            List<PropertyInfo> propertyList)
            : base(TypeKind.Class, ns, name) {
            IsAbstract = isAbstract;
            IsSealed = isSealed;
            BaseClass = baseClass;
            PropertyList = propertyList;
        }
        public readonly bool IsAbstract;
        public readonly bool IsSealed;
        public readonly ClassInfo BaseClass;//opt
        public readonly List<PropertyInfo> PropertyList;//opt
        public PropertyInfo GetProperty(string name) {
            if (PropertyList != null) {
                foreach (var prop in PropertyList) {
                    if (prop.Name == name) {
                        return prop;
                    }
                }
            }
            return null;
        }
        public PropertyInfo GetPropertyInHierarchy(string name) {
            if (PropertyList != null) {
                foreach (var prop in PropertyList) {
                    if (prop.Name == name) {
                        return prop;
                    }
                }
            }
            if (BaseClass != null) {
                return BaseClass.GetPropertyInHierarchy(name);
            }
            return null;
        }
        public void MapAndCheckProperties(bool isRef) {
            var typeSymbol = Symbol;
            if (typeSymbol != null) {
                var memberSymbolList = typeSymbol.GetMembers().Where(i => {
                    var kind = i.Kind;
                    return kind == SymbolKind.Property || kind == SymbolKind.Field;
                }).ToList();
                for (var i = 0; i < memberSymbolList.Count; ) {
                    var memberSymbol = memberSymbolList[i];
                    var propAttData = memberSymbol.GetAttributeData(CSEX.ContractPropertyAttributeNameParts);
                    if (propAttData != null) {
                        var propName = CSEX.GetFirstArgumentAsString(propAttData);
                        if (propName == null) {
                            DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractPropertyAttribute),
                                CSEX.GetTextSpan(propAttData));
                        }
                        var propInfo = GetProperty(propName);
                        if (propInfo == null) {
                            DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractPropertyAttributeName, propName),
                                CSEX.GetTextSpan(propAttData));
                        }
                        if (propInfo.Symbol != null) {
                            DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.DuplicateContractPropertyAttributeName, propName),
                                CSEX.GetTextSpan(propAttData));
                        }
                        propInfo.Symbol = memberSymbol;
                        memberSymbolList.RemoveAt(i);
                        continue;
                    }
                    ++i;
                }
                foreach (var memberSymbol in memberSymbolList) {
                    var propName = memberSymbol.Name;
                    var propInfo = GetProperty(propName);
                    if (propInfo != null) {
                        if (propInfo.Symbol == null) {
                            propInfo.Symbol = memberSymbol;
                        }
                    }
                }

                if (PropertyList != null) {
                    foreach (var propInfo in PropertyList) {
                        var memberSymbol = propInfo.Symbol;
                        if (memberSymbol != null) {
                            if (memberSymbol.IsStatic) {
                                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.ContractPropertyCannotBeStatic),
                                    CSEX.GetTextSpan(memberSymbol));
                            }
                            var propSymbol = propInfo.PropertySymbol;
                            var fieldSymbol = propInfo.FieldSymbol;
                            if (propSymbol != null) {
                                if (propSymbol.IsReadOnly || propSymbol.IsWriteOnly) {
                                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.ContractPropertyMustHaveGetterAndSetter),
                                        CSEX.GetTextSpan(memberSymbol));
                                }
                                if (propSymbol.IsIndexer) {
                                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.ContractPropertyCannotBeIndexer),
                                        CSEX.GetTextSpan(memberSymbol));
                                }
                            }
                            else {
                                if (fieldSymbol.IsConst) {
                                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.ContractFieldCannotBeConst),
                                        CSEX.GetTextSpan(memberSymbol));
                                }
                            }
                            propInfo.CSName = memberSymbol.Name;
                            propInfo.IsCSProperty = propSymbol != null;
                            if (!isRef) {
                                propInfo.CheckSymbol();
                            }
                        }
                        else {
                            propInfo.CSName = propInfo.Name;
                            propInfo.IsCSProperty = true;
                        }
                    }
                }

            }
        }
        public void GetSyntax(List<MemberDeclarationSyntax> list, List<TypeOfExpressionSyntax> typeList) {
            var memberList = new List<MemberDeclarationSyntax>();
            if (PropertyList != null) {
                foreach (var propInfo in PropertyList) {
                    propInfo.GetSyntax(memberList);
                }
            }
            var baseClass = BaseClass;
            if (baseClass == null) {
                //>public TextSpan __TextSpan {get; private set;}
                memberList.Add(CS.Property(null, CS.PublicTokenList, CSEX.TextSpanName, CS.Id(CSEX.TextSpanNameStr), CS.GetPrivateSetAccessorList));
            }
            //>public static bool TryLoad(string filePath, System.IO.TextReader reader, DiagContext context, out XXX result) {
            //  return CData.Serializer.TryLoad<XXX>(filePath, reader, context, __ThisMetadata, out result);
            //}
            memberList.Add(CS.Method(CS.PublicStaticTokenList, CS.BoolType, "TryLoad", new[] {
                    CS.Parameter(CS.StringType, "filePath"),
                    CS.Parameter(CS.TextReaderName, "reader"),
                    CS.Parameter(CSEX.DiagContextName, "context"),
                    CS.OutParameter(FullNameSyntax, "result")
                },
                CS.ReturnStm(CS.InvoExpr(CS.MemberAccessExpr(CSEX.SerializerExpr, CS.GenericName("TryLoad", FullNameSyntax)),
                    SyntaxFactory.Argument(CS.IdName("filePath")), SyntaxFactory.Argument(CS.IdName("reader")), SyntaxFactory.Argument(CS.IdName("context")),
                    SyntaxFactory.Argument(CS.IdName(CSEX.ThisMetadataNameStr)), CS.OutArgument("result")))));
            if (baseClass == null) {
                //>public void Save(TextWriter writer, string indentString = "\t", string newLineString = "\n") {
                //>  CData.Serializer.Save(this, __Metadata, writer, indentString, newLineString);
                //>}
                memberList.Add(CS.Method(CS.PublicTokenList, CS.VoidType, "Save", new[] { 
                        CS.Parameter(CS.TextWriterName, "writer"),
                        CS.Parameter(CS.StringType, "indentString", CS.Literal("\t")),
                        CS.Parameter(CS.StringType, "newLineString", CS.Literal("\n"))
                    },
                    CS.ExprStm(CS.InvoExpr(CS.MemberAccessExpr(CSEX.SerializerExpr, "Save"), SyntaxFactory.ThisExpression(),
                        CS.IdName(CSEX.MetadataNameStr), CS.IdName("writer"), CS.IdName("indentString"), CS.IdName("newLineString")
                    ))));
                //>public void Save(StringBuilder stringBuilder, string indentString = "\t", string newLineString = "\n") {
                //>  CData.Serializer.Save(this, __Metadata, stringBuilder, indentString, newLineString);
                //>}
                memberList.Add(CS.Method(CS.PublicTokenList, CS.VoidType, "Save", new[] { 
                        CS.Parameter(CS.StringBuilderName, "stringBuilder"),
                        CS.Parameter(CS.StringType, "indentString", CS.Literal("\t")),
                        CS.Parameter(CS.StringType, "newLineString", CS.Literal("\n"))
                    },
                    CS.ExprStm(CS.InvoExpr(CS.MemberAccessExpr(CSEX.SerializerExpr, "Save"), SyntaxFactory.ThisExpression(),
                        CS.IdName(CSEX.MetadataNameStr), CS.IdName("stringBuilder"), CS.IdName("indentString"), CS.IdName("newLineString")
                    ))));
            }

            //>new public static readonly ClassMetadata __ThisMetadata =
            //>  new ClassMetadata(FullName fullName, bool isAbstract, ClassMetadata baseClass, PropertyMetadata[] properties, Type clrType);
            memberList.Add(CS.Field(baseClass == null ? CS.PublicStaticReadOnlyTokenList : CS.NewPublicStaticReadOnlyTokenList,
                CSEX.ClassMetadataName, CSEX.ThisMetadataNameStr,
                CS.NewObjExpr(CSEX.ClassMetadataName, CSEX.Literal(FullName),
                    CS.Literal(IsAbstract),
                    baseClass == null ? (ExpressionSyntax)CS.NullLiteral : CS.MemberAccessExpr(baseClass.FullExprSyntax, CSEX.ThisMetadataNameStr),
                    CS.NewArrOrNullExpr(CSEX.PropertyMetadataArrayType, PropertyList == null ? null : PropertyList.Select(i => i.GetMetadataSyntax())),
                    CS.TypeOfExpr(FullNameSyntax)
                )));
            //>public virtual/override ClassMetadata __Metadata {
            //>    get { return __ThisMetadata; }
            //>}
            memberList.Add(CS.Property(baseClass == null ? CS.PublicVirtualTokenList : CS.PublicOverrideTokenList,
                CSEX.ClassMetadataName, CSEX.MetadataNameStr, true, default(SyntaxTokenList),
                new StatementSyntax[] { 
                    CS.ReturnStm(CS.IdName(CSEX.ThisMetadataNameStr))
                }));

            list.Add(SyntaxFactory.ClassDeclaration(
                attributeLists: default(SyntaxList<AttributeListSyntax>),
                modifiers: IsAbstract ? CS.PublicAbstractPartialTokenList : CS.PublicPartialTokenList,
                identifier: CS.Id(CS.EscapeId(DottedName.LastName)),
                typeParameterList: null,
                baseList: baseClass == null ? null : CS.BaseList(baseClass.FullNameSyntax),
                constraintClauses: default(SyntaxList<TypeParameterConstraintClauseSyntax>),
                members: SyntaxFactory.List(memberList)
                ));
            typeList.Add(CS.TypeOfExpr(FullNameSyntax));
        }
        public override void GetSyntax(List<MemberDeclarationSyntax> list, ExpressionSyntax assMdExpr, List<ExpressionSyntax> globalTypeMdList) {
            throw new NotImplementedException();
        }
    }
    internal sealed class PropertyInfo {
        public PropertyInfo(string name, LocalTypeInfo type) {
            Name = name;
            Type = type;
        }
        public readonly string Name;
        public readonly LocalTypeInfo Type;
        public string CSName;
        public bool IsCSProperty;
        public ISymbol Symbol;//opt
        public IPropertySymbol PropertySymbol {
            get {
                return Symbol as IPropertySymbol;
            }
        }
        public IFieldSymbol FieldSymbol {
            get {
                return Symbol as IFieldSymbol;
            }
        }
        public void CheckSymbol() {
            Type.CheckSymbol(Symbol, PropertySymbol != null ? PropertySymbol.Type : FieldSymbol.Type, ".");
        }
        public void GetSyntax(List<MemberDeclarationSyntax> list) {
            if (Symbol == null) {
                list.Add(CS.Property(null, CS.PublicTokenList, Type.GetTypeSyntax(), CS.Id(CS.EscapeId(CSName)), CS.GetPrivateSetAccessorList));
            }
        }
        public ExpressionSyntax GetMetadataSyntax() {
            //>new PropertyMetadata(string name, TypeMetadata type, string clrName, bool isClrProperty)
            return CS.NewObjExpr(CSEX.PropertyMetadataName, CS.Literal(Name), Type.GetMetadataSyntax(),
                CS.Literal(CSName), CS.Literal(IsCSProperty));
        }
    }

    internal abstract class LocalTypeInfo : TypeInfo {
        protected LocalTypeInfo(TypeKind kind)
            : base(kind) {
        }
        public bool IsNullable;
        public abstract void CheckSymbol(ISymbol propSymbol, ITypeSymbol typeSymbol, string parentTypeName);
        public abstract TypeSyntax GetTypeSyntax();
        public abstract ExpressionSyntax GetMetadataSyntax();
    }
    //internal sealed class AtomRefTypeInfo : LocalTypeInfo {
    //    public AtomRefTypeInfo(AtomInfo atom)
    //        : base(atom.Kind) {
    //        Atom = atom;
    //    }
    //    public readonly AtomInfo Atom;
    //    public override void CheckType(ISymbol propSymbol, ITypeSymbol typeSymbol, string parentTypeName) {
    //        if (!CSEX.IsAtomType(Kind, IsNullable, typeSymbol)) {
    //            DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractPropertyType,
    //                parentTypeName, IsNullable ? Atom.NullableTypeDisplayName : Atom.TypeDisplayName),
    //                CSEX.GetTextSpan(propSymbol));
    //        }
    //    }
    //    public override TypeSyntax GetTypeSyntax() {
    //        return IsNullable ? Atom.NullableTypeSyntax : Atom.TypeSyntax;
    //    }
    //    public override ExpressionSyntax GetMetadataSyntax() {
    //        //>AtomRefTypeMetadata.Get(kind, isNullable)
    //        return CS.InvoExpr(CS.MemberAccessExpr(CSEX.AtomRefTypeMetadataExpr, "Get"), CSEX.Literal(Kind), CS.Literal(IsNullable));
    //    }
    //}
    internal sealed class GlobalTypeRefInfo : LocalTypeInfo {
        public GlobalTypeRefInfo(GlobalTypeInfo globalType)
            : base(globalType.Kind) {
            GlobalType = globalType;
        }
        public readonly GlobalTypeInfo GlobalType;
        public ClassInfo Class {
            get { return GlobalType as ClassInfo; }
        }
        public EnumInfo Enum {
            get { return GlobalType as EnumInfo; }
        }
        public AtomInfo Atom {
            get { return GlobalType as AtomInfo; }
        }
        public AtomInfo EffectiveAtom {
            get {
                var atom = Atom;
                if (atom == null && Kind == TypeKind.Enum) {
                    var enumAtom = Enum.Atom;
                    if (!enumAtom.Kind.IsClrEnum()) {
                        atom = enumAtom;
                    }
                }
                return atom;
            }
        }
        public override void CheckSymbol(ISymbol propSymbol, ITypeSymbol typeSymbol, string parentTypeName) {
            var effAtom = EffectiveAtom;
            var isNullable = IsNullable;
            if (effAtom != null) {
                if (!CSEX.IsAtomType(effAtom.Kind, isNullable, typeSymbol)) {
                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractPropertyType,
                        parentTypeName, isNullable ? effAtom.NullableTypeDisplayName : effAtom.TypeDisplayName),
                        CSEX.GetTextSpan(propSymbol));
                }
            }
            else {
                if (IsClass) {
                    isNullable = false;
                }
                if (!typeSymbol.FullNameEquals(GlobalType.DottedName.NameParts, isNullable)) {
                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractPropertyType,
                        parentTypeName, GetTypeSyntax().ToString()), CSEX.GetTextSpan(propSymbol));
                }
            }
        }
        public override TypeSyntax GetTypeSyntax() {
            return GlobalType.FullNameSyntax;
        }
        public override ExpressionSyntax GetMetadataSyntax() {
            //>new ClassRefTypeMetadata(class.__ThisMetadata, isNullable)
            return CS.NewObjExpr(CSEX.ClassRefTypeMetadataName,
                CS.MemberAccessExpr(GlobalType.FullExprSyntax, CSEX.ThisMetadataNameStr),
                CS.Literal(IsNullable));
        }
    }
    internal sealed class ObjectSetKeySelector : List<PropertyInfo> {
        public GlobalTypeRefInfo KeyType {
            get { return this[Count - 1].Type as GlobalTypeRefInfo; }
        }
    }
    internal sealed class CollectionInfo : LocalTypeInfo {
        public CollectionInfo(TypeKind kind, LocalTypeInfo itemOrValueType, GlobalTypeRefInfo mapKeyType,
            ObjectSetKeySelector objectSetKeySelector)
            : base(kind) {
            ItemOrValueType = itemOrValueType;
            MapKeyType = mapKeyType;
            ObjectSetKeySelector = objectSetKeySelector;
        }
        public readonly LocalTypeInfo ItemOrValueType;
        public readonly GlobalTypeRefInfo MapKeyType;//opt, for map
        public readonly ObjectSetKeySelector ObjectSetKeySelector;//opt
        public INamedTypeSymbol CollectionSymbol;//opt
        public override void CheckSymbol(ISymbol propSymbol, ITypeSymbol typeSymbol, string parentTypeName) {
            INamedTypeSymbol collSymbol;
            var kind = Kind;
            if (kind == TypeKind.List) {
                collSymbol = typeSymbol.GetSelfOrInterface(CS.ICollection1NameParts);
                if (collSymbol == null) {
                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractPropertyType,
                        parentTypeName, "System.Collections.Generic.ICollection<T> or implementing type"),
                        CSEX.GetTextSpan(propSymbol));
                }
                ItemOrValueType.CheckSymbol(propSymbol, collSymbol.TypeArguments[0], parentTypeName + @"\list item");
            }
            else if (kind == TypeKind.Map) {
                collSymbol = typeSymbol.GetSelfOrInterface(CS.IDictionary2TNameParts);
                if (collSymbol == null) {
                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractPropertyType,
                        parentTypeName, "System.Collections.Generic.IDictionary<TKey, TValue> or implementing type"),
                        CSEX.GetTextSpan(propSymbol));
                }
                var typeArgs = collSymbol.TypeArguments;
                MapKeyType.CheckSymbol(propSymbol, typeArgs[0], parentTypeName + @"\map key");
                ItemOrValueType.CheckSymbol(propSymbol, typeArgs[1], parentTypeName + @"\map value");
            }
            else if (kind == TypeKind.ObjectSet) {
                collSymbol = typeSymbol.GetSelfOrInterface(CSEX.IOjectSet2NameParts);
                if (collSymbol == null) {
                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractPropertyType,
                        parentTypeName, "CData.IObjectSet<TKey, TObject> or implementing type"),
                        CSEX.GetTextSpan(propSymbol));
                }
                var typeArgs = collSymbol.TypeArguments;
                ObjectSetKeySelector.KeyType.CheckSymbol(propSymbol, typeArgs[0], parentTypeName + @"\object set key");
                ItemOrValueType.CheckSymbol(propSymbol, typeArgs[1], parentTypeName + @"\object set item");
            }
            else {//AtomSet
                collSymbol = typeSymbol.GetSelfOrInterface(CS.ISet1NameParts);
                if (collSymbol == null) {
                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractPropertyType,
                        parentTypeName, "System.Collections.Generic.ISet<T> or implementing type"),
                        CSEX.GetTextSpan(propSymbol));
                }
                ItemOrValueType.CheckSymbol(propSymbol, collSymbol.TypeArguments[0], parentTypeName + @"\atom set item");
            }
            //
            if (collSymbol.TypeKind == Microsoft.CodeAnalysis.TypeKind.Interface) {
            }
            else {
                if (collSymbol.IsAbstract || !collSymbol.HasParameterlessConstructor()) {
                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractPropertyType,
                        parentTypeName, "Non-abstract parameterless-constructor type"),
                        CSEX.GetTextSpan(propSymbol));
                }
                CollectionSymbol = collSymbol;
            }
        }
        public override TypeSyntax GetTypeSyntax() {
            if (CollectionSymbol != null) {
                return CollectionSymbol.ToNameSyntax();
            }
            var kind = Kind;
            if (kind == TypeKind.List) {
                return CS.ListOf(ItemOrValueType.GetTypeSyntax());
            }
            else if (kind == TypeKind.Map) {
                return CS.DictionaryOf(MapKeyType.GetTypeSyntax(), ItemOrValueType.GetTypeSyntax());
            }
            else if (kind == TypeKind.ObjectSet) {
                return CSEX.ObjectSetOf(ObjectSetKeySelector.KeyType.GetTypeSyntax(), ItemOrValueType.GetTypeSyntax());
            }
            else {//AtomSet
                return CS.HashSetOf(ItemOrValueType.GetTypeSyntax());
            }
        }
        public override ExpressionSyntax GetMetadataSyntax() {
            var kind = Kind;
            ExpressionSyntax objectSetKeySelectorExpr = null;
            if (kind == TypeKind.ObjectSet) {
                //>(Func<ObjType, KeyType>)(obj => obj.Prop1.Prop2)
                ExpressionSyntax bodyExpr = CS.IdName("obj");
                foreach (var propInfo in ObjectSetKeySelector) {
                    bodyExpr = CS.MemberAccessExpr(bodyExpr, CS.EscapeId(propInfo.CSName));
                }
                objectSetKeySelectorExpr = CS.CastExpr(
                    CS.FuncOf(ItemOrValueType.GetTypeSyntax(), ObjectSetKeySelector.KeyType.GetTypeSyntax()),
                    CS.ParedExpr(CS.SimpleLambdaExpr("obj", bodyExpr)));
            }
            //>new CollectionTypeMetadata(kind, isNullable, itemOrValueType, mapKeyType, objectSetKeySelector, clrType)
            return CS.NewObjExpr(CSEX.CollectionTypeMetadataName,
                CSEX.Literal(Kind), CS.Literal(IsNullable), ItemOrValueType.GetMetadataSyntax(),
                kind == TypeKind.Map ? MapKeyType.GetMetadataSyntax() : CS.NullLiteral,
                objectSetKeySelectorExpr ?? CS.NullLiteral,
                CS.TypeOfExpr(GetTypeSyntax())
                );
        }
    }


}
