// Copyright 2018 Twitter, Inc.
// Licensed under the Apache License, Version 2.0
// http://www.apache.org/licenses/LICENSE-2.0

using System.Collections.Generic;
using System.Linq;

namespace TwitterText
{
    /**
     * A class to parse tweet text with a {@link TwitterTextConfiguration} and returns a
     * {@link TwitterTextParseResults} object
     */
    public class TwitterTextParser
    {

        private TwitterTextParser()
        {
        }

        public static TwitterTextParseResults EMPTY_TWITTER_TEXT_PARSE_RESULTS =
          new TwitterTextParseResults(0, 0, false, Range.EMPTY, Range.EMPTY);

        /**
         * v1.json is the legacy, traditional code point counting configuration
         */
        public static TwitterTextConfiguration TWITTER_TEXT_CODE_POINT_COUNT_CONFIG =
          TwitterTextConfiguration.configurationFromJson("v1.json", true);

        /**
         * v2.json has the following unicode code point blocks defined
         * 0x0000 (0)    - 0x10FF (4351) Basic Latin to Georgian block: Weight 100
         * 0x2000 (8192) - 0x200D (8205) Spaces in the General Punctuation Block: Weight 100
         * 0x2010 (8208) - 0x201F (8223) Hyphens &amp; Quotes in the General Punctuation Block: Weight 100
         * 0x2032 (8242) - 0x2037 (8247) Quotes in the General Punctuation Block: Weight 100
         */
        public static TwitterTextConfiguration TWITTER_TEXT_WEIGHTED_CHAR_COUNT_CONFIG =
            TwitterTextConfiguration.configurationFromJson("v2.json", true);

        /**
         * v3.json supports counting emoji as one weighted character
         */
        public static TwitterTextConfiguration TWITTER_TEXT_EMOJI_CHAR_COUNT_CONFIG =
            TwitterTextConfiguration.configurationFromJson("v3.json", true);

        public static TwitterTextConfiguration TWITTER_TEXT_DEFAULT_CONFIG =
            TWITTER_TEXT_EMOJI_CHAR_COUNT_CONFIG;

        private static Extractor EXTRACTOR = new Extractor();

        /**
         * Parses a given tweet text with the weighted character count configuration (v2.json).
         *
         * @param tweet which is to be parsed
         * @return {@link TwitterTextParseResults} object
         */
        public static TwitterTextParseResults parseTweet(string tweet)
        {
            return parseTweet(tweet, TWITTER_TEXT_WEIGHTED_CHAR_COUNT_CONFIG);
        }

        /**
         * Parses a given tweet text with the given {@link TwitterTextConfiguration}
         *
         * @param tweet which is to be parsed
         * @param config {@link TwitterTextConfiguration}
         * @return {@link TwitterTextParseResults} object
         */

        public static TwitterTextParseResults parseTweet(string tweet,
                                                         TwitterTextConfiguration config)
        {
            return parseTweet(tweet, config, true);
        }

        /**
         * Returns the weighted length of a tweet without doing any URL processing.
         * Used by Twitter Backend to validate the visible tweet in the  step of tweet creation.
         *
         * @param tweet which is to be parsed
         * @return {@link TwitterTextParseResults} object
         */

        public static TwitterTextParseResults parseTweetWithoutUrlExtraction(
            string tweet)
        {
            return parseTweet(tweet, TWITTER_TEXT_WEIGHTED_CHAR_COUNT_CONFIG, false);
        }

        /**
         * Parses a given tweet text with the given {@link TwitterTextConfiguration} and
         * optionally control if urls should/shouldn't be normalized to
         * {@link TwitterTextConfiguration.DEFAULT_TRANSFORMED_URL_LENGTH}
         *
         * @param tweet which is to be parsed
         * @param config {@link TwitterTextConfiguration}
         * @param extractURLs boolean indicating if URLs should be extracted for counting
         * @return {@link TwitterTextParseResults} object
         */

