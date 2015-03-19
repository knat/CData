using System;
using System.Collections.Generic;
using System.IO;

namespace CData.Compiler {
    public static class ParserConstants {
        public const string AbstractKeyword = "abstract";
        public const string AsKeyword = "as";
        public const string ClassKeyword = "class";
        public const string ExtendsKeyword = "extends";
        public const string ImportKeyword = "import";
        public const string ListKeyword = "list";
        public const string MapKeyword = "map";
        public const string NamespaceKeyword = "namespace";
        public const string NullableKeyword = "nullable";
        public const string SealedKeyword = "sealed";
        public const string SetKeyword = "set";
        public static readonly HashSet<string> KeywordSet = new HashSet<string>
        {
            AbstractKeyword,
            AsKeyword,
            ClassKeyword,
            ExtendsKeyword,
            ImportKeyword,
            ListKeyword,
            MapKeyword,
            NamespaceKeyword,
            NullableKeyword,
            SealedKeyword,
            SetKeyword,
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
                var ns = new NamespaceNode();
                ns.Uri = UriExpected();
                TokenExpected('{');
                while (Import(ns)) ;
                while (Class(ns)) ;
                TokenExpected('}');
                Extensions.CreateAndAdd(ref cu.NamespaceList, ns);
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
                    if (ns.ImportList.CountOrZero() > 0) {
                        foreach (var import in ns.ImportList) {
                            if (import.Alias == alias) {
                                ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.DuplicateImportAlias, alias.ToString()), alias.TextSpan);
                            }
                        }
                    }
                }
                Extensions.CreateAndAdd(ref ns.ImportList, new ImportNode(uri, alias));
                return true;
            }
            return false;
        }
        private bool Class(NamespaceNode ns) {
            if (Keyword(ParserConstants.ClassKeyword)) {
                var name = NameExpected();
                if (ns.MemberList.CountOrZero() > 0) {
                    foreach (var member in ns.MemberList) {
                        if (member.Name == name) {
                            ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.DuplicateClassName, name.ToString()), name.TextSpan);
                        }
                    }
                }
                var cls = new ClassNode(ns) { Name = name };
                if (Token('[')) {
                    if (!Keyword(ParserConstants.AbstractKeyword, out cls.AbstractOrSealed)) {
                        Keyword(ParserConstants.SealedKeyword, out cls.AbstractOrSealed);
                    }
                    TokenExpected(']');
                }
                if (Keyword(ParserConstants.ExtendsKeyword)) {
                    cls.BaseQName = QualifiableNameExpected();
                }
                TokenExpected('{');
                while (Property(ns, cls)) ;
                TokenExpected('}');
                Extensions.CreateAndAdd(ref ns.MemberList, cls);
                return true;
            }
            return false;
        }
        private bool Property(NamespaceNode ns, ClassNode cls) {
            NameNode name;
            if (Name(out name)) {
                if (cls.PropertyList.CountOrZero() > 0) {
                    foreach (var propitem in cls.PropertyList) {
                        if (propitem.Name == name) {
                            ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.DuplicatePropertyName, name.ToString()), name.TextSpan);
                        }
                    }
                }
                KeywordExpected(ParserConstants.AsKeyword);
                Extensions.CreateAndAdd(ref cls.PropertyList,
                    new PropertyNode(ns) {
                        Name = name,
                        Type = TypeExpected(ns, TypeFlags.Ref | TypeFlags.Nullable | TypeFlags.List | TypeFlags.Set | TypeFlags.Map)
                    });
                return true;
            }
            return false;
        }
        [Flags]
        private enum TypeFlags {
            Ref = 1,
            Nullable = 2,
            List = 4,
            Set = 8,
            Map = 16,
        }
        private bool Type(NamespaceNode ns, TypeFlags flags, out TypeNode result) {
            if ((flags & TypeFlags.Nullable) != 0) {
                NullableTypeNode r;
                if (NullableType(ns, out r)) {
                    result = r;
                    return true;
                }
            }
            if ((flags & TypeFlags.List) != 0) {
                ListTypeNode r;
                if (ListType(ns, out r)) {
                    result = r;
                    return true;
                }
            }
            if ((flags & TypeFlags.Set) != 0) {
                SetTypeNode r;
                if (SetType(ns, out r)) {
                    result = r;
                    return true;
                }
            }
            if ((flags & TypeFlags.Map) != 0) {
                MapTypeNode r;
                if (MapType(ns, out r)) {
                    result = r;
                    return true;
                }
            }
            if ((flags & TypeFlags.Ref) != 0) {
                RefTypeNode r;
                if (RefType(ns, out r)) {
                    result = r;
                    return true;
                }
            }
            result = null;
            return false;
        }
        private TypeNode TypeExpected(NamespaceNode ns, TypeFlags flags) {
            TypeNode type;
            if (!Type(ns, flags, out type)) {
                ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.TypeExpected, flags.ToString()));
            }
            return type;
        }
        private bool RefType(NamespaceNode ns, out RefTypeNode result) {
            QualifiableNameNode qName;
            if (QualifiableName(out qName)) {
                result = new RefTypeNode(ns) { TextSpan = qName.TextSpan, QName = qName };
                return true;
            }
            result = null;
            return false;
        }
        private bool NullableType(NamespaceNode ns, out NullableTypeNode result) {
            TextSpan ts;
            if (Keyword(ParserConstants.NullableKeyword, out ts)) {
                TokenExpected('<');
                var element = TypeExpected(ns, TypeFlags.Ref | TypeFlags.List | TypeFlags.Set | TypeFlags.Map);
                TokenExpected('>');
                result = new NullableTypeNode(ns) { TextSpan = ts, Element = element };
                return true;
            }
            result = null;
            return false;
        }
        private bool ListType(NamespaceNode ns, out ListTypeNode result) {
            TextSpan ts;
            if (Keyword(ParserConstants.ListKeyword, out ts)) {
                TokenExpected('<');
                var item = TypeExpected(ns, TypeFlags.Ref | TypeFlags.Nullable | TypeFlags.List | TypeFlags.Set | TypeFlags.Map);
                TokenExpected('>');
                result = new ListTypeNode(ns) { TextSpan = ts, Item = item };
                return true;
            }
            result = null;
            return false;
        }
        private bool SetType(NamespaceNode ns, out SetTypeNode result) {
            TextSpan ts;
            if (Keyword(ParserConstants.SetKeyword, out ts)) {
                TokenExpected('<');
                var item = (RefTypeNode)TypeExpected(ns, TypeFlags.Ref);
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
                result = new SetTypeNode(ns) { TextSpan = ts, Item = item, KeyNameList = keyNameList, CloseTextSpan = closeTs };
                return true;
            }
            result = null;
            return false;
        }
        private bool MapType(NamespaceNode ns, out MapTypeNode result) {
            TextSpan ts;
            if (Keyword(ParserConstants.MapKeyword, out ts)) {
                TokenExpected('<');
                var key = (RefTypeNode)TypeExpected(ns, TypeFlags.Ref);
                TokenExpected(',');
                var value = TypeExpected(ns, TypeFlags.Ref | TypeFlags.Nullable | TypeFlags.List | TypeFlags.Set | TypeFlags.Map);
                TokenExpected('>');
                result = new MapTypeNode(ns) { TextSpan = ts, Key = key, Value = value };
                return true;
            }
            result = null;
            return false;
        }

        private bool QualifiableName(out QualifiableNameNode result) {
            NameNode name;
            if (Name(out name)) {
                if (Token(':')) {
                    result = new QualifiableNameNode(name, NameExpected());
                }
                else {
                    result = new QualifiableNameNode(default(NameNode), name);
                }
                return true;
            }
            result = default(QualifiableNameNode);
            return false;
        }
        private QualifiableNameNode QualifiableNameExpected() {
            QualifiableNameNode qName;
            if (!QualifiableName(out qName)) {
                ErrorDiagAndThrow("Qualifiable name expected.");
            }
            return qName;
        }

    }
}
