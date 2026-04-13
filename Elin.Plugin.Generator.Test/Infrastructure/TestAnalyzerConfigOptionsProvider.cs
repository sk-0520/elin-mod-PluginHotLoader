using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;

namespace Elin.Plugin.Generator.Test.Infrastructure
{
    file sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        public TestAnalyzerConfigOptions(IReadOnlyDictionary<string, string> values)
        {
            Values = values;
        }

        #region property

        private IReadOnlyDictionary<string, string> Values { get; }

        #endregion

        #region AnalyzerConfigOptions

        public override bool TryGetValue(string key, out string value)
        {
            return Values.TryGetValue(key, out value!);
        }

        #endregion
    }

    internal sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        public TestAnalyzerConfigOptionsProvider(IReadOnlyDictionary<string, string> values)
        {
            GlobalOptions = new TestAnalyzerConfigOptions(values);
        }

        public override AnalyzerConfigOptions GlobalOptions { get; }

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return GlobalOptions;
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return GlobalOptions;
        }
    }
}
