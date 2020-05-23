using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    internal class IdentifierTestData
    {
        private static readonly string[] _validIdentifiers =
        {
            "a",
            "a1",
            "abc123",

            // Latin-1
            "\u00E1", // á
            "\u00E1\u00FF", // áÿ
            "\u00E1123",

            // Mn category
            "a\u0301", // á
            "a\u0301123",

            "\u0639\u064E\u0631\u064E\u0628\u06CC", // عَرَبی (Arabic)
            "\u3042\u30FC", // あー (Katakana-Hiragana Prolonged Sound Mark)
            "\u6F22\u5B57", // 漢字
            "\u0621\u06F1", // ء۱ (Arabic Letter + Digit)
            "\u02B0\u02B1", // ʰʱ (Lm category)
            "\u2160\u2161", // ⅠⅡ (Nl category)
            "a\u203Fb\u2040c", // Tie (Pc category)

            // contains Cf characters
            "a\u200Db", // ZWJ
            "a\u200Fb", // Right-to-Left mark

        };

        private static readonly string[] _invalidIdentifiers =
        {
            "1",
            "\u06F1", // ء (Arabic Digit)
            "\u2200", // ∀
            "\u2015", // ― (Horizontal Bar)
        };

        public static readonly IEnumerable<object[]> Identifiers;

        static IdentifierTestData()
        {
            Identifiers = BuildIdentifierList();
        }

        private static IEnumerable<object[]> BuildIdentifierList()
        {
            StringBuilder builder = new StringBuilder();
            List<object[]> identifiers = new List<object[]>();

            foreach (var x in _validIdentifiers)
            {
                identifiers.Add(new object[] { true, x, removeCf(x) });
            }

            foreach (var x in _invalidIdentifiers)
            {
                identifiers.Add(new object[] { false, x, removeCf(x) });
            }

            return identifiers;

            string removeCf(string s)
            {
                if (!s.Any(c => CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.Format))
                {
                    return s;
                }

                builder.Clear();
                foreach (var c in s)
                {
                    if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.Format)
                    {
                        builder.Append(c);
                    }
                }
                return builder.ToString();
            }
        }
    }
}
