// Copyright 2018 Twitter, Inc.
// Licensed under the Apache License, Version 2.0
// http://www.apache.org/licenses/LICENSE-2.0

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using SystemRegex = System.Text.RegularExpressions.Regex;

namespace TwitterText
{
    public class Regex
    {
        protected Regex()
        {
        }

        private static readonly string URL_VALID_GTLD =
          "(?:(?:" +
          join(TldLists.GTLDS) +
          ")(?=[^a-z0-9@]|$))";
        private static readonly string URL_VALID_CCTLD =
          "(?:(?:" +
          join(TldLists.CTLDS) +
          ")(?=[^a-z0-9@]|$))";

        private const string INVALID_CHARACTERS =
          "\\uFFFE" +           // BOM
          "\\uFEFF" +           // BOM
          "\\uFFFF";            // Special

        private const string DIRECTIONAL_CHARACTERS =
          "\\u061C" + // ARABIC LETTER MARK (ALM)
          "\\u200E" + // LEFT-TO-RIGHT MARK (LRM)
          "\\u200F" + // RIGHT-TO-LEFT MARK (RLM)
          "\\u202A" + // LEFT-TO-RIGHT EMBEDDING (LRE)
          "\\u202B" + // RIGHT-TO-LEFT EMBEDDING (RLE)
          "\\u202C" + // POP DIRECTIONAL FORMATTING (PDF)
          "\\u202D" + // LEFT-TO-RIGHT OVERRIDE (LRO)
          "\\u202E" + // RIGHT-TO-LEFT OVERRIDE (RLO)
          "\\u2066" + // LEFT-TO-RIGHT ISOLATE (LRI)
          "\\u2067" + // RIGHT-TO-LEFT ISOLATE (RLI)
          "\\u2068" + // FIRST STRONG ISOLATE (FSI)
          "\\u2069";  // POP DIRECTIONAL ISOLATE (PDI)


        private const string UNICODE_SPACES = "[" +
          "\\u0009-\\u000d" +     //  # White_Space # Cc   [5] <control-0009>..<control-000D>
          "\\u0020" +             // White_Space # Zs       SPACE
          "\\u0085" +             // White_Space # Cc       <control-0085>
          "\\u00a0" +             // White_Space # Zs       NO-BREAK SPACE
          "\\u1680" +             // White_Space # Zs       OGHAM SPACE MARK
          "\\u180E" +             // White_Space # Zs       MONGOLIAN VOWEL SEPARATOR
          "\\u2000-\\u200a" +     // # White_Space # Zs  [11] EN QUAD..HAIR SPACE
          "\\u2028" +             // White_Space # Zl       LINE SEPARATOR
          "\\u2029" +             // White_Space # Zp       PARAGRAPH SEPARATOR
          "\\u202F" +             // White_Space # Zs       NARROW NO-BREAK SPACE
          "\\u205F" +             // White_Space # Zs       MEDIUM MATHEMATICAL SPACE
          "\\u3000" +             // White_Space # Zs       IDEOGRAPHIC SPACE
        "]";

        private const string LATIN_ACCENTS_CHARS =
            // Latin-1
            "\\u00c0-\\u00d6\\u00d8-\\u00f6\\u00f8-\\u00ff" +
                // Latin Extended A and B
                "\\u0100-\\u024f" +
                // IPA Extensions
                "\\u0253\\u0254\\u0256\\u0257\\u0259\\u025b\\u0263\\u0268\\u026f\\u0272\\u0289\\u028b" +
                // Hawaiian
                "\\u02bb" +
                // Combining diacritics
                "\\u0300-\\u036f" +
                // Latin Extended Additional (mostly for Vietnamese)
                "\\u1e00-\\u1eff";

        private const string CYRILLIC_CHARS = "\\u0400-\\u04ff";

