using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CData.Compiler {

    internal sealed class NamespaceSymbol {
        public NamespaceSymbol(string uri) {
            Uri = uri;
            MemberList = new List<NamespaceMemberSymbol>();
        }
        public readonly string Uri;
        public readonly List<NamespaceMemberSymbol> MemberList;
        public CSharpNamespaceNameNode CSharpNamespaceName;
        public bool IsCSharpNamespaceRef;
        public ClassSymbol GetClass(string name) {
            foreach (var member in MemberList) {
                if (member.Name == name) {
                    return (ClassSymbol)member;
                }
            }
            return null;
        }
    }

    internal abstract class NamespaceMemberSymbol {
        protected NamespaceMemberSymbol(NamespaceSymbol ns, string name) {
            Namespace = ns;
            Name = name;
        }
        public readonly NamespaceSymbol Namespace;
        public readonly string Name;
        public abstract TypeSymbol CreateType();
    }

    internal sealed class AtomSymbol : NamespaceMemberSymbol {
        private AtomSymbol(TypeKind kind)
            : base(null, null) {
            Kind = kind;
        }
        public readonly TypeKind Kind;
        public override TypeSymbol CreateType() {
            return new AtomTypeSymbol(this);
        }
    }
    internal sealed class ClassSymbol : NamespaceMemberSymbol {
        public ClassSymbol(NamespaceSymbol ns, string name, bool isAbstract, bool isSealed, ClassSymbol baseClass,
            List<PropertySymbol> propertyList)
            : base(ns, name) {
            IsAbstract = isAbstract;
            IsSealed = isSealed;
            BaseClass = baseClass;
            PropertyList = propertyList;
        }
        public readonly bool IsAbstract;
        public readonly bool IsSealed;
        public readonly ClassSymbol BaseClass;//opt
        public readonly List<PropertySymbol> PropertyList;//opt
        public string CSFullNameString;
        private NameSyntax _CSFullName;
        public NameSyntax CSFullName {
            get { return _CSFullName ?? (_CSFullName = CSFullNameString.ToNameSyntax()); }
            set { _CSFullName = value; }
        }
        private ExpressionSyntax _CSFullExpr;
        public ExpressionSyntax CSFullExpr {
            get { return _CSFullExpr ?? (_CSFullExpr = CSFullNameString.ToExprSyntax()); }
            set { _CSFullExpr = value; }
        }


        public override TypeSymbol CreateType() {
            return new ClassTypeSymbol(this);
        }
        public PropertySymbol GetProperty(string name) {
            if (PropertyList != null) {
                foreach (var prop in PropertyList) {
                    if (prop.Name == name) {
                        return prop;
                    }
                }
            }
            if (BaseClass != null) {
                return BaseClass.GetProperty(name);
            }
            return null;
        }

    }
    internal sealed class PropertySymbol {
        public PropertySymbol(string name, TypeSymbol type) {
            Name = name;
            Type = type;
        }
        public readonly string Name;
        public readonly TypeSymbol Type;
        public string CSName;
        public bool IsCSProperty;

    }

    internal abstract class TypeSymbol {
        protected TypeSymbol(TypeKind kind) {
            Kind = kind;
        }
        public readonly TypeKind Kind;
        public bool IsNullable;
    }
    internal sealed class AtomTypeSymbol : TypeSymbol {
        public AtomTypeSymbol(AtomSymbol atom)
            : base(atom.Kind) {
            Atom = atom;
        }
        public readonly AtomSymbol Atom;
    }
    internal sealed class ClassTypeSymbol : TypeSymbol {
        public ClassTypeSymbol(ClassSymbol cls)
            : base(TypeKind.Class) {
            Class = cls;
        }
        public readonly ClassSymbol Class;
    }
    internal sealed class CollectionTypeSymbol : TypeSymbol {
        public CollectionTypeSymbol(TypeKind kind, TypeSymbol itemOrValueType, AtomTypeSymbol keyType,
            List<PropertySymbol> objectSetKeySelectorList)
            : base(kind) {
            ItemOrValueType = itemOrValueType;
            KeyType = keyType;
            ObjectSetKeySelectorList = objectSetKeySelectorList;
        }
        public readonly TypeSymbol ItemOrValueType;
        public readonly AtomTypeSymbol KeyType;//opt, for map
        public readonly List<PropertySymbol> ObjectSetKeySelectorList;//opt
    }


}
