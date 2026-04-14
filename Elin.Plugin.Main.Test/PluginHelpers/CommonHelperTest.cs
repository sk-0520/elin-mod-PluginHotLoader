using Elin.Plugin.Main.PluginHelpers;
using System.Collections.Generic;

namespace Elin.Plugin.Main.Test.PluginHelpers
{
    public class CommonHelperTest
    {
        #region function

        [Theory]
        [InlineData(new string[0], "")]
        [InlineData(new[] { "", }, "\r")]
        [InlineData(new[] { "", }, "\n")]
        [InlineData(new[] { "", }, "\r\n")]
        [InlineData(new[] { "line1", "line2", "line3" }, "line1\rline2\rline3")]
        [InlineData(new[] { "line1", "line2", "line3" }, "line1\nline2\nline3")]
        [InlineData(new[] { "line1", "line2", "line3" }, "line1\r\nline2\r\nline3")]
        public void ReadLinesTest(IEnumerable<string> expecteds, string lines)
        {
            var helper = new CommonHelper();
            var actual = helper.ReadLines(lines);
            Assert.Equal(expecteds, actual);
        }

        #endregion
    }
}
