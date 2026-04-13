using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Reflection;

namespace Elin.Plugin.Generator.Test.Infrastructure
{
    internal static class TestCompilation
    {
        #region function

        public static CSharpCompilation Create<TGenerator>(IEnumerable<SyntaxTree>? syntaxTrees = null)
            where TGenerator : IIncrementalGenerator
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var inputCompilation = CSharpCompilation.Create(
                typeof(TGenerator).Assembly.FullName,
               syntaxTrees,
                [MetadataReference.CreateFromFile(assemblyLocation)],
                new CSharpCompilationOptions(OutputKind.ConsoleApplication)
            );

            return inputCompilation;
        }

        #endregion
    }
}
