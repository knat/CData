using System;
using System.Collections.Generic;
using System.Linq;
using CData;

//[assembly: UriNamespaceMap]
//[assembly: UriNamespaceMap]

namespace Test {
    class Program {
        static void Main(string[] args) {
            Binary bin = new Binary(new byte[] { 1, 2, 3 }, false);
            //bin = bin.AsReadOnly(); 
            bin.RemoveAt(2);
            bin.Insert(0, 5);
            bin.RemoveAt(2);
            bin.Remove(5);
            for (var i = 42; i < 50; ++i) {
                bin.Add((byte)i);
            }
        }
    }
}
