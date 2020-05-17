using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IdentifierTests
    {
        public static readonly object[][] ValidIdentifiers =
        {
            new [] { "a" },
            new [] { "a1" },
            new [] { "abc123" },
            new [] { "\u00E1" }, // á
            new [] { "a\u0301" }, // á
            new [] { "\u00E1123" },
            new [] { "a\u0301123" },
            new [] { "\u0639\u064E\u0631\u064E\u0628\u06CC" }, // عَرَبی (Arabic)
            new [] { "\u3042\u30FC" }, // あー (Katakana-Hiragana Prolonged Sound Mark)
            new [] { "\u6F22\u5B57" }, // 漢字
            new [] { "\u0621\u06F1" }, // ء۱ (Arabic Letter + Digit)
            new [] { "a\u200Db" }, // ZWJ (Cf category)
            new [] { "\u02B0\u02B1" }, // ʰʱ (Lm category)
            new [] { "\u2160\u2161" }, // ⅠⅡ (Nl category)
            new [] { "a\u203Fb\u2040c" }, // Tie (Pc category)
        };

        public static readonly object[][] InvalidIdentifiers =
        {
            new [] { "" },
            new [] { "1" },
            new [] { "+" },
            new [] { "\u06F1" }, // ء (Arabic Digit)
            new [] { "\u2200" }, // ∀
            new [] { "ab+" },
            new [] { "ab\u2200" },
            new [] { "\u3042\u2015" }, // あ― (Horizontal Bar)
            // Incomplete Surrogates
            new [] { "\uD800" },
            new [] { "a\uD800" },
            new [] { "\uD800a" },
            new [] { "\uDF00" },
            new [] { "a\uDF00" },
            new [] { "\uDF00a" },
            new [] { "\uD800\u200D\uDF00" }, // ZWJ between a valid surrogate pair 𐌀 (Old Italic A)
        };

        [Theory]
        [MemberData(nameof(ValidIdentifiers))]
        public void TestValidIdentifiers(string identifierName)
        {
            Assert.True(SyntaxFacts.IsValidIdentifier(identifierName));
        }

        [Theory]
        [MemberData(nameof(InvalidIdentifiers))]
        public void TestInvalidIdentifiers(string identifierName)
        {
            Assert.False(SyntaxFacts.IsValidIdentifier(identifierName));
        }
    }
}
