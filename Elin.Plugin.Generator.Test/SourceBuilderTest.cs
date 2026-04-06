using Microsoft.CodeAnalysis;
using System;

#pragma warning disable CA1707 // 識別子はアンダースコアを含むことはできません
#pragma warning disable CA1711 // 識別子は、不適切なサフィックスを含むことはできません
#pragma warning disable CA1515 // パブリック型を内部にすることを検討してください
#pragma warning disable CA1052 // スタティック ホルダー型は Static または NotInheritable でなければなりません
#pragma warning disable CA1034 // 入れ子にされた型を参照可能にすることはできません

#pragma warning disable CA1050 // 名前空間で型を宣言します
public enum EmptyNamespaceEnum
{
    A,
}

public class EmptyNamespaceClass
{
}
#pragma warning restore CA1050 // 名前空間で型を宣言します

namespace Elin.Plugin.Generator.Test
{

    public enum TestEnum
    {
        A
    }

    public class TestClass
    {
    }

    public class SourceBuilderTest
    {
        #region define

        public enum InnerEnum
        {
            A
        }

        public class InnerClass
        {
        }

        #endregion

        #region function

        [Theory]
        [InlineData("global::EmptyNamespaceEnum.A", EmptyNamespaceEnum.A)]
        [InlineData("global::Elin.Plugin.Generator.Test.TestEnum.A", TestEnum.A)]
        [InlineData("global::Elin.Plugin.Generator.Test.SourceBuilderTest+InnerEnum.A", InnerEnum.A)]
        public void ToCode_enum_Test(string expected, Enum input)
        {
            var sourceBuilder = new SourceBuilder();
            var actual = sourceBuilder.ToCode(input);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ToCode_EmptyNamespaceClass_Test()
        {
            var sourceBuilder = new SourceBuilder();
            var actual = sourceBuilder.ToCode<EmptyNamespaceClass>();
            var expected = "global::EmptyNamespaceClass";
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ToCode_TestClass_Test()
        {
            var sourceBuilder = new SourceBuilder();
            var actual = sourceBuilder.ToCode<TestClass>();
            var expected = "global::Elin.Plugin.Generator.Test.TestClass";
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ToCode_InnerClass_Test()
        {
            var sourceBuilder = new SourceBuilder();
            var actual = sourceBuilder.ToCode<InnerClass>();
            var expected = "global::Elin.Plugin.Generator.Test.SourceBuilderTest+InnerClass";
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("private", Accessibility.Private)]
        [InlineData("private protected", Accessibility.ProtectedAndInternal)]
        [InlineData("protected", Accessibility.Protected)]
        [InlineData("internal", Accessibility.Internal)]
        [InlineData("protected internal", Accessibility.ProtectedOrInternal)]
        [InlineData("public", Accessibility.Public)]
        public void ToCode_Accessibility_Test(string expected, Accessibility accessibility)
        {
            var sourceBuilder = new SourceBuilder();
            var actual = sourceBuilder.ToCode(accessibility);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(Accessibility.NotApplicable)]
        public void ToCode_Accessibility_Throw_Test(Accessibility accessibility)
        {
            var sourceBuilder = new SourceBuilder();
            Assert.Throws<NotSupportedException>(() => sourceBuilder.ToCode(accessibility));
        }

        [Theory]
        [InlineData("' '", ' ')]
        [InlineData("'a'", 'a')]
        [InlineData("'\\r'", '\r')]
        [InlineData("'\\n'", '\n')]
        [InlineData("'\\t'", '\t')]
        public void ToCharLiteralTest(string expected, char input)
        {
            var sourceBuilder = new SourceBuilder();
            var actual = sourceBuilder.ToCharLiteral(input);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("\"\"", "")]
        [InlineData("\"a\"", "a")]
        [InlineData("\"\\r\"", "\r")]
        [InlineData("\"\\n\"", "\n")]
        [InlineData("\"\\r\\n\"", "\r\n")]
        [InlineData("\"\\t\"", "\t")]
        public void ToStringLiteralTest(string expected, string input)
        {
            var sourceBuilder = new SourceBuilder();
            var actual = sourceBuilder.ToStringLiteral(input);
            Assert.Equal(expected, actual);
        }

        public static TheoryData<string, string, string> ApplyIndentData => new()
        {
            {
                "",
                "",
                ""
            },
            {
                "",
                "<INDENT>",
                ""
            },
            {
                "<INDENT>a",
                "<INDENT>",
                "a"
            },
             {
                $"<INDENT>a{Environment.NewLine}<INDENT>b{Environment.NewLine}<INDENT>c{Environment.NewLine}<INDENT>d",
                "<INDENT>",
                "a\rb\nc\r\nd"
            },
        };

        [Theory]
        [MemberData(nameof(ApplyIndentData))]
        public void ApplyIndentTest(string expected, string indent, string source)
        {
            var sourceBuilder = new SourceBuilder();
            var actual = sourceBuilder.ApplyIndent(indent, source);
            Assert.Equal(expected, actual);
        }

        public static TheoryData<string, string, string> ApplyXmlDocumentCommentData => new()
        {
            {
                "",
                "",
                ""
            },
            {
                "",
                "<INDENT>",
                ""
            },
            {
                "<INDENT>/// a",
                "<INDENT>",
                "a"
            },
             {
                $"<INDENT>/// a{Environment.NewLine}<INDENT>/// b{Environment.NewLine}<INDENT>/// c{Environment.NewLine}<INDENT>/// d",
                "<INDENT>",
                "a\rb\nc\r\nd"
            },
        };

        [Theory]
        [MemberData(nameof(ApplyXmlDocumentCommentData))]
        public void ApplyXmlDocumentCommentTest(string expected, string indent, string source)
        {
            var sourceBuilder = new SourceBuilder();
            var actual = sourceBuilder.ApplyXmlDocumentComment(indent, source);
            Assert.Equal(expected, actual);
        }

        #endregion
    }
}
