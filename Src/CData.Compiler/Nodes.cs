using System;
using System.Collections.Generic;

namespace CData.Compiler {
    internal sealed class CompilationUnitNode {
        public List<NamespaceNode> NamespaceList;
    }

    internal sealed class LogicalNamespaceMap : Dictionary<string, LogicalNamespace> { }
    internal sealed class LogicalNamespace : List<NamespaceNode> {
        public string Uri {
            get { return this[0].UriValue; }
        }
        public NamespaceInfo NamespaceInfo;
        public CSFullName CSFullName {
            get { return NamespaceInfo.CSFullName; }
            set { NamespaceInfo.CSFullName = value; }
        }
        public bool IsCSRef {
            get { return NamespaceInfo.IsCSRef; }
            set { NamespaceInfo.IsCSRef = value; }
        }

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
                            DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.DuplicateNamespaceMemberName, otherMember.Name.ToString()),
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
        public void CreateInfos() {
            if (MemberList != null) {
                foreach (var member in MemberList) {
                    member.CreateInfo();
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
        public AtomNode ResolveQNameAsAtom(QualifiableNameNode qName) {
            var result = ResolveQName(qName) as AtomNode;
            if (result == null) {
                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidAtomNameReference, qName.ToString()),
                    qName.TextSpan);
            }
            return result;
        }

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

    internal abstract class NamespaceMemberNode : NamespaceDescendantNode {
        public NamespaceMemberNode(NamespaceNode ns) : base(ns) { }
        public NameNode Name;
        public abstract void Resolve();
        protected NamespaceMemberInfo _info;
        private bool _isProcessing;
        public NamespaceMemberInfo CreateInfo() {
            if (_info == null) {
                if (_isProcessing) {
                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.CircularReferenceNotAllowed), Name.TextSpan);
                }
                _isProcessing = true;
                var nsInfo = Namespace.LogicalNamespace.NamespaceInfo;
                var info = CreateInfoCore(nsInfo);
                nsInfo.MemberList.Add(info);
                _info = info;
                _isProcessing = false;
            }
            return _info;
        }
        protected abstract NamespaceMemberInfo CreateInfoCore(NamespaceInfo nsInfo);
    }

    internal sealed class AtomNode : NamespaceMemberNode {
        private static readonly Dictionary<string, AtomNode> _map;
        static AtomNode() {
            _map = new Dictionary<string, AtomNode>();
            for (var kind = Extensions.AtomTypeStart; kind <= Extensions.AtomTypeEnd; ++kind) {
                _map.Add(kind.ToString(), new AtomNode(AtomInfo.Get(kind)));
            }
        }
        public static AtomNode TryGet(string name) {
            AtomNode result;
            _map.TryGetValue(name, out result);
            return result;
        }
        private AtomNode(AtomInfo info)
            : base(null) {
            _info = info;
        }
        public override void Resolve() {
            throw new NotImplementedException();
        }
        protected override NamespaceMemberInfo CreateInfoCore(NamespaceInfo nsInfo) {
            throw new NotImplementedException();
        }
    }
    internal sealed class EnumNode : NamespaceMemberNode {
        public EnumNode(NamespaceNode ns) : base(ns) { }
        public QualifiableNameNode UnderlyingQName;
        public AtomNode UnderlyingType;
        public List<EnumMemberNode> MemberList;//opt
        public override void Resolve() {
            UnderlyingType = Namespace.ResolveQNameAsAtom(UnderlyingQName);
        }
        protected override NamespaceMemberInfo CreateInfoCore(NamespaceInfo nsInfo) {
            var underlyingTypeInfo = (AtomInfo)UnderlyingType.CreateInfo();
            List<NameValuePair> memberInfoList = null;
            if (MemberList != null) {
                var kind = underlyingTypeInfo.Kind;
                foreach (var member in MemberList) {
                    Extensions.CreateAndAdd(ref memberInfoList, member.CreateInfo(kind));
                }
            }
            return new EnumInfo(nsInfo, Name.Value, underlyingTypeInfo, memberInfoList);
        }
    }
    internal sealed class EnumMemberNode {
        public EnumMemberNode(NameNode name, AtomValueNode value) {
            Name = name;
            Value = value;
        }
        public readonly NameNode Name;
        public readonly AtomValueNode Value;
        public NameValuePair CreateInfo(TypeKind typeKind) {
            var avNode = Value;
            var value = Extensions.TryParse(typeKind, avNode.Value, true);
            if (value == null) {
                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidAtomValue, avNode.Value, typeKind.ToString()), avNode.TextSpan);
            }
            return new NameValuePair(Name.Value, value);
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
        public QualifiableNameNode BaseQName;//opt
        public ClassNode BaseClass;
        public List<PropertyNode> PropertyList;//opt
        public override void Resolve() {
            if (BaseQName.IsValid) {
                BaseClass = Namespace.ResolveQNameAsClass(BaseQName);
            }
            if (PropertyList != null) {
                foreach (var prop in PropertyList) {
                    prop.Resolve();
                }
            }
        }
        protected override NamespaceMemberInfo CreateInfoCore(NamespaceInfo nsInfo) {
            ClassInfo baseClassInfo = null;
            if (BaseClass != null) {
                baseClassInfo = (ClassInfo)BaseClass.CreateInfo();
                if (baseClassInfo.IsSealed) {
                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.BaseClassIsSealed), BaseQName.TextSpan);
                }
            }
            List<PropertyInfo> propInfoList = null;
            if (PropertyList != null) {
                foreach (var prop in PropertyList) {
                    if (baseClassInfo != null && baseClassInfo.GetPropertyInHierarchy(prop.Name.Value) != null) {
                        DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.DuplicatePropertyName, prop.Name.Value), prop.Name.TextSpan);
                    }
                    Extensions.CreateAndAdd(ref propInfoList, prop.CreateInfo());
                }
            }
            return new ClassInfo(nsInfo, Name.Value, IsAbstract, IsSealed, baseClassInfo, propInfoList);
        }
    }
    internal sealed class PropertyNode : NamespaceDescendantNode {
        public PropertyNode(NamespaceNode ns) : base(ns) { }
        public NameNode Name;
        public TypeNode Type;
        public void Resolve() {
            Type.Resolve();
        }
        public PropertyInfo CreateInfo() {
            return new PropertyInfo(Name.Value, Type.CreateTypeInfo());
        }
    }

    internal abstract class TypeNode : NamespaceDescendantNode {
        protected TypeNode(NamespaceNode ns) : base(ns) { }
        public TextSpan TextSpan;
        public abstract void Resolve();
        public abstract TypeInfo CreateTypeInfo();
    }
    internal sealed class RefTypeNode : TypeNode {
        public RefTypeNode(NamespaceNode ns) : base(ns) { }
        public QualifiableNameNode QName;
        public NamespaceMemberNode Member;
        public bool IsAtom {
            get {
                return Member is AtomNode;
            }
        }
        public bool IsClass {
            get {
                return !IsAtom;
            }
        }
        public override void Resolve() {
            Member = Namespace.ResolveQName(QName);
        }
        public override TypeInfo CreateTypeInfo() {
            return Member.CreateInfo().CreateType();
        }
    }
    //nullable<element>
    internal sealed class NullableTypeNode : TypeNode {
        public NullableTypeNode(NamespaceNode ns) : base(ns) { }
        public TypeNode Element;
        public override void Resolve() {
            Element.Resolve();
        }
        public override TypeInfo CreateTypeInfo() {
            var res = Element.CreateTypeInfo();
            res.IsNullable = true;
            return res;
        }
    }
    //list<item>
    internal sealed class ListTypeNode : TypeNode {
        public ListTypeNode(NamespaceNode ns) : base(ns) { }
        public TypeNode Item;
        public override void Resolve() {
            Item.Resolve();
        }
        public override TypeInfo CreateTypeInfo() {
            return new CollectionTypeInfo(TypeKind.List, Item.CreateTypeInfo(), null, null);
        }
    }
    //set<Class1 \ Prop1.prop2>
    //set<Int32>
    internal sealed class SetTypeNode : TypeNode {
        public SetTypeNode(NamespaceNode ns) : base(ns) { }
        public RefTypeNode Item;
        public List<NameNode> KeyNameList;//opt
        public TextSpan CloseTextSpan;
        public bool IsObjectSet {
            get {
                return KeyNameList != null;
            }
        }
        public bool IsAtomSet {
            get {
                return !IsObjectSet;
            }
        }
        public override void Resolve() {
            Item.Resolve();
            if (Item.IsAtom) {
                if (KeyNameList != null) {
                    DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.KeySelectorNotAllowedForAtomSet), KeyNameList[0].TextSpan);
                }
            }
            else if (KeyNameList == null) {
                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.KeySelectorRequiredForObjectSet), CloseTextSpan);
            }
        }
        public override TypeInfo CreateTypeInfo() {
            var itemTypeInfo = Item.CreateTypeInfo();
            ObjectSetKeySelector selector = null;
            if (IsObjectSet) {
                selector = new ObjectSetKeySelector();
                var clsTypeInfo = (ClassRefTypeInfo)itemTypeInfo;
                var keyCount = KeyNameList.Count;
                for (var i = 0; i < keyCount; ++i) {
                    var keyName = KeyNameList[i];
                    var propInfo = clsTypeInfo.Class.GetPropertyInHierarchy(keyName.Value);
                    if (propInfo == null) {
                        DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidPropertyNameReference), keyName.TextSpan);
                    }
                    var propTypeInfo = propInfo.Type;
                    if (propTypeInfo.IsNullable) {
                        DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.ObjectSetKeyCannotBeNullable), keyName.TextSpan);
                    }
                    if (propTypeInfo.Kind.IsAtom()) {
                        if (i < keyCount - 1) {
                            DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidObjectSetKey), KeyNameList[i + 1].TextSpan);
                        }
                        selector.Add(propInfo);
                    }
                    else if (propTypeInfo.Kind == TypeKind.Class) {
                        if (i == keyCount - 1) {
                            DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.ObjectSetKeyMustBeAtom), keyName.TextSpan);
                        }
                        selector.Add(propInfo);
                        clsTypeInfo = (ClassRefTypeInfo)propTypeInfo;
                    }
                    else {
                        DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.ObjectSetKeyMustBeAtom), keyName.TextSpan);
                    }
                }
            }
            return new CollectionTypeInfo(IsObjectSet ? TypeKind.ObjectSet : TypeKind.AtomSet, itemTypeInfo, null, selector);
        }
    }
    //map<key = value>
    internal sealed class MapTypeNode : TypeNode {
        public MapTypeNode(NamespaceNode ns) : base(ns) { }
        public RefTypeNode Key;
        public TypeNode Value;
        public override void Resolve() {
            Key.Resolve();
            if (Key.IsClass) {
                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidAtomNameReference, Key.QName.ToString()),
                    Key.QName.TextSpan);
            }
            Value.Resolve();
        }
        public override TypeInfo CreateTypeInfo() {
            return new CollectionTypeInfo(TypeKind.Map, Value.CreateTypeInfo(), (AtomRefTypeInfo)Key.CreateTypeInfo(), null);
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
