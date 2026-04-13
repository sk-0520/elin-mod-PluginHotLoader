using Elin.Plugin.Generator.Test.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Elin.Plugin.Generator.Test
{
    public class PluginInfoGeneratorTest
    {
        #region function

        [Theory]
        [InlineData("Test")]
        [InlineData("AssemblyName")]
        public void BasicDebugTest(string assemblyName)
        {
            var generator = new PluginInfoGenerator();
            var driver = CSharpGeneratorDriver.Create(
                [
                    generator.AsSourceGenerator()
                ],
                parseOptions: new CSharpParseOptions(LanguageVersion.Latest)
                    .WithPreprocessorSymbols(ImmutableArray.Create("DEBUG"))
                ,
                additionalTexts: [
                    new TestAdditionalText(
                        @"Z:\NUL\Plugin.json",
                        //lang=json,strict
                        """
                        {
                            "$schema": "./dev-items/Plugin.schema.json",
                            "package": {
                                "title": "ユーザーに表示される Mod 名",
                                "id": "replace.with.your.mod.id",
                                "author": "作者",
                                "loadPriority": 100,
                                "elinVersion": "0.23.295",
                                "description": [
                                    "説明1行目",
                                    "説明2行目"
                                ],
                                "tags": [
                                    "General"
                                ]
                            },
                            "mod": {
                                "version": "0.0.0",
                                "useDebugId": true,
                                "log": "NUL"
                            }
                        }
                        """
                    )
                ],
                optionsProvider: new TestAnalyzerConfigOptionsProvider(new Dictionary<string, string>()
                {
                    ["build_property.AssemblyName"] = assemblyName
                })
            );

            var inputCompilation = TestCompilation.Create<PluginInfoGenerator>();

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
            Assert.Equal("PluginInfo.g.cs", actualSource.HintName);

            var root = actualSource.SyntaxTree.GetRoot(TestContext.Current.CancellationToken);

            // ユーザー使用型とビルド時使用型の確認
            var classDeclarations = root
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .ToArray()
            ;
            Assert.Contains(classDeclarations, a => a.Identifier.Text == "MsBuildOnlyPackageXml");
            Assert.Contains(classDeclarations, a => a.Identifier.Text == "Package");
            Assert.Contains(classDeclarations, a => a.Identifier.Text == "Mod");

            // Mod クラスの Name は渡されたアセンブリ名であること
            var actualMod = classDeclarations.First(a => a.Identifier.Text == "Mod");

            var nameField = actualMod
                .DescendantNodes()
                .OfType<FieldDeclarationSyntax>()
                .SelectMany(a => a.Declaration.Variables)
                .First(a => a.Identifier.ValueText == "Name")
            ;
            var nameFieldInitializer = nameField.Initializer;
            Assert.NotNull(nameFieldInitializer);
            var nameValue = nameFieldInitializer.Value as LiteralExpressionSyntax;
            Assert.NotNull(nameValue);
            Assert.Equal(assemblyName, nameValue.Token.ValueText);

            var logFileField = actualMod
                .DescendantNodes()
                .OfType<FieldDeclarationSyntax>()
                .SelectMany(a => a.Declaration.Variables)
                .First(a => a.Identifier.ValueText == "LogFile")
            ;
            var logFileFieldInitializer = logFileField.Initializer;
            Assert.NotNull(logFileFieldInitializer);
            var logFileValue = logFileFieldInitializer.Value as LiteralExpressionSyntax;
            Assert.NotNull(logFileValue);
            Assert.Equal("NUL", logFileValue.Token.ValueText);
        }

        [Fact]
        public void BasicReleaseTest()
        {
            var generator = new PluginInfoGenerator();
            var driver = CSharpGeneratorDriver.Create(
                [
                    generator.AsSourceGenerator()
                ],
                parseOptions: new CSharpParseOptions(LanguageVersion.Latest)
                ,
                additionalTexts: [
                    new TestAdditionalText(
                        @"Z:\NUL\Plugin.json",
                        //lang=json,strict
                        """
                        {
                            "$schema": "./dev-items/Plugin.schema.json",
                            "package": {
                                "title": "ユーザーに表示される Mod 名",
                                "id": "replace.with.your.mod.id",
                                "author": "作者",
                                "loadPriority": 100,
                                "elinVersion": "0.23.295",
                                "description": [
                                    "説明1行目",
                                    "説明2行目"
                                ],
                                "tags": [
                                    "General"
                                ]
                            },
                            "mod": {
                                "version": "0.0.0",
                                "useDebugId": true,
                                "log": "NUL"
                            }
                        }
                        """
                    )
                ],
                optionsProvider: new TestAnalyzerConfigOptionsProvider(new Dictionary<string, string>()
                {
                    ["build_property.AssemblyName"] = "AssemblyName"
                })
            );

            var inputCompilation = TestCompilation.Create<PluginInfoGenerator>();

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
            Assert.Equal("PluginInfo.g.cs", actualSource.HintName);

            var root = actualSource.SyntaxTree.GetRoot(TestContext.Current.CancellationToken);

            // ユーザー使用型とビルド時使用型の確認
            var classDeclarations = root
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .ToArray()
            ;
            Assert.Contains(classDeclarations, a => a.Identifier.Text == "MsBuildOnlyPackageXml");
            Assert.Contains(classDeclarations, a => a.Identifier.Text == "Package");
            Assert.Contains(classDeclarations, a => a.Identifier.Text == "Mod");

            var actualMod = classDeclarations.First(a => a.Identifier.Text == "Mod");

            // リリース版(DEBUG未定義)は LogFile フィールドが生成されない
            var logFileField = actualMod
                .DescendantNodes()
                .OfType<FieldDeclarationSyntax>()
                .SelectMany(a => a.Declaration.Variables)
                .FirstOrDefault(a => a.Identifier.ValueText == "LogFile")
            ;
            Assert.Null(logFileField);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Bad\\Name")]
        [InlineData("Bad/Name")]
        [InlineData("Bad:Name")]
        [InlineData("Bad*Name")]
        [InlineData("Bad?Name")]
        [InlineData("Bad\"Name")]
        [InlineData("Bad<Name")]
        [InlineData("Bad>Name")]
        [InlineData("Bad|Name")]
        public void InvalidAssemblyNameTest(string assemblyName)
        {
            var generator = new PluginInfoGenerator();
            var driver = CSharpGeneratorDriver.Create(
                [
                    generator.AsSourceGenerator()
                ],
                additionalTexts: [
                    new TestAdditionalText(
                        @"Z:\NUL\Plugin.json",
                        //lang=json,strict
                        """
                        {
                            "$schema": "./dev-items/Plugin.schema.json",
                            "package": {
                                "title": "ユーザーに表示される Mod 名",
                                "id": "replace.with.your.mod.id",
                                "author": "作者",
                                "loadPriority": 100,
                                "elinVersion": "0.23.295",
                                "description": [
                                    "説明1行目",
                                    "説明2行目"
                                ],
                                "tags": [
                                    "General"
                                ]
                            },
                            "mod": {
                                "version": "0.0.0",
                                "useDebugId": true,
                                "log": "NUL"
                            }
                        }
                        """
                    )
                ],
                optionsProvider: new TestAnalyzerConfigOptionsProvider(new Dictionary<string, string>()
                {
                    ["build_property.AssemblyName"] = assemblyName
                })
            );

            var inputCompilation = TestCompilation.Create<PluginInfoGenerator>();

            var generatorDriver = driver.RunGeneratorsAndUpdateCompilation(
                inputCompilation,
                out var outputCompilation,
                out var diagnostics,
                TestContext.Current.CancellationToken
            );
            var runResult = generatorDriver.GetRunResult();
            var generatorResult = runResult.Results.FirstOrDefault();

            Assert.False(diagnostics.IsEmpty);
            Assert.Null(generatorResult.Exception);

            // ジェネレータ側で EPG011 が報告されていること
            Assert.Contains(generatorResult.Diagnostics, d => d.Id == "EPG011");

            // 不正なため生成ソースは出力されないこと
            Assert.Empty(generatorResult.GeneratedSources);
        }

        [Fact]
        public void PluginDevJsonTest()
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var pluginDevJsonDirectoryPath = Path.Combine(Path.GetDirectoryName(assemblyLocation)!, nameof(PluginInfoGeneratorTest), nameof(PluginDevJsonTest));

            var generator = new PluginInfoGenerator();
            var driver = CSharpGeneratorDriver.Create(
                [
                    generator.AsSourceGenerator()
                ],
                parseOptions: new CSharpParseOptions(LanguageVersion.Latest)
                    .WithPreprocessorSymbols(ImmutableArray.Create("DEBUG"))
                ,
                additionalTexts: [
                    new TestAdditionalText(
                        Path.Combine(pluginDevJsonDirectoryPath, "Plugin.json"),
                        //lang=json,strict
                        """
                        {
                            "$schema": "./dev-items/Plugin.schema.json",
                            "package": {
                                "title": "ユーザーに表示される Mod 名",
                                "id": "replace.with.your.mod.id",
                                "author": "作者",
                                "loadPriority": 100,
                                "elinVersion": "0.23.295",
                                "description": [
                                    "説明1行目",
                                    "説明2行目"
                                ],
                                "tags": [
                                    "General"
                                ]
                            },
                            "mod": {
                                "version": "0.0.0",
                                "useDebugId": true,
                                "log": "NUL"
                            }
                        }
                        """
                    )
                ],
                optionsProvider: new TestAnalyzerConfigOptionsProvider(new Dictionary<string, string>()
                {
                    ["build_property.AssemblyName"] = "AssemblyName"
                })
            );

            var inputCompilation = TestCompilation.Create<PluginInfoGenerator>();

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
            Assert.Equal("PluginInfo.g.cs", actualSource.HintName);

            var root = actualSource.SyntaxTree.GetRoot(TestContext.Current.CancellationToken);

            // ユーザー使用型とビルド時使用型の確認
            var classDeclarations = root
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .ToArray()
            ;
            Assert.Contains(classDeclarations, a => a.Identifier.Text == "MsBuildOnlyPackageXml");
            Assert.Contains(classDeclarations, a => a.Identifier.Text == "Package");
            Assert.Contains(classDeclarations, a => a.Identifier.Text == "Mod");

            var actualMod = classDeclarations.First(a => a.Identifier.Text == "Mod");

            var logFileField = actualMod
                .DescendantNodes()
                .OfType<FieldDeclarationSyntax>()
                .SelectMany(a => a.Declaration.Variables)
                .First(a => a.Identifier.ValueText == "LogFile")
            ;
            var logFileFieldInitializer = logFileField.Initializer;
            Assert.NotNull(logFileFieldInitializer);
            var logFileValue = logFileFieldInitializer.Value as LiteralExpressionSyntax;
            Assert.NotNull(logFileValue);
            Assert.Equal("dev.log", logFileValue.Token.ValueText);
        }

        #endregion
    }
}
