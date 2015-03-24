using System;
using System.Collections.Generic;
using System.Linq;
using CData;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using Microsoft.CodeAnalysis;
using System.Reflection;

//[assembly: UriNamespaceMap]
//[assembly: UriNamespaceMap]
[assembly: ContractTypesAttribute(new Type[] { typeof(int), typeof(string) })]


class X {
    static X() {
        Console.WriteLine("X");
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(ZZZ).TypeHandle);
    }
}



internal class ZZZ {
    public static X x = new X();
    static ZZZ() {
        Console.WriteLine("ZZZ");
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(X).TypeHandle);
    }
}

namespace Test {
    class Program {
        static void Main(string[] args) {
            Test();
            //RoslynTest();
            //Console.WriteLine("fd");
        }
        public enum MyEnum  {
            M1 = 1,
            M2 = 10,
        }
        public  class MyClass {
            //public const string @int = "fd";
            //public static readonly DateTime M2 = DateTime.Now;
            public MyEnum Prop1 { get; set; }
        }
        static void Test() {
            var prop1 = typeof(MyClass).GetTypeInfo().GetProperty("Prop1");
            var myclass = new MyClass();
            prop1.SetValue(myclass, 1);
            //var o1 = fis[0].GetValue(null);
            //var o2 = fis[1].GetValue(null);

            //var name = Enum.GetName(typeof(MyEnum), 16);
            //object o = MyEnum.M2;
            //var tm1 = o.GetType();
            ////var ss = ff.ToString();

            //var ti = typeof(ObjectSet<int, string>).GetTypeInfo();
            //var add = ti.GetDeclaredMethod("Add");

            //var ass1 = typeof(Program).Assembly;
            //var ass2 = typeof(ZZZ).Assembly;
            //var e = ass1 == ass2;

            //var s = typeof(ZZZ);
            //System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(ZZZ).TypeHandle);
            //System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(ZZZ).TypeHandle);
            //System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(X).TypeHandle);

            //System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(ZZZ).TypeHandle);
            //System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(ZZZ).TypeHandle);

            //AppDomain currentDomain = AppDomain.CurrentDomain;
            //currentDomain.AssemblyLoad += new AssemblyLoadEventHandler(MyAssemblyLoadEventHandler);
            //PrintLoadedAssemblies(currentDomain);
            //     var dd = "".Substring(1);
            //     var s = "".Split(new char[] { '.' } );
            //var gs=     SyntaxFacts.IsValidIdentifier("int ");
        }
        static void PrintLoadedAssemblies(AppDomain domain) {
            Console.WriteLine("LOADED ASSEMBLIES:");
            foreach (Assembly a in domain.GetAssemblies()) {
                Console.WriteLine(a.FullName);
            }
            Console.WriteLine();
        }
        static void MyAssemblyLoadEventHandler(object sender, AssemblyLoadEventArgs args) {
            Console.WriteLine("ASSEMBLY LOADED: " + args.LoadedAssembly.FullName);
            Console.WriteLine();
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
            //AppDomain
            //Func<int>
            //ISet<int> f;
        }
    }
}