        private static TwitterTextParseResults parseTweet(string tweet,
                                                          TwitterTextConfiguration config,
                                                          bool extractURLs)
        {
            if (tweet == null || tweet.Trim().Length == 0)
            {
                return EMPTY_TWITTER_TEXT_PARSE_RESULTS;
            }

            var normalizedTweet = tweet.Normalize(System.Text.NormalizationForm.FormC);
            var tweetLength = normalizedTweet.Length;

            if (tweetLength == 0)
            {
                return EMPTY_TWITTER_TEXT_PARSE_RESULTS;
            }

            var scale = config.getScale();
            var maxWeightedTweetLength = config.getMaxWeightedTweetLength();
            var scaledMaxWeightedTweetLength = maxWeightedTweetLength * scale;
            var transformedUrlWeight = config.getTransformedURLLength() * scale;
            var ranges = config.getRanges();

            var urlEntities = EXTRACTOR.extractURLsWithIndices(normalizedTweet);

            var hasInvalidCharacters = false;
            var weightedCount = 0;
            var offset = 0;
            var validOffset = 0;

            var emojiMap = new Dictionary<int, int>();
            if (config.getEmojiParsingEnabled())
            {
                foreach (var match in TwitterTextEmojiRegex.VALID_EMOJI_PATTERN.Matches(normalizedTweet).ToList())
                {
                    emojiMap.Add(match.Index, match.Length);
                }
            }

            while (offset < tweetLength)
            {
                var charWeight = config.getDefaultWeight();

                if (extractURLs)
                {
                    for (int i =0; i< urlEntities.Count; i++)
                    {
                        var urlEntity = urlEntities[i];
                        if (urlEntity.start == offset)
                        {
                            var urlLength = urlEntity.end - urlEntity.start;
                            weightedCount += transformedUrlWeight;
                            offset += urlLength;
                            if (weightedCount <= scaledMaxWeightedTweetLength)
                            {
                                validOffset += urlLength;
                            }
                            urlEntities.RemoveAt(i);
                            break;
                        }
                    }
                }

                if (offset < tweetLength)
                {
                    int codePoint = char.ConvertToUtf32(normalizedTweet, offset);

                    var emojiLength = -1;
                    if (emojiMap.ContainsKey(offset))
                    {
                        charWeight = config.getDefaultWeight();
                        emojiLength = emojiMap[offset];
                    }

                    if (emojiLength == -1)
                    {
                        foreach(var weightedRange in ranges)
                        {
                            if (weightedRange.getRange().IsInRange(codePoint))
                            {
                                charWeight = weightedRange.getWeight();
                                break;
                            }
                        }
                    }

                    weightedCount += charWeight;

                    hasInvalidCharacters = hasInvalidCharacters ||
                        Validator.hasInvalidCharacters(normalizedTweet.Substring(offset, offset + 1));

                    int offsetDelta;
                    if (emojiLength != -1)
                    {
                        offsetDelta = emojiLength;
                    }
                    else
                    {
                        offsetDelta = CharCount(codePoint);
                    }
                    offset += offsetDelta;
                    if (!hasInvalidCharacters && weightedCount <= scaledMaxWeightedTweetLength)
                    {
                        validOffset += offsetDelta;
                    }
                }
            }
            var normalizedTweetOffset = tweet.Length - normalizedTweet.Length;
            var scaledWeightedLength = weightedCount / scale;
            var isValid = !hasInvalidCharacters && scaledWeightedLength <= maxWeightedTweetLength;
            var permillage = scaledWeightedLength * 1000 / maxWeightedTweetLength;
            return new TwitterTextParseResults(scaledWeightedLength, permillage, isValid,
                new Range(0, offset + normalizedTweetOffset - 1),
                new Range(0, validOffset + normalizedTweetOffset - 1));
        }

        private static int CharCount(int codePoint) => codePoint >= 0x10000 ? 2 : 1;
    }

    
}