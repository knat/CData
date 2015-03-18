﻿using System;
using System.Collections.Generic;

namespace CData.Compiler {
    internal sealed class CompilationUnitNode {
        public List<NamespaceNode> NamespaceList;
    }

    internal sealed class LogicalNamespaceMap : Dictionary<string, LogicalNamespace> { }
    internal sealed class LogicalNamespace : List<NamespaceNode> {
        public string Uri {
            get {
                return this[0].UriValue;
            }
        }
        //public CSharpNamespaceNameNode CSharpNamespaceName;
        //public bool IsCSharpNamespaceRef;
        //public NamespaceSymbol NamespaceSymbol;
        public void CheckDuplicateMembers() {
            var count = Count;
            for (var i = 0; i < count - 1; ++i) {
                for (var j = i + 1; j < count; ++j) {
                    this[i].CheckDuplicateMembers(this[j].MemberList);
                }
            }
        }
        public NamespaceMemberNode TryGetMember(NameNode name) {
            var count = Count;
            for (var i = 0; i < count; ++i) {
                var memberList = this[i].MemberList;
                if (memberList != null) {
                    foreach (var member in memberList) {
                        if (member.Name == name) {
                            return member;
                        }
                    }
                }
            }
            return null;
        }
    }
    internal sealed class NamespaceNode {
        public AtomValueNode Uri;
        public string UriValue {
            get {
                return Uri.Value;
            }
        }
        public List<ImportNode> ImportList;
        public List<NamespaceMemberNode> MemberList;
        public LogicalNamespace LogicalNamespace;
        //
        public void ResolveImports(LogicalNamespaceMap nsMap) {
            if (ImportList != null) {
                foreach (var import in ImportList) {
                    if (!nsMap.TryGetValue(import.Uri.Value, out import.LogicalNamespace)) {
                        DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidNamespaceReference, import.Uri.Value),
                            import.Uri.TextSpan);
                    }
                }
            }
        }
        public void CheckDuplicateMembers(List<NamespaceMemberNode> otherList) {
            if (MemberList != null && otherList != null) {
                foreach (var thisMember in MemberList) {
                    foreach (var otherMember in otherList) {
                        if (thisMember.Name == otherMember.Name) {
                            DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.DuplicateClassName, otherMember.Name.ToString()),
                                otherMember.Name.TextSpan);
                        }
                    }
                }
            }
        }
        public void Resolve() {
            if (MemberList != null) {
                foreach (var member in MemberList) {
                    member.Resolve();
                }
            }
        }
        public void CreateSymbols() {
            if (MemberList != null) {
                foreach (var member in MemberList) {
                    //member.CreateSymbol();
                }
            }
        }
        public NamespaceMemberNode ResolveQName(QualifiableNameNode qName) {
            NamespaceMemberNode result = null;
            var name = qName.Name;
            if (qName.IsQualified) {
                var alias = qName.Alias;
                if (alias.Value == "sys") {
                    result = AtomNode.TryGet(name.Value);
                }
                else {
                    ImportNode import = null;
                    if (ImportList != null) {
                        foreach (var item in ImportList) {
                            if (item.Alias == alias) {
                                import = item;
                                break;
                            }
                        }
                    }
                    if (import == null) {
                        DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidImportAliasReference, alias.ToString()),
                            alias.TextSpan);
                    }
                    result = import.LogicalNamespace.TryGetMember(name);
                }
            }
            else {
                result = LogicalNamespace.TryGetMember(name);
                if (result == null) {
                    result = AtomNode.TryGet(name.Value);
                    if (ImportList != null) {
                        foreach (var item in ImportList) {
                            var member = item.LogicalNamespace.TryGetMember(name);
                            if (member != null) {
                                if (result != null) {
                                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.AmbiguousNameReference, name.ToString()),
                                        name.TextSpan);
                                }
                                result = member;
                            }
                        }
                    }
                }
            }
            if (result == null) {
                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidNameReference, name.ToString()), name.TextSpan);
            }
            return result;
        }
        public ClassNode ResolveQNameAsClass(QualifiableNameNode qName) {
            var result = ResolveQName(qName) as ClassNode;
            if (result == null) {
                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidClassNameReference, qName.ToString()),
                    qName.TextSpan);
            }
            return result;
        }
        //public AtomNode ResolveQNameAsAtom(QualifiableNameNode qName) {
        //    var result = ResolveQName(qName) as AtomNode;
        //    if (result == null) {
        //        DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidAtomNameReference, qName.ToString()),
        //            qName.TextSpan);
        //    }
        //    return result;
        //}

    }
    internal sealed class ImportNode {
        public ImportNode(AtomValueNode uri, NameNode alias) {
            Uri = uri;
            Alias = alias;
            LogicalNamespace = null;
        }
        public readonly AtomValueNode Uri;
        public readonly NameNode Alias;//opt
        public LogicalNamespace LogicalNamespace;
    }

    internal abstract class NamespaceDescendantNode {
        protected NamespaceDescendantNode(NamespaceNode ns) {
            Namespace = ns;
        }
        public readonly NamespaceNode Namespace;
    }
    internal abstract class TypeNode : NamespaceDescendantNode {
        protected TypeNode(NamespaceNode ns) : base(ns) { }
        public TextSpan TextSpan;
        public abstract void Resolve();
    }
    internal sealed class RefTypeNode : TypeNode {
        public RefTypeNode(NamespaceNode ns) : base(ns) { }
        public QualifiableNameNode MemberQName;
        public NamespaceMemberNode Member;
        public override void Resolve() {
            Member = Namespace.ResolveQName(MemberQName);
        }
    }
    //nullable<element>
    internal sealed class NullableTypeNode : TypeNode {
        public NullableTypeNode(NamespaceNode ns) : base(ns) { }
        public TypeNode Element;
        public override void Resolve() {
            Element.Resolve();
        }
    }
    //list<item>
    internal sealed class ListTypeNode : TypeNode {
        public ListTypeNode(NamespaceNode ns) : base(ns) { }
        public TypeNode Item;
        public override void Resolve() {
            Item.Resolve();
        }
    }
    //set<Class1 \ Prop1.prop2>
    internal sealed class SetTypeNode : TypeNode {
        public SetTypeNode(NamespaceNode ns) : base(ns) { }
        public RefTypeNode Item;
        public List<NameNode> KeyNameList;//opt
        public override void Resolve() {
            Item.Resolve();
            if (Item.Member is ClassNode) {
                if (KeyNameList == null) {
                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.KeySelectorRequiredForObjectSet), Item.TextSpan);
                }
            }
            else if (KeyNameList != null) {
                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.KeySelectorNotAllowedForAtomSet), KeyNameList[0].TextSpan);
            }
        }
    }
    //map<key = value>
    internal sealed class MapTypeNode : TypeNode {
        public MapTypeNode(NamespaceNode ns) : base(ns) { }
        public RefTypeNode Key;
        public TypeNode Value;
        public override void Resolve() {
            Key.Resolve();
            if (!(Key.Member is AtomNode)) {
                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidAtomNameReference, Key.MemberQName.ToString()),
                    Key.MemberQName.TextSpan);
            }
            Value.Resolve();
        }
    }

    internal abstract class NamespaceMemberNode : NamespaceDescendantNode {
        public NamespaceMemberNode(NamespaceNode ns) : base(ns) { }
        public NameNode Name;
        public abstract void Resolve();
    }

    internal sealed class AtomNode : NamespaceMemberNode {
        private AtomNode() : base(null) { }
        private static readonly Dictionary<string, AtomNode> _dict;
        static AtomNode() {
            _dict = new Dictionary<string, AtomNode>();
            //var sysNs = NamespaceSymbol.System;
            //for (var kind = Extensions.TypeStart; kind <= Extensions.TypeEnd; ++kind) {
            //    var name = kind.ToString();
            //    _dict.Add(name, new AtomNode() { _objectSymbol = sysNs.TryGetGlobalObject(name) });
            //}
        }
        public static AtomNode TryGet(string name) {
            AtomNode result;
            _dict.TryGetValue(name, out result);
            return result;
        }
        public override void Resolve() {
            throw new NotImplementedException();
        }
    }

    internal sealed class ClassNode : NamespaceMemberNode {
        public ClassNode(NamespaceNode ns) : base(ns) { }
        public NameNode AbstractOrSealed;
        public bool IsAbstract {
            get {
                return AbstractOrSealed.Value == ParserConstants.AbstractKeyword;
            }
        }
        public bool IsSealed {
            get {
                return AbstractOrSealed.Value == ParserConstants.SealedKeyword;
            }
        }
        private string _csName;
        public string CSName {
            get {
                return _csName ?? (_csName = Name.Value.EscapeId());
            }
        }
        private FullName? _fullName;
        public FullName FullName {
            get {
                if (_fullName == null) {
                    _fullName = new FullName(Namespace.UriValue, Name.Value);
                }
                return _fullName.Value;
            }
        }
        public QualifiableNameNode BaseQName;//opt
        public ClassNode Base;
        public List<PropertyNode> PropertyList;
        public override void Resolve() {
            if (BaseQName.IsValid) {
                Base = Namespace.ResolveQNameAsClass(BaseQName);
            }
            if (PropertyList != null) {
                foreach (var prop in PropertyList) {
                    prop.Resolve();
                }
            }
        }
    }
    internal sealed class PropertyNode : NamespaceDescendantNode {
        public PropertyNode(NamespaceNode ns) : base(ns) { }
        public NameNode Name;
        public TypeNode Type;
        public void Resolve() {
            Type.Resolve();
        }
    }
    internal struct QualifiableNameNode {
        public QualifiableNameNode(NameNode alias, NameNode name) {
            Alias = alias;
            Name = name;
        }
        public readonly NameNode Alias;//opt
        public readonly NameNode Name;
        public bool IsQualified {
            get {
                return Alias.IsValid;
            }
        }
        public bool IsValid {
            get {
                return Name.IsValid;
            }
        }
        public TextSpan TextSpan {
            get {
                return Name.TextSpan;
            }
        }
        public override string ToString() {
            if (IsQualified) {
                return Alias.ToString() + ":" + Name.ToString();
            }
            return Name.ToString();
        }
    }

}
