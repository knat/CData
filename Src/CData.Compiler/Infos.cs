using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CData.Compiler {
    internal sealed class NamespaceInfo {
        public NamespaceInfo(string uri) {
            Uri = uri;
            MemberList = new List<NamespaceMemberInfo>();
        }
        public readonly string Uri;
        public readonly List<NamespaceMemberInfo> MemberList;
        public CSFullName CSFullName;
        public bool IsCSRef;
        public ClassInfo GetClass(string name) {
            foreach (ClassInfo cls in MemberList) {
                if (cls.Name == name) {
                    return cls;
                }
            }
            return null;
        }
        public void SetMembersCSFullName() {
            var nsFullName = CSFullName;
            foreach (var member in MemberList) {
                string csName;
                if (member.CSClassSymbol != null) {
                    csName = member.CSClassSymbol.Name;
                }
                else {
                    csName = member.Name;
                }
                member.CSFullName = new CSFullName(nsFullName, csName);
            }
        }
        public void MapAndCheckClassProperties() {
            foreach (ClassInfo cls in MemberList) {
                cls.MapAndCheckProperties(IsCSRef);
            }
        }
        public void Generate(List<MemberDeclarationSyntax> list, List<TypeOfExpressionSyntax> typeList) {
            if (!IsCSRef) {
                var memberList = new List<MemberDeclarationSyntax>();
                foreach (ClassInfo cls in MemberList) {
                    cls.Generate(list, typeList);
                }
                list.Add(SyntaxFactory.NamespaceDeclaration(CSFullName.FullNameSyntax,
                    default(SyntaxList<ExternAliasDirectiveSyntax>), default(SyntaxList<UsingDirectiveSyntax>),
                    SyntaxFactory.List(memberList)));
            }
        }

    }

    internal abstract class NamespaceMemberInfo {
        protected NamespaceMemberInfo(NamespaceInfo ns, string name) {
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
        public CSFullName CSFullName;
        public NameSyntax CSFullNameSyntax {
            get {
                return CSFullName.FullNameSyntax;
            }
        }
        public ExpressionSyntax CSFullExprSyntax {
            get {
                return CSFullName.FullExprSyntax;
            }
        }
        public INamedTypeSymbol CSClassSymbol;//opt
        public abstract TypeInfo CreateType();
    }

    internal sealed class AtomInfo : NamespaceMemberInfo {
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
            : base(null, null) {
            Kind = kind;
            TypeSyntax = typeSyntax;
            NullableTypeSyntax = nullableTypeSyntax;
            TypeDisplayName = typeSyntax.ToString();
            NullableTypeDisplayName = nullableTypeSyntax.ToString();
        }
        public readonly TypeKind Kind;
        public readonly TypeSyntax TypeSyntax;
        public readonly TypeSyntax NullableTypeSyntax;
        public readonly string TypeDisplayName;
        public readonly string NullableTypeDisplayName;
        public override TypeInfo CreateType() {
            return new AtomRefTypeInfo(this);
        }
    }
    internal sealed class ClassInfo : NamespaceMemberInfo {
        public ClassInfo(NamespaceInfo ns, string name, bool isAbstract, bool isSealed, ClassInfo baseClass,
            List<PropertyInfo> propertyList)
            : base(ns, name) {
            IsAbstract = isAbstract;
            IsSealed = isSealed;
            BaseClass = baseClass;
            PropertyList = propertyList;
        }
        public readonly bool IsAbstract;
        public readonly bool IsSealed;
        public readonly ClassInfo BaseClass;//opt
        public readonly List<PropertyInfo> PropertyList;//opt
        public override TypeInfo CreateType() {
            return new ClassRefTypeInfo(this);
        }
        public void MapAndCheckProperties(bool isRef) {
            var typeSymbol = CSClassSymbol;
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
                        if (propInfo.HasCSSymbol) {
                            DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.DuplicateContractPropertyAttributeName, propName),
                                CSEX.GetTextSpan(propAttData));
                        }
                        propInfo.CSSymbol = memberSymbol;
                        memberSymbolList.RemoveAt(i);
                        continue;
                    }
                    ++i;
                }
                foreach (var memberSymbol in memberSymbolList) {
                    var propName = memberSymbol.Name;
                    var propInfo = GetProperty(propName);
                    if (propInfo != null) {
                        if (!propInfo.HasCSSymbol) {
                            propInfo.CSSymbol = memberSymbol;
                        }
                    }
                }

                if (PropertyList != null) {
                    foreach (var propInfo in PropertyList) {
                        var memberSymbol = propInfo.CSSymbol;
                        if (memberSymbol != null) {
                            if (memberSymbol.IsStatic) {
                                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.ContractPropertyOrFieldCannotBeStatic),
                                    CSEX.GetTextSpan(memberSymbol));
                            }
                            var propSymbol = propInfo.CSPropertySymbol;
                            var fieldSymbol = propInfo.CSFieldSymbol;
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
                                propInfo.CheckCSSymbol();
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
        public void Generate(List<MemberDeclarationSyntax> list, List<TypeOfExpressionSyntax> typeList) {
            var memberList = new List<MemberDeclarationSyntax>();
            if (PropertyList != null) {
                foreach (var propInfo in PropertyList) {
                    propInfo.Generate(memberList);
                }
            }
            if (BaseClass == null) {
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
                    CS.OutParameter(CSFullNameSyntax, "result")
                },
                CS.ReturnStm(CS.InvoExpr(CS.MemberAccessExpr(CSEX.SerializerExpr, CS.GenericName("TryLoad", CSFullNameSyntax)),
                    SyntaxFactory.Argument(CS.IdName("filePath")), SyntaxFactory.Argument(CS.IdName("reader")), SyntaxFactory.Argument(CS.IdName("context")),
                    SyntaxFactory.Argument(CS.IdName(CSEX.ThisMetadataNameStr)), CS.OutArgument("result")))));
            if (BaseClass == null) {
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
            memberList.Add(CS.Field(BaseClass == null ? CS.PublicStaticReadOnlyTokenList : CS.NewPublicStaticReadOnlyTokenList,
                CSEX.ClassMetadataName, CSEX.ThisMetadataNameStr,
                CS.NewObjExpr(CSEX.ClassMetadataName, CSEX.Literal(FullName),
                    CS.Literal(IsAbstract),
                    BaseClass == null ? (ExpressionSyntax)CS.NullLiteral : CS.MemberAccessExpr(BaseClass.CSFullExprSyntax, CSEX.ThisMetadataNameStr),
                    CS.NewArrOrNullExpr(CSEX.PropertyMetadataArrayType, PropertyList == null ? null : PropertyList.Select(i => i.GenerateMetadata())),
                    CS.TypeOfExpr(CSFullNameSyntax)
                )));
            //>public virtual/override ClassMetadata __Metadata {
            //>    get { return __ThisMetadata; }
            //>}
            memberList.Add(CS.Property(BaseClass == null ? CS.PublicVirtualTokenList : CS.PublicOverrideTokenList,
                CSEX.ClassMetadataName, CSEX.MetadataNameStr, true, default(SyntaxTokenList),
                new StatementSyntax[] { 
                    CS.ReturnStm(CS.IdName(CSEX.ThisMetadataNameStr))
                }));

            list.Add(SyntaxFactory.ClassDeclaration(
                attributeLists: default(SyntaxList<AttributeListSyntax>),
                modifiers: IsAbstract ? CS.PublicAbstractPartialTokenList : CS.PublicPartialTokenList,
                identifier: CS.Id(CS.EscapeId(CSFullName.LastName)),
                typeParameterList: null,
                baseList: BaseClass == null ? null : CS.BaseList(BaseClass.CSFullNameSyntax),
                constraintClauses: default(SyntaxList<TypeParameterConstraintClauseSyntax>),
                members: SyntaxFactory.List(memberList)
                ));
            typeList.Add(CS.TypeOfExpr(CSFullNameSyntax));
        }
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

    }
    internal sealed class PropertyInfo {
        public PropertyInfo(string name, TypeInfo type) {
            Name = name;
            Type = type;
        }
        public readonly string Name;
        public readonly TypeInfo Type;
        public string CSName;
        public bool IsCSProperty;
        public ISymbol CSSymbol;//opt
        public bool HasCSSymbol {
            get {
                return CSSymbol != null;
            }
        }
        public IPropertySymbol CSPropertySymbol {
            get {
                return CSSymbol as IPropertySymbol;
            }
        }
        public IFieldSymbol CSFieldSymbol {
            get {
                return CSSymbol as IFieldSymbol;
            }
        }
        public void CheckCSSymbol() {
            Type.CheckCSType(CSSymbol, CSPropertySymbol != null ? CSPropertySymbol.Type : CSFieldSymbol.Type, ".");
        }
        public void Generate(List<MemberDeclarationSyntax> list) {
            if (CSSymbol == null) {
                list.Add(CS.Property(null, CS.PublicTokenList, Type.GetTypeSyntax(), CS.Id(CS.EscapeId(CSName)), CS.GetPrivateSetAccessorList));
            }
        }
        public ExpressionSyntax GenerateMetadata() {
            //>new PropertyMetadata(string name, TypeMetadata type, string clrName, bool isClrProperty)
            return CS.NewObjExpr(CSEX.PropertyMetadataName, CS.Literal(Name), Type.GenerateMetadata(),
                CS.Literal(CSName), CS.Literal(IsCSProperty));
        }
    }

    internal abstract class TypeInfo {
        protected TypeInfo(TypeKind kind) {
            Kind = kind;
        }
        public readonly TypeKind Kind;
        public bool IsNullable;
        public abstract void CheckCSType(ISymbol propSymbol, ITypeSymbol typeSymbol, string parentTypeName);
        public abstract TypeSyntax GetTypeSyntax();
        public abstract ExpressionSyntax GenerateMetadata();
    }
    internal sealed class AtomRefTypeInfo : TypeInfo {
        public AtomRefTypeInfo(AtomInfo atom)
            : base(atom.Kind) {
            Atom = atom;
        }
        public readonly AtomInfo Atom;
        public override void CheckCSType(ISymbol propSymbol, ITypeSymbol typeSymbol, string parentTypeName) {
            if (!CSEX.IsAtomType(Kind, IsNullable, typeSymbol)) {
                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractPropertyType,
                    parentTypeName, IsNullable ? Atom.NullableTypeDisplayName : Atom.TypeDisplayName),
                    CSEX.GetTextSpan(propSymbol));
            }
        }
        public override TypeSyntax GetTypeSyntax() {
            return IsNullable ? Atom.NullableTypeSyntax : Atom.TypeSyntax;
        }
        public override ExpressionSyntax GenerateMetadata() {
            //>AtomRefTypeMetadata.Get(kind, isNullable)
            return CS.InvoExpr(CS.MemberAccessExpr(CSEX.AtomRefTypeMetadataExpr, "Get"), CSEX.Literal(Kind), CS.Literal(IsNullable));
        }
    }
    internal sealed class ClassRefTypeInfo : TypeInfo {
        public ClassRefTypeInfo(ClassInfo cls)
            : base(TypeKind.Class) {
            Class = cls;
        }
        public readonly ClassInfo Class;
        public override void CheckCSType(ISymbol propSymbol, ITypeSymbol typeSymbol, string parentTypeName) {
            if (typeSymbol.FullNameEquals(Class.CSFullName.NameParts)) {
                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractPropertyType,
                    parentTypeName, Class.CSFullName.ToString()),
                    CSEX.GetTextSpan(propSymbol));
            }
        }
        public override TypeSyntax GetTypeSyntax() {
            return Class.CSFullNameSyntax;
        }
        public override ExpressionSyntax GenerateMetadata() {
            //>new ClassRefTypeMetadata(class.__ThisMetadata, isNullable)
            return CS.NewObjExpr(CSEX.ClassRefTypeMetadataName,
                CS.MemberAccessExpr(Class.CSFullExprSyntax, CSEX.ThisMetadataNameStr),
                CS.Literal(IsNullable));
        }
    }
    internal sealed class ObjectSetKeySelector : List<PropertyInfo> {
        public TypeInfo KeyType {
            get { return this[Count - 1].Type; }
        }
    }
    internal sealed class CollectionTypeInfo : TypeInfo {
        public CollectionTypeInfo(TypeKind kind, TypeInfo itemOrValueType, AtomRefTypeInfo mapKeyType,
            ObjectSetKeySelector objectSetKeySelector)
            : base(kind) {
            ItemOrValueType = itemOrValueType;
            MapKeyType = mapKeyType;
            ObjectSetKeySelector = objectSetKeySelector;
        }
        public readonly TypeInfo ItemOrValueType;
        public readonly AtomRefTypeInfo MapKeyType;//opt, for map
        public readonly ObjectSetKeySelector ObjectSetKeySelector;//opt
        public INamedTypeSymbol CSCollectionSymbol;//opt
        public override void CheckCSType(ISymbol propSymbol, ITypeSymbol typeSymbol, string parentTypeName) {
            INamedTypeSymbol collSymbol;
            var kind = Kind;
            if (kind == TypeKind.List) {
                collSymbol = typeSymbol.GetSelfOrInterface(CS.ICollection1NameParts);
                if (collSymbol == null) {
                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractPropertyType,
                        parentTypeName, "System.Collections.Generic.ICollection<T> or implementing type"),
                        CSEX.GetTextSpan(propSymbol));
                }
                ItemOrValueType.CheckCSType(propSymbol, collSymbol.TypeArguments[0], parentTypeName + @"\list item");
            }
            else if (kind == TypeKind.Map) {
                collSymbol = typeSymbol.GetSelfOrInterface(CS.IDictionary2TNameParts);
                if (collSymbol == null) {
                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractPropertyType,
                        parentTypeName, "System.Collections.Generic.IDictionary<TKey, TValue> or implementing type"),
                        CSEX.GetTextSpan(propSymbol));
                }
                var typeArgs = collSymbol.TypeArguments;
                MapKeyType.CheckCSType(propSymbol, typeArgs[0], parentTypeName + @"\map key");
                ItemOrValueType.CheckCSType(propSymbol, typeArgs[1], parentTypeName + @"\map value");
            }
            else if (kind == TypeKind.ObjectSet) {
                collSymbol = typeSymbol.GetSelfOrInterface(CSEX.IOjectSet2NameParts);
                if (collSymbol == null) {
                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractPropertyType,
                        parentTypeName, "CData.IObjectSet<TKey, TObject> or implementing type"),
                        CSEX.GetTextSpan(propSymbol));
                }
                var typeArgs = collSymbol.TypeArguments;
                ObjectSetKeySelector.KeyType.CheckCSType(propSymbol, typeArgs[0], parentTypeName + @"\object set key");
                ItemOrValueType.CheckCSType(propSymbol, typeArgs[1], parentTypeName + @"\object set item");
            }
            else {//AtomSet
                collSymbol = typeSymbol.GetSelfOrInterface(CS.ISet1NameParts);
                if (collSymbol == null) {
                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractPropertyType,
                        parentTypeName, "System.Collections.Generic.ISet<T> or implementing type"),
                        CSEX.GetTextSpan(propSymbol));
                }
                ItemOrValueType.CheckCSType(propSymbol, collSymbol.TypeArguments[0], parentTypeName + @"\atom set item");
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
                CSCollectionSymbol = collSymbol;
            }
        }
        public override TypeSyntax GetTypeSyntax() {
            if (CSCollectionSymbol != null) {
                return CSCollectionSymbol.ToNameSyntax();
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
        public override ExpressionSyntax GenerateMetadata() {
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
                CSEX.Literal(Kind), CS.Literal(IsNullable), ItemOrValueType.GenerateMetadata(),
                kind == TypeKind.Map ? MapKeyType.GenerateMetadata() : CS.NullLiteral,
                objectSetKeySelectorExpr ?? CS.NullLiteral,
                CS.TypeOfExpr(GetTypeSyntax())
                );
        }
    }


}
