// Copyright 2018 Twitter, Inc.
// Licensed under the Apache License, Version 2.0
// http://www.apache.org/licenses/LICENSE-2.0

using System;

namespace TwitterText
{
    public class Validator
    {
        public const int MAX_TWEET_LENGTH = 280;

        protected int shortUrlLength = 23;
        protected int shortUrlLengthHttps = 23;

        private Extractor extractor = new Extractor();

        /**
         * Returns the weighted length of a given tweet text
         *
         * @param tweetText the source to mark as modified.
         * @return length of a tweet
         * @deprecated Use TwitterTextParser
         */
        [Obsolete]
        public int getTweetLength(string tweetText)
        {
            return TwitterTextParser.parseTweet(tweetText).weightedLength;
        }

        /**
         * Checks if a given tweet text is valid.
         * @deprecated Use TwitterTextParser
         */
        [Obsolete]
        public bool isValidTweet(string text)
        {
            return TwitterTextParser.parseTweet(text).isValid;
        }

        public static bool hasInvalidCharacters(string text)
        {
            return Regex.INVALID_CHARACTERS_PATTERN.IsMatch(text);
        }

        public int getShortUrlLength()
        {
            return shortUrlLength;
        }

        public void setShortUrlLength(int shortUrlLength)
        {
            this.shortUrlLength = shortUrlLength;
        }

        public int getShortUrlLengthHttps()
        {
            return shortUrlLengthHttps;
        }

        public void setShortUrlLengthHttps(int shortUrlLengthHttps)
        {
            this.shortUrlLengthHttps = shortUrlLengthHttps;
        }
    }
}
