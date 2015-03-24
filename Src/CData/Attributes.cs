using System;

namespace CData {
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class ContractNamespaceAttribute : Attribute {
        public ContractNamespaceAttribute(string uri, string namespaceName) { }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ContractClassAttribute : Attribute {
        public ContractClassAttribute(string name) { }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class ContractPropertyAttribute : Attribute {
        public ContractPropertyAttribute(string name) { }
    }

}
