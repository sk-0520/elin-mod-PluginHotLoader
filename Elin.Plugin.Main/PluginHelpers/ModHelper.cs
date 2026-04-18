using BepInEx;
using BepInEx.Logging;
using Elin.Plugin.Generated;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Elin.Plugin.Main.PluginHelpers
{
    /// <summary>
    /// Mod 開発のヘルパークラス。
    /// </summary>
    /// <remarks>Mod 用テンプレート組み込み想定。</remarks>
    public static class ModHelper
    {
        #region property

        /// <summary>
        /// デバッグモードで実行中か。
        /// </summary>
        public static bool IsDebug =>
#if DEBUG
             true
#else
            false
#endif
             ;

        /// <summary>
        /// プラグイン。
        /// </summary>
        public static BaseUnityPlugin Plugin { get; private set; } = default!;
        /// <summary>
        /// <see cref="BepInEx"/>の提供するロガー。
        /// </summary>
        public static ManualLogSource Logger { get; private set; } = default!;
        public static SynchronizationContext Context { get; set; } = default!;
        /// <summary>
        /// メッセージ出力ヘルパー。
        /// </summary>
        public static MessageHelper Message { get; private set; } = default!;

#if DEBUG
        /// <summary>
        /// ファイル出力ヘルパー。
        /// </summary>
        /// <remarks>DEBUG 時のみ有効。</remarks>
        private static FileLogger FileLogger { get; set; } = default!;
#endif

        /// <summary>
        /// プラグイン用言語ヘルパー。
        /// </summary>
        /// <remarks>言語定義は Localization.json を編集することで自動的に適用されます。</remarks>
        internal static PluginLocalization Lang { get; } = new PluginLocalization();

        /// <summary>
        /// Elin ヘルパー。
        /// </summary>
        internal static ElinHelper Elin { get; } = new ElinHelper();

        /// <summary>
        /// 共通的な処理。
        /// </summary>
        internal static CommonHelper Common { get; } = new CommonHelper();

        #endregion

        #region function

        /// <summary>
        /// 初期化。
        /// </summary>
        /// <remarks><see cref="BaseUnityPlugin"/>の開始時に設定する想定。</remarks>
        /// <param name="plugin"></param>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        /// <example>
        /// <code>
        /// class Plugin : BaseUnityPlugin
        /// {
        ///     public void Awake()
        ///     {
        ///         ModHelper.Initialize(this, Logger);
        ///     }
        /// }
        /// </code>
        /// </example>
        public static void Initialize(BaseUnityPlugin plugin, ManualLogSource logger, SynchronizationContext context)
        {
            Plugin = plugin;
            Logger = logger;
            Context = context;
            Message = new MessageHelper(context);

#if DEBUG
            FileLogger = new FileLogger(Mod.LogFile);
#endif
        }

        /// <summary>
        /// <remarks><see cref="BaseUnityPlugin"/>の終了時に設定する想定。</remarks>
        /// </summary>
        /// <remarks>Finalizeだと怒られるんや。</remarks>
        /// <example>
        /// <code>
        /// class Plugin : BaseUnityPlugin
        /// {
        ///     public void OnDestroy()
        ///     {
        ///         ModHelper.Destroy();
        ///     }
        /// }
        /// </code>
        /// </example>
        public static void Destroy()
        {
#if DEBUG
            FileLogger?.Dispose();
#endif
        }

        public static string ToStringFromInformation()
        {
            static string ToStringFrom(Type type)
            {
                var bindingFlags = BindingFlags.Static | BindingFlags.Public;
                var members = type.GetMembers(bindingFlags).OrderBy(a => a.Name);

                var sb = new StringBuilder();
                sb.Append('[');
                sb.Append(type.FullName);
                sb.Append(']');
                sb.AppendLine();

                foreach (var member in members)
                {
                    object value;
                    switch (member)
                    {
                        case FieldInfo i:
                            value = i.GetValue(null);
                            break;

                        case PropertyInfo i:
                            value = i.GetValue(null, null);
                            break;

                        case MethodInfo:
                            continue;

                        default:
                            throw new System.NotImplementedException($"[{member.GetType()}] {member.MemberType} {member.Name}");
                    }
                    sb.Append(" > ");
                    sb.Append(member.Name);
                    sb.Append(": ");
                    sb.Append(value);
                    sb.AppendLine();
                }

                return sb.ToString();
            }

            return string.Join(
                Environment.NewLine,
                ToStringFrom(typeof(Package)),
                ToStringFrom(typeof(Mod))
            );
        }

        /// <summary>
        /// 開発時専用処理。
        /// </summary>
        /// <param name="action"></param>
        [Conditional("DEBUG")]
        public static void DoDev(Action action)
        {
            if (!IsDebug)
            {
                return;
            }

            action();
        }

        /// <summary>
        /// 開発ログ出力。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s">出力ログ。</param>
        /// <param name="color">メッセージ色。</param>
        /// <param name="outputMessage">メッセージ出力するか。</param>
        /// <param name="outputLogFile">デバッグログファイルに出力するか。</param>
        /// <param name="callerMemberName"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerLineNumber"></param>
        [Conditional("DEBUG")]
        private static void LogDevCore<T>(T s, Color color, bool outputMessage, bool outputLogFile, string callerMemberName, string callerFilePath, int callerLineNumber)
        {
            if (!IsDebug)
            {
                return;
            }

            var timestamp = Message.ToTimestamp(DateTime.Now);
            var header = Message.ToLogHeader(timestamp, callerMemberName, callerFilePath, callerLineNumber);
            var data = Message.ToLogData(s);
#if DEBUG
            if (outputLogFile)
            {
                if (Mod.IsEnabledLogFile)
                {
                    var lines = new[] {
                        header,
                        data,
                    };
                    FileLogger.Log(lines);
                }
            }
#endif
            Logger.LogDebug($"{header}: {data}");

            if (outputMessage)
            {
                var lines = data.SplitNewline();
                Message.DoMessage(() =>
                {
                    using (Message.PreserveColor())
                    {
                        Msg.NewLine();

                        // 本メソッドはユーザー向けではなく実装者向けのため、Package.Title ではなく Mod.Name を使用する
                        // 膨大なログが表示されていても、それが Mod.Name となっていればリリース版には表示されないため、誤ってリリース版にログを混入させたかどうかの不安は減る
                        // デバッグ版をリリースしたのであれば知らない。。。
                        Msg.SetColor(Color.cyan);
                        Msg.SayRaw($"<{Mod.Name}> ");

                        Msg.SetColor(color);
                        foreach (var line in lines)
                        {
                            Message.OutputLineWithoutContext(line);
                        }
                    }
                });
            }
        }

        /// <summary>
        /// 開発メッセージ出力。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s">出力メッセージ。</param>
        /// <param name="color">メッセージ色。</param>
        /// <param name="callerMemberName"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerLineNumber"></param>
        [Conditional("DEBUG")]
        public static void MessageDev<T>(T s, Color color, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            LogDevCore(s, color, true, false, callerMemberName, callerFilePath, callerLineNumber);
        }

        /// <inheritdoc cref="MessageDev{T}(T, Color, string, string, int)"/>
        [Conditional("DEBUG")]
        public static void MessageDev<T>(T s, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            LogDevCore(s, Msg.currentColor, true, false, callerMemberName, callerFilePath, callerLineNumber);
        }

        /// <summary>
        /// 開発ファイルログ出力。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s">出力ログ。</param>
        /// <param name="callerMemberName"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerLineNumber"></param>
        [Conditional("DEBUG")]
        public static void WriteDev<T>(T s, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            LogDevCore(s, Msg.currentColor, false, true, callerMemberName, callerFilePath, callerLineNumber);
        }

        /// <summary>
        /// 開発ログメッセージと開発ログ出力。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s">出力ログ。</param>
        /// <param name="color">メッセージ色。</param>
        /// <param name="callerMemberName"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerLineNumber"></param>
        [Conditional("DEBUG")]
        public static void LogDev<T>(T s, Color color, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            LogDevCore(s, color, true, true, callerMemberName, callerFilePath, callerLineNumber);
        }

        /// <inheritdoc cref="LogDev{T}(T, Color, string, string, int)"/>
        [Conditional("DEBUG")]
        public static void LogDev<T>(T s, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            LogDevCore(s, Msg.currentColor, true, true, callerMemberName, callerFilePath, callerLineNumber);
        }

        /// <summary>
        /// 未想定のログ出力。
        /// </summary>
        /// <param name="lines">ログ一覧。</param>
        /// <param name="callerMemberName"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerLineNumber"></param>
        public static void LogNotExpected(IEnumerable<string> lines, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            var timestamp = Message.ToTimestamp(DateTime.Now);
            var header = Message.ToLogHeader(timestamp, callerMemberName, callerFilePath, callerLineNumber);
#if DEBUG
            if (Mod.IsEnabledLogFile)
            {
                var debugLines = new List<string> {
                    header,
                };
                debugLines.AddRange(lines);
                FileLogger.Log(debugLines);
            }
#endif
            Logger.LogError($"{header}: {string.Join(Environment.NewLine, lines)}");

            Message.DoMessage(() =>
            {
                using (Message.UseColor(Color.yellow))
                {
                    Msg.NewLine();
                    Msg.SayRaw($"!!!! {Package.Title}:NotExpected !!!!");
                    Msg.SayRaw($"[{callerMemberName}] {Path.GetFileName(callerFilePath)}:{callerLineNumber}");
                    foreach (var line in lines)
                    {
                        Message.OutputLineWithoutContext(line);
                    }
                }
            });
        }

        /// <inheritdoc cref="LogNotExpected(IEnumerable{string}, string, string, int)"/>
        /// <param name="s">ログ内容。</param>
        /// <param name="callerMemberName"><inheritdoc cref="LogNotExpected(IEnumerable{string}, string, string, int)"/></param>
        /// <param name="callerFilePath"><inheritdoc cref="LogNotExpected(IEnumerable{string}, string, string, int)"/></param>
        /// <param name="callerLineNumber"><inheritdoc cref="LogNotExpected(IEnumerable{string}, string, string, int)"/></param>
        public static void LogNotExpected(string s, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            LogNotExpected([s], callerMemberName, callerFilePath, callerLineNumber);
        }

        /// <inheritdoc cref="LogNotExpected(IEnumerable{string}, string, string, int)"/>
        /// <param name="exception">例外。</param>
        /// <param name="callerMemberName"><inheritdoc cref="LogNotExpected(IEnumerable{string}, string, string, int)"/></param>
        /// <param name="callerFilePath"><inheritdoc cref="LogNotExpected(IEnumerable{string}, string, string, int)"/></param>
        /// <param name="callerLineNumber"><inheritdoc cref="LogNotExpected(IEnumerable{string}, string, string, int)"/></param>
        public static void LogNotExpected(Exception exception, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            LogNotExpected(exception.ToString(), callerMemberName, callerFilePath, callerLineNumber);
        }

        /// <summary>
        /// ユーザー通知処理。
        /// </summary>
        /// <remarks>
        /// <para>プログラムからのユーザーへの通知として機能する。</para>
        /// <para>ゲーム体験はガン無視でよい。</para>
        /// </remarks>
        /// <param name="logLevel"></param>
        /// <param name="message"></param>
        /// <param name="outputMessage"></param>
        /// <param name="callerMemberName"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="callerLineNumber"></param>
        public static void LogNotify(LogLevel logLevel, string message, bool outputMessage = true, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            Logger.Log(logLevel, message);
#if DEBUG
            FileLogger.Log($"{logLevel} {message}");
#endif
            if (outputMessage)
            {
                Message.DoMessage(() =>
                {
                    using (Message.PreserveColor())
                    {
                        var color = Message.GetLogLevelColor(logLevel);

                        Msg.SetColor(Color.cyan);
                        Msg.SayRaw($"<{Package.Title}> ");

                        Msg.SetColor(color);
                        Message.OutputLineWithoutContext(message);
                    }
                });
            }
        }

        #endregion
    }
}
