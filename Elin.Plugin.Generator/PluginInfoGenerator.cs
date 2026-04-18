using Microsoft.CodeAnalysis;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Elin.Plugin.Generator
{
    [Generator(LanguageNames.CSharp)]
    internal class PluginInfoGenerator : IIncrementalGenerator
    {
        #region property

        private JsonSerializerOptions JsonSerializerOptions = new System.Text.Json.JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
        };

        #endregion

        #region IIncrementalGenerator

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var define = context.AdditionalTextsProvider
                .Where(file => Path.GetFileName(file.Path) == GeneratorConstants.PluginInfoFileName)
                .Select((file, _) => (file: file, json: file.GetText()?.ToString()))
                .Where(a => a.json != null)
                .Select((a, _) => JsonSerializer.Deserialize<PluginDefine>(a.json!, JsonSerializerOptions))
                .Collect()
                .Select((arr, _) => arr.FirstOrDefault())
            ;

            var devDefine = context.AdditionalTextsProvider
                .Where(file => Path.GetFileName(file.Path) == GeneratorConstants.PluginInfoDevFileName)
                .Select((file, _) => (file: file, json: file.GetText()?.ToString()))
                .Where(a => a.json != null)
                .Select((a, _) => JsonSerializer.Deserialize<PluginDevDefine>(a.json!, JsonSerializerOptions))
                .Collect()
                .Select((arr, _) => arr.FirstOrDefault())
            ;

            var macroProvider = context.AnalyzerConfigOptionsProvider
                .Select((configOptions, token) =>
                {
                    var macro = new PluginMacro();

                    if (configOptions.GlobalOptions.TryGetValue("build_property.ElinPath", out var elinPath))
                    {
                        //TODO: 今後使う予定。使えればいいな。使えねぇなぁ。
                        macro.ElinModulePath = Path.Combine(elinPath, "Elin_Data", "Managed", "Elin.dll");
                    }
                    if (configOptions.GlobalOptions.TryGetValue("build_property.AssemblyName", out var assemblyName))
                    {
                        macro.AssemblyName = assemblyName;
                    }

                    return macro;
                })
            ;

            context.RegisterSourceOutput(define.Combine(devDefine).Combine(macroProvider), (c, x) =>
            {
                var define = x.Left.Left;
                var devDefine = x.Left.Right;
                var macro = x.Right;
                if (define is null)
                {
                    return;
                }

                if (devDefine is not null)
                {
                    if (devDefine.Log is not null)
                    {
                        define.Mod.Log = devDefine.Log;
                    }
                }

                /*
                //TODO: バージョンとって来たかったけど、どこで定義されてるか分からんし、アセンブリから取れるのかもわからん
                var elinVersion = define.Package.ElinVersion;
                if (elinVersion == GeneratorConstants.ForceLatestElinVersion)
                {
                    if (File.Exists(macro.ElinModulePath))
                    {
                        // バージョンってどこで定義されてんねん
                    }
                }
                if (elinVersion == GeneratorConstants.ForceLatestElinVersion)
                {
                    throw new InvalidOperationException($"最新バージョン指定にもかかわらず、 Elin からバージョン情報を取得できなかった");
                }
                */

                if (string.IsNullOrEmpty(macro.AssemblyName) || macro.AssemblyName.IndexOfAny(['\\', '/', ':', '*', '?', '\"', '<', '>', '|']) != -1)
                {
                    c.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.EPG011,
                        Location.None
                    ));
                    return;
                }


                var sourceBuilder = new SourceBuilder();

                string docHeader(string parent, string property)
                {
                    return sourceBuilder.Xml.Build(g => g.Summary($"{GeneratorConstants.PluginInfoFileName}: $.{parent}.{property}"));
                }

                //lang=c#
                var source = $$"""
                {{sourceBuilder.Header}}

                using System;
                using System.IO;
                using System.Text;
                using System.Xml.Serialization;

                namespace {{GeneratorConstants.GeneratedNamespace}};

                #pragma warning disable CS1591 // XML コメントがありません
                {{sourceBuilder.Xml.Build(g =>
                {
                    return g.Fragment([
                        g.Summary("package.xml を生成するためだけのクラス。"),
                        g.Remarks([
                            g.SeeLangword("public"),
                            g.Text("だがプラグイン側では使用せず、ビルド時のみ使用する想定。")
                        ]),
                        g.SeeAlsoCref("Package")
                    ]);
                })}}
                [XmlRoot("Meta")]
                public class MsBuildOnlyPackageXml
                {
                    [XmlElement("title")]
                    public string Title { get; set; } =
                #if DEBUG
                    "<DEBUG> " + // package.xml のタイトルにデバッグ印をつけてデバッグ版アップロードのミスを減らす努力
                #endif
                    {{sourceBuilder.ToStringLiteral(define.Package.Title)}}
                    ;

                    [XmlElement("id")]
                    public string Id { get; set; } = {{sourceBuilder.ToStringLiteral(define.Package.Id)}}
                #if DEBUG
                    + {{(define.Mod.UseDebugId ? $"{sourceBuilder.ToStringLiteral(".debug")} // デバッグ実行時に公開版との ID 重複を避ける努力。 公開したことないから被るか知らん" : sourceBuilder.EmptyStringLiteral)}}
                #endif
                    ;
                
                    [XmlElement("author")]
                    public string Author { get; set; } = {{sourceBuilder.ToStringLiteral(define.Package.Author)}};
                
                    [XmlElement("loadPriority")]
                    public int LoadPriority { get; set; } = {{define.Package.LoadPriority}};
                
                    [XmlElement("version")]
                    public string Version { get; set; } = {{sourceBuilder.ToStringLiteral(define.Package.ElinVersion)}};

                    [XmlElement("description")]
                    public string Description { get; set; } = {{sourceBuilder.ToStringLiteral(sourceBuilder.JoinLines(define.Package.Description))}};

                    [XmlElement("tags")]
                    public string _Tags
                    {
                        get => Tags.Length == 0 ? string.Empty : string.Join(",", Tags);
                        set => Tags = value.Split(new[] { "," }, StringSplitOptions.None);
                    }

                    [XmlIgnore]
                    public string[] Tags { get; set; } = {{(define.Package.Tags == null ? "[]" : "[" + string.Join(", ", define.Package.Tags.Select(a => sourceBuilder.ToStringLiteral(a))) + "]")}};

                    [XmlElement("visibility")]
                    public string Visibility { get; set; } = {{sourceBuilder.ToStringLiteral(define.Package.Visibility ?? "Public")}};

                    private sealed class StringWriterUTF8 : StringWriter
                    {
                        public override System.Text.Encoding Encoding
                        {
                            get { return System.Text.Encoding.UTF8; }
                        }
                    }

                    public override string ToString() 
                    {
                        // PowerShell で雑に package.xml を生成するだけの処理

                        var serializer = new XmlSerializer(typeof(MsBuildOnlyPackageXml));

                        var ns = new XmlSerializerNamespaces();
                        ns.Add("", ""); 

                        using var stringWriter = new StringWriterUTF8();
                        serializer.Serialize(stringWriter, this, ns);
                        return stringWriter.ToString();
                    }
                }
                #pragma warning restore CS1591 // XML コメントがありません

                {{sourceBuilder.Xml.Build(g => g.Fragment([
                    g.Summary("package.xml 参照情報。"),
                    g.SeeAlsoHref("https://docs.google.com/document/d/e/2PACX-1vQSITB8aYTycrnn3PxxGnPjNZ2_y1G3LDfXjC_PM5S_mTPCh6fv1vcj1bkfPbbUZ88WVb5_7T-62zYc/pub"),
                ]))}}
                internal static class Package
                {
                    {{docHeader("package", "title")}}
                    {{sourceBuilder.Xml.Build(g => g.Remarks([
                        "このタグの中には、あなたのModのタイトルを記入してください。最初にWorkshopにModをアップロードする際に、ここで記述されたテキストがModのタイトルとして表示されます。",
                        "Modを更新する際は、このテキストは無視されます。タイトルの変更が必要な場合は、Workshopから変更してください。"
                    ]))}}
                    public const string Title = {{sourceBuilder.ToStringLiteral(define.Package.Title)}};

                    {{docHeader("package", "id")}}
                    {{sourceBuilder.Xml.Build(g => g.Remarks([
                        "ここでは、あなたのModを識別するためのユニークなIDを考えて記入してください。既に存在するModとIDが被っている場合は、Modはアップロードできません。IDは、容易に被らないような名前が好ましいです。"
                    ]))}}
                    public const string Id = {{sourceBuilder.ToStringLiteral(define.Package.Id)}}
                #if DEBUG
                    + {{(define.Mod.UseDebugId ? $"{sourceBuilder.ToStringLiteral(".debug")}" : sourceBuilder.EmptyStringLiteral)}}
                #endif
                    ;
                
                    {{docHeader("package", "author")}}
                    {{sourceBuilder.Xml.Build(g => g.Remarks([
                        "あなたの作者名を記述してください。何でも構いません。"
                    ]))}}
                    public const string Author = {{sourceBuilder.ToStringLiteral(define.Package.Author)}};

                    {{docHeader("package", "loadPriority")}}
                    {{sourceBuilder.Xml.Build(g => g.Remarks([
                        "Modがロードされる順番を指定します。0～任意の数字を記述してください。数字が低いModほど先に読み込まれます。"
                    ]))}}
                    public const int LoadPriority = {{define.Package.LoadPriority}};

                    {{docHeader("package", "version")}}
                    {{sourceBuilder.Xml.Build(g => g.Remarks([
                        "あなたのModの動作が最後に確認できたElin本体のバージョンを記述してください。現在のところは、面倒なら頻繁にバージョンの記述は更新しなくてもかまいません。",
                        "Elin本体にModに関わる大きな変更があった場合、その時の本体のバージョンより古いバージョンが記述されているModは読み込まれなくなります。"
                    ]))}}
                    public const string Version = {{sourceBuilder.ToStringLiteral(define.Package.ElinVersion)}};

                    {{docHeader("package", "description")}}
                    {{sourceBuilder.Xml.Build(g => g.Remarks([
                        "あなたのModの説明文です。最初にWorkshopにModをアップロードする際に、ここで記述されたテキストがModの説明文として表示されます。Modを更新する際は、このテキストは無視されます。説明文の変更が必要な場合は、Workshopから変更してください。"
                    ]))}}
                    public const string Description = {{sourceBuilder.ToStringLiteral(sourceBuilder.JoinLines(define.Package.Description))}};

                    {{docHeader("package", "tags")}}
                    {{sourceBuilder.Xml.Build(g => g.Fragment([
                        g.Remarks([
                            "Workshopに登録するタグをカンマ区切りで指定します（複数ある場合）。任意のタグを登録しても構いません。公式タグを設定すると、Workshopのカテゴリに表示されるようになります。詳しくはElin Workshop Tagをご覧ください。"
                        ]),
                        g.SeeAlsoHref("https://docs.google.com/document/u/0/d/15XNbNsMmv1SPfFaomMq_crvJsOaBxhg-mb6JiaE1z4I/edit&sa=D&source=editors&ust=1774327536481473&usg=AOvVaw3pWzcZXUsacwVbBd511J4p")
                    ]))}}
                    public static readonly string[] Tags = {{(define.Package.Tags == null ? "[]" : "[" + string.Join(", ", define.Package.Tags.Select(a => sourceBuilder.ToStringLiteral(a))) + "]")}};

                    {{docHeader("package", "visibility")}}
                    {{sourceBuilder.Xml.Build(g => g.Remarks([
                        g.Paragraph("アップロードしたModの公開範囲を指定できます。指定できる値は以下の通りです。"),
                        g.List(XmlDocumentListType.Bullet, [
                            "Public",
                            "Unlisted",
                            "Private",
                            "FriendsOnly",
                        ])
                    ]))}}
                    public static string Visibility { get; set; } = {{sourceBuilder.ToStringLiteral(define.Package.Visibility ?? "Public")}};
                }

                internal static class Mod
                {
                    {{docHeader("mod", "name")}}
                    {{sourceBuilder.Xml.Build(g => g.Remarks([
                        "MOD の内部名です。",
                        "アセンブリ名を参照しています。"
                    ]))}}
                    public const string Name = {{sourceBuilder.ToStringLiteral(macro.AssemblyName)}};
                    {{docHeader("mod", "version")}}
                    {{sourceBuilder.Xml.Build(g => g.Remarks([
                        "作成している MOD のバージョンです。",
                        "アセンブリバージョンにも適用されます。"
                    ]))}}
                    public const string Version = {{sourceBuilder.ToStringLiteral(define.Mod.Version)}};

                    {{docHeader("mod", "useDebugId")}}
                    {{sourceBuilder.Xml.Build(g => g.Remarks("デバッグ時に Mod の ID を変更するか"))}}
                    public const bool UseDebugId = {{define.Mod.UseDebugId.ToString().ToLowerInvariant()}};
                
                #if DEBUG
                    {{docHeader("mod", "log")}}
                    {{sourceBuilder.Xml.Build(g => g.Remarks([
                        g.Paragraph("デバッグ時に出力されるファイルパス。"),
                        g.Paragraph([
                            g.SeeCref("IsEnabledLogFile"),
                            g.Text("が有効な場合に使用可能です。"),
                        ]),
                    ]))}}
                    public const string LogFile = {{(string.IsNullOrWhiteSpace(define.Mod.Log) ? sourceBuilder.ToStringLiteral("") : sourceBuilder.ToStringLiteral(Environment.ExpandEnvironmentVariables(define.Mod.Log)))}};

                    {{sourceBuilder.Xml.Build(g => g.Remarks([
                        g.Paragraph([
                            g.SeeCref("LogFile"),
                            g.Text("が有効かどうかを示します。"),
                        ]),
                    ]))}}
                    public static bool IsEnabledLogFile => !string.IsNullOrWhiteSpace(LogFile) && !LogFile.Equals("NUL", StringComparison.OrdinalIgnoreCase);

                #endif
                }
                """;
                c.AddSource("PluginInfo.g.cs", sourceBuilder.Format(source));
            });
        }

        #endregion
    }
}
