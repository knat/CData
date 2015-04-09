using System;
using System.Collections.Generic;
using System.IO;

namespace CData.Compiler {
    public static class ParserConstants {
        public const string AbstractKeyword = "abstract";
        public const string AsKeyword = "as";
        public const string ClassKeyword = "class";
        public const string EnumKeyword = "enum";
        public const string ExtendsKeyword = "extends";
        public const string ImportKeyword = "import";
        public const string ListKeyword = "list";
        public const string MapKeyword = "map";
        public const string NamespaceKeyword = "namespace";
        public const string NullableKeyword = "nullable";
        public const string SealedKeyword = "sealed";
        public const string SetKeyword = "set";
        public static readonly HashSet<string> KeywordSet = new HashSet<string> {
            AbstractKeyword,
            AsKeyword,
            ClassKeyword,
            EnumKeyword,
            ExtendsKeyword,
            ImportKeyword,
            ListKeyword,
            MapKeyword,
            NamespaceKeyword,
            NullableKeyword,
            SealedKeyword,
            SetKeyword,
            //"true",
            //"false"
        };
    }
    internal sealed class Parser : ParserBase {
        [ThreadStatic]
        private static Parser _instance;
        public static bool Parse(string filePath, TextReader reader, DiagContext context, out CompilationUnitNode result) {
            return (_instance ?? (_instance = new Parser())).CompilationUnit(filePath, reader, context, out result);
        }
        private Parser() {
        }
        private void ErrorDiagAndThrow(DiagMsgEx diagMsg, TextSpan textSpan) {
            ErrorDiag((int)diagMsg.Code, diagMsg.GetMessage(), textSpan);
            Throw();
        }
        private void ErrorDiagAndThrow(DiagMsgEx diagMsg) {
            ErrorDiagAndThrow(diagMsg, GetTextSpan());
        }
        private bool CompilationUnit(string filePath, TextReader reader, DiagContext context, out CompilationUnitNode result) {
            try {
                Set(filePath, reader, context);
                var cu = new CompilationUnitNode();
                while (Namespace(cu)) ;
                EndOfFileExpected();
                result = cu;
                return true;
            }
            catch (ParsingException) { }
            finally {
                Clear();
            }
            result = null;
            return false;
        }
        private bool Namespace(CompilationUnitNode cu) {
            if (Keyword(ParserConstants.NamespaceKeyword)) {
                var uri = UriExpected();
                TokenExpected('{');
                var ns = new NamespaceNode(uri);
                while (Import(ns)) ;
                while (GlobalType(ns)) ;
                TokenExpected('}');
                cu.NamespaceList.Add(ns);
                return true;
            }
            return false;
        }
        private AtomValueNode UriExpected() {
            var uri = StringValueExpected();
            if (uri.Value == Extensions.SystemUri) {
                ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.UriSystemReserved), uri.TextSpan);
            }
            return uri;
        }
        private bool Import(NamespaceNode ns) {
            if (Keyword(ParserConstants.ImportKeyword)) {
                var uri = UriExpected();
                var alias = default(NameNode);
                if (Keyword(ParserConstants.AsKeyword)) {
                    alias = NameExpected();
                    if (alias.Value == "sys") {
                        ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.AliasSysReserved), alias.TextSpan);
                    }
                    if (ns.ImportList.Count > 0) {
                        foreach (var import in ns.ImportList) {
                            if (import.Alias == alias) {
                                ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.DuplicateNamespaceAlias, alias.Value), alias.TextSpan);
                            }
                        }
                    }
                }
                ns.ImportList.Add(new ImportNode(uri, alias));
                return true;
            }
            return false;
        }
        private void CheckDuplicateGlobalType(NamespaceNode ns, NameNode name) {
            if (ns.GlobalTypeList.Count > 0) {
                foreach (var globalType in ns.GlobalTypeList) {
                    if (globalType.Name == name) {
                        ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.DuplicateGlobalTypeName, name.Value), name.TextSpan);
                    }
                }
            }
        }
        private bool GlobalType(NamespaceNode ns) {
            if (Class(ns)) {
                return true;
            }
            return Enum(ns);
        }
        private bool Enum(NamespaceNode ns) {
            if (Keyword(ParserConstants.EnumKeyword)) {
                var name = NameExpected();
                CheckDuplicateGlobalType(ns, name);
                KeywordExpected(ParserConstants.AsKeyword);
                var atomQName = QualifiableNameExpected();
                TokenExpected('{');
                var en = new EnumNode(ns, name, atomQName);
                while (EnumMember(en)) ;
                TokenExpected('}');
                ns.GlobalTypeList.Add(en);
                return true;
            }
            return false;
        }
        private bool EnumMember(EnumNode en) {
            NameNode name;
            if (Name(out name)) {
                if (en.MemberList.Count > 0) {
                    foreach (var item in en.MemberList) {
                        if (item.Name == name) {
                            ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.DuplicateEnumMemberName, name.Value), name.TextSpan);
                        }
                    }
                }
                TokenExpected('=');
                en.MemberList.Add(new EnumMemberNode(name, NonNullAtomValueExpected()));
                return true;
            }
            return false;
        }
        private bool Class(NamespaceNode ns) {
            if (Keyword(ParserConstants.ClassKeyword)) {
                var name = NameExpected();
                CheckDuplicateGlobalType(ns, name);
                var abstractOrSealed = default(NameNode);
                if (Token('[')) {
                    if (!Keyword(ParserConstants.AbstractKeyword, out abstractOrSealed)) {
                        Keyword(ParserConstants.SealedKeyword, out abstractOrSealed);
                    }
                    TokenExpected(']');
                }
                var baseClassQName = default(QualifiableNameNode);
                if (Keyword(ParserConstants.ExtendsKeyword)) {
                    baseClassQName = QualifiableNameExpected();
                }
                TokenExpected('{');
                var cls = new ClassNode(ns, name, abstractOrSealed, baseClassQName);
                while (Property(ns, cls)) ;
                TokenExpected('}');
                ns.GlobalTypeList.Add(cls);
                return true;
            }
            return false;
        }
        private bool Property(NamespaceNode ns, ClassNode cls) {
            NameNode name;
            if (Name(out name)) {
                if (cls.PropertyList.Count > 0) {
                    foreach (var item in cls.PropertyList) {
                        if (item.Name == name) {
                            ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.DuplicatePropertyName, name.Value), name.TextSpan);
                        }
                    }
                }
                KeywordExpected(ParserConstants.AsKeyword);
                cls.PropertyList.Add(new PropertyNode(ns, name,
                    LocalTypeExpected(ns, LocalTypeFlags.GlobalTypeRef | LocalTypeFlags.Nullable | LocalTypeFlags.List | LocalTypeFlags.Set | LocalTypeFlags.Map)));
                return true;
            }
            return false;
        }
        [Flags]
        private enum LocalTypeFlags {
            GlobalTypeRef = 1,
            Nullable = 2,
            List = 4,
            Set = 8,
            Map = 16,
        }
        private LocalTypeNode LocalTypeExpected(NamespaceNode ns, LocalTypeFlags flags) {
            LocalTypeNode type;
            if (!LocalType(ns, flags, out type)) {
                ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.SpecificTypeExpected, flags.ToString()));
            }
            return type;
        }
        private bool LocalType(NamespaceNode ns, LocalTypeFlags flags, out LocalTypeNode result) {
            if ((flags & LocalTypeFlags.Nullable) != 0) {
                NullableNode r;
                if (Nullable(ns, out r)) {
                    result = r;
                    return true;
                }
            }
            if ((flags & LocalTypeFlags.List) != 0) {
                ListNode r;
                if (List(ns, out r)) {
                    result = r;
                    return true;
                }
            }
            if ((flags & LocalTypeFlags.Set) != 0) {
                SetNode r;
                if (Set(ns, out r)) {
                    result = r;
                    return true;
                }
            }
            if ((flags & LocalTypeFlags.Map) != 0) {
                MapNode r;
                if (Map(ns, out r)) {
                    result = r;
                    return true;
                }
            }
            if ((flags & LocalTypeFlags.GlobalTypeRef) != 0) {
                GlobalTypeRefNode r;
                if (GlobalTypeRef(ns, out r)) {
                    result = r;
                    return true;
                }
            }
            result = null;
            return false;
        }
        private bool GlobalTypeRef(NamespaceNode ns, out GlobalTypeRefNode result) {
            QualifiableNameNode qName;
            if (QualifiableName(out qName)) {
                result = new GlobalTypeRefNode(ns, qName.TextSpan, qName);
                return true;
            }
            result = null;
            return false;
        }
        private bool Nullable(NamespaceNode ns, out NullableNode result) {
            TextSpan ts;
            if (Keyword(ParserConstants.NullableKeyword, out ts)) {
                TokenExpected('<');
                var element = LocalTypeExpected(ns, LocalTypeFlags.GlobalTypeRef | LocalTypeFlags.List | LocalTypeFlags.Set | LocalTypeFlags.Map);
                TokenExpected('>');
                result = new NullableNode(ns, ts, element);
                return true;
            }
            result = null;
            return false;
        }
        private bool List(NamespaceNode ns, out ListNode result) {
            TextSpan ts;
            if (Keyword(ParserConstants.ListKeyword, out ts)) {
                TokenExpected('<');
                var item = LocalTypeExpected(ns, LocalTypeFlags.GlobalTypeRef | LocalTypeFlags.Nullable | LocalTypeFlags.List | LocalTypeFlags.Set | LocalTypeFlags.Map);
                TokenExpected('>');
                result = new ListNode(ns, ts, item);
                return true;
            }
            result = null;
            return false;
        }
        private bool Map(NamespaceNode ns, out MapNode result) {
            TextSpan ts;
            if (Keyword(ParserConstants.MapKeyword, out ts)) {
                TokenExpected('<');
                var key = (GlobalTypeRefNode)LocalTypeExpected(ns, LocalTypeFlags.GlobalTypeRef);
                TokenExpected(',');
                var value = LocalTypeExpected(ns, LocalTypeFlags.GlobalTypeRef | LocalTypeFlags.Nullable | LocalTypeFlags.List | LocalTypeFlags.Set | LocalTypeFlags.Map);
                TokenExpected('>');
                result = new MapNode(ns, ts, key, value);
                return true;
            }
            result = null;
            return false;
        }
        private bool Set(NamespaceNode ns, out SetNode result) {
            TextSpan ts;
            if (Keyword(ParserConstants.SetKeyword, out ts)) {
                TokenExpected('<');
                var item = (GlobalTypeRefNode)LocalTypeExpected(ns, LocalTypeFlags.GlobalTypeRef);
                List<NameNode> keyNameList = null;
                if (Token('\\')) {
                    keyNameList = new List<NameNode> { NameExpected() };
                    while (true) {
                        if (Token('.')) {
                            keyNameList.Add(NameExpected());
                        }
                        else {
                            break;
                        }
                    }
                }
                TextSpan closeTs;
                TokenExpected('>', out closeTs);
                result = new SetNode(ns, ts, item, keyNameList, closeTs);
                return true;
            }
            result = null;
            return false;
        }


    }
}