        // Generated from unicode_regex/unicode_regex_groups.scala, more inclusive than Java's \p{L}\p{M}
        private const string HASHTAG_LETTERS_AND_MARKS = "\\u037f\\u0528-\\u052f\\u08a0-\\u08b2\\u08e4-\\u08ff\\u0978\\u0980\\u0c00\\u0c34\\u0c81\\u0d01\\u0ede\\u0edf" +
            "\\u10c7\\u10cd\\u10fd-\\u10ff\\u16f1-\\u16f8\\u17b4\\u17b5\\u191d\\u191e\\u1ab0-\\u1abe\\u1bab-\\u1bad\\u1bba-" +
            "\\u1bbf\\u1cf3-\\u1cf6\\u1cf8\\u1cf9\\u1de7-\\u1df5\\u2cf2\\u2cf3\\u2d27\\u2d2d\\u2d66\\u2d67\\u9fcc\\ua674-" +
            "\\ua67b\\ua698-\\ua69d\\ua69f\\ua792-\\ua79f\\ua7aa-\\ua7ad\\ua7b0\\ua7b1\\ua7f7-\\ua7f9\\ua9e0-\\ua9ef\\ua9fa-" +
            "\\ua9fe\\uaa7c-\\uaa7f\\uaae0-\\uaaef\\uaaf2-\\uaaf6\\uab30-\\uab5a\\uab5c-\\uab5f\\uab64\\uab65\\uf870-\\uf87f" +
            "\\uf882\\uf884-\\uf89f\\uf8b8\\uf8c1-\\uf8d6\\ufa2e\\ufa2f\\ufe27-\\ufe2d\\u102e0\\u1031f\\u10350-\\u1037a" +
            "\\u10500-\\u10527\\u10530-\\u10563\\u10600-\\u10736\\u10740-\\u10755\\u10760-\\u10767" +
            "\\u10860-\\u10876\\u10880-\\u1089e\\u10980-\\u109b7\\u109be\\u109bf\\u10a80-\\u10a9c" +
            "\\u10ac0-\\u10ac7\\u10ac9-\\u10ae6\\u10b80-\\u10b91\\u1107f\\u110d0-\\u110e8\\u11100-" +
            "\\u11134\\u11150-\\u11173\\u11176\\u11180-\\u111c4\\u111da\\u11200-\\u11211\\u11213-" +
            "\\u11237\\u112b0-\\u112ea\\u11301-\\u11303\\u11305-\\u1130c\\u1130f\\u11310\\u11313-" +
            "\\u11328\\u1132a-\\u11330\\u11332\\u11333\\u11335-\\u11339\\u1133c-\\u11344\\u11347" +
            "\\u11348\\u1134b-\\u1134d\\u11357\\u1135d-\\u11363\\u11366-\\u1136c\\u11370-\\u11374" +
            "\\u11480-\\u114c5\\u114c7\\u11580-\\u115b5\\u115b8-\\u115c0\\u11600-\\u11640\\u11644" +
            "\\u11680-\\u116b7\\u118a0-\\u118df\\u118ff\\u11ac0-\\u11af8\\u1236f-\\u12398\\u16a40-" +
            "\\u16a5e\\u16ad0-\\u16aed\\u16af0-\\u16af4\\u16b00-\\u16b36\\u16b40-\\u16b43\\u16b63-" +
            "\\u16b77\\u16b7d-\\u16b8f\\u16f00-\\u16f44\\u16f50-\\u16f7e\\u16f8f-\\u16f9f\\u1bc00-" +
            "\\u1bc6a\\u1bc70-\\u1bc7c\\u1bc80-\\u1bc88\\u1bc90-\\u1bc99\\u1bc9d\\u1bc9e\\u1e800-" +
            "\\u1e8c4\\u1e8d0-\\u1e8d6\\u1ee00-\\u1ee03\\u1ee05-\\u1ee1f\\u1ee21\\u1ee22\\u1ee24" +
            "\\u1ee27\\u1ee29-\\u1ee32\\u1ee34-\\u1ee37\\u1ee39\\u1ee3b\\u1ee42\\u1ee47\\u1ee49" +
            "\\u1ee4b\\u1ee4d-\\u1ee4f\\u1ee51\\u1ee52\\u1ee54\\u1ee57\\u1ee59\\u1ee5b\\u1ee5d\\u1ee5f" +
            "\\u1ee61\\u1ee62\\u1ee64\\u1ee67-\\u1ee6a\\u1ee6c-\\u1ee72\\u1ee74-\\u1ee77\\u1ee79-" +
            "\\u1ee7c\\u1ee7e\\u1ee80-\\u1ee89\\u1ee8b-\\u1ee9b\\u1eea1-\\u1eea3\\u1eea5-\\u1eea9" +
            "\\u1eeab-\\u1eebb";

        // Generated from unicode_regex/unicode_regex_groups.scala, more inclusive than Java's \p{Nd}
        private const string HASHTAG_NUMERALS = "\\\\pNd" +
            "\\u0de6-\\u0def\\ua9f0-\\ua9f9\\u110f0-\\u110f9\\u11136-\\u1113f\\u111d0-\\u111d9\\u112f0-" +
            "\\u112f9\\u114d0-\\u114d9\\u11650-\\u11659\\u116c0-\\u116c9\\u118e0-\\u118e9\\u16a60-" +
            "\\u16a69\\u16b50-\\u16b59";

