using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Elin.Plugin.Generator
{
    [Generator(LanguageNames.CSharp)]
    internal class PluginConfigGenerator : IIncrementalGenerator
    {
        #region property

        private HashSet<string> GeneratedClassNames { get; } = new HashSet<string>();

        #endregion

        #region function

        private bool IsConfigTarget(ISymbol symbol)
        {
            if (symbol.IsImplicitlyDeclared)
            {
                return false;
            }

            if (symbol.IsStatic)
            {
                return false;
            }

            if (symbol.Kind != SymbolKind.Property)
            {
                return false;
            }

            var attributes = symbol.GetAttributes();
            if (attributes.Any(a => a.AttributeClass!.ToDisplayString() == $"{GeneratorConstants.GeneratedNamespace}.IgnorePluginConfigAttribute"))
            {
                return false;
            }

            return true;
        }

        private bool IsSupportedType(ITypeSymbol type)
        {
            if (type.TypeKind == TypeKind.Array)
            {
                return false;
            }

            var cliTypes = new[]
            {
                SpecialType.System_String,
                SpecialType.System_Boolean,
                SpecialType.System_Byte,
                SpecialType.System_SByte,
                SpecialType.System_Int16,
                SpecialType.System_UInt16,
                SpecialType.System_Int32,
                SpecialType.System_UInt32,
                SpecialType.System_Int64,
                SpecialType.System_UInt64,
                SpecialType.System_Single,
                SpecialType.System_Double,
                SpecialType.System_Decimal,
                SpecialType.System_Enum,
            };

            if (cliTypes.Contains(type.SpecialType))
            {
                return true;
            }

            var unityTypes = new[]
            {
                "UnityEngine.Color",
                "UnityEngine.Vector2",
                "UnityEngine.Vector3",
                "UnityEngine.Vector4",
                "UnityEngine.Quaternion",
            };

            if (unityTypes.Contains(type.ToDisplayString()))
            {
                return true;
            }

            return false;
        }

        private bool IsProxyTarget(IPropertySymbol symbol)
        {
            return symbol.IsVirtual
                &&
                (
                    symbol.Type.TypeKind == TypeKind.Struct
                    ||
                    symbol.Type.SpecialType == SpecialType.System_String
                )
                ;
        }

        private static bool IsAssignable(Compilation compilation, ITypeSymbol target, ITypeSymbol source)
        {
            if (SymbolEqualityComparer.Default.Equals(target, source))
            {
                return true;
            }

            // コンパイル時の変換情報を使って、source -> target の暗黙変換が存在するか調べる
            var conversion = compilation.ClassifyConversion(source, target);
            return conversion.IsImplicit;
        }

        private string ToEntriesClassName(ISymbol targetSymbol)
        {
            return $"{targetSymbol.Name}ConfigEntries";
        }

        private string ToProxyClassName(ISymbol targetSymbol)
        {
            return $"{targetSymbol.Name}Proxy";
        }

        private string ToEntriesCreateMethodName(ISymbol symbol)
        {
            return $"Create{ToEntriesClassName(symbol)}";
        }

        private List<string> ExpandDocumentCommentCore(XElement element)
        {
            var result = new List<string>();

            foreach (var node in element.Nodes())
            {
                if (node.NodeType == System.Xml.XmlNodeType.Text)
                {
                    var textNode = (XText)node;
                    result.Add(textNode.Value.Trim());
                }
                else if (node.NodeType == System.Xml.XmlNodeType.Element)
                {
                    var childElement = (XElement)node;

                    if (childElement.Name == "see")
                    {
                        var cref = childElement.Attribute("cref")!.Value;
                        var index = cref.LastIndexOf('.');
                        var value = index == -1 ? cref : cref.Substring(index + 1);
                        result.Add(value);
                    }
                    else if (childElement.Name == "para")
                    {
                        result.Add(Environment.NewLine);
                        var results = ExpandDocumentCommentCore(childElement);
                        result.AddRange(results);
                    }
                    else
                    {
                        var results = ExpandDocumentCommentCore(childElement);
                        result.AddRange(results);
                    }
                }
                else
                {
                    result.Add(node.ToString());
                }
            }

            return result;
        }

        private string? ExpandDocumentComment(XElement? element)
        {
            if (element is null)
            {
                return null;
            }

            return string.Join(
                string.Empty,
                ExpandDocumentCommentCore(element)
            );
        }

        private string? GetDocumentComment(ISymbol symbol)
        {
            var xmlComment = symbol.GetDocumentationCommentXml(expandIncludes: true);

            if (!string.IsNullOrWhiteSpace(xmlComment))
            {
                var doc = XDocument.Parse(xmlComment);
                var items = new[]
                {
                    ExpandDocumentComment(doc.Root!.Element("summary")),
                    ExpandDocumentComment(doc.Root!.Element("remarks")),
                };
                return string.Join(
                    Environment.NewLine,
                    items
                        .Where(a => !string.IsNullOrEmpty(a))
                        .Select(a => a!.Trim())
                );
            }

            return xmlComment!;
        }

        private string? GetAcceptableValue(SourceProductionContext context, Compilation compilation, SourceBuilder sourceBuilder, IPropertySymbol symbol)
        {
            var attributes = symbol.GetAttributes();

            var rangePluginConfigAttribute = attributes.FirstOrDefault(a => a.AttributeClass!.ToDisplayString() == $"{GeneratorConstants.GeneratedNamespace}.RangePluginConfigAttribute");
            if (rangePluginConfigAttribute != null)
            {
                var args = rangePluginConfigAttribute.ConstructorArguments;
                if (!IsAssignable(compilation, symbol.Type, args[0].Type!))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.EPG004,
                        sourceBuilder.GetLocation(symbol), //TODO: プロパティじゃなくて引数の位置を指定したい
                        symbol.ToDisplayString(), args[0].Type!.ToDisplayString()
                    ));
                    return null;
                }
                return $"new AcceptableValueRange<{args[0].Type}>({string.Join(", ", args.Select(a => a.Type!.SpecialType == SpecialType.System_String ? sourceBuilder.ToStringLiteral((string)a.Value!) : $"({a.Type}){a.Value}"))})";
            }

            var listPluginConfigAttribute = attributes.FirstOrDefault(a => a.AttributeClass!.ToDisplayString() == $"{GeneratorConstants.GeneratedNamespace}.ListPluginConfigAttribute");
            if (listPluginConfigAttribute != null)
            {
                var args = listPluginConfigAttribute.ConstructorArguments[0].Values;
                if (!IsAssignable(compilation, symbol.Type, args[0].Type!))
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.EPG004, sourceBuilder.GetLocation(symbol), symbol.ToDisplayString()));
                    return null;
                }
                return $"new AcceptableValueList<{args[0].Type}>({string.Join(", ", args.Select(a => a.Type!.SpecialType == SpecialType.System_String ? sourceBuilder.ToStringLiteral((string)a.Value!) : $"({a.Type}){a.Value}"))})";
            }

            return null;
        }

        private IEnumerable<IPropertySymbol> GetProperties(INamedTypeSymbol symbol)
        {

            var properties = symbol
                .GetMembers()
                .Where(a => IsConfigTarget(a))
                .Cast<IPropertySymbol>()
            ;

            if (symbol.BaseType != null)
            {
                //TODO: オーバーライドとか new とかはもう知らんよ
                // 設定クラスではそんなことしないでとは思いつつもこのライブラリがオーバーライドして差し替える手法なので、まぁうん
                var baseProperties = GetProperties(symbol.BaseType);
                return properties.Concat(baseProperties);
            }

            return properties;
        }

        private IEnumerable<IPropertySymbol> GetNestedProperties(IEnumerable<IPropertySymbol> properties)
        {
            var nestedProperties = properties
                .Where(a => !IsProxyTarget(a))
                .Where(a => a.Type.TypeKind != TypeKind.Struct)
            ;
            return nestedProperties;
        }

        private string GenerateMemberSource(IPropertySymbol symbol)
        {
            if (IsProxyTarget(symbol))
            {
                return $$"""

                public ConfigEntry<{{symbol.Type.ToDisplayString()}}> {{symbol.Name}} { get; set; }
                
                """;
            }

            return $$"""

            public {{ToEntriesClassName(symbol.Type)}} {{symbol.Name}} { get; set; } = new {{ToEntriesClassName(symbol.Type)}}();

            """;
        }

        private string GenerateProxyMemberSource(IPropertySymbol symbol)
        {
            return $$"""

            public override {{symbol.Type.ToDisplayString()}} {{symbol.Name}}
            {
                get => Entries.{{symbol.Name}}.Value;
                set => Entries.{{symbol.Name}}.Value = value;
            }

            """;
        }


        private string GenerateConfigSource(SourceProductionContext context, SourceBuilder sourceBuilder, INamedTypeSymbol targetSymbol, IEnumerable<IPropertySymbol> properties, bool overrideReset)
        {
            foreach (var property in properties)
            {
                if (property.IsVirtual)
                {
                    if (property.Type.TypeKind == TypeKind.Class && property.Type.SpecialType != SpecialType.System_String)
                    {
                        var location = sourceBuilder.GetLocation(property);
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.EPG002,
                            location,
                            targetSymbol.ToDisplayString(), property.Name
                        ));
                    }
                    if (!IsSupportedType(property.Type))
                    {
                        var location = sourceBuilder.GetLocation(property);
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.EPG003,
                            location,
                            targetSymbol.ToDisplayString(), property.Name, property.Type.ToDisplayString()
                        ));
                    }
                }
            }

            var source = $$"""
            {{sourceBuilder.Header}}

            {{sourceBuilder.ToNamespaceCode(targetSymbol)}}

            using BepInEx.Configuration;

            internal class {{ToEntriesClassName(targetSymbol)}}
            {
            {{sourceBuilder.JoinLines(
                properties.Select(a => GenerateMemberSource(a))
            )}}
            }

            internal class {{ToProxyClassName(targetSymbol)}}: {{targetSymbol.ToDisplayString()}}
            {
                public {{ToProxyClassName(targetSymbol)}}({{ToEntriesClassName(targetSymbol)}} entries)
                {
                    Entries = entries;

                    {{sourceBuilder.JoinLines(
                        properties
                            .Where(a => !IsProxyTarget(a))
                            .Where(a => a.Type.TypeKind != TypeKind.Struct)
                            .Select(a => $"{a.Name} = new {ToProxyClassName(a.Type)}(Entries.{a.Name});")
                    )}}
                }

                #region property

                {{ToEntriesClassName(targetSymbol)}} Entries { get; }

                #endregion

                #region {{targetSymbol.ToDisplayString()}}

                {{sourceBuilder.JoinLines(
                    properties
                        .Where(a => IsProxyTarget(a))
                        .Select(a => GenerateProxyMemberSource(a))
                )}}

                internal {{(overrideReset ? "override" : "")}} void Reset()
                {
                    // ネストプロパティをリセット(子は全部プロキシに差し替えられている)
                    {{sourceBuilder.JoinLines(
                        properties
                            .Where(a => !IsProxyTarget(a))
                            .Where(a => a.Type.TypeKind != TypeKind.Struct)
                            .Select(a => $"(({ToProxyClassName(a.Type)}){a.Name}).Reset();")
                    )}}

                    // プロパティをリセット
                    {{sourceBuilder.JoinLines(
                        properties
                            .Where(a => IsProxyTarget(a))
                            .Select(a => $"Entries.{a.Name}.Value = ({a.Type.ToDisplayString()})Entries.{a.Name}.DefaultValue;")
                    )}}
                }

                #endregion
            }
            
            """;

            return source;
        }

        private IEnumerable<(string source, string fileName)> GenerateConfigSources(SourceProductionContext context, SourceBuilder sourceBuilder, INamedTypeSymbol targetSymbol, bool overrideReset)
        {
            if (GeneratedClassNames.Contains(targetSymbol.ToDisplayString()))
            {
                yield break;
            }
            GeneratedClassNames.Add(targetSymbol.ToDisplayString());

            var properties = GetProperties(targetSymbol).ToArray();
            var nestedProperties = GetNestedProperties(properties);

            foreach (var nestedProperty in nestedProperties)
            {
                foreach (var configSource in GenerateConfigSources(context, sourceBuilder, (INamedTypeSymbol)nestedProperty.Type, false))
                {
                    yield return configSource;
                }
            }

            var sourceFileName = Path.Combine(Path.GetDirectoryName(sourceBuilder.ToSourceFilePath(targetSymbol)), $"{targetSymbol.Name}.g.cs");

            var source = GenerateConfigSource(context, sourceBuilder, targetSymbol, properties, overrideReset);
            yield return (source, sourceFileName);
        }

        private IEnumerable<string> GenerateBindSources(SourceProductionContext context, Compilation compilation, SourceBuilder sourceBuilder, string parentSection, INamedTypeSymbol typeSymbol, IPropertySymbol? propertySymbol)
        {
            var properties = GetProperties(typeSymbol).ToArray();
            var nestedProperties = GetNestedProperties(properties);

            var sectionName = string.IsNullOrWhiteSpace(parentSection)
                ? typeSymbol.Name
                : parentSection + "." + propertySymbol!.Name
            ;

            foreach (var nestedProperty in nestedProperties)
            {
                var nestedSources = GenerateBindSources(context, compilation, sourceBuilder, sectionName, (INamedTypeSymbol)nestedProperty.Type, nestedProperty);
                foreach (var nestedSource in nestedSources)
                {
                    yield return nestedSource;
                }
            }

            var source = $$"""

            private static {{ToEntriesClassName(typeSymbol)}} {{ToEntriesCreateMethodName(typeSymbol)}}{{propertySymbol?.Name}}(ConfigFile config, {{typeSymbol.Name}} defaultValue)
            {
                var entries = new {{ToEntriesClassName(typeSymbol)}}();

                {{sourceBuilder.JoinLines(
                    properties.Select(a =>
                    {
                        if (IsProxyTarget(a))
                        {
                            var documentComment = GetDocumentComment(a);
                            var acceptableValue = GetAcceptableValue(context, compilation, sourceBuilder, a);

                            return $$"""

                            entries.{{a.Name}} = config.Bind(
                                {{sourceBuilder.ToStringLiteral(sectionName)}},
                                {{sourceBuilder.ToStringLiteral(a.Name)}},
                                defaultValue.{{a.Name}},
                                new ConfigDescription(
                                    {{sourceBuilder.ToStringLiteral(documentComment ?? string.Empty)}},
                                    {{acceptableValue ?? "null"}}
                                )
                            );

                            """;
                        }
                        else
                        {
                            return $"entries.{a.Name} = {ToEntriesCreateMethodName(a.Type)}{a.Name}(config, defaultValue.{a.Name});";
                        }
                    })
                )}}

                return entries;
            }

            """;

            yield return source;
        }

        private string GenerateBindSource(SourceProductionContext context, Compilation compilation, SourceBuilder sourceBuilder, INamedTypeSymbol targetSymbol)
        {
            var bindSources = GenerateBindSources(context, compilation, sourceBuilder, string.Empty, targetSymbol, null);

            var source = $$"""
            {{sourceBuilder.Header}}
            {{sourceBuilder.ToNamespaceCode(targetSymbol)}}

            using BepInEx.Configuration;

            partial class {{targetSymbol.Name}}
            {
                {{sourceBuilder.JoinLines(bindSources)}}

                /// <summary>
                /// 設定値を BepInEx.Configuration にバインドします。
                /// </summary>
                /// <param name="config">バインドする設定ファイル。</param>
                /// <param name="defaultValue">初期値。</param>
                /// <returns>バインドされた設定値を参照する <see cref="{{targetSymbol.ToDisplayString()}}" /> 互換オブジェクト。設定値を参照する実装側ではこのオブジェクトを使用すること。</returns>
                internal static {{ToProxyClassName(targetSymbol)}} Bind(ConfigFile config, {{targetSymbol.Name}} defaultValue)
                {
                    var entries = {{ToEntriesCreateMethodName(targetSymbol)}}(config, defaultValue);
                    var proxy = new {{ToProxyClassName(targetSymbol)}}(entries);

                    return proxy;
                }

                /// <summary>
                /// 設定値を初期化する。
                /// </summary>
                /// <remarks><see cref="Bind"/> の戻り値で使用する必要があります。</remarks>
                internal virtual void Reset()
                {
                    throw new System.NotSupportedException("Reset is not supported: {{targetSymbol.Name}}");
                }
            }

            """;
            return source;
        }

        private void GenerateSource(SourceProductionContext context, ImmutableArray<GeneratorAttributeSyntaxContext> array)
        {
            var sourceBuilder = new SourceBuilder();
            foreach (var attribute in array)
            {
                var compilation = attribute.SemanticModel.Compilation;
                var targetSymbol = (INamedTypeSymbol)attribute.TargetSymbol;

                // ネストは無理
                if (targetSymbol.ContainingType != null)
                {
                    var location = sourceBuilder.GetLocation(targetSymbol);
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.EPG001,
                        location,
                        targetSymbol.ToDisplayString(), $"{GeneratorConstants.GeneratedNamespace}.{GeneratorConstants.GeneratePluginConfigAttributeName}"
                    ));
                    continue;
                }

                var configSources = GenerateConfigSources(context, sourceBuilder, targetSymbol, true);
                foreach (var configSource in configSources)
                {
                    context.AddSource(configSource.fileName, sourceBuilder.Format(configSource.source));
                }

                var bindSource = GenerateBindSource(context, compilation, sourceBuilder, targetSymbol);
                var sourceFileName = Path.Combine(Path.GetDirectoryName(sourceBuilder.ToSourceFilePath(targetSymbol)), $"{targetSymbol.Name}.bind.g.cs");
                context.AddSource(sourceFileName, sourceBuilder.Format(bindSource));
            }
        }

        #endregion

        #region IIncrementalGenerator

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var sourceBuilder = new SourceBuilder();
            var acceptableAttributes = new
            {
                Types = new[] {
                    typeof(int),
                    typeof(float),
                    typeof(double),
                    typeof(string)
                },
                Attributes = new[] {
                    new  {
                        Name = "RangePluginConfigAttribute",
                        Template = "<TYPE> min, <TYPE> max",
                    },
                    new  {
                        Name = "ListPluginConfigAttribute",
                        Template = "params <TYPE>[] values",
                    }
                }
            };

            context.RegisterPostInitializationOutput(initContext =>
            {
                var attributeSource = $$"""
                {{sourceBuilder.Header}}

                namespace {{GeneratorConstants.GeneratedNamespace}};

                {{sourceBuilder.ApplyXmlDocumentComment(
                    string.Empty,
                    // lang=xml
                    """
                    <summary>
                    Mod の設定クラスのルートに付与する属性。
                    </summary>
                    <remarks>
                    <para>この属性を付与したクラスは設定用のインフラが構築されます。</para>
                    <para>注意点として、値は <see langword="virtual"/> を指定すること、値の型は `BepInEx.Configuration.ConfigEntry{T}` が使用できる型であること、ネスト対象するプロパティは <see langword="virtual"/> にしないこと、ネストするクラスの名前空間は全て同じであること。</para>
                    <para>
                    <example>
                    設定クラスは以下のように定義。
                    <code>
                    class MyNestedSetting
                    {
                        public virtual int NestedValue { get; set; }
                    }
                    [GeneratePluginConfig]
                    partial class MySetting
                    {
                        public virtual int Value { get; set; }
                        public MyNestedSetting Nested { get; set; }
                    }
                    </code>
                    設定を適用するには以下のようにします。
                    <code>
                    public Plugin: BaseUnityPlugin
                    {
                        public void Awake()
                        {
                            // 適用時に初期値も一緒に渡すこと。
                            var defaultSetting = new MySetting() {
                                Value = 123,
                                Nested = new MyNestedSetting() {
                                    NestedValue = 456,
                                },
                            };
                            var setting = MySetting.Bind(Config, defaultSetting);
                            // 以降 setting を使用して設定値を参照・変更できます。
                        }
                    }
                    </code>
                    </example>
                    </para>
                    </remarks>
                    """
                )}}
                [System.Serializable]
                [{{sourceBuilder.ToCode<System.AttributeUsageAttribute>()}}({{sourceBuilder.ToCode(AttributeTargets.Class)}}, AllowMultiple = false)]
                internal sealed class {{GeneratorConstants.GeneratePluginConfigAttributeName}}: {{sourceBuilder.ToCode<System.Attribute>()}}
                {
                    public {{GeneratorConstants.GeneratePluginConfigAttributeName}}()
                    {
                        //NOP
                    }
                }

                {{sourceBuilder.ApplyXmlDocumentComment(
                    string.Empty,
                    // lang=xml
                    """
                    <summary>
                    設定クラス内のプロパティを設定構築から除外する属性。
                    </summary>
                    """
                )}}
                [{{sourceBuilder.ToCode<System.AttributeUsageAttribute>()}}({{sourceBuilder.ToCode(AttributeTargets.Property)}}, AllowMultiple = false)]
                internal sealed class IgnorePluginConfigAttribute : {{sourceBuilder.ToCode<System.Attribute>()}}
                {
                    public IgnorePluginConfigAttribute()
                    {
                        //NOP
                    }
                }

                {{sourceBuilder.JoinLines(
                    acceptableAttributes.Attributes.Select(attr =>
                    {
                        return $$"""

                        [{{sourceBuilder.ToCode<System.AttributeUsageAttribute>()}}({{sourceBuilder.ToCode(AttributeTargets.Property)}}, AllowMultiple = false)]
                        internal sealed class {{attr.Name}} : {{sourceBuilder.ToCode<System.Attribute>()}}
                        {
                            {{sourceBuilder.JoinLines(
                                acceptableAttributes.Types.Select(type =>
                                {
                                    var parameter = attr.Template.Replace("<TYPE>", type.FullName);

                                    return $$"""

                                    public {{attr.Name}}({{parameter}})
                                    {
                                        //NOP
                                    }

                                    """;
                                })
                            )}}
                        }

                        """;
                    })
                )}}

                """;

                initContext.AddSource($"Attributes.g.cs", sourceBuilder.Format(attributeSource));
            });

            var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
                $"{GeneratorConstants.GeneratedNamespace}.{GeneratorConstants.GeneratePluginConfigAttributeName}",
                (node, cancellationToken) => true,
                (context, cancellationToken) => context
            ).Collect();

            context.RegisterSourceOutput(provider, GenerateSource);
        }

        #endregion
    }
}
