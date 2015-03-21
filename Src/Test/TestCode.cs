using System;
using System.Collections.Generic;
using System.Reflection;
[assembly: AssemblyTitle("fd")]


namespace NS1.NS2 {
    public partial class MyList<T> : List<T> {
        //public MyList(int i) { }
        public ICollection<int> Prop1;

    }
    partial class MyList<T> { }
}
