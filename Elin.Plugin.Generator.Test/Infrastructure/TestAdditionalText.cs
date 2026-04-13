using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Threading;

namespace Elin.Plugin.Generator.Test.Infrastructure
{
    internal sealed class TestAdditionalText : AdditionalText
    {
        public TestAdditionalText(string path, string content)
        {
            Path = path;
            Content = content;
        }

        #region property

        private string Content { get; }

        #endregion

        #region AdditionalText

        public override string Path { get; }

        public override SourceText? GetText(CancellationToken cancellationToken = default)
        {
            return SourceText.From(Content);
        }

        #endregion
    }
}
