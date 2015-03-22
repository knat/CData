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
            if (!IsCSRef) {
                foreach (ClassInfo cls in MemberList) {
                    cls.MapAndCheckProperties();
                }
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
        public INamedTypeSymbol CSClassSymbol;//opt
        public CSFullName CSFullName;


        //public string CSName {
        //    get { return CSFullName.LastName; }
        //}
        public abstract TypeInfo CreateType();
    }

    internal sealed class AtomInfo : NamespaceMemberInfo {
        private AtomInfo(TypeKind kind)
            : base(null, null) {
            Kind = kind;
        }
        public readonly TypeKind Kind;
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
        public void MapAndCheckProperties() {
            var typeSymbol = CSClassSymbol;
            if (typeSymbol != null) {
                foreach (var memberSymbol in typeSymbol.GetMembers()) {
                    var propSymbol = memberSymbol as IPropertySymbol;
                    var fieldSymbol = memberSymbol as IFieldSymbol;
                    if (propSymbol != null || fieldSymbol != null) {
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
                            propInfo.CSPropertySymbol = propSymbol;
                            propInfo.CSFieldSymbol = fieldSymbol;
                        }
                    }
                }
                foreach (var memberSymbol in typeSymbol.GetMembers()) {
                    var propSymbol = memberSymbol as IPropertySymbol;
                    var fieldSymbol = memberSymbol as IFieldSymbol;
                    if (propSymbol != null || fieldSymbol != null) {
                        var propName = memberSymbol.Name;
                        var propInfo = GetProperty(propName);
                        if (propInfo != null) {
                            if (!propInfo.HasCSSymbol) {
                                propInfo.CSPropertySymbol = propSymbol;
                                propInfo.CSFieldSymbol = fieldSymbol;
                            }
                        }
                    }
                }
                if (PropertyList != null) {
                    foreach (var propInfo in PropertyList) {
                        var propSymbol = propInfo.CSPropertySymbol;
                        var fieldSymbol = propInfo.CSFieldSymbol;
                        var memberSymbol = (ISymbol)propSymbol ?? fieldSymbol;
                        if (memberSymbol != null) {
                            if (memberSymbol.IsStatic) {
                                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.ContractPropertyOrFieldCannotBeStatic),
                                    CSEX.GetTextSpan(memberSymbol));
                            }
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
                            propInfo.CheckCSSymbol();
                        }
                        else {
                            propInfo.CSName = propInfo.Name;
                            propInfo.IsCSProperty = true;
                        }
                    }
                }
            }
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
        public IPropertySymbol CSPropertySymbol;//opt
        public IFieldSymbol CSFieldSymbol;//opt
        public ISymbol CSSymbol {
            get { return (ISymbol)CSPropertySymbol ?? CSFieldSymbol; }
        }
        public bool HasCSSymbol {
            get { return CSPropertySymbol != null || CSFieldSymbol != null; }
        }
        public void CheckCSSymbol() {
            Type.CheckCSType(CSSymbol, CSPropertySymbol != null ? CSPropertySymbol.Type : CSFieldSymbol.Type, ".");
        }
    }

    internal abstract class TypeInfo {
        protected TypeInfo(TypeKind kind) {
            Kind = kind;
        }
        public readonly TypeKind Kind;
        public bool IsNullable;
        public abstract void CheckCSType(ISymbol propSymbol, ITypeSymbol typeSymbol, string parentTypeName);
    }
    internal sealed class AtomRefTypeInfo : TypeInfo {
        public AtomRefTypeInfo(AtomInfo atom)
            : base(atom.Kind) {
            Atom = atom;
        }
        public readonly AtomInfo Atom;
        public override void CheckCSType(ISymbol propSymbol, ITypeSymbol typeSymbol, string parentTypeName) {
            if (!CSEX.IsAtomType(Kind, IsNullable, typeSymbol)) {
                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractPropertyOrFieldType, parentTypeName),
                    CSEX.GetTextSpan(propSymbol));
            }
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
                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractPropertyOrFieldType, parentTypeName),
                    CSEX.GetTextSpan(propSymbol));
            }
        }
    }
    internal sealed class ObjectSetKeySelector : List<PropertyInfo> {
        public TypeInfo KeyType {
            get { return this[Count - 1].Type; }
        }
    }
    internal sealed class CollectionTypeInfo : TypeInfo {
        public CollectionTypeInfo(TypeKind kind, TypeInfo itemOrValueType, AtomRefTypeInfo keyType,
            ObjectSetKeySelector objectSetKeySelector)
            : base(kind) {
            ItemOrValueType = itemOrValueType;
            KeyType = keyType;
            ObjectSetKeySelector = objectSetKeySelector;
        }
        public readonly TypeInfo ItemOrValueType;
        public readonly AtomRefTypeInfo KeyType;//opt, for map
        public readonly ObjectSetKeySelector ObjectSetKeySelector;//opt
        public INamedTypeSymbol CSCollectionSymbol;//opt

        public override void CheckCSType(ISymbol propSymbol, ITypeSymbol typeSymbol, string parentTypeName) {
            var kind = Kind;
            INamedTypeSymbol collSymbol;
            if (kind == TypeKind.List) {
                collSymbol = typeSymbol.GetSelfOrInterface(CS.ICollection1NameParts);
                if (collSymbol == null) {
                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractPropertyOrFieldType, parentTypeName),
                        CSEX.GetTextSpan(propSymbol));
                }
                ItemOrValueType.CheckCSType(propSymbol, collSymbol.TypeArguments[0], parentTypeName + @"\list item");
            }
            else if (kind == TypeKind.Map) {
                collSymbol = typeSymbol.GetSelfOrInterface(CS.IDictionary2TNameParts);
                if (collSymbol == null) {
                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractPropertyOrFieldType, parentTypeName),
                        CSEX.GetTextSpan(propSymbol));
                }
                var typeArgs = collSymbol.TypeArguments;
                KeyType.CheckCSType(propSymbol, typeArgs[0], parentTypeName + @"\map key");
                ItemOrValueType.CheckCSType(propSymbol, typeArgs[1], parentTypeName + @"\map value");
            }
            else if (kind == TypeKind.ObjectSet) {
                collSymbol = typeSymbol.GetSelfOrInterface(CSEX.IOjectSet2NameParts);
                if (collSymbol == null) {
                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractPropertyOrFieldType, parentTypeName),
                        CSEX.GetTextSpan(propSymbol));
                }
                var typeArgs = collSymbol.TypeArguments;
                ObjectSetKeySelector.KeyType.CheckCSType(propSymbol, typeArgs[0], parentTypeName + @"\object set key");
                ItemOrValueType.CheckCSType(propSymbol, typeArgs[1], parentTypeName + @"\object set item");
            }
            else {//AtomSet
                collSymbol = typeSymbol.GetSelfOrInterface(CS.ISet1NameParts);
                if (collSymbol == null) {
                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractPropertyOrFieldType, parentTypeName),
                        CSEX.GetTextSpan(propSymbol));
                }
                ItemOrValueType.CheckCSType(propSymbol, collSymbol.TypeArguments[0], parentTypeName + @"\atom set item");
            }
            //
            if (collSymbol.TypeKind == Microsoft.CodeAnalysis.TypeKind.Interface) {
            }
            else {
                if (collSymbol.IsAbstract || collSymbol.IsGenericType || !collSymbol.HasParameterlessConstructor()) {
                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractPropertyOrFieldCollectionType, parentTypeName),
                        CSEX.GetTextSpan(propSymbol));

                }
                CSCollectionSymbol = collSymbol;
            }
        }
    }


}
