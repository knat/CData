//
//Auto-generated, DO NOT EDIT.
//Visit https://github.com/knat/CData for more information.
//

namespace CData.Compiler {
    public abstract partial class MdGlobalType {
        public global::CData.TextSpan __TextSpan {
            get;
            private set;
        }

        public static bool TryLoad(string filePath, global::System.IO.TextReader reader, global::CData.DiagContext context, out global::CData.Compiler.MdGlobalType result) {
            return global::CData.Serializer.TryLoad<global::CData.Compiler.MdGlobalType>(filePath, reader, context, global::AssemblyMetadata_CData_Compiler.Instance, __ThisMetadata, out result);
        }

        public void Save(global::System.IO.TextWriter writer, string indentString = "\t", string newLineString = "\n") {
            global::CData.Serializer.Save(this, __Metadata, writer, indentString, newLineString);
        }

        public void Save(global::System.Text.StringBuilder stringBuilder, string indentString = "\t", string newLineString = "\n") {
            global::CData.Serializer.Save(this, __Metadata, stringBuilder, indentString, newLineString);
        }

        public static readonly global::CData.ClassMetadata __ThisMetadata = new global::CData.ClassMetadata(new global::CData.FullName("urn:CData:Compiler", "MdGlobalType"), true, null, new global::CData.PropertyMetadata[]
        {
        new global::CData.PropertyMetadata("Name", global::CData.GlobalTypeRefMetadata.GetAtom(global::CData.TypeKind.String, false), "Name", false), new global::CData.PropertyMetadata("CSName", global::CData.GlobalTypeRefMetadata.GetAtom(global::CData.TypeKind.String, false), "CSName", false)}

        , typeof(global::CData.Compiler.MdGlobalType));
        public virtual global::CData.ClassMetadata __Metadata {
            get {
                return __ThisMetadata;
            }
        }
    }

    public partial class MdNamespace {
        public global::CData.TextSpan __TextSpan {
            get;
            private set;
        }

        public static bool TryLoad(string filePath, global::System.IO.TextReader reader, global::CData.DiagContext context, out global::CData.Compiler.MdNamespace result) {
            return global::CData.Serializer.TryLoad<global::CData.Compiler.MdNamespace>(filePath, reader, context, global::AssemblyMetadata_CData_Compiler.Instance, __ThisMetadata, out result);
        }

        public void Save(global::System.IO.TextWriter writer, string indentString = "\t", string newLineString = "\n") {
            global::CData.Serializer.Save(this, __Metadata, writer, indentString, newLineString);
        }

        public void Save(global::System.Text.StringBuilder stringBuilder, string indentString = "\t", string newLineString = "\n") {
            global::CData.Serializer.Save(this, __Metadata, stringBuilder, indentString, newLineString);
        }

        public static readonly global::CData.ClassMetadata __ThisMetadata = new global::CData.ClassMetadata(new global::CData.FullName("urn:CData:Compiler", "MdNamespace"), false, null, new global::CData.PropertyMetadata[]
        {
        new global::CData.PropertyMetadata("GlobalTypeList", new global::CData.CollectionMetadata(global::CData.TypeKind.List, false, new global::CData.GlobalTypeRefMetadata(global::CData.Compiler.MdGlobalType.__ThisMetadata, false), null, null, typeof (global::System.Collections.Generic.List<global::CData.Compiler.MdGlobalType>)), "GlobalTypeList", false)}

        , typeof(global::CData.Compiler.MdNamespace));
        public virtual global::CData.ClassMetadata __Metadata {
            get {
                return __ThisMetadata;
            }
        }
    }

    public partial class MdEnum : global::CData.Compiler.MdGlobalType {
        public static bool TryLoad(string filePath, global::System.IO.TextReader reader, global::CData.DiagContext context, out global::CData.Compiler.MdEnum result) {
            return global::CData.Serializer.TryLoad<global::CData.Compiler.MdEnum>(filePath, reader, context, global::AssemblyMetadata_CData_Compiler.Instance, __ThisMetadata, out result);
        }

