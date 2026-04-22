using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Elin.Plugin.Generator
{
    [Generator(LanguageNames.CSharp)]
    public class PluginLocalizationGenerator : IIncrementalGenerator
    {
        #region property

        private const string FormatHead = "${";
        private const string FormatTail = "}";
        private static readonly Regex FormatParameterRegex = new Regex(@"\$\{(.+?)\}");

        #endregion

        #region function

        public string ToPascalCase(string text)
        {
            // Localization.schema.json で縛ってる制約もあるので簡単に対応

            StringBuilder sb = new StringBuilder();
            var textSpan = text.AsSpan();
            for (var i = 0; i < textSpan.Length; i++)
            {
                var c = textSpan[i];
                if (i == 0)
                {
                    sb.Append(char.ToUpper(c));
                    continue;
                }

                if (c == '_' || c == '-')
                {
                    if (i + 1 < textSpan.Length)
                    {
                        var nextC = textSpan[i + 1];
                        i += 1;

                        if (nextC != '_' && nextC != '-')
                        {
                            continue;
                        }
                        sb.Append(char.ToUpper(nextC));

                        continue;
                    }
                    break;
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        public string ToParameterName(string text)
        {
            // クッソ適当
            return string.Join(
                "",
                text
                    .ToLowerInvariant()
                    .Split('_', '-')
                    .Select((a, i) => i == 0 ? a : char.ToUpper(a[0]) + a.Substring(1))
            );
        }

        public HashSet<string> GetMessageParameters(string s)
        {
            return new HashSet<string>(
                FormatParameterRegex.Matches(s)
                    .Cast<Match>()
                    .Select(a => a.Groups[1].Value)
            );
        }

        IXmlDocumentNode GenerateLanguageSamples(XmlNodeGenerator generator, LocalizationItem item)
        {
            return generator.Table(
                new KeyValuePair<string, string>("言語", "翻訳"),
                item.Languages.Select(a => new KeyValuePair<string, string>(a.Key, a.Value ?? string.Empty))
            );
        }

        private string GeneratePluginLocalizationGroup(SourceBuilder sourceBuilder, string className, IReadOnlyDictionary<string, LocalizationItem> items, Func<string>? classExtension = null)
        {
            return $$"""

            internal class {{className}}
            {
                public {{className}}(ILanguageSystem languageSystem)
                {
                    LanguageSystem = languageSystem;
                    Items = new Dictionary<string, PluginLocalizationItem>() {
                    {{sourceBuilder.JoinLines(
                        items.Select(a =>
                        {
                            return $$"""
                            
                            [{{sourceBuilder.ToStringLiteral(a.Key)}}] = new PluginLocalizationItem(
                                {{sourceBuilder.ToStringLiteral(a.Value.JP)}},
                                {{sourceBuilder.ToStringLiteral(a.Value.EN)}}
                            ) {
                                {{(sourceBuilder.JoinLines(
                                    a.Value.OptionalLanguages
                                        .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                                        .Select(kv => $"{kv.Key} = {sourceBuilder.ToStringLiteral(kv.Value!)},")
                                    )
                                )}}
                            },

                            """;
                        })
                    )}}
                    };
                }

                #region property

                private ILanguageSystem LanguageSystem { get; }
                public IReadOnlyDictionary<string, PluginLocalizationItem> Items { get; }

                {{sourceBuilder.JoinLines(
                    items.Select(a =>
                    {
                        //TODO: 言語を動的にしたいけど、順序固定考えると今は面倒

                        return $$"""

                        {{sourceBuilder.Xml.Build(g => g.Fragment([
                            g.Summary($"`{a.Key}` に対応するローカライズされた文字列を取得します。"),
                            g.Remarks([GenerateLanguageSamples(g, a.Value)])
                        ]))}}
                        public string {{ToPascalCase(a.Key)}} => Items[{{sourceBuilder.ToStringLiteral(a.Key)}}].GetText(LanguageSystem);

                        """;
                    })
                )}}

                #endregion

                {{(classExtension != null ? classExtension() : string.Empty)}}
            }

            """;
        }

        #endregion

        #region IIncrementalGenerator

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 実装メモ: Elin 側で言語周りの調整してくれると思ってたんだが、
            // Lang/ の扱いとか実装見てるとなんかそうでもなさそうだったので作成

            var define = SourceGeneratorHelper.CollectJsonClass<LocalizationDefine>(
                context.AdditionalTextsProvider,
                file => Path.GetFileName(file.Path) == GeneratorConstants.LocalizeFileName
            );

            context.RegisterSourceOutput(define, (c, define) =>
            {
                if (define == null)
                {
                    return;
                }

                var sourceBuilder = new SourceBuilder();
                var templateLocalizationItem = new LocalizationItem();

                //lang=c#
                var source = $$"""
                {{sourceBuilder.Header}}

                using System;
                using System.IO;
                using System.Text;
                using System.Linq;
                using System.Xml.Serialization;
                using System.Collections.Generic;
                using System.Text.RegularExpressions;

                namespace {{GeneratorConstants.GeneratedNamespace}};

                internal interface ILanguageSystem
                {
                    #region property

                    {{sourceBuilder.Xml.Build(g => g.Fragment([
                        g.Summary("現在システム側が使用している言語。"),
                        g.SeeAlsoCref("global::Lang.langCode"),
                    ]))}}
                    public string LangCode { get; }

                    {{sourceBuilder.Xml.Build(g => g.SeeAlsoCref("global::Lang.isJP"))}}
                    public bool IsJP { get; }

                    {{sourceBuilder.Xml.Build(g => g.SeeAlsoCref("global::Lang.isEN"))}}
                    public bool IsEN { get; }
                
                    #endregion
                }

                internal class PluginLocalizationItem
                {
                    public PluginLocalizationItem({{string.Join(", ", templateLocalizationItem.RequiredLanguages.Select(a => $"string {ToParameterName(a.Key)}"))}})
                    {
                        {{sourceBuilder.JoinLines(templateLocalizationItem.RequiredLanguages.Select(a => $"{a.Key} = {ToParameterName(a.Key)};"))}}
                    }

                    #region property

                    {{sourceBuilder.JoinLines(templateLocalizationItem.RequiredLanguages.Select(a => $"public string {a.Key} {{ get; set; }}"))}}
                    {{sourceBuilder.JoinLines(templateLocalizationItem.OptionalLanguages.Select(a => $"public string? {a.Key} {{ get; set; }}"))}}
                
                    #endregion

                    #region function

                    public string GetText(ILanguageSystem languageSystem)
                    { {{/* 必須言語の処理だけここはべた書き */ ""}}
                        if(languageSystem.IsJP) {
                            return JP;
                        }
                
                        if(languageSystem.IsEN) {
                            if(string.IsNullOrEmpty(EN)) {
                                return JP;
                            }
                            return EN;
                        }
                
                        var data = languageSystem.LangCode switch {
                            {{sourceBuilder.JoinLines(templateLocalizationItem.OptionalLanguages.Select(a => $"\"{a.Key}\" => {a.Key},"))}}
                            _ => null
                        };
                
                        if(!string.IsNullOrEmpty(data)) {
                            return data!;
                        }
                
                        data = EN;
                
                        if(string.IsNullOrEmpty(data)) {
                            return JP;
                        }
                
                        return data;
                    }

                    public IEnumerable<KeyValuePair<string, string>> GetLanguages()
                    {
                        return new [] {
                            {{sourceBuilder.JoinLines(templateLocalizationItem.Languages
                                .Select(a => $"new KeyValuePair<string, string?>({sourceBuilder.ToStringLiteral(a.Key)}, {a.Key}),"))}}
                        }
                            .Where(a => !string.IsNullOrEmpty(a.Value))
                            .Select(a => new KeyValuePair<string, string>(a.Key, a.Value!))
                        ;
                    }

                    #endregion
                }

                {{GeneratePluginLocalizationGroup(sourceBuilder, "PluginLocalizationGeneral", define.General)}}
                {{GeneratePluginLocalizationGroup(sourceBuilder, "PluginLocalizationFormatter", define.Format,
                    classExtension: () =>
                    {
                        // TODO: パラメータチェック処理

                        return sourceBuilder.JoinLines(
                            define.Format.Select(a =>
                            {
                                var baseLanguage = nameof(a.Value.JP);
                                var baseFormatParameters = GetMessageParameters(a.Value[baseLanguage]!);

                                if (a.Value.Parameters is null)
                                {
                                    c.ReportDiagnostic(Diagnostic.Create(
                                        DiagnosticDescriptors.EPG007,
                                        Location.None,
                                        GeneratorConstants.LocalizeFileName, $"$.format.{a.Key}"
                                    ));
                                    // なんもできん！
                                    return string.Empty;
                                }

                                if (baseFormatParameters.Count == 0)
                                {
                                    c.ReportDiagnostic(Diagnostic.Create(
                                        DiagnosticDescriptors.EPG006,
                                        Location.None,
                                        GeneratorConstants.LocalizeFileName, $"$.format.{a.Key}"
                                    ));
                                }

                                if (baseFormatParameters.Count != a.Value.Parameters.Count)
                                {
                                    // パラメーター数が違っても問題ない状態もあるが、一応警告としておく(後続できちんと対応するので無くてもいいけど)
                                    c.ReportDiagnostic(Diagnostic.Create(
                                        DiagnosticDescriptors.EPG008,
                                        Location.None,
                                        GeneratorConstants.LocalizeFileName, $"$.format.{a.Key}", baseFormatParameters.Count, a.Value.Parameters.Count
                                    ));
                                }

                                foreach (var param in baseFormatParameters)
                                {
                                    if (!a.Value.Parameters.ContainsKey(param))
                                    {
                                        c.ReportDiagnostic(Diagnostic.Create(
                                            DiagnosticDescriptors.EPG009,
                                            Location.None,
                                            GeneratorConstants.LocalizeFileName, $"$.format.{a.Key}", param
                                        ));
                                    }
                                }

                                var otherLanguages = a.Value.Languages
                                    .Where(a => a.Key != baseLanguage)
                                    .Where(a => a.Value is not null)
                                ;
                                foreach (var lang in otherLanguages)
                                {
                                    var otherFormatParameters = GetMessageParameters(a.Value[lang.Key]!);
                                    foreach (var param in a.Value.Parameters)
                                    {
                                        if (!otherFormatParameters.Contains(param.Key))
                                        {
                                            c.ReportDiagnostic(Diagnostic.Create(
                                                DiagnosticDescriptors.EPG010,
                                                Location.None,
                                                GeneratorConstants.LocalizeFileName, $"$.format.{a.Key}", param.Key, lang.Key
                                            ));
                                        }
                                    }
                                }

                                return $$"""

                                {{sourceBuilder.Xml.Build(g => g.Fragment([
                                    g.Summary($"{a.Key} に対応するローカライズされた文字列を、指定したパラメーターでフォーマットして取得します。"),
                                    g.Remarks([GenerateLanguageSamples(g, a.Value)]),
                                    g.Param("parameters", [
                                        g.Text("対象キー:"),
                                        g.List(XmlDocumentListType.Bullet, baseFormatParameters)
                                    ]),
                                    g.Returns("フォーマットされた文字列"),
                                ]))}}
                                public string Format{{ToPascalCase(a.Key)}}(IReadOnlyDictionary<string, string> parameters)
                                {
                                    var item = Items[{{sourceBuilder.ToStringLiteral(a.Key)}}];
                                    var format = item.GetText(LanguageSystem);
                                    return Format(format, parameters);
                                }
                                {{sourceBuilder.Xml.Build(g => g.Fragment([
                                    g.InheritDoc($"Format{ToPascalCase(a.Key)}(IReadOnlyDictionary{{string, string}})"),
                                    g.Fragment(
                                        a.Value.Parameters.Select(kv => g.Param(ToParameterName(kv.Key), kv.Value.ToString()))
                                    )
                                ]))}}
                                [{{GeneratorConstants.GeneratedNamespace}}.{{GeneratorConstants.RequireNamedArgumentsAttributeName}}]
                                public string Format{{ToPascalCase(a.Key)}}({{string.Join(", ", a.Value.Parameters.Select(kv => $"{kv.Value.Type} {ToParameterName(kv.Key)}"))}})
                                {
                                    var parameters = new Dictionary<string, string>() {
                                        {{string.Join(
                                            ",",
                                            a.Value.Parameters
                                                .Select(kv => $"[{sourceBuilder.ToStringLiteral(kv.Key)}] = {ToParameterName(kv.Key) + (kv.Value.Type == "string" ? string.Empty : $".ToString({sourceBuilder.ToStringLiteral(kv.Value.Format ?? string.Empty)})")}")
                                        )}}
                                    };
                                    return Format{{ToPascalCase(a.Key)}}(parameters);
                                }

                                """;
                            })
                        ) + $$"""

                        // 大昔の別プロジェクトからコピペ
                        // 現行のやつはだいぶ最適化してるのでコピペにはこっちのほうが楽ちん

                        private string FormatCore(string format, string head, string tail, Func<string, string> dg)
                        {
                            var escHead = Regex.Escape(head);
                            var escTail = Regex.Escape(tail);
                            var pattern = escHead + "(.+?)" + escTail;
                            var replacedText = Regex.Replace(format, pattern, (Match m) => dg(m.Groups[1].Value));
                            return replacedText;
                        }

                        private string Format(string format, IReadOnlyDictionary<string, string> parameters)
                        {
                            return FormatCore(
                                format,
                                {{sourceBuilder.ToStringLiteral(FormatHead)}},
                                {{sourceBuilder.ToStringLiteral(FormatTail)}},
                                s => parameters.TryGetValue(s, out var value)
                                    ? value
                                    : {{sourceBuilder.ToStringLiteral(FormatHead)}} + s + {{sourceBuilder.ToStringLiteral(FormatTail)}}
                            );
                        }

                        """;
                    }
                )}}
                {{GeneratePluginLocalizationGroup(sourceBuilder, "PluginLocalizationConfig", define.Config)}}

                internal class PluginLocalization: ILanguageSystem
                {
                    public PluginLocalization()
                    {
                        General = new PluginLocalizationGeneral(this);
                        Formatter = new PluginLocalizationFormatter(this);
                        Config = new PluginLocalizationConfig(this);
                    }

                    #region property

                    {{sourceBuilder.Xml.Build(g => g.Fragment([
                       g.Summary("一般的なローカライズされた文字列を取得します。"),
                          g.Remarks($"{GeneratorConstants.LocalizeFileName}: $.general")
                    ]))}}
                    public PluginLocalizationGeneral General { get; }

                    {{sourceBuilder.Xml.Build(g => g.Fragment([
                       g.Summary("フォーマット用のローカライズされた文字列を取得します。"),
                          g.Remarks($"{GeneratorConstants.LocalizeFileName}: $.format")
                    ]))}}
                    public PluginLocalizationFormatter Formatter { get; }

                    {{sourceBuilder.Xml.Build(g => g.Fragment([
                       g.Summary("設定用のローカライズされた文字列を取得します。"),
                          g.Remarks($"{GeneratorConstants.LocalizeFileName}: $.config")
                    ]))}}
                    public PluginLocalizationConfig Config { get; }

                    #endregion

                    #region ILanguageSystem

                    public string LangCode => Lang.langCode;
                    public bool IsJP => Lang.isJP;
                    public bool IsEN => Lang.isEN;

                    #endregion
                }


                """;
                c.AddSource("Localization.g.cs", sourceBuilder.Format(source));
            });
        }

        #endregion
    }
}
