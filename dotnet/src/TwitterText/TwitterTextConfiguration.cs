// Copyright 2018 Twitter, Inc.
// Licensed under the Apache License, Version 2.0
// http://www.apache.org/licenses/LICENSE-2.0

using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace TwitterText
{
    public class TwitterTextConfiguration
    {
        // These defaults should be kept up to date with v2.json config.
        // There's a unit test to ensure that.
        private const int DEFAULT_VERSION = 2;
        private const int DEFAULT_WEIGHTED_LENGTH = 280;
        private const int DEFAULT_SCALE = 100;
        private const int DEFAULT_WEIGHT = 200;
        private const int DEFAULT_TRANSFORMED_URL_LENGTH = 23;
        private static List<TwitterTextWeightedRange> DEFAULT_RANGES;

        static TwitterTextConfiguration()
        {
            DEFAULT_RANGES = new List<TwitterTextWeightedRange>
            {
                new TwitterTextWeightedRange().setStart(0).setEnd(4351).setWeight(100),
                new TwitterTextWeightedRange().setStart(8192).setEnd(8205).setWeight(100),
                new TwitterTextWeightedRange().setStart(8208).setEnd(8223).setWeight(100),
                new TwitterTextWeightedRange().setStart(8242).setEnd(8247).setWeight(100)
            };
        }

        private int version;
        private int maxWeightedTweetLength;
        private int scale;
        private int defaultWeight;
        private bool emojiParsingEnabled;
        private int transformedURLLength;
        private List<TwitterTextWeightedRange> ranges;

        /**
         * Create a {@link TwitterTextConfiguration} object from JSON
         * The JSON can have the following properties
         * version (required, integer, min value 0)
         * maxWeightedTweetLength (required, integer, min value 0)
         * scale (required, integer, min value 1)
         * defaultWeight (required, integer, min value 0)
         * transformedURLLength (integer, min value 0)
         * ranges (array of range items)
         * A range item has the following properties:
         * start (required, integer, min value 0)
         * end (required, integer, min value 0)
         * weight (required, integer, min value 0)
         *
         * @param json       The configuration string or file name in the config directory
         * @param isResource boolean indicating if the json refers to a file name for the configuration.
         * @return a {@link TwitterTextConfiguration} object that provides all the configuration values.
         */

        public static TwitterTextConfiguration configurationFromJson(string json,
                                                                     bool isResource)
        {
            TwitterTextConfiguration config;
            try
            {
                if (isResource)
                {
                    json = File.ReadAllText(json);
                }
                config = JsonConvert.DeserializeObject<TwitterTextConfiguration>(json);
            }
            catch (IOException ex)
            {
                config = getDefaultConfig();
            }
            return config;
        }

        private static TwitterTextConfiguration getDefaultConfig()
        {
            return new TwitterTextConfiguration()
                .setVersion(DEFAULT_VERSION)
                .setMaxWeightedTweetLength(DEFAULT_WEIGHTED_LENGTH)
                .setScale(DEFAULT_SCALE)
                .setDefaultWeight(DEFAULT_WEIGHT)
                .setRanges(DEFAULT_RANGES)
                .setTransformedURLLength(DEFAULT_TRANSFORMED_URL_LENGTH);
        }

        private TwitterTextConfiguration setVersion(int version)
        {
            this.version = version;
            return this;
        }

        private TwitterTextConfiguration setMaxWeightedTweetLength(int maxWeightedTweetLength)
        {
            this.maxWeightedTweetLength = maxWeightedTweetLength;
            return this;
        }

        private TwitterTextConfiguration setScale(int scale)
        {
            this.scale = scale;
            return this;
        }

        private TwitterTextConfiguration setDefaultWeight(int defaultWeight)
        {
            this.defaultWeight = defaultWeight;
            return this;
        }

        private TwitterTextConfiguration setEmojiParsingEnabled(bool emojiParsingEnabled)
        {
            this.emojiParsingEnabled = emojiParsingEnabled;
            return this;
        }

        private TwitterTextConfiguration setTransformedURLLength(int urlLength)
        {
            this.transformedURLLength = urlLength;
            return this;
        }

        private TwitterTextConfiguration setRanges(List<TwitterTextWeightedRange> ranges)
        {
            this.ranges = ranges;
            return this;
        }

        /**
         * Get the current version. This is an integer that will monotonically
         * increase in future releases. The legacy version of the string is version 1;
         * weighted code point ranges and 280-character “long” tweets are supported in version 2.
         *
         * @return The version for the configuration string.
         */
        public int getVersion()
        {
            return version;
        }

        /**
         * Get the maximum weighted length in the config. Legacy v1 tweets had a maximum
         * weighted length of 140 and all characters were weighted the same.
         * In the new configuration format, this is represented as a {@link maxWeightedTweetLength} of 140
         * and a {@link defaultWeight} of 1 for all code points.
         * @return The maximum length of the tweet, weighted.
         */
        public int getMaxWeightedTweetLength()
        {
            return maxWeightedTweetLength;
        }

        /**
         * Get the scale.
         *
         * @return The Tweet length is the (weighted length / scale).
         */
        public int getScale()
        {
            return scale;
        }

        /**
         * Get the default weight. This is overridden in one or more range items.
         *
         * @return The default weight applied to all code points.
         */
        public int getDefaultWeight()
        {
            return defaultWeight;
        }

        /**
         * Get whether emoji parsing is enabled.
         *
         * @return true if emoji parsing is enabled, otherwise false.
         */
        public bool getEmojiParsingEnabled()
        {
            return emojiParsingEnabled;
        }

        /**
         * In previous versions of twitter-text, which was the "shortened URL length."
         * Differentiating between the http and https shortened length for URLs has been deprecated
         * (https is used for all t.co URLs). The default value is 23.
         *
         * @return The length counted for URLs against the total weight of the Tweet.
         */
        public int getTransformedURLLength()
        {
            return transformedURLLength;
        }

        /**
         * Get an array of range items that describe ranges of Unicode code points and the weight to
         * apply to each code point. Each range is defined by its start, end, and weight.
         * Surrogate pairs have a length that is equivalent to the length of the first code unit in the
         * surrogate pair. Note that certain graphemes are the result of joining code points together,
         * such as by a zero-width joiner; unlike a surrogate pair, the length of such a grapheme will be
         * the sum of the weighted length of all included code points.
         *
         * @return An array of range items.
         */

        public List<TwitterTextWeightedRange> getRanges()
        {
            return ranges;
        }


        public override int GetHashCode()
        {
            int result = 17;
            result = result * 31 + version;
            result = result * 31 + maxWeightedTweetLength;
            result = result * 31 + scale;
            result = result * 31 + defaultWeight;
            result = result * 31 + (emojiParsingEnabled ? 1 : 0);
            result = result * 31 + transformedURLLength;
            result = result * 31 + ranges.GetHashCode();
            return result;
        }


        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }
            if (o == null || GetType() != o.GetType())
            {
                return false;
            }

            TwitterTextConfiguration that = (TwitterTextConfiguration)o;
            return version == that.version && maxWeightedTweetLength == that.maxWeightedTweetLength &&
                scale == that.scale && defaultWeight == that.defaultWeight &&
                emojiParsingEnabled == that.emojiParsingEnabled &&
                transformedURLLength == that.transformedURLLength && ranges.Equals(that.ranges);
        }

        public class TwitterTextWeightedRange
        {
            private int start;
            private int end;
            private int weight;

            public TwitterTextWeightedRange setStart(int start)
            {
                this.start = start;
                return this;
            }

            public TwitterTextWeightedRange setEnd(int end)
            {
                this.end = end;
                return this;
            }

            public TwitterTextWeightedRange setWeight(int weight)
            {
                this.weight = weight;
                return this;
            }

            /**
             * Get the contiguous unicode region
             *
             * @return range object
             */

            public Range getRange()
            {
                return new Range(start, end);
            }

            /**
             * Get the Weight for each unicode point in the region
             *
             * @return integer indicating the weight
             */
            public int getWeight()
            {
                return weight;
            }


            public override int GetHashCode()
            {
                return 31 * start + 31 * end + 31 * weight;
            }


            public override bool Equals(object o)
            {
                if (this == o)
                {
                    return true;
                }
                if (o == null || GetType() != o.GetType())
                {
                    return false;
                }
                TwitterTextWeightedRange that = (TwitterTextWeightedRange)o;
                return start == that.start && end == that.end && weight == that.weight;
            }
        }
    }
}