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
            bin.Add(99);

            ////bin = bin.AsReadOnly(); 
            //bin.RemoveAt(2);
            //bin.Insert(0, 5);
            //bin.RemoveAt(2);
            //bin.Remove(5);
            //for (var i = 42; i < 50; ++i) {
            //    bin.Add((byte)i);
            //}
            bin.AddRange(new byte[] { 100,101,102,103,104,105,106,107}, 1, 5);
            bin.RemoveRange(1, 4);
            
        }
    }
}
