using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CData.Compiler {
    public static class CDataCompiler {
        public static bool Compile(List<string> contractFileList,
            List<string> csFileList, List<string> csPpList, List<MetadataReference> csRefList,
            out DiagContext context, out string code) {
            context = null;
            code = GeneratedFileBanner;
            var contractFileCount = contractFileList.CountOrZero();
            if (contractFileCount == 0) {
                return true;
            }
            try {
                context = new DiagContextEx();
                var cuList = new List<CompilationUnitNode>();
                foreach (var filePath in contractFileList) {
                    using (var reader = new StreamReader(filePath)) {
                        CompilationUnitNode cuNode;
                        if (Parser.Parse(filePath, reader, context, out cuNode)) {
                            cuList.Add(cuNode);
                        }
                        else {
                            return false;
                        }
                    }
                }
                return CompileCore((DiagContextEx)context, cuList, csFileList, csPpList, csRefList, ref code);
            }
            catch (Exception ex) {
                context.AddDiag(DiagSeverity.Error, (int)DiagCodeEx.InternalCompilerError, "Internal compiler error: " + ex.ToString(), default(TextSpan));
            }
            return false;
        }
        private const string GeneratedFileBanner = @"//
//Auto-generated, DO NOT EDIT.
//Visit https://github.com/knat/CData for more information.
//

";
        private static readonly CSharpCompilationOptions _compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        private static bool CompileCore(DiagContextEx context, List<CompilationUnitNode> cuList,
            List<string> csFileList, List<string> csPpList, List<MetadataReference> csRefList,
            ref string code) {
            try {
                DiagContextEx.Current = context;
                var nsList = new List<NamespaceNode>();
                if (cuList != null) {
                    foreach (var cu in cuList) {
                        if (cu.NamespaceList != null) {
                            nsList.AddRange(cu.NamespaceList);
                        }
                    }
                }
                if (nsList.Count == 0) {
                    return true;
                }
                //
                var nsMap = new LogicalNamespaceMap();
                foreach (var ns in nsList) {
                    var uri = ns.UriValue;
                    LogicalNamespace logicalNS;
                    if (!nsMap.TryGetValue(uri, out logicalNS)) {
                        logicalNS = new LogicalNamespace();
                        nsMap.Add(uri, logicalNS);
                    }
                    logicalNS.Add(ns);
                    ns.LogicalNamespace = logicalNS;
                }
                //
                foreach (var ns in nsList) {
                    ns.ResolveImports(nsMap);
                }
                foreach (var logicalNs in nsMap.Values) {
                    logicalNs.CheckDuplicateMembers();
                }
                foreach (var ns in nsList) {
                    ns.Resolve();
                }
                //
                foreach (var logicalNs in nsMap.Values) {
                    logicalNs.NamespaceInfo = new NamespaceInfo(logicalNs.Uri);
                }
                foreach (var ns in nsList) {
                    ns.CreateInfos();
                }
                //
                if (csFileList.CountOrZero() > 0) {
                    var csParseOpts = new CSharpParseOptions(preprocessorSymbols: csPpList, documentationMode: DocumentationMode.None);
                    var csCompilation = CSharpCompilation.Create(
                        assemblyName: "__TEMP__",
                        syntaxTrees: csFileList.Select(csFile => CSharpSyntaxTree.ParseText(text: File.ReadAllText(csFile),
                            options: csParseOpts, path: csFile)),
                        references: csRefList,
                        options: _compilationOptions);
                    List<IAssemblySymbol> refAssSymbolList = null;
                    if (csRefList != null) {
                        foreach (var csRef in csRefList) {
                            if (csRef.Properties.Kind == MetadataImageKind.Assembly) {
                                var assSymbol = csCompilation.GetAssemblyOrModuleSymbol(csRef) as IAssemblySymbol;
                                if (assSymbol != null) {
                                    if (CSEX.MapNamespaces(nsMap, assSymbol, false) > 0) {
                                        Extensions.CreateAndAdd(ref refAssSymbolList, assSymbol);
                                    }
                                }
                            }
                        }
                    }
                    var csCompilationAssSymbol = csCompilation.Assembly;
                    if (CSEX.MapNamespaces(nsMap, csCompilationAssSymbol, true) > 0) {
                        foreach (var logicalNs in nsMap.Values) {
                            if (logicalNs.CSFullName == null) {
                                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.ContractNamespaceAttributeRequired, logicalNs.Uri), default(TextSpan));
                            }
                        }
                        if (refAssSymbolList != null) {
                            foreach (var refAssSymbol in refAssSymbolList) {
                                CSEX.MapClasses(nsMap, refAssSymbol.GlobalNamespace);
                            }
                        }
                        CSEX.MapClasses(nsMap, csCompilationAssSymbol.GlobalNamespace);
                        foreach (var logicalNs in nsMap.Values) {
                            logicalNs.NamespaceInfo.SetMembersCSFullName();
                        }
                        foreach (var logicalNs in nsMap.Values) {
                            logicalNs.NamespaceInfo.MapAndCheckClassProperties();
                        }


                    }



                    //var needGenCode = false;


                    ////var ddd = csCompilation.GlobalNamespace;



                    //var globalNsSymbol = csCompilation.Assembly.GlobalNamespace;
                    //foreach (var attData in globalNsSymbol.GetAttributes()) {
                    //    if (attData.AttributeClass.FullNameEquals(ContractNamespaceAttributeNameParts)) {
                    //        var attCtorArgs = attData.ConstructorArguments;
                    //        var uri = (string)attCtorArgs[0].Value;
                    //        LogicalNamespace logicalNs;
                    //        if (uri != null && nsMap.TryGetValue(uri, out logicalNs)) {
                    //            if (logicalNs.CSNamespaceName != null) {
                    //                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.DuplicateContractNamespaceAttributeUri, uri), default(TextSpan));
                    //            }
                    //            var csns = (string)attCtorArgs[1].Value;
                    //            var csnsArr = CSEX.GetIdsBySplitDot(csns);
                    //            if (csnsArr == null) {
                    //                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractNamespaceAttributeCSharpNamespace, csns), default(TextSpan));
                    //            }
                    //            logicalNs.CSNamespaceName = new CSNamespaceNameNode(csnsArr);
                    //            logicalNs.IsCSNamespaceRef = (bool)attCtorArgs[2].Value;
                    //            needGenCode = true;
                    //        }
                    //        else {
                    //            DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidContractNamespaceAttribute, uri), default(TextSpan));
                    //        }
                    //    }
                    //}
                    //if (needGenCode) {
                    //    foreach (var logicalNs in nsMap.Values) {
                    //        if (logicalNs.CSNamespaceName == null) {
                    //        }
                    //    }
                    //    //foreach(var x in )


                    //}
                }



                //var needGenCode = indicatorList.Count > 0;
                //if (needGenCode) {
                //    var memberList = new List<MemberDeclarationSyntax>();
                //    foreach (var ns in nsSymbolList) {
                //        ns.Generate(memberList);
                //    }
                //    //>internal sealed class XDataProgramInfo : ProgramInfo {
                //    //>    private XDataProgramInfo() { }
                //    //>    public static readonly XDataProgramInfo Instance = new XDataProgramInfo();
                //    //>    protected override List<NamespaceInfo> GetNamespaces() {
                //    //>        return new List<NamespaceInfo>() {
                //    //>            ...
                //    //>        };
                //    //>    }
                //    //>}
                //    memberList.Add(CS.Class(null, CS.InternalSealedTokenList, "XDataProgramInfo", new[] { CSEX.ProgramInfoName },
                //        CS.Constructor(CS.PrivateTokenList, "XDataProgramInfo", null, null),
                //        CS.Field(CS.PublicStaticReadOnlyTokenList, CS.IdName("XDataProgramInfo"), "Instance", CS.NewObjExpr(CS.IdName("XDataProgramInfo"))),
                //        CS.Method(CS.ProtectedOverrideTokenList, CS.ListOf(CSEX.NamespaceInfoName), "GetNamespaces", null,
                //            CS.ReturnStm(CS.NewObjExpr(CS.ListOf(CSEX.NamespaceInfoName), null, nsSymbolList.Select(i => i.InfoExpr))))));
                //    code = GeneratedFileBanner + SyntaxFactory.CompilationUnit(default(SyntaxList<ExternAliasDirectiveSyntax>), default(SyntaxList<UsingDirectiveSyntax>), default(SyntaxList<AttributeListSyntax>),
                //          SyntaxFactory.List(memberList)).NormalizeWhitespace().ToString();
                //}
                return true;
            }
            catch (DiagContextEx.ContextException) { }
            return false;
        }


    }
}
