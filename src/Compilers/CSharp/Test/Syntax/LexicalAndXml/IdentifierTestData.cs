using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;

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

            // Surrogate Pairs
            "\U00013000", // 𓀀 (Egyptian Hieroglyph)
            "\U00012000", // 𒀀 (Cuneiform)
            "\U00010480\U00010481\U000104A0\U000104A1", // 𐒀𐒁𐒠𐒡 (Osmanya Letter + Digit)
            "\U0001D538\U0001D7D8", // double-struck A0
            "\U00020000", // 𠀀 (Supplementary Ideographic Plane)
            "\u845B\U000E0100", // 葛󠄀 (葛 + Ideographic Variation Selector)
            "a\U000E0072\U000E006F\U000E0073\U000E006C\U000E0079\U000E006E\U000E007Fb", // Tag (Cf category)
        };

        private static readonly string[] _invalidIdentifiers =
        {
            "1",
            "\u06F1", // ء (Arabic Digit)
            "\u2200", // ∀
            "\u2015", // ― (Horizontal Bar)

            // Surrogate Pairs
            "\U0001F600", // 😀 (Emoji)

            // Incomplete Surrogates
            "\uD800",
            "\uDF00",
        };

        public static readonly IEnumerable<object[]> Identifiers;

        static IdentifierTestData()
        {
            Identifiers =
                _validIdentifiers.Select(x => new object[] { true, x })
                .Concat(_invalidIdentifiers.Select(x => new object[] { false, x }))
                .ToArray();
        }

        public static string RemoveCf(string s)
        {
            bool anyFormat = false;
            foreach (var c in EnumerateCodePoints(s))
            {
                if (GetUnicodeCategory(c) == UnicodeCategory.Format)
                {
                    anyFormat = true;
                }
            }

            if (!anyFormat)
            {
                return s;
            }

            StringBuilder builder = PooledStringBuilder.GetInstance();
            builder.Clear();
            foreach (var c in EnumerateCodePoints(s))
            {
                if (GetUnicodeCategory(c) != UnicodeCategory.Format)
                {
                    if (c >= 0x10000)
                    {
                        builder.Append(char.ConvertFromUtf32(c));
                    }
                    else
                    {
                        builder.Append((char)c);
                    }
                }
            }
            return builder.ToString();
        }

        public static IEnumerable<string> GetEscapeStrings(string s, bool cref = false)
        {
            StringBuilder builder = PooledStringBuilder.GetInstance();
            yield return EscapeAll(s, _unicode4dig, builder);
            if (s.Length > 1)
            {
                yield return EscapeFirst(s, _unicode4dig, builder);
            }
            yield return EscapeAll(s, _unicode8dig, builder);
            if (s.Length > 1)
            {
                yield return EscapeFirst(s, _unicode8dig, builder);
            }

            if (cref)
            {
                yield return EscapeAll(s, _entityDig, builder);
                if (s.Length > 1)
                {
                    yield return EscapeFirst(s, _entityDig, builder);
                }
                yield return EscapeAll(s, _entityHex, builder);
                if (s.Length > 1)
                {
                    yield return EscapeFirst(s, _entityHex, builder);
                }
            }
        }

        private static readonly Action<int, StringBuilder> _unicode4dig = (codePoint, builder) =>
        {
            if (codePoint >= 0x10000)
            {
                string pair = char.ConvertFromUtf32(codePoint);
                builder.Append("\\u");
                builder.AppendFormat("{0:X4}", (int)pair[0]);
                builder.Append("\\u");
                builder.AppendFormat("{0:X4}", (int)pair[1]);
            }
            else
            {
                builder.Append("\\u");
                builder.AppendFormat("{0:X4}", codePoint);
            }
        };

        private static readonly Action<int, StringBuilder> _unicode8dig = (codePoint, builder) =>
        {
            builder.Append("\\U");
            builder.AppendFormat("{0:X8}", codePoint);
        };

        private static readonly Action<int, StringBuilder> _entityDig = (codePoint, builder) =>
        {
            builder.Append("&#");
            builder.Append(codePoint);
            builder.Append(';');
        };

        private static readonly Action<int, StringBuilder> _entityHex = (codePoint, builder) =>
        {
            builder.Append("&#x");
            builder.AppendFormat("{0:X}", codePoint);
            builder.Append(';');
        };

        private static string EscapeAll(string s, Action<int, StringBuilder> escape, StringBuilder builder)
        {
            builder.Clear();
            foreach (var c in EnumerateCodePoints(s))
            {
                escape(c, builder);
            }
            return builder.ToString();
        }

        private static string EscapeFirst(string s, Action<int, StringBuilder> escape, StringBuilder builder)
        {
            builder.Clear();
            bool first = true;
            foreach (var c in EnumerateCodePoints(s))
            {
                if (first)
                {
                    escape(c, builder);
                    first = false;
                }
                else
                {
                    if (c >= 0x10000)
                    {
                        builder.Append(char.ConvertFromUtf32(c));
                    }
                    else
                    {
                        builder.Append((char)c);
                    }
                }
            }
            return builder.ToString();
        }

        private static IEnumerable<int> EnumerateCodePoints(string s)
        {
            char high = '\0';

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (char.IsHighSurrogate(c))
                {
                    high = c;
                }
                else
                {
                    if (high != 0)
                    {
                        if (char.IsLowSurrogate(c))
                        {
                            yield return char.ConvertToUtf32(high, c);
                        }
                        else
                        {
                            // imcomplete surrogates
                            yield return high;
                            yield return c;
                        }
                        high = '\0';
                    }
                    else
                    {
                        yield return c;
                    }
                }
            }
        }

        private static UnicodeCategory GetUnicodeCategory(int ch)
        {
            if (ch < 0x10000)
            {
                return CharUnicodeInfo.GetUnicodeCategory((char)ch);
            }
            else
            {
                string s = char.ConvertFromUtf32(ch);
                return CharUnicodeInfo.GetUnicodeCategory(s, 0);
            }
        }
    }
}
