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
            foreach (var member in MemberList) {
                if ((member.CSName ?? member.Name) == name) {
                    return (ClassInfo)member;
                }
            }
            return null;
        }
    }

    internal abstract class NamespaceMemberInfo {
        protected NamespaceMemberInfo(NamespaceInfo ns, string name) {
            Namespace = ns;
            Name = name;
        }
        public readonly NamespaceInfo Namespace;
        public readonly string Name;
        public CSFullName CSFullName;
        public string CSName {
            get { return CSFullName.LastName; }
        }
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
        

        //public string[] CSNameParts;
        //public INamedTypeSymbol CSSymbol;
        //private string _csFullNameString;
        //public string CSFullNameString {
        //    get {
        //        if (_csFullNameString == null) {

        //        }
        //        return _csFullNameString;
        //    }
        //}


        //private NameSyntax _CSFullName;
        //public NameSyntax CSFullName {
        //    get { return _CSFullName ?? (_CSFullName = CSFullNameString.ToNameSyntax()); }
        //    set { _CSFullName = value; }
        //}
        //private ExpressionSyntax _CSFullExpr;
        //public ExpressionSyntax CSFullExpr {
        //    get { return _CSFullExpr ?? (_CSFullExpr = CSFullNameString.ToExprSyntax()); }
        //    set { _CSFullExpr = value; }
        //}


        public override TypeInfo CreateType() {
            return new ClassRefTypeInfo(this);
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
        //public ITypeSymbol CSTypeSymbol;//opt

    }

    internal abstract class TypeInfo {
        protected TypeInfo(TypeKind kind) {
            Kind = kind;
        }
        public readonly TypeKind Kind;
        public bool IsNullable;
        public abstract void CheckCSType(ITypeSymbol typeSymbol);
    }
    internal sealed class AtomRefTypeInfo : TypeInfo {
        public AtomRefTypeInfo(AtomInfo atom)
            : base(atom.Kind) {
            Atom = atom;
        }
        public readonly AtomInfo Atom;
        public override void CheckCSType(ITypeSymbol typeSymbol) {
            if (!CSEX.IsAtomType(Kind, typeSymbol)) {
                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractClassAttributeName), default(TextSpan));
            }
        }
    }
    internal sealed class ClassRefTypeInfo : TypeInfo {
        public ClassRefTypeInfo(ClassInfo cls)
            : base(TypeKind.Class) {
            Class = cls;
        }
        public readonly ClassInfo Class;
        public override void CheckCSType(ITypeSymbol typeSymbol) {
            if (typeSymbol.FullNameEquals(Class.CSFullName.NameParts)) {
                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractClassAttributeName), default(TextSpan));
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
        public override void CheckCSType(ITypeSymbol typeSymbol) {
            var kind = Kind;
            if (kind == TypeKind.List) {
                var collSymbol = typeSymbol.GetSelfOrInterface(CS.ICollection1NameParts);
                if (collSymbol == null) {
                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractClassAttributeName), default(TextSpan));
                }
                ItemOrValueType.CheckCSType(collSymbol.TypeArguments[0]);
            }
            else if (kind == TypeKind.Map) {
                var dictSymbol = typeSymbol.GetSelfOrInterface(CS.IDictionary2TNameParts);
                if (dictSymbol == null) {
                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractClassAttributeName), default(TextSpan));
                }
                var typeArgs = dictSymbol.TypeArguments;
                KeyType.CheckCSType(typeArgs[0]);
                ItemOrValueType.CheckCSType(typeArgs[1]);
            }
            else if (kind == TypeKind.ObjectSet) {
                var objSetSymbol = typeSymbol.GetSelfOrInterface(CSEX.IOjectSet2NameParts);
                if (objSetSymbol == null) {
                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractClassAttributeName), default(TextSpan));
                }
                var typeArgs = objSetSymbol.TypeArguments;
                ObjectSetKeySelector.KeyType.CheckCSType(typeArgs[0]);
                ItemOrValueType.CheckCSType(typeArgs[1]);
            }
            else {//ATomSet
                var setSymbol = typeSymbol.GetSelfOrInterface(CS.ISet1NameParts);
                if (setSymbol == null) {
                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractClassAttributeName), default(TextSpan));
                }
                ItemOrValueType.CheckCSType(setSymbol.TypeArguments[0]);

            }

        }
    }


}
