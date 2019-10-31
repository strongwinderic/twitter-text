// Copyright 2018 Twitter, Inc.
// Licensed under the Apache License, Version 2.0
// http://www.apache.org/licenses/LICENSE-2.0

namespace TwitterText
{

    /**
     * A class that represents a parsed tweet structure that contains the length of the tweet,
     * its validity, display ranges etc.
     */
    public class TwitterTextParseResults
    {
        /**
         * The adjusted tweet length based on char weights.
         * true weightedLength is weightedLength / configuration.scale
         */
        public int weightedLength;

        /**
         * true weightedLength by configuration.maxWeightedTweetLength permillage.
         */
        public int permillage;

        /**
         * If the tweet is valid or not.
         */
        public bool isValid;

        /**
         * Text range that is visible
         */
        public Range displayTextRange;

        /**
         * Text range that is valid for a Tweet
         */
        public Range validTextRange;

        public TwitterTextParseResults(int weightedLength, int permillage, bool isValid,
                                       Range displayTextRange, Range validTextRange)
        {
            this.weightedLength = weightedLength;
            this.permillage = permillage;
            this.isValid = isValid;
            this.displayTextRange = displayTextRange;
            this.validTextRange = validTextRange;
        }


        public override int GetHashCode()
        {
            int result = weightedLength;
            result = 31 * result + permillage;
            result = 31 * result + isValid.GetHashCode();
            result = 31 * result + displayTextRange.GetHashCode();
            result = 31 * result + validTextRange.GetHashCode();
            return result;
        }

        public override bool Equals(object obj)
        {
            return this == obj || obj is TwitterTextParseResults &&
                Equals((TwitterTextParseResults)obj);
        }

        private bool Equals(TwitterTextParseResults obj)
        {
            return obj != null && obj.weightedLength == weightedLength && obj.permillage == permillage
                && obj.isValid == isValid && obj.displayTextRange.Equals(displayTextRange)
                && obj.validTextRange.Equals(validTextRange);
        }
    }
}