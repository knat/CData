//
//Auto-generated, DO NOT EDIT.
//Visit https://github.com/knat/CData for more information.
//

[assembly: global::CData.__CompilerContractNamespaceAttribute("http://example.com/business", "Example.Business", "a0:MdNamespace <a0 = @\"urn:CData:Compiler\"> {\n\tGlobalTypeList = [\n\t\ta0:MdClass {\n\t\t\tName = @\"Person\"\n\t\t\tCSName = @\"Person\"\n\t\t\tPropertyList = [\n\t\t\t\ta0:MdProperty {\n\t\t\t\t\tName = @\"Id\"\n\t\t\t\t\tCSName = @\"Id\"\n\t\t\t\t\tIsCSProperty = true\n\t\t\t\t}\n\t\t\t\ta0:MdProperty {\n\t\t\t\t\tName = @\"Name\"\n\t\t\t\t\tCSName = @\"Name\"\n\t\t\t\t\tIsCSProperty = true\n\t\t\t\t}\n\t\t\t\ta0:MdProperty {\n\t\t\t\t\tName = @\"RegDate\"\n\t\t\t\t\tCSName = @\"RegDate\"\n\t\t\t\t\tIsCSProperty = true\n\t\t\t\t}\n\t\t\t]\n\t\t}\n\t\ta0:MdEnum {\n\t\t\tName = @\"Reputation\"\n\t\t\tCSName = @\"Reputation\"\n\t\t}\n\t\ta0:MdClass {\n\t\t\tName = @\"Order\"\n\t\t\tCSName = @\"Order\"\n\t\t\tPropertyList = [\n\t\t\t\ta0:MdProperty {\n\t\t\t\t\tName = @\"Amount\"\n\t\t\t\t\tCSName = @\"Amount\"\n\t\t\t\t\tIsCSProperty = true\n\t\t\t\t}\n\t\t\t\ta0:MdProperty {\n\t\t\t\t\tName = @\"IsUrgent\"\n\t\t\t\t\tCSName = @\"IsUrgent\"\n\t\t\t\t\tIsCSProperty = true\n\t\t\t\t}\n\t\t\t]\n\t\t}\n\t\ta0:MdClass {\n\t\t\tName = @\"Customer\"\n\t\t\tCSName = @\"Customer\"\n\t\t\tPropertyList = [\n\t\t\t\ta0:MdProperty {\n\t\t\t\t\tName = @\"Reputation\"\n\t\t\t\t\tCSName = @\"Reputation\"\n\t\t\t\t\tIsCSProperty = true\n\t\t\t\t}\n\t\t\t\ta0:MdProperty {\n\t\t\t\t\tName = @\"OrderList\"\n\t\t\t\t\tCSName = @\"OrderList\"\n\t\t\t\t\tIsCSProperty = true\n\t\t\t\t}\n\t\t\t]\n\t\t}\n\t\ta0:MdClass {\n\t\t\tName = @\"Supplier\"\n\t\t\tCSName = @\"Supplier\"\n\t\t\tPropertyList = [\n\t\t\t\ta0:MdProperty {\n\t\t\t\t\tName = @\"BankAccount\"\n\t\t\t\t\tCSName = @\"BankAccount\"\n\t\t\t\t\tIsCSProperty = true\n\t\t\t\t}\n\t\t\t\ta0:MdProperty {\n\t\t\t\t\tName = @\"ProductIdSet\"\n\t\t\t\t\tCSName = @\"ProductIdSet\"\n\t\t\t\t\tIsCSProperty = true\n\t\t\t\t}\n\t\t\t]\n\t\t}\n\t]\n}")]
[assembly: global::CData.__CompilerContractNamespaceAttribute("http://example.com/business/api", "Example.Business.API", "a0:MdNamespace <a0 = @\"urn:CData:Compiler\"> {\n\tGlobalTypeList = [\n\t\ta0:MdClass {\n\t\t\tName = @\"DataSet\"\n\t\t\tCSName = @\"DataSet\"\n\t\t\tPropertyList = [\n\t\t\t\ta0:MdProperty {\n\t\t\t\t\tName = @\"PersonMap\"\n\t\t\t\t\tCSName = @\"PersonMap\"\n\t\t\t\t\tIsCSProperty = true\n\t\t\t\t}\n\t\t\t]\n\t\t}\n\t]\n}")]
namespace Example.Business
{
    public abstract partial class Person
    {
        public int Id
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public global::System.DateTimeOffset RegDate
        {
            get;
            set;
        }

        public global::CData.TextSpan __TextSpan
        {
            get;
            private set;
        }

        public static bool TryLoad(string filePath, global::System.IO.TextReader reader, global::CData.DiagContext context, out global::Example.Business.Person result)
        {
            return global::CData.Serializer.TryLoad<global::Example.Business.Person>(filePath, reader, context, global::AssemblyMetadata_Biz.Instance, __ThisMetadata, out result);
        }

        public void Save(global::System.IO.TextWriter writer, string indentString = "\t", string newLineString = "\n")
        {
            global::CData.Serializer.Save(this, __Metadata, writer, indentString, newLineString);
        }

        public void Save(global::System.Text.StringBuilder stringBuilder, string indentString = "\t", string newLineString = "\n")
        {
            global::CData.Serializer.Save(this, __Metadata, stringBuilder, indentString, newLineString);
        }

        public static readonly global::CData.ClassMetadata __ThisMetadata = new global::CData.ClassMetadata(new global::CData.FullName("http://example.com/business", "Person"), true, null, new global::CData.PropertyMetadata[]
        {
        new global::CData.PropertyMetadata("Id", global::CData.GlobalTypeRefMetadata.GetAtom(global::CData.TypeKind.Int32, false), "Id", true), new global::CData.PropertyMetadata("Name", global::CData.GlobalTypeRefMetadata.GetAtom(global::CData.TypeKind.String, false), "Name", true), new global::CData.PropertyMetadata("RegDate", global::CData.GlobalTypeRefMetadata.GetAtom(global::CData.TypeKind.DateTimeOffset, false), "RegDate", true)}

        , typeof (global::Example.Business.Person));
        public virtual global::CData.ClassMetadata __Metadata
        {
            get
            {
                return __ThisMetadata;
            }
        }
    }

    public static partial class Reputation
    {
        public const int None = 0;
        public const int Bronze = 1;
        public const int Silver = 2;
        public const int Gold = 3;
        public const int Bad = -1;
        public static readonly global::CData.EnumMetadata __ThisMetadata = new global::CData.EnumMetadata(new global::CData.FullName("http://example.com/business", "Reputation"), new global::CData.NameValuePair[]
        {
        new global::CData.NameValuePair("None", None), new global::CData.NameValuePair("Bronze", Bronze), new global::CData.NameValuePair("Silver", Silver), new global::CData.NameValuePair("Gold", Gold), new global::CData.NameValuePair("Bad", Bad)}

        );
    }

    public partial class Order
    {
        public decimal Amount
        {
            get;
            set;
        }

        public bool IsUrgent
        {
            get;
            set;
        }

        public global::CData.TextSpan __TextSpan
        {
            get;
            private set;
        }

        public static bool TryLoad(string filePath, global::System.IO.TextReader reader, global::CData.DiagContext context, out global::Example.Business.Order result)
        {
            return global::CData.Serializer.TryLoad<global::Example.Business.Order>(filePath, reader, context, global::AssemblyMetadata_Biz.Instance, __ThisMetadata, out result);
        }

        public void Save(global::System.IO.TextWriter writer, string indentString = "\t", string newLineString = "\n")
        {
            global::CData.Serializer.Save(this, __Metadata, writer, indentString, newLineString);
        }

        public void Save(global::System.Text.StringBuilder stringBuilder, string indentString = "\t", string newLineString = "\n")
        {
            global::CData.Serializer.Save(this, __Metadata, stringBuilder, indentString, newLineString);
        }

        public static readonly global::CData.ClassMetadata __ThisMetadata = new global::CData.ClassMetadata(new global::CData.FullName("http://example.com/business", "Order"), false, null, new global::CData.PropertyMetadata[]
        {
        new global::CData.PropertyMetadata("Amount", global::CData.GlobalTypeRefMetadata.GetAtom(global::CData.TypeKind.Decimal, false), "Amount", true), new global::CData.PropertyMetadata("IsUrgent", global::CData.GlobalTypeRefMetadata.GetAtom(global::CData.TypeKind.Boolean, false), "IsUrgent", true)}

        , typeof (global::Example.Business.Order));
        public virtual global::CData.ClassMetadata __Metadata
        {
            get
            {
                return __ThisMetadata;
            }
        }
    }

    public partial class Customer : global::Example.Business.Person
    {
        public int Reputation
        {
            get;
            set;
        }

        public global::System.Collections.Generic.List<global::Example.Business.Order> OrderList
        {
            get;
            set;
        }

        public static bool TryLoad(string filePath, global::System.IO.TextReader reader, global::CData.DiagContext context, out global::Example.Business.Customer result)
        {
            return global::CData.Serializer.TryLoad<global::Example.Business.Customer>(filePath, reader, context, global::AssemblyMetadata_Biz.Instance, __ThisMetadata, out result);
        }

