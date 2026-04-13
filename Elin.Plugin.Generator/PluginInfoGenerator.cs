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
                .Select((a, _) =>
                {
                    var define = JsonSerializer.Deserialize<PluginDefine>(a.json!, JsonSerializerOptions);

                    if (define is not null)
                    {
                        var devPath = Path.Combine(Path.GetDirectoryName(a.file.Path), GeneratorConstants.PluginInfoDevFileName);
                        if (File.Exists(devPath))
                        {
                            var devJson = File.ReadAllText(devPath);
                            var devDefine = JsonSerializer.Deserialize<PluginDevDefine>(devJson, JsonSerializerOptions);
                            if (devDefine is not null)
                            {
                                // たとえ空文字列でも未設定でないのであれば上書きする
                                if (devDefine.Log is not null)
                                {
                                    define.Mod.Log = devDefine.Log;
                                }
                            }
                        }
                    }

                    return define;
                })
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

            context.RegisterSourceOutput(define.Combine(macroProvider), (c, x) =>
            {
                var define = x.Left;
                var macro = x.Right;
                if (define == null)
                {
                    return;
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
                    return $$"""
                    /// <summary>{{GeneratorConstants.PluginInfoFileName}}: $.{{parent}}.{{property}}</summary>
                    """;
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
                /// <summary>
                /// package.xml を生成するためだけのクラス。
                /// </summary>
                /// <remarks><see langword="public" />だがプラグイン側では使用せず、ビルド時のみ使用する想定。</remarks>
                /// <see cref="Package"/>
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
                
                /// <summary>
                /// package.xml 参照情報。
                /// </summary>
                /// <seealso href="https://docs.google.com/document/d/e/2PACX-1vQSITB8aYTycrnn3PxxGnPjNZ2_y1G3LDfXjC_PM5S_mTPCh6fv1vcj1bkfPbbUZ88WVb5_7T-62zYc/pub" />
                internal static class Package
                {
                    {{docHeader("package", "title")}}
                    /// <remarks>
                    /// <para>このタグの中には、あなたのModのタイトルを記入してください。最初にWorkshopにModをアップロードする際に、ここで記述されたテキストがModのタイトルとして表示されます。</para>
                    /// <para>Modを更新する際は、このテキストは無視されます。タイトルの変更が必要な場合は、Workshopから変更してください。</para>
                    /// </remarks>
                    public const string Title = {{sourceBuilder.ToStringLiteral(define.Package.Title)}};

                    {{docHeader("package", "id")}}
                    /// <remarks>
                    /// <para>ここでは、あなたのModを識別するためのユニークなIDを考えて記入してください。既に存在するModとIDが被っている場合は、Modはアップロードできません。IDは、容易に被らないような名前が好ましいです。</para>
                    /// </remarks>
                    public const string Id = {{sourceBuilder.ToStringLiteral(define.Package.Id)}}
                #if DEBUG
                    + {{(define.Mod.UseDebugId ? $"{sourceBuilder.ToStringLiteral(".debug")}" : sourceBuilder.EmptyStringLiteral)}}
                #endif
                    ;
                
                    {{docHeader("package", "author")}}
                    /// <remarks>
                    /// <para>あなたの作者名を記述してください。何でも構いません。</para>
                    /// </remarks>
                    public const string Author = {{sourceBuilder.ToStringLiteral(define.Package.Author)}};

                    {{docHeader("package", "loadPriority")}}
                    /// <remarks>
                    /// <para>Modがロードされる順番を指定します。0～任意の数字を記述してください。数字が低いModほど先に読み込まれます。</para>
                    /// </remarks>
                    public const int LoadPriority = {{define.Package.LoadPriority}};

                    {{docHeader("package", "version")}}
                    /// <remarks>
                    /// <para>あなたのModの動作が最後に確認できたElin本体のバージョンを記述してください。現在のところは、面倒なら頻繁にバージョンの記述は更新しなくてもかまいません。</para>
                    /// <para>Elin本体にModに関わる大きな変更があった場合、その時の本体のバージョンより古いバージョンが記述されているModは読み込まれなくなります。</para>
                    /// </remarks>
                    public const string Version = {{sourceBuilder.ToStringLiteral(define.Package.ElinVersion)}};

                    {{docHeader("package", "description")}}
                    /// <remarks>
                    /// <para>あなたのModの説明文です。最初にWorkshopにModをアップロードする際に、ここで記述されたテキストがModの説明文として表示されます。Modを更新する際は、このテキストは無視されます。説明文の変更が必要な場合は、Workshopから変更してください。</para>
                    /// </remarks>
                    public const string Description = {{sourceBuilder.ToStringLiteral(sourceBuilder.JoinLines(define.Package.Description))}};

                #pragma warning disable CS1570 // XML コメントの XML 形式が正しくありません
                    {{docHeader("package", "tags")}}
                    /// <remarks>
                    /// <para>Workshopに登録するタグをカンマ区切りで指定します（複数ある場合）。任意のタグを登録しても構いません。公式タグを設定すると、Workshopのカテゴリに表示されるようになります。詳しくはElin Workshop Tagをご覧ください。</para>
                    /// </remarks>
                    /// <seealso href="https://docs.google.com/document/u/0/d/15XNbNsMmv1SPfFaomMq_crvJsOaBxhg-mb6JiaE1z4I/edit&sa=D&source=editors&ust=1774327536481473&usg=AOvVaw3pWzcZXUsacwVbBd511J4p" />
                    public static readonly string[] Tags = {{(define.Package.Tags == null ? "[]" : "[" + string.Join(", ", define.Package.Tags.Select(a => sourceBuilder.ToStringLiteral(a))) + "]")}};
                #pragma warning restore CS1570 // XML コメントの XML 形式が正しくありません

                    {{docHeader("package", "visibility")}}
                    /// <remarks>
                    /// <para>アップロードしたModの公開範囲を指定できます。指定できる値は以下の通りです。</para>
                    /// <list type="bullet">
                    /// <item>Public</item>
                    /// <item>Unlisted</item>
                    /// <item>Private</item>
                    /// <item>FriendsOnly</item>
                    /// </list>
                    /// </remarks>
                    public static string Visibility { get; set; } = {{sourceBuilder.ToStringLiteral(define.Package.Visibility ?? "Public")}};
                }

                internal static class Mod
                {
                    {{docHeader("mod", "name")}}
                    /// <remarks>
                    /// <para>MOD の内部名です。</para>
                    /// <para>アセンブリ名を参照しています。</para>
                    /// </remarks>
                    public const string Name = {{sourceBuilder.ToStringLiteral(macro.AssemblyName)}};
                    {{docHeader("mod", "version")}}
                    /// <remarks>
                    /// <para>作成している MOD のバージョンです。</para>
                    /// <para>アセンブリバージョンにも適用されます。</para>
                    /// </remarks>
                    public const string Version = {{sourceBuilder.ToStringLiteral(define.Mod.Version)}};

                    {{docHeader("mod", "useDebugId")}}
                    /// <remarks>デバッグ時に Mod の ID を変更するか</remarks>
                    public const bool UseDebugId = {{define.Mod.UseDebugId.ToString().ToLowerInvariant()}};
                
                #if DEBUG
                    {{docHeader("mod", "log")}}
                    /// <remarks>
                    /// <para>デバッグ時に出力されるファイルパス。</para>
                    /// </remarks>
                    /// <remarks><see cref="IsEnabledLogFile"/> が有効な場合に使用可能です。</remarks>
                    public const string LogFile = {{(string.IsNullOrWhiteSpace(define.Mod.Log) ? sourceBuilder.ToStringLiteral("") : sourceBuilder.ToStringLiteral(Environment.ExpandEnvironmentVariables(define.Mod.Log)))}};

                    /// <remarks>
                    /// <see cref="LogFile"/> が有効かどうかを示します。
                    /// </remarks>
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