        private const string HASHTAG_SPECIAL_CHARS = "_" + //underscore
            "\\u200c" + // ZERO WIDTH NON-JOINER (ZWNJ)
            "\\u200d" + // ZERO WIDTH JOINER (ZWJ)
            "\\ua67e" + // CYRILLIC KAVYKA
            "\\u05be" + // HEBREW PUNCTUATION MAQAF
            "\\u05f3" + // HEBREW PUNCTUATION GERESH
            "\\u05f4" + // HEBREW PUNCTUATION GERSHAYIM
            "\\uff5e" + // FULLWIDTH TILDE
            "\\u301c" + // WAVE DASH
            "\\u309b" + // KATAKANA-HIRAGANA VOICED SOUND MARK
            "\\u309c" + // KATAKANA-HIRAGANA SEMI-VOICED SOUND MARK
            "\\u30a0" + // KATAKANA-HIRAGANA DOUBLE HYPHEN
            "\\u30fb" + // KATAKANA MIDDLE DOT
            "\\u3003" + // DITTO MARK
            "\\u0f0b" + // TIBETAN MARK INTERSYLLABIC TSHEG
            "\\u0f0c" + // TIBETAN MARK DELIMITER TSHEG BSTAR
            "\\u00b7";  // MIDDLE DOT

        private const string HASHTAG_LETTERS_NUMERALS =
            HASHTAG_LETTERS_AND_MARKS + HASHTAG_NUMERALS + HASHTAG_SPECIAL_CHARS;
        private const string HASHTAG_LETTERS_SET = "[" + HASHTAG_LETTERS_AND_MARKS + "]";
        private const string HASHTAG_LETTERS_NUMERALS_SET = "[" + HASHTAG_LETTERS_NUMERALS + "]";

        /* URL related hash regex collection */
        private const string URL_VALID_PRECEDING_CHARS =
            "(?:[^a-z0-9@＠$#＃" + INVALID_CHARACTERS + "]|[" + DIRECTIONAL_CHARACTERS + "]|^)";

        private const string URL_VALID_CHARS = "[a-z0-9" + LATIN_ACCENTS_CHARS + "]";
        private const string URL_VALID_SUBDOMAIN =
            "(?>(?:" + URL_VALID_CHARS + "[" + URL_VALID_CHARS + "\\-_]*)?" + URL_VALID_CHARS + "\\.)";
        private const string URL_VALID_DOMAIN_NAME =
            "(?:(?:" + URL_VALID_CHARS + "[" + URL_VALID_CHARS + "\\-]*)?" + URL_VALID_CHARS + "\\.)";

        private const string PUNCTUATION_CHARS = "-_!\"#$%&'\\(\\)*+,./:;<=>?@\\[\\]^`\\{|}~";

        // Any non-space, non-punctuation characters.
        // \p{Z} = any kind of whitespace or invisible separator.
        private const string URL_VALID_UNICODE_CHARS =
            "[^" + PUNCTUATION_CHARS + "\\s\\p{Z}]";
        private const string URL_VALID_UNICODE_DOMAIN_NAME =
            "(?:(?:" + URL_VALID_UNICODE_CHARS + "[" + URL_VALID_UNICODE_CHARS + "\\-]*)?" +
                URL_VALID_UNICODE_CHARS + "\\.)";

        private const string URL_PUNYCODE = "(?:xn--[-0-9a-z]+)";

        private static readonly string URL_VALID_DOMAIN =
          "(?:" +                                                   // optional sub-domain + domain + TLD
              URL_VALID_SUBDOMAIN + "*" + URL_VALID_DOMAIN_NAME +   // e.g. twitter.com, foo.co.jp ...
              "(?:" + URL_VALID_GTLD + "|" + URL_VALID_CCTLD + "|" + URL_PUNYCODE + ")" +
          ")" +
          "|(?:" + "(?<=https?://)" +
            "(?:" +
              "(?:" + URL_VALID_DOMAIN_NAME + URL_VALID_CCTLD + ")" +  // protocol + domain + ccTLD
                "|(?:" +
                  URL_VALID_UNICODE_DOMAIN_NAME +                      // protocol + unicode domain + TLD
                  "(?:" + URL_VALID_GTLD + "|" + URL_VALID_CCTLD + ")" +
                ")" +
            ")" +
          ")" +
          "|(?:" +                                                  // domain + ccTLD + '/'
            URL_VALID_DOMAIN_NAME + URL_VALID_CCTLD + "(?=/)" +     // e.g. t.co/
          ")";

        private const string URL_VALID_PORT_NUMBER = "[0-9]+";

