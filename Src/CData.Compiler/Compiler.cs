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
            List<string> csFileList, List<string> csPpList, List<MetadataReference> csRefList, string assemblyName,
            out DiagContext context, out string code) {
            if (contractFileList == null) throw new ArgumentNullException("contractFileList");
            if (csFileList == null) throw new ArgumentNullException("csFileList");
            if (csPpList == null) throw new ArgumentNullException("csPpList");
            if (csRefList == null) throw new ArgumentNullException("csRefList");
            if (string.IsNullOrEmpty(assemblyName)) throw new ArgumentNullException("assemblyName");

            context = null;
            code = GeneratedFileBanner;
            if (contractFileList.Count == 0) {
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
                return CompileCore((DiagContextEx)context, cuList, csFileList, csPpList, csRefList, assemblyName, ref code);
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
            List<string> csFileList, List<string> csPpList, List<MetadataReference> csRefList, string assemblyName, ref string code) {
            try {
                DiagContextEx.Current = context;
                var nsList = new List<NamespaceNode>();
                foreach (var cu in cuList) {
                    nsList.AddRange(cu.NamespaceList);
                }
                if (nsList.Count == 0) {
                    return true;
                }
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
                foreach (var ns in nsList) {
                    ns.ResolveImports(nsMap);
                }
                foreach (var logicalNs in nsMap.Values) {
                    logicalNs.CheckDuplicateGlobalTypes();
                }
                foreach (var ns in nsList) {
                    ns.Resolve();
                }
                foreach (var logicalNs in nsMap.Values) {
                    logicalNs.NamespaceInfo = new NamespaceInfo(logicalNs.Uri);
                }
                foreach (var ns in nsList) {
                    ns.CreateInfos();
                }
                //
                if (csFileList.Count > 0) {
                    var parseOpts = new CSharpParseOptions(preprocessorSymbols: csPpList, documentationMode: DocumentationMode.None);
                    var compilation = CSharpCompilation.Create(
                        assemblyName: "__TEMP__",
                        syntaxTrees: csFileList.Select(csFile => CSharpSyntaxTree.ParseText(text: File.ReadAllText(csFile), options: parseOpts, path: csFile)),
                        references: csRefList,
                        options: _compilationOptions);
                    foreach (var csRef in csRefList) {
                        if (csRef.Properties.Kind == MetadataImageKind.Assembly) {
                            var assSymbol = compilation.GetAssemblyOrModuleSymbol(csRef) as IAssemblySymbol;
                            if (assSymbol != null) {
                                EX.MapNamespaces(nsMap, assSymbol, true);
                            }
                        }
                    }
                    var compilationAssSymbol = compilation.Assembly;
                    if (EX.MapNamespaces(nsMap, compilationAssSymbol, false) > 0) {
                        foreach (var logicalNs in nsMap.Values) {
                            if (logicalNs.DottedName == null) {
                                DiagContextEx.ErrorDiagAndThrow(new DiagMsgEx(DiagCodeEx.ContractNamespaceAttributeRequired, logicalNs.Uri), default(TextSpan));
                            }
                        }
                        EX.MapClasses(nsMap, compilationAssSymbol.GlobalNamespace);
                        foreach (var logicalNs in nsMap.Values) {
                            logicalNs.NamespaceInfo.SetGlobalTypeDottedNames();
                        }
                        foreach (var logicalNs in nsMap.Values) {
                            logicalNs.NamespaceInfo.MapGlobalTypeMembers();
                        }
                        List<AttributeListSyntax> cuCompierAttList = new List<AttributeListSyntax>();
                        List<MemberDeclarationSyntax> cuMemberSyntaxList = new List<MemberDeclarationSyntax>();
                        List<ExpressionSyntax> globalTypeMdSyntaxList = new List<ExpressionSyntax>();
                        var userAssemblyMetadataName = EX.UserAssemblyMetadataName(assemblyName);
                        var assMdExpr = CS.MemberAccessExpr(CS.GlobalAliasQualifiedName(userAssemblyMetadataName), "Instance");
                        foreach (var logicalNs in nsMap.Values) {
                            var nsInfo = logicalNs.NamespaceInfo;
                            string uri, csns;
                            var mdns = nsInfo.GetMdNamespace(out uri, out csns);
                            if (mdns != null) {
                                var sb = Extensions.AcquireStringBuilder();
                                mdns.Save(sb);
                                var data = sb.ToStringAndRelease();
                                cuCompierAttList.Add(CS.AttributeList("assembly", EX.__CompilerContractNamespaceAttributeName,
                                    SyntaxFactory.AttributeArgument(CS.Literal(uri)),
                                    SyntaxFactory.AttributeArgument(CS.Literal(csns)),
                                    SyntaxFactory.AttributeArgument(CS.Literal(data))));
                            }
                            nsInfo.GetSyntax(cuMemberSyntaxList, assMdExpr, globalTypeMdSyntaxList);
                        }
                        if (globalTypeMdSyntaxList.Count > 0) {
                            //>public sealed class AssemblyMetadata_XX : AssemblyMetadata {
                            //>  public static readonly AssemblyMetadata Instance = new AssemblyMetadata_XX(new GlobalTypeMetadata[]{ ... });
                            //>  private AssemblyMetadata_XX(GlobalTypeMetadata[] globalTypes):base(globalTypes) { }
                            //>}
                            cuMemberSyntaxList.Add(CS.Class(null, CS.PublicSealedTokenList, userAssemblyMetadataName, new[] { EX.AssemblyMetadataName },
                                CS.Field(CS.PublicStaticReadOnlyTokenList, EX.AssemblyMetadataName, "Instance",
                                    CS.NewObjExpr(CS.IdName(userAssemblyMetadataName), CS.NewArrExpr(EX.GlobalTypeMetadataArrayType, globalTypeMdSyntaxList))),
                                CS.Constructor(CS.PrivateTokenList, userAssemblyMetadataName,
                                    new[] { CS.Parameter(EX.GlobalTypeMetadataArrayType, "globalTypes") },
                                    CS.ConstructorInitializer(true, CS.IdName("globalTypes")))
                                ));
                        }
                        code = GeneratedFileBanner +
                            SyntaxFactory.CompilationUnit(default(SyntaxList<ExternAliasDirectiveSyntax>), default(SyntaxList<UsingDirectiveSyntax>),
                                SyntaxFactory.List(cuCompierAttList), SyntaxFactory.List(cuMemberSyntaxList)).NormalizeWhitespace().ToString();
                    }
                }
                return true;
            }
            catch (DiagContextEx.ContextException) { }
            return false;
        }


    }
}
