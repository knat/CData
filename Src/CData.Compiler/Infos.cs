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
        public CSNamespaceNameNode CSNamespaceName;
        public bool IsCSNamespaceRef;
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
        public string CSName;
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
        //public INamedTypeSymbol CSSymbol;

        //public string CSFullNameString;
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
        //public ISymbol CS

    }

    internal abstract class TypeInfo {
        protected TypeInfo(TypeKind kind) {
            Kind = kind;
        }
        public readonly TypeKind Kind;
        public bool IsNullable;
    }
    internal sealed class AtomRefTypeInfo : TypeInfo {
        public AtomRefTypeInfo(AtomInfo atom)
            : base(atom.Kind) {
            Atom = atom;
        }
        public readonly AtomInfo Atom;
    }
    internal sealed class ClassRefTypeInfo : TypeInfo {
        public ClassRefTypeInfo(ClassInfo cls)
            : base(TypeKind.Class) {
            Class = cls;
        }
        public readonly ClassInfo Class;
    }
    internal sealed class CollectionTypeInfo : TypeInfo {
        public CollectionTypeInfo(TypeKind kind, TypeInfo itemOrValueType, AtomRefTypeInfo keyType,
            List<PropertyInfo> objectSetKeySelectorList)
            : base(kind) {
            ItemOrValueType = itemOrValueType;
            KeyType = keyType;
            ObjectSetKeySelectorList = objectSetKeySelectorList;
        }
        public readonly TypeInfo ItemOrValueType;
        public readonly AtomRefTypeInfo KeyType;//opt, for map
        public readonly List<PropertyInfo> ObjectSetKeySelectorList;//opt
    }


}
