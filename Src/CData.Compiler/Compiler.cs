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
            List<string> csFileList, List<string> ppList, List<MetadataReference> refList,
            out DiagContext context, out string code) {
            context = null;
            code = null;
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


                if (!CompileCore((DiagContextEx)context, cuList, out code)) {
                    return false;
                }
                return true;
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
        private static bool CompileCore(DiagContextEx context, List<CompilationUnitNode> cuList,
            out string code) {
            code = null;
            DiagContextEx.Current = context;
            try {
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
                //if (indicatorList.Count > 0) {
                //    foreach (var indicator in indicatorList) {
                //        LogicalNamespace logicalNS;
                //        if (!nsSet.TryGetValue(indicator.Uri, out logicalNS)) {
                //            DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.InvalidNamespaceReference, indicator.Uri),
                //                indicator.UriNode.TextSpan);
                //        }
                //        if (logicalNS.CSharpNamespaceName == null) {
                //            logicalNS.CSharpNamespaceName = indicator.CSharpNamespaceName;
                //            logicalNS.IsCSharpNamespaceRef = indicator.IsRef;
                //        }
                //        else {
                //            DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.DuplicateIndicator, indicator.Uri),
                //                indicator.UriNode.TextSpan);
                //        }
                //    }
                //    foreach (var logicalNS in nsSet.Values) {
                //        if (logicalNS.CSharpNamespaceName == null) {
                //            DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.IndicatorRequiredForNamespace, logicalNS.Uri),
                //                indicatorList[0].TextSpan);
                //        }
                //    }
                //}
                //else {
                //    var idx = 0;
                //    foreach (var logicalNS in nsSet.Values) {
                //        logicalNS.CSharpNamespaceName = new CSharpNamespaceNameNode() { "__fake_ns__" + (idx++).ToInvString() };
                //        logicalNS.IsCSharpNamespaceRef = true;
                //    }
                //}
                //
                foreach (var ns in nsList) {
                    ns.ResolveImports(nsMap);
                }
                foreach (var logicalNS in nsMap.Values) {
                    logicalNS.CheckDuplicateMembers();
                }
                foreach (var ns in nsList) {
                    ns.Resolve();
                }
                //
                //var nsSymbolList = new List<NamespaceSymbol>();
                //foreach (var logicalNS in nsMap.Values) {
                //    logicalNS.NamespaceSymbol = new NamespaceSymbol(logicalNS.Uri, logicalNS.CSharpNamespaceName, logicalNS.IsCSharpNamespaceRef);
                //    nsSymbolList.Add(logicalNS.NamespaceSymbol);
                //}
                //foreach (var ns in nsList) {
                //    ns.CreateSymbols();
                //}
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
