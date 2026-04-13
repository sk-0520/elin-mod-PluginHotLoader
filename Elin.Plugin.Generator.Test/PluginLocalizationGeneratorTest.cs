using Elin.Plugin.Generator.Test.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Elin.Plugin.Generator.Test
{
    public class PluginLocalizationGeneratorTest
    {
        #region property

        private string[] IgnorePropertyNames { get; } = new[] { "LanguageSystem", "Items" };
        private string CommonFormatArgumentType { get; } = "IReadOnlyDictionary<string, string>";

        #endregion
        #region function

        [Fact]
        public void EmptyTest()
        {
            var generator = new PluginLocalizationGenerator();
            var driver = CSharpGeneratorDriver.Create(
                [
                    generator.AsSourceGenerator()
                ],
                parseOptions: new CSharpParseOptions(LanguageVersion.Latest),
                additionalTexts: [
                    new TestAdditionalText(
                        @"Z:\NUL\Localization.json",
                        //lang=json,strict
                        """
                        {
                            "$schema": "./dev-items/localization.schema.json",
                            "general": {
                            },
                            "format": {
                            }
                        }
                        """
                    )
                ]
            );

            var inputCompilation = TestCompilation.Create<PluginLocalizationGenerator>();

            var generatorDriver = driver.RunGeneratorsAndUpdateCompilation(
                inputCompilation,
                out var outputCompilation,
                out var diagnostics,
                TestContext.Current.CancellationToken
            );
            var runResult = generatorDriver.GetRunResult();
            var generatorResult = runResult.Results.FirstOrDefault();

            Assert.True(diagnostics.IsEmpty);
            Assert.Null(generatorResult.Exception);

            // 1ファイルに全部出力される
            Assert.Single(generatorResult.GeneratedSources);
            var actualSource = generatorResult.GeneratedSources[0];
            Assert.Equal("Localization.g.cs", actualSource.HintName);

            var root = actualSource.SyntaxTree.GetRoot(TestContext.Current.CancellationToken);

            // ユーザー使用型とビルド時使用型の確認
            var classDeclarations = root
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .ToArray()
            ;
            Assert.Contains(classDeclarations, a => a.Identifier.Text == "PluginLocalizationItem");
            Assert.Contains(classDeclarations, a => a.Identifier.Text == "PluginLocalization");
            Assert.Contains(classDeclarations, a => a.Identifier.Text == "PluginLocalizationGeneral");
            Assert.Contains(classDeclarations, a => a.Identifier.Text == "PluginLocalizationFormatter");

            var interfaceDeclarations = root
                .DescendantNodes()
                .OfType<InterfaceDeclarationSyntax>()
                .ToArray()
            ;
            Assert.Contains(interfaceDeclarations, a => a.Identifier.Text == "ILanguageSystem");

            var actualGeneral = classDeclarations.First(a => a.Identifier.Text == "PluginLocalizationGeneral");
            var actualGeneralProps = actualGeneral
                .DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .Where(a => !IgnorePropertyNames.Contains(a.Identifier.ValueText))
            ;
            Assert.Empty(actualGeneralProps);

            var actualFormatter = classDeclarations.First(a => a.Identifier.Text == "PluginLocalizationFormatter");
            var actualFormatterProps = actualFormatter
                .DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .Where(a => !IgnorePropertyNames.Contains(a.Identifier.ValueText))
            ;
            Assert.Empty(actualFormatterProps);
        }

        [Fact]
        public void GeneralTest()
        {
            var generator = new PluginLocalizationGenerator();
            var driver = CSharpGeneratorDriver.Create(
                [
                    generator.AsSourceGenerator()
                ],
                parseOptions: new CSharpParseOptions(LanguageVersion.Latest),
                additionalTexts: [
                    new TestAdditionalText(
                        @"Z:\NUL\Localization.json",
                        //lang=json,strict
                        """
                        {
                            "$schema": "./dev-items/localization.schema.json",
                            "general": {
                                "abc": {
                                    "JP": "あいうえお",
                                    "EN": "abcde"
                                },
                                "localizeTest": {
                                    "JP": "テスト",
                                    "EN": "test"
                                }
                            },
                            "format": {
                            }
                        }
                        """
                    )
                ]
            );

            var inputCompilation = TestCompilation.Create<PluginLocalizationGenerator>();

            var generatorDriver = driver.RunGeneratorsAndUpdateCompilation(
                inputCompilation,
                out var outputCompilation,
                out var diagnostics,
                TestContext.Current.CancellationToken
            );
            var runResult = generatorDriver.GetRunResult();
            var generatorResult = runResult.Results.FirstOrDefault();

            Assert.True(diagnostics.IsEmpty);
            Assert.Null(generatorResult.Exception);

            var actualSource = generatorResult.GeneratedSources[0];
            var root = actualSource.SyntaxTree.GetRoot(TestContext.Current.CancellationToken);

            // ユーザー使用型とビルド時使用型の確認
            var classDeclarations = root
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .ToArray()
            ;

            var actualGeneral = classDeclarations.First(a => a.Identifier.Text == "PluginLocalizationGeneral");
            var actualGeneralProps = actualGeneral
                .DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .Where(a => !IgnorePropertyNames.Contains(a.Identifier.ValueText))
                .ToArray()
            ;

            Assert.Contains(actualGeneralProps, a => a.Identifier.ValueText == "Abc");
            Assert.Contains(actualGeneralProps, a => a.Identifier.ValueText == "LocalizeTest");
        }

        [Fact]
        public void FormatterTest()
        {
            var generator = new PluginLocalizationGenerator();
            var driver = CSharpGeneratorDriver.Create(
                [
                    generator.AsSourceGenerator()
                ],
                parseOptions: new CSharpParseOptions(LanguageVersion.Latest),
                additionalTexts: [
                    new TestAdditionalText(
                        @"Z:\NUL\Localization.json",
                        //lang=json,strict
                        """
                        {
                            "$schema": "./dev-items/localization.schema.json",
                            "general": {
                            },
                            "format": {
                                "result": {
                                    "JP": "結果：${RESULT}",
                                    "EN": "Result: ${RESULT}",
                                    "parameters": {
                                        "RESULT": {
                                            "type": "int"
                                        }
                                    }
                                },
                                "testName": {
                                    "JP": "テスト：${ARG-1} (${ARG_2})",
                                    "EN": "テスト：${ARG-1} (${ARG_2})",
                                    "parameters": {
                                        "ARG-1": {
                                            "type": "string"
                                        },
                                        "ARG_2": {
                                            "type": "int"
                                        }
                                    }
                                }
                            }
                        }
                        """
                    )
                ]
            );

            var inputCompilation = TestCompilation.Create<PluginLocalizationGenerator>();

            var generatorDriver = driver.RunGeneratorsAndUpdateCompilation(
                inputCompilation,
                out var outputCompilation,
                out var diagnostics,
                TestContext.Current.CancellationToken
            );
            var runResult = generatorDriver.GetRunResult();
            var generatorResult = runResult.Results.FirstOrDefault();

            Assert.True(diagnostics.IsEmpty);
            Assert.Null(generatorResult.Exception);

            var actualSource = generatorResult.GeneratedSources[0];
            var root = actualSource.SyntaxTree.GetRoot(TestContext.Current.CancellationToken);

            // ユーザー使用型とビルド時使用型の確認
            var classDeclarations = root
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .ToArray()
            ;

            var actualFormatter = classDeclarations.First(a => a.Identifier.Text == "PluginLocalizationFormatter");
            var actualFormatterProps = actualFormatter
                .DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .Where(a => !IgnorePropertyNames.Contains(a.Identifier.ValueText))
                .ToArray()
            ;

            Assert.Contains(actualFormatterProps, a => a.Identifier.ValueText == "Result");
            Assert.Contains(actualFormatterProps, a => a.Identifier.ValueText == "TestName");

            var actualFormatterMethods = actualFormatter
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .ToArray()
            ;

            // FormatResult
            var actualFormatResult = actualFormatterMethods.Where(a => a.Identifier.ValueText == "FormatResult").ToArray();
            Assert.Equal(2, actualFormatResult.Length);
            // 共通 辞書引数
            Assert.Contains(actualFormatResult, a => a.ParameterList.Parameters.Count == 1 && a.ParameterList.Parameters[0].Type!.ToString() == CommonFormatArgumentType);
            // 個別引数
            var actualFormatParams = actualFormatResult.First(a => !(a.ParameterList.Parameters.Count == 1 && a.ParameterList.Parameters[0].Type!.ToString() == CommonFormatArgumentType));
            Assert.Single(actualFormatParams.ParameterList.Parameters);
            Assert.Contains(actualFormatParams.ParameterList.Parameters, a => a.Type!.ToString() == "int" && a.Identifier.ValueText == "result");

            // FormatTestName
            var actualFormatTestName = actualFormatterMethods.Where(a => a.Identifier.ValueText == "FormatTestName").ToArray();
            Assert.Equal(2, actualFormatTestName.Length);
            // 共通 辞書引数
            Assert.Contains(actualFormatTestName, a => a.ParameterList.Parameters.Count == 1 && a.ParameterList.Parameters[0].Type!.ToString() == CommonFormatArgumentType);
            // 個別引数
            var actualFormatTestNameParams = actualFormatTestName.First(a => !(a.ParameterList.Parameters.Count == 1 && a.ParameterList.Parameters[0].Type!.ToString() == CommonFormatArgumentType));
            Assert.Equal(2, actualFormatTestNameParams.ParameterList.Parameters.Count);
            // JSONの辞書は並びを持たないので生成時の引数順序は保証できない
            Assert.Contains(actualFormatTestNameParams.ParameterList.Parameters, a => a.Type!.ToString() == "string" && a.Identifier.ValueText == "arg1");
            Assert.Contains(actualFormatTestNameParams.ParameterList.Parameters, a => a.Type!.ToString() == "int" && a.Identifier.ValueText == "arg2");
        }

        #endregion
    }
}