        new public static readonly global::CData.ClassMetadata __ThisMetadata = new global::CData.ClassMetadata(new global::CData.FullName("http://example.com/business", "Customer"), false, global::Example.Business.Person.__ThisMetadata, new global::CData.PropertyMetadata[]
        {
        new global::CData.PropertyMetadata("Reputation", new global::CData.GlobalTypeRefMetadata(global::Example.Business.Reputation.__ThisMetadata, false), "Reputation", true), new global::CData.PropertyMetadata("OrderList", new global::CData.CollectionMetadata(global::CData.TypeKind.List, true, new global::CData.GlobalTypeRefMetadata(global::Example.Business.Order.__ThisMetadata, false), null, null, typeof (global::System.Collections.Generic.List<global::Example.Business.Order>)), "OrderList", true)}

        , typeof (global::Example.Business.Customer));
        public override global::CData.ClassMetadata __Metadata
        {
            get
            {
                return __ThisMetadata;
            }
        }
    }

    public partial class Supplier : global::Example.Business.Person
    {
        public string BankAccount
        {
            get;
            set;
        }

        public global::System.Collections.Generic.HashSet<int> ProductIdSet
        {
            get;
            set;
        }

        public static bool TryLoad(string filePath, global::System.IO.TextReader reader, global::CData.DiagContext context, out global::Example.Business.Supplier result)
        {
            return global::CData.Serializer.TryLoad<global::Example.Business.Supplier>(filePath, reader, context, global::AssemblyMetadata_Biz.Instance, __ThisMetadata, out result);
        }

        new public static readonly global::CData.ClassMetadata __ThisMetadata = new global::CData.ClassMetadata(new global::CData.FullName("http://example.com/business", "Supplier"), false, global::Example.Business.Person.__ThisMetadata, new global::CData.PropertyMetadata[]
        {
        new global::CData.PropertyMetadata("BankAccount", global::CData.GlobalTypeRefMetadata.GetAtom(global::CData.TypeKind.String, false), "BankAccount", true), new global::CData.PropertyMetadata("ProductIdSet", new global::CData.CollectionMetadata(global::CData.TypeKind.SimpleSet, false, global::CData.GlobalTypeRefMetadata.GetAtom(global::CData.TypeKind.Int32, false), null, null, typeof (global::System.Collections.Generic.HashSet<int>)), "ProductIdSet", true)}

        , typeof (global::Example.Business.Supplier));
        public override global::CData.ClassMetadata __Metadata
        {
            get
            {
                return __ThisMetadata;
            }
        }
    }
}

namespace Example.Business.API
{
    public partial class DataSet
    {
        public global::System.Collections.Generic.Dictionary<int, global::Example.Business.Person> PersonMap
        {
            get;
            set;
        }

        public global::CData.TextSpan __TextSpan
        {
            get;
            private set;
        }

        public static bool TryLoad(string filePath, global::System.IO.TextReader reader, global::CData.DiagContext context, out global::Example.Business.API.DataSet result)
        {
            return global::CData.Serializer.TryLoad<global::Example.Business.API.DataSet>(filePath, reader, context, global::AssemblyMetadata_Biz.Instance, __ThisMetadata, out result);
        }

        public void Save(global::System.IO.TextWriter writer, string indentString = "\t", string newLineString = "\n")
        {
            global::CData.Serializer.Save(this, __Metadata, writer, indentString, newLineString);
        }

        public void Save(global::System.Text.StringBuilder stringBuilder, string indentString = "\t", string newLineString = "\n")
        {
            global::CData.Serializer.Save(this, __Metadata, stringBuilder, indentString, newLineString);
        }

        public static readonly global::CData.ClassMetadata __ThisMetadata = new global::CData.ClassMetadata(new global::CData.FullName("http://example.com/business/api", "DataSet"), false, null, new global::CData.PropertyMetadata[]
        {
        new global::CData.PropertyMetadata("PersonMap", new global::CData.CollectionMetadata(global::CData.TypeKind.Map, false, new global::CData.GlobalTypeRefMetadata(global::Example.Business.Person.__ThisMetadata, false), global::CData.GlobalTypeRefMetadata.GetAtom(global::CData.TypeKind.Int32, false), null, typeof (global::System.Collections.Generic.Dictionary<int, global::Example.Business.Person>)), "PersonMap", true)}

        , typeof (global::Example.Business.API.DataSet));
        public virtual global::CData.ClassMetadata __Metadata
        {
            get
            {
                return __ThisMetadata;
            }
        }
    }
}

public sealed class AssemblyMetadata_Biz : global::CData.AssemblyMetadata
{
    public static readonly global::CData.AssemblyMetadata Instance = new AssemblyMetadata_Biz(new global::CData.GlobalTypeMetadata[]
    {
    global::Example.Business.Person.__ThisMetadata, global::Example.Business.Reputation.__ThisMetadata, global::Example.Business.Order.__ThisMetadata, global::Example.Business.Customer.__ThisMetadata, global::Example.Business.Supplier.__ThisMetadata, global::Example.Business.API.DataSet.__ThisMetadata
    }

    );
    private AssemblyMetadata_Biz(global::CData.GlobalTypeMetadata[] globalTypes): base (globalTypes)
    {
    }
}