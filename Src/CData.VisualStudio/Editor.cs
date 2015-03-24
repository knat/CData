using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Language.StandardClassification;
using CData.Compiler;
using CData.MSBuild;

namespace CData.VisualStudio.Editors {
    internal static class ContentTypeDefinitions {
        //
        internal const string CDataContractContentType = "CDataContract";
        internal const string CDataContractFileExtension = ".cdc";
        [Export, BaseDefinition("code"), Name(CDataContractContentType)]
        internal static ContentTypeDefinition CDataContractContentTypeDefinition = null;
        [Export, ContentType(CDataContractContentType), FileExtension(CDataContractFileExtension)]
        internal static FileExtensionToContentTypeDefinition CDataContractFileExtensionDefinition = null;
    }

    [Export(typeof(IClassifierProvider)),
        ContentType(ContentTypeDefinitions.CDataContractContentType)]
    internal sealed class LanguageClassifierProvider : IClassifierProvider {
        [Import]
        internal IStandardClassificationService StandardService = null;
        public IClassifier GetClassifier(ITextBuffer textBuffer) {
            return textBuffer.Properties.GetOrCreateSingletonProperty<LanguageClassifier>(
                () => new LanguageClassifier(textBuffer, StandardService));
        }
    }
    internal sealed class LanguageClassifier : LanguageClassifierBase {
        internal LanguageClassifier(ITextBuffer textBuffer, IStandardClassificationService standardService)
            : base(textBuffer, standardService, ParserConstants.KeywordSet) {
        }
    }
    //
    //
    [Export(typeof(ITaggerProvider)), TagType(typeof(IErrorTag)),
        ContentType(ContentTypeDefinitions.CDataContractContentType)]
    internal sealed class LanguageErrorTaggerProvider : LanguageErrorTaggerProviderBase {
        internal LanguageErrorTaggerProvider()
            : base(DiagStore.FileName, DiagStore.TryLoad) {
        }
    }

}
