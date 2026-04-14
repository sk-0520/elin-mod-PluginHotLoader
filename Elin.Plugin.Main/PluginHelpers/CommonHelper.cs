using System.Collections.Generic;
using System.IO;

namespace Elin.Plugin.Main.PluginHelpers
{
    /// <summary>
    /// よく使う共通処理を集約しておく。
    /// </summary>
    /// <remarks>このプロジェクトでは定義するがこのプロジェクト内では使用しない。</remarks>
    public class CommonHelper
    {
        #region function

        /// <summary>
        /// 改行で分割。
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <remarks>Elin 側で改行処理あるので理由がなければそちらを～。</remarks>
        /// <seealso cref="ClassExtension.SplitNewline(string)"/>
        /// <seealso cref="ClassExtension.SplitByNewline(string)"/>
        public IEnumerable<string> ReadLines(string input)
        {
            using var reader = new StringReader(input);
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                yield return line;
            }
        }

        #endregion
    }
}
