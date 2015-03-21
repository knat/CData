using System;
using System.Collections.Generic;
using System.Linq;
using CData;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using Microsoft.CodeAnalysis;

//[assembly: UriNamespaceMap]
//[assembly: UriNamespaceMap]

namespace Test {
    class Program {
        static void Main(string[] args) {
            //Test();
            RoslynTest();
        }
        static void Test() {
            var dd = "".Substring(1);
            var s = "".Split(new char[] { '.' } );
       var gs=     SyntaxFacts.IsValidIdentifier("int ");
        }

        private static readonly CSharpCompilationOptions _compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        static void RoslynTest() {

            var compilation = CSharpCompilation.Create(
                assemblyName: "__TEMP__",
                syntaxTrees: new[]{ CSharpSyntaxTree.ParseText(text: File.ReadAllText("TestCode.cs"),
                     path: "TestCode.cs")},
                references: new[] { MetadataReference.CreateFromAssembly(typeof(object).Assembly) },
                options: _compilationOptions);
            var ass = compilation.Assembly;
            var locs = ass.Locations;

            var atts = ass.GetAttributes();
            AttributeData attData = atts[0];
            var sref = attData.ApplicationSyntaxReference;
            SyntaxNode zzz = sref.GetSyntax();
            Location location = zzz.GetLocation();
            FileLinePositionSpan flps = location.GetLineSpan();

            foreach (var i in ass.GlobalNamespace.GetMembers()) {
                Console.WriteLine(i.Name);

            }
            Console.WriteLine("=====");
            var globalns = compilation.GlobalNamespace;
            atts = globalns.GetAttributes();

            foreach (var member in globalns.GetMembers()) {
                Console.WriteLine(member.Name);
            }


            var myList = compilation.GetTypeByMetadataName("NS1.NS2.MyList`1");
            locs = myList.Locations;

            //var sss = myList.ToDisplayParts(SymbolDisplayFormat.MinimallyQualifiedFormat);

            //var prop1 = myList.GetMembers("Prop1")[0];
            //var prop1Type = ((IFieldSymbol)prop1).Type;
            //var itfs = typeSymbol.AllInterfaces;
        }

        class Base { }
        class Derivied : Base { }
        static void XX() {
            ICollection<Derivied> f = new List<Derivied>();
            //Func<int>
            //ISet<int> f;
        }
    }
}
