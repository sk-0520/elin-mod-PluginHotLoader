using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Elin.Plugin.Generator
{
    [Generator(LanguageNames.CSharp)]
    internal class RequireNamedArgumentsGenerator : IIncrementalGenerator
    {
        #region IIncrementalGenerator

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var sourceBuilder = new SourceBuilder();

            // RegisterPostInitializationOutput でやる必要あるんかとは思いつつ、動いてるからOK
            context.RegisterPostInitializationOutput(initContext =>
            {
                // 目印用の属性を生成するだけ
                var source = $$"""
                {{sourceBuilder.Header}}
                namespace {{GeneratorConstants.GeneratedNamespace}};

                [{{sourceBuilder.ToCode<System.AttributeUsageAttribute>()}}({{sourceBuilder.ToCode(AttributeTargets.Method)}}, AllowMultiple = false)]
                internal sealed class {{GeneratorConstants.RequireNamedArgumentsAttributeName}}: {{sourceBuilder.ToCode<System.Attribute>()}}
                {
                    public {{GeneratorConstants.RequireNamedArgumentsAttributeName}}()
                    {
                        //NOP
                    }
                }

                """;
                initContext.AddSource($"{GeneratorConstants.RequireNamedArgumentsAttributeName}.g.cs", source);
            });
        }

        #endregion
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class RequireNamedArgumentsDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        #region function

        private void AnalyzeContext(SyntaxNodeAnalysisContext context)
        {
            var syntax = (InvocationExpressionSyntax)context.Node;
            var semanticModel = context.SemanticModel;

            // 呼び出し式のシンボル情報を取得
            var symbolInfo = semanticModel.GetSymbolInfo(syntax.Expression);

            var methodSymbol = symbolInfo.Symbol as IMethodSymbol;

            // 確定していない場合は候補シンボルからメソッドシンボルを探す
            if (methodSymbol is null && symbolInfo.CandidateSymbols.Length > 0)
            {
                foreach (var candidate in symbolInfo.CandidateSymbols)
                {
                    methodSymbol = candidate as IMethodSymbol;
                    if (methodSymbol is not null)
                    {
                        break;
                    }
                }
            }

            if (methodSymbol is null)
            {
                // メソッド定義が見つからない（例えば不完全なコード等）
                return;
            }

            // メソッドに付与されている属性を取得
            var requireNamedArgumentsAttribute = methodSymbol
                .GetAttributes()
                .FirstOrDefault(a => a.AttributeClass!.ToDisplayString() == $"{GeneratorConstants.GeneratedNamespace}.{GeneratorConstants.RequireNamedArgumentsAttributeName}")
            ;

            if (requireNamedArgumentsAttribute is null)
            {
                return;
            }

            foreach (var argument in syntax.ArgumentList.Arguments)
            {
                if (argument.NameColon is null)
                {
                    // 名前付き引数でない引数がある場合は診断を報告
                    var diagnostic = Diagnostic.Create(DiagnosticDescriptors.EPG005, argument.GetLocation(), argument);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        #endregion

        #region DiagnosticAnalyzer

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.EPG005];

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeContext, SyntaxKind.InvocationExpression);
        }

        #endregion
    }
}