        private const string URL_VALID_GENERAL_PATH_CHARS =
            "[a-z0-9!\\*';:=\\+,.\\$/%#\\[\\]\\-\\u2013_~\\|&@" +
                LATIN_ACCENTS_CHARS + CYRILLIC_CHARS + "]";

        /**
         * Allow URL paths to contain up to two nested levels of balanced parens
         *  1. Used in Wikipedia URLs like /Primer_(film)
         *  2. Used in IIS sessions like /S(dfd346)/
         *  3. Used in Rdio URLs like /track/We_Up_(Album_Version_(Edited))/
         */
        private const string URL_BALANCED_PARENS = "\\(" +
          "(?:" +
            URL_VALID_GENERAL_PATH_CHARS +
            "|" +
            // allow one nested level of balanced parentheses
            "(?:" +
              URL_VALID_GENERAL_PATH_CHARS + "*" +
              "\\(" +
                URL_VALID_GENERAL_PATH_CHARS +
              "\\)" +
              URL_VALID_GENERAL_PATH_CHARS + "*" +
            ")" +
          ")" +
        "\\)";

        /**
         * Valid end-of-path characters (so /foo. does not gobble the period).
         *   2. Allow =&# for empty URL parameters and other URL-join artifacts
         */
        private const string URL_VALID_PATH_ENDING_CHARS =
            "[a-z0-9=_#/\\-\\+" + LATIN_ACCENTS_CHARS + CYRILLIC_CHARS + "]|(?:" +
                URL_BALANCED_PARENS + ")";

        private const string URL_VALID_PATH = "(?:" +
          "(?:" +
            URL_VALID_GENERAL_PATH_CHARS + "*" +
            "(?:" + URL_BALANCED_PARENS + URL_VALID_GENERAL_PATH_CHARS + "*)*" +
            URL_VALID_PATH_ENDING_CHARS +
          ")|(?:@" + URL_VALID_GENERAL_PATH_CHARS + "/)" +
        ")";

        private const string URL_VALID_URL_QUERY_CHARS =
            "[a-z0-9!?\\*'\\(\\);:&=\\+\\$/%#\\[\\]\\-_\\.,~\\|@]";
        private const string URL_VALID_URL_QUERY_ENDING_CHARS = "[a-z0-9\\-_&=#/]";
        private static readonly string VALID_URL_PATTERN_STRING =
        "(" +                                                            //  $1 total match
          "(" + URL_VALID_PRECEDING_CHARS + ")" +                        //  $2 Preceding character
          "(" +                                                          //  $3 URL
            "(https?://)?" +                                             //  $4 Protocol (optional)
            "(" + URL_VALID_DOMAIN + ")" +                               //  $5 Domain(s)
            "(?::(" + URL_VALID_PORT_NUMBER + "))?" +                    //  $6 Port number (optional)
            "(/" +
              URL_VALID_PATH + "*" +
            ")?" +                                                       //  $7 URL Path and anchor
            "(\\?" + URL_VALID_URL_QUERY_CHARS + "*" +                   //  $8 Query string
                    URL_VALID_URL_QUERY_ENDING_CHARS + ")?" +
          ")" +
        ")";

        private const string AT_SIGNS_CHARS = "@\uFF20";
        private const string DOLLAR_SIGN_CHAR = "\\$";
        private const string CASHTAG = "[a-z]{1,6}(?:[._][a-z]{1,2})?";

        /* Begin public constants */

        public static readonly SystemRegex  INVALID_CHARACTERS_PATTERN;
        public static readonly SystemRegex  VALID_HASHTAG;
        public const int VALID_HASHTAG_GROUP_BEFORE = 1;
        public const int VALID_HASHTAG_GROUP_HASH = 2;
        public const int VALID_HASHTAG_GROUP_TAG = 3;
        public static readonly SystemRegex  INVALID_HASHTAG_MATCH_END;
        public static readonly SystemRegex  RTL_CHARACTERS;

        public static readonly SystemRegex  AT_SIGNS;
        public static readonly SystemRegex  VALID_MENTION_OR_LIST;
        public const int VALID_MENTION_OR_LIST_GROUP_BEFORE = 1;
        public const int VALID_MENTION_OR_LIST_GROUP_AT = 2;
        public const int VALID_MENTION_OR_LIST_GROUP_USERNAME = 3;
        public const int VALID_MENTION_OR_LIST_GROUP_LIST = 4;

        public static readonly SystemRegex  VALID_REPLY;
        public const int VALID_REPLY_GROUP_USERNAME = 1;

        public static readonly SystemRegex  INVALID_MENTION_MATCH_END;

