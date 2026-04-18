using Elin.Plugin.Main.PluginHelpers;
using System.Threading;

namespace Elin.Plugin.Main.Test.PluginHelpers
{
    public class MessageHelperTest
    {
        #region function

        [Theory]
        [InlineData("<null>", null)]
        [InlineData("", "")]
        [InlineData("abc", "abc")]
        [InlineData("123", 123)]
        public void ToLogDataTest(string expected, object? data)
        {
            var test = new MessageHelper(SynchronizationContext.Current);
            var result = test.ToLogData(data);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToLogData_Anonymous_Test()
        {
            var test = new MessageHelper(SynchronizationContext.Current);
            var obj = new { Name = "name", Age = 123 };
            var expected = "{ Name = name, Age = 123 }";
            var result = test.ToLogData(obj);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToLogData_Tuple_Test()
        {
            var test = new MessageHelper(SynchronizationContext.Current);
            var obj = (name: "Name", age: 123);
            var expected = "(Name, 123)";
            var result = test.ToLogData(obj);
            Assert.Equal(expected, result);
        }

        private sealed class ToLogData_Object_NotNull_Class
        { }

        [Fact]
        public void ToLogData_Object_NotNull_Test()
        {
            var test = new MessageHelper(SynchronizationContext.Current);
            var obj = new ToLogData_Object_NotNull_Class();
            var expected = typeof(ToLogData_Object_NotNull_Class).FullName;
            var result = test.ToLogData(obj);
            Assert.Equal(expected, result);
        }

        private sealed class ToLogData_Object_Nullable_Class
        {
            public ToLogData_Object_Nullable_Class(int value)
            {
                Value = value;
            }

            #region property

            private int Value { get; }

            #endregion

            #region object

            public override string? ToString()
            {
                return (Value % 10 == 0)
                    ? null
                    : Value.ToString() + "!"
                ;
            }

            #endregion
        }

        private const string MessageHelperTest_Header = "Elin.Plugin.Main.Test.PluginHelpers." + nameof(MessageHelperTest);
        [Theory]
        [InlineData("1!", 1)]
        [InlineData("9!", 9)]
        [InlineData("<" + MessageHelperTest_Header + "+" + nameof(ToLogData_Object_Nullable_Class) + ">", 0)]
        [InlineData("<" + MessageHelperTest_Header + "+" + nameof(ToLogData_Object_Nullable_Class) + ">", 10)]
        [InlineData("<" + MessageHelperTest_Header + "+" + nameof(ToLogData_Object_Nullable_Class) + ">", 20)]
        public void ToLogData_Object_Nullable_Test(string expected, int input)
        {
            var test = new MessageHelper(SynchronizationContext.Current);
            var obj = new ToLogData_Object_Nullable_Class(input);
            var result = test.ToLogData(obj);
            Assert.Equal(expected, result);
        }

        #endregion
    }
}
