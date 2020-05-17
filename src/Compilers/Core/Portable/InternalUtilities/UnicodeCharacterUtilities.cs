// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Roslyn.Utilities
{
    /// <summary>
    /// Defines a set of helper methods to classify Unicode characters.
    /// </summary>
    internal static partial class UnicodeCharacterUtilities
    {
        private static UnicodeCategory GetUnicodeCategory(int ch)
        {
#if NETCOREAPP
            return CharUnicodeInfo.GetUnicodeCategory(ch);
#else
            if (ch < 0x10000)
            {
                return CharUnicodeInfo.GetUnicodeCategory((char)ch);
            }
            else
            {
                string s = char.ConvertFromUtf32(ch);
                return CharUnicodeInfo.GetUnicodeCategory(s, 0);
            }
#endif
        }

        public static bool IsIdentifierStartCharacter(int ch)
        {
            // identifier-start-character:
            //   letter-character
            //   _ (the underscore character U+005F)

            if (ch < 'a') // '\u0061'
            {
                if (ch < 'A') // '\u0041'
                {
                    return false;
                }

                return ch <= 'Z'  // '\u005A'
                    || ch == '_'; // '\u005F'
            }

            if (ch <= 'z') // '\u007A'
            {
                return true;
            }

            if (ch <= '\u007F') // max ASCII
            {
                return false;
            }

            return IsLetterChar(GetUnicodeCategory(ch));
        }

        /// <summary>
        /// Returns true if the Unicode character can be a part of an identifier.
        /// </summary>
        /// <param name="ch">The Unicode character.</param>
        public static bool IsIdentifierPartCharacter(int ch)
        {
            // identifier-part-character:
            //   letter-character
            //   decimal-digit-character
            //   connecting-character
            //   combining-character
            //   formatting-character

            if (ch < 'a') // '\u0061'
            {
                if (ch < 'A') // '\u0041'
                {
                    return ch >= '0'  // '\u0030'
                        && ch <= '9'; // '\u0039'
                }

                return ch <= 'Z'  // '\u005A'
                    || ch == '_'; // '\u005F'
            }

            if (ch <= 'z') // '\u007A'
            {
                return true;
            }

            if (ch <= '\u007F') // max ASCII
            {
                return false;
            }

            UnicodeCategory cat = GetUnicodeCategory(ch);
            return IsLetterChar(cat)
                || IsDecimalDigitChar(cat)
                || IsConnectingChar(cat)
                || IsCombiningChar(cat)
                || IsFormattingChar(cat);
        }

        /// <summary>
        /// Check that the name is a valid Unicode identifier.
        /// </summary>
        public static bool IsValidIdentifier([NotNullWhen(returnValue: true)] string? name)
        {
            if (RoslynString.IsNullOrEmpty(name))
            {
                return false;
            }

            int i = 1;
            int nameLength = name.Length;

            char c0 = name[0];
            if (!IsIdentifierStartCharacter(c0))
            {
                if (char.IsHighSurrogate(c0) && 1 < nameLength)
                {
                    char c1 = name[1];
                    if (!char.IsLowSurrogate(c1))
                    {
                        return false;
                    }

                    int c = char.ConvertToUtf32(c0, c1);

                    if (!IsIdentifierStartCharacter(c))
                    {
                        return false;
                    }

                    i = 2;
                }
                else
                {
                    return false;
                }
            }

            for (; i < nameLength; i++)
            {
                c0 = name[i];
                if (!IsIdentifierPartCharacter(c0))
                {
                    if (char.IsHighSurrogate(c0))
                    {
                        i++;
                        if (i >= nameLength)
                        {
                            return false;
                        }

                        char c1 = name[i];
                        int c = char.ConvertToUtf32(c0, c1);

                        if (!IsIdentifierPartCharacter(c))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsLetterChar(UnicodeCategory cat)
        {
            // letter-character:
            //   A Unicode character of classes Lu, Ll, Lt, Lm, Lo, or Nl 
            //   A Unicode-escape-sequence representing a character of classes Lu, Ll, Lt, Lm, Lo, or Nl

            switch (cat)
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.OtherLetter:
                case UnicodeCategory.LetterNumber:
                    return true;
            }

            return false;
        }

        private static bool IsCombiningChar(UnicodeCategory cat)
        {
            // combining-character:
            //   A Unicode character of classes Mn or Mc 
            //   A Unicode-escape-sequence representing a character of classes Mn or Mc

            switch (cat)
            {
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.SpacingCombiningMark:
                    return true;
            }

            return false;
        }

        private static bool IsDecimalDigitChar(UnicodeCategory cat)
        {
            // decimal-digit-character:
            //   A Unicode character of the class Nd 
            //   A unicode-escape-sequence representing a character of the class Nd

            return cat == UnicodeCategory.DecimalDigitNumber;
        }

        private static bool IsConnectingChar(UnicodeCategory cat)
        {
            // connecting-character:  
            //   A Unicode character of the class Pc
            //   A unicode-escape-sequence representing a character of the class Pc

            return cat == UnicodeCategory.ConnectorPunctuation;
        }

        /// <summary>
        /// Returns true if the Unicode character is a formatting character (Unicode class Cf).
        /// </summary>
        /// <param name="ch">The Unicode character.</param>
        internal static bool IsFormattingChar(int ch)
        {
            // There are no FormattingChars in ASCII range

            return ch > 127 && IsFormattingChar(GetUnicodeCategory(ch));
        }

        /// <summary>
        /// Returns true if the Unicode character is a formatting character (Unicode class Cf).
        /// </summary>
        /// <param name="cat">The Unicode character.</param>
        private static bool IsFormattingChar(UnicodeCategory cat)
        {
            // formatting-character:  
            //   A Unicode character of the class Cf
            //   A unicode-escape-sequence representing a character of the class Cf

            return cat == UnicodeCategory.Format;
        }
    }
}