        /**
         * Regex to extract URL (it also includes the text preceding the url).
         *
         * This regex does not reflect its name and {@link Regex#VALID_URL_GROUP_URL} match
         * should be checked in order to match a valid url. This is not ideal, but the behavior is
         * being kept to ensure backwards compatibility. Ideally this regex should be
         * implemented with a negative lookbehind as opposed to a negated character class
         * but lack of JS support increases maint overhead if the logic is different by
         * platform.
         */

        public static readonly SystemRegex VALID_URL;
        public const int VALID_URL_GROUP_ALL = 1;
        public const int VALID_URL_GROUP_BEFORE = 2;
        public const int VALID_URL_GROUP_URL = 3;
        public const int VALID_URL_GROUP_PROTOCOL = 4;
        public const int VALID_URL_GROUP_DOMAIN = 5;
        public const int VALID_URL_GROUP_PORT = 6;
        public const int VALID_URL_GROUP_PATH = 7;
        public const int VALID_URL_GROUP_QUERY_STRING = 8;

        public static readonly SystemRegex VALID_TCO_URL;
        public static readonly SystemRegex INVALID_URL_WITHOUT_PROTOCOL_MATCH_BEGIN;

        public static readonly SystemRegex VALID_CASHTAG;
        public const int VALID_CASHTAG_GROUP_BEFORE = 1;
        public const int VALID_CASHTAG_GROUP_DOLLAR = 2;
        public const int VALID_CASHTAG_GROUP_CASHTAG = 3;

        public static readonly SystemRegex VALID_DOMAIN;

        // initializing in a static synchronized block,
        // there appears to be thread safety issues with Pattern.compile in android
        static Regex()
        {
            INVALID_CHARACTERS_PATTERN = new SystemRegex(".*[" + INVALID_CHARACTERS + "].*");
            VALID_HASHTAG = new SystemRegex("(^|\\uFE0E|\\uFE0F|[^&" + HASHTAG_LETTERS_NUMERALS +
                "])([#\uFF03])(?![\uFE0F\u20E3])(" + HASHTAG_LETTERS_NUMERALS_SET + "*" +
                HASHTAG_LETTERS_SET + HASHTAG_LETTERS_NUMERALS_SET + "*)", RegexOptions.IgnoreCase);
            INVALID_HASHTAG_MATCH_END = new SystemRegex("^(?:[#＃]|://)");
            RTL_CHARACTERS = new SystemRegex("[\u0600-\u06FF\u0750-\u077F\u0590-\u05FF\uFE70-\uFEFF]");
            AT_SIGNS = new SystemRegex("[" + AT_SIGNS_CHARS + "]");
            VALID_MENTION_OR_LIST = new SystemRegex("([^a-z0-9_!#$%&*" + AT_SIGNS_CHARS +
                "]|^|(?:^|[^a-z0-9_+~.-])RT:?)(" + AT_SIGNS +
                "+)([a-z0-9_]{1,20})(/[a-z][a-z0-9_\\-]{0,24})?", RegexOptions.IgnoreCase);
            VALID_REPLY = new SystemRegex("^(?:" + UNICODE_SPACES + "|" + DIRECTIONAL_CHARACTERS + ")*" +
                AT_SIGNS + "([a-z0-9_]{1,20})", RegexOptions.IgnoreCase);
            INVALID_MENTION_MATCH_END =
                new SystemRegex("^(?:[" + AT_SIGNS_CHARS + LATIN_ACCENTS_CHARS + "]|://)");
            INVALID_URL_WITHOUT_PROTOCOL_MATCH_BEGIN = new SystemRegex("[-_./]$");

            VALID_URL = new SystemRegex(VALID_URL_PATTERN_STRING, RegexOptions.IgnoreCase);
            VALID_TCO_URL = new SystemRegex("^https?://t\\.co/([a-z0-9]+)(?:\\?" +
                URL_VALID_URL_QUERY_CHARS + "*" + URL_VALID_URL_QUERY_ENDING_CHARS + ")?",
                RegexOptions.IgnoreCase);
            VALID_CASHTAG = new SystemRegex("(^|" + UNICODE_SPACES + "|" + DIRECTIONAL_CHARACTERS + ")(" +
                DOLLAR_SIGN_CHAR + ")(" + CASHTAG + ")" + "(?=$|\\s)",
                RegexOptions.IgnoreCase);
            VALID_DOMAIN = new SystemRegex(URL_VALID_DOMAIN, RegexOptions.IgnoreCase);
        }


        private static string join(List<string> col)
        {
            var sb = new StringBuilder();
            foreach (var item in col)
            {
                sb.Append(item.ToString());
            }
            return sb.ToString();
        }
    }
}