        new public static readonly global::CData.ClassMetadata __ThisMetadata = new global::CData.ClassMetadata(new global::CData.FullName("urn:CData:Compiler", "MdEnum"), false, global::CData.Compiler.MdGlobalType.__ThisMetadata, null, typeof(global::CData.Compiler.MdEnum));
        public override global::CData.ClassMetadata __Metadata {
            get {
                return __ThisMetadata;
            }
        }
    }

    public partial class MdProperty {
        public global::CData.TextSpan __TextSpan {
            get;
            private set;
        }

        public static bool TryLoad(string filePath, global::System.IO.TextReader reader, global::CData.DiagContext context, out global::CData.Compiler.MdProperty result) {
            return global::CData.Serializer.TryLoad<global::CData.Compiler.MdProperty>(filePath, reader, context, global::AssemblyMetadata_CData_Compiler.Instance, __ThisMetadata, out result);
        }

        public void Save(global::System.IO.TextWriter writer, string indentString = "\t", string newLineString = "\n") {
            global::CData.Serializer.Save(this, __Metadata, writer, indentString, newLineString);
        }

        public void Save(global::System.Text.StringBuilder stringBuilder, string indentString = "\t", string newLineString = "\n") {
            global::CData.Serializer.Save(this, __Metadata, stringBuilder, indentString, newLineString);
        }

        public static readonly global::CData.ClassMetadata __ThisMetadata = new global::CData.ClassMetadata(new global::CData.FullName("urn:CData:Compiler", "MdProperty"), false, null, new global::CData.PropertyMetadata[]
        {
        new global::CData.PropertyMetadata("Name", global::CData.GlobalTypeRefMetadata.GetAtom(global::CData.TypeKind.String, false), "Name", false), new global::CData.PropertyMetadata("CSName", global::CData.GlobalTypeRefMetadata.GetAtom(global::CData.TypeKind.String, false), "CSName", false), new global::CData.PropertyMetadata("IsCSProperty", global::CData.GlobalTypeRefMetadata.GetAtom(global::CData.TypeKind.Boolean, false), "IsCSProperty", false)}

        , typeof(global::CData.Compiler.MdProperty));
        public virtual global::CData.ClassMetadata __Metadata {
            get {
                return __ThisMetadata;
            }
        }
    }

    public partial class MdClass : global::CData.Compiler.MdGlobalType {
        public static bool TryLoad(string filePath, global::System.IO.TextReader reader, global::CData.DiagContext context, out global::CData.Compiler.MdClass result) {
            return global::CData.Serializer.TryLoad<global::CData.Compiler.MdClass>(filePath, reader, context, global::AssemblyMetadata_CData_Compiler.Instance, __ThisMetadata, out result);
        }

        new public static readonly global::CData.ClassMetadata __ThisMetadata = new global::CData.ClassMetadata(new global::CData.FullName("urn:CData:Compiler", "MdClass"), false, global::CData.Compiler.MdGlobalType.__ThisMetadata, new global::CData.PropertyMetadata[]
        {
        new global::CData.PropertyMetadata("PropertyList", new global::CData.CollectionMetadata(global::CData.TypeKind.List, false, new global::CData.GlobalTypeRefMetadata(global::CData.Compiler.MdProperty.__ThisMetadata, false), null, null, typeof (global::System.Collections.Generic.List<global::CData.Compiler.MdProperty>)), "PropertyList", false)}

        , typeof(global::CData.Compiler.MdClass));
        public override global::CData.ClassMetadata __Metadata {
            get {
                return __ThisMetadata;
            }
        }
    }
}

public sealed class AssemblyMetadata_CData_Compiler : global::CData.ProgramMetadata {
    public static readonly global::CData.ProgramMetadata Instance = new AssemblyMetadata_CData_Compiler(new global::CData.GlobalTypeMetadata[]
    {
    global::CData.Compiler.MdGlobalType.__ThisMetadata, global::CData.Compiler.MdNamespace.__ThisMetadata, global::CData.Compiler.MdEnum.__ThisMetadata, global::CData.Compiler.MdProperty.__ThisMetadata, global::CData.Compiler.MdClass.__ThisMetadata
    }

    );
    private AssemblyMetadata_CData_Compiler(global::CData.GlobalTypeMetadata[] globalTypes)
        : base() {
    }
}