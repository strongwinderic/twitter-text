// Copyright 2018 Twitter, Inc.
// Licensed under the Apache License, Version 2.0
// http://www.apache.org/licenses/LICENSE-2.0

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TwitterText
{
    internal class Extractor
    {
        /**
         * The maximum url length that the Twitter backend supports.
         */
        public const int MAX_URL_LENGTH = 4096;

        /**
         * The maximum t.co path length that the Twitter backend supports.
         */
        public const int MAX_TCO_SLUG_LENGTH = 40;

        /**
         * The backend adds http:// for normal links and https to *.twitter.com URLs
         * (it also rewrites http to https for URLs matching *.twitter.com).
         * We're better off adding https:// all the time. By making the assumption that
         * URL_GROUP_PROTOCOL_LENGTH is https, the trade off is we'll disallow a http URL
         * that is 4096 characters.
         */
        private static int URL_GROUP_PROTOCOL_LENGTH = "https://".Length;

        public class Entity
        {
            public enum Type
            {
                URL, HASHTAG, MENTION, CASHTAG
            }

            public int start;
            public int end;
            public string value;
            // listSlug is used to store the list portion of @mention/list.
            public string listSlug;
            public Type type;

            public string displayURL = null;
            public string expandedURL = null;

            public Entity(int start, int end, string value, string listSlug, Type type)
            {
                this.start = start;
                this.end = end;
                this.value = value;
                this.listSlug = listSlug;
                this.type = type;
            }

            public Entity(int start, int end, string value, Type type) : this(start, end, value, null, type)
            {
            }

            public Entity(Match match, Type type, int groupNumber) :
                this(match, type, groupNumber, -1) // Offset -1 on start index to include @, # symbols for mentions and hashtags
            {
            }

            public Entity(Match matcher, Type type, int groupNumber, int startOffset)
                : this(matcher.Groups[groupNumber].Index + startOffset,
                      matcher.Groups[groupNumber].Index + matcher.Groups[groupNumber].Length,
                      matcher.Groups[groupNumber].Value,
                      type)
            {
            }

            public override bool Equals(object obj)
            {
                if (this == obj)
                {
                    return true;
                }

                if (!(obj is Entity))
                {
                    return false;
                }

                Entity other = (Entity)obj;

                return this.type.Equals(other.type) &&
                    this.start == other.start &&
                    this.end == other.end &&
                    this.value.Equals(other.value);
            }


            public override int GetHashCode()
            {
                return this.type.GetHashCode() + this.value.GetHashCode() + this.start + this.end;
            }


            public override string ToString()
            {
                return value + "(" + type + ") [" + start + "," + end + "]";
            }

            public int getStart()
            {
                return start;
            }

            public int getEnd()
            {
                return end;
            }

            public string getValue()
            {
                return value;
            }

            public string getListSlug()
            {
                return listSlug;
            }

            public Type getType()
            {
                return type;
            }

            public string getDisplayURL()
            {
                return displayURL;
            }

            public void setDisplayURL(string displayURL)
            {
                this.displayURL = displayURL;
            }

            public string getExpandedURL()
            {
                return expandedURL;
            }

            public void setExpandedURL(string expandedURL)
            {
                this.expandedURL = expandedURL;
            }
        }

        protected bool extractURLWithoutProtocol = true;

        /**
         * Create a new extractor.
         */
        public Extractor()
        {
        }

        private void removeOverlappingEntities(List<Entity> entities)
        {
            entities = entities.OrderBy(e => e.start).ToList();

            // Remove overlapping entities.
            // Two entities overlap only when one is URL and the other is hashtag/mention
            // which is a part of the URL. When it happens, we choose URL over hashtag/mention
            // by selecting the one with smaller start index.
            for (int i = 0; i < entities.Count; i++)
            {
                for (int j = i + 1; j < entities.Count; j++)
                {
                    if (entities[i].getEnd() > entities[j].getStart())
                    {
                        entities.RemoveAt(i);
                    }
                }
            }
        }

        /**
         * Extract URLs, @mentions, lists and #hashtag from a given text/tweet.
         *
         * @param text text of tweet
         * @return list of extracted entities
         */
        public List<Entity> extractEntitiesWithIndices(string text)
        {
            List<Entity> entities = new List<Entity>();
            entities.AddRange(extractURLsWithIndices(text));
            entities.AddRange(extractHashtagsWithIndices(text, false));
            entities.AddRange(extractMentionsOrListsWithIndices(text));
            entities.AddRange(extractCashtagsWithIndices(text));

            removeOverlappingEntities(entities);
            return entities;
        }

        /**
         * Extract @username references from Tweet text. A mention is an occurrence of @username anywhere
         * in a Tweet.
         *
         * @param text of the tweet from which to extract usernames
         * @return List of usernames referenced (without the leading @ sign)
         */
        public List<string> extractMentionedScreennames(string text)
        {
            if (isEmptyString(text))
            {
                return new List<string>();
            }

            var extracted = new List<string>();
            foreach (var entity in extractMentionedScreennamesWithIndices(text))
            {
                extracted.Add(entity.value);
            }
            return extracted;
        }

        /**
         * Extract @username references from Tweet text. A mention is an occurrence of @username anywhere
         * in a Tweet.
         *
         * @param text of the tweet from which to extract usernames
         * @return List of usernames referenced (without the leading @ sign)
         */
        public List<Entity> extractMentionedScreennamesWithIndices(string text)
        {
            var extracted = new List<Entity>();
            foreach (var entity in extractMentionsOrListsWithIndices(text))
            {
                if (entity.listSlug == null)
                {
                    extracted.Add(entity);
                }
            }
            return extracted;
        }

        /**
         * Extract @username and an optional list reference from Tweet text. A mention is an occurrence
         * of @username anywhere in a Tweet. A mention with a list is a @username/list.
         *
         * @param text of the tweet from which to extract usernames
         * @return List of usernames (without the leading @ sign) and an optional lists referenced
         */
        public List<Entity> extractMentionsOrListsWithIndices(string text)
        {
            if (isEmptyString(text))
            {
                return new List<Entity>();
            }

            // Performance optimization.
            // If text doesn't contain @/＠ at all, the text doesn't
            // contain @mention. So we can simply return an empty list.
            bool found = false;
            foreach (char c in text)
            {
                if (c == '@' || c == '＠')
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                return new List<Entity>();
            }

            List<Entity> extracted = new List<Entity>();

            var matches = Regex.VALID_MENTION_OR_LIST.Matches(text).ToList();
            foreach (var match in matches)
            {
                string after = text.Substring(match.Index + match.Length);
                if (!Regex.INVALID_MENTION_MATCH_END.IsMatch(after))
                {
                    if (match.Groups[Regex.VALID_MENTION_OR_LIST_GROUP_LIST] == null)
                    {
                        extracted.Add(new Entity(match, Entity.Type.MENTION,
                            Regex.VALID_MENTION_OR_LIST_GROUP_USERNAME));
                    }
                    else
                    {
                        extracted.Add(new Entity(match.Groups[Regex.VALID_MENTION_OR_LIST_GROUP_USERNAME].Index - 1,
                            match.Groups[Regex.VALID_MENTION_OR_LIST_GROUP_LIST].Index + match.Groups[Regex.VALID_MENTION_OR_LIST_GROUP_LIST].Length,
                            match.Groups[Regex.VALID_MENTION_OR_LIST_GROUP_USERNAME].Value,
                            match.Groups[Regex.VALID_MENTION_OR_LIST_GROUP_LIST].Value,
                            Entity.Type.MENTION));
                    }
                }
            }
            return extracted;
        }

        /**
         * Extract a @username reference from the beginning of Tweet text. A reply is an occurrence
         * of @username at the beginning of a Tweet, preceded by 0 or more spaces.
         *
         * @param text of the tweet from which to extract the replied to username
         * @return username referenced, if any (without the leading @ sign).
         * Returns null if this is not a reply.
         */
        public string extractReplyScreenname(string text)
        {
            if (text == null)
            {
                return null;
            }

            var match = Regex.VALID_REPLY.Match(text);
            if (match != null)
            {
                string after = text.Substring(match.Index + match.Length);
                if (Regex.INVALID_MENTION_MATCH_END.Match(after) != null)
                {
                    return null;
                }
                else
                {
                    return match.Groups[Regex.VALID_REPLY_GROUP_USERNAME].Value;
                }
            }
            else
            {
                return null;
            }
        }

        /**
         * Extract URL references from Tweet text.
         *
         * @param text of the tweet from which to extract URLs
         * @return List of URLs referenced.
         */

        public List<string> extractURLs(string text)
        {
            if (isEmptyString(text))
            {
                return new List<string>();
            }

            List<string> urls = new List<string>();
            foreach (Entity entity in extractURLsWithIndices(text))
            {
                urls.Add(entity.value);
            }
            return urls;
        }

        /**
         * Extract URL references from Tweet text.
         *
         * @param text of the tweet from which to extract URLs
         * @return List of URLs referenced.
         */

        public List<Entity> extractURLsWithIndices(string text)
        {
            if (isEmptyString(text) ||
                (extractURLWithoutProtocol ? text.IndexOf('.') : text.IndexOf(':')) == -1)
            {
                // Performance optimization.
                // If text doesn't contain '.' or ':' at all, text doesn't contain URL,
                // so we can simply return an empty list.
                return new List<Entity>();
            }

            List<Entity> urls = new List<Entity>();

            var matches = Regex.VALID_URL.Matches(text).ToList();
            foreach (var match in matches)
            {
                string protocol = match.Groups[Regex.VALID_URL_GROUP_PROTOCOL].Value;
                if (isEmptyString(protocol))
                {
                    // skip if protocol is not present and 'extractURLWithoutProtocol' is false
                    // or URL is preceded by invalid character.
                    if (!extractURLWithoutProtocol
                        || Regex.INVALID_URL_WITHOUT_PROTOCOL_MATCH_BEGIN
                        .IsMatch(match.Groups[Regex.VALID_URL_GROUP_BEFORE].Value))
                    {
                        continue;
                    }
                }
                string url = match.Groups[Regex.VALID_URL_GROUP_URL].Value;
                int start = match.Groups[Regex.VALID_URL_GROUP_URL].Index;
                int end = start + match.Groups[Regex.VALID_URL_GROUP_URL].Length;
                var tcoMatch = Regex.VALID_TCO_URL.Match(url);
                if (tcoMatch != null)
                {
                    string tcoUrl = tcoMatch.Groups[0].Value;
                    string tcoUrlSlug = tcoMatch.Groups[1].Value;
                    // In the case of t.co URLs, don't allow additional path characters and
                    // ensure that the slug is under 40 chars.
                    if (tcoUrlSlug.Length > MAX_TCO_SLUG_LENGTH)
                    {
                        continue;
                    }
                    else
                    {
                        url = tcoUrl;
                        end = start + url.Length;
                    }
                }
                string host = match.Groups[Regex.VALID_URL_GROUP_DOMAIN].Value;
                if (isValidHostAndLength(url.Length, protocol, host))
                {
                    urls.Add(new Entity(start, end, url, Entity.Type.URL));
                }
            }

            return urls;
        }

        /**
         * Verifies that the host name adheres to RFC 3490 and 1035
         * Also, verifies that the entire url (including protocol) doesn't exceed MAX_URL_LENGTH
         *
         * @param originalUrlLength The length of the entire URL, including protocol if any
         * @param protocol The protocol used
         * @param originalHost The hostname to check validity of
         * @return true if the host is valid
         */
        public static bool isValidHostAndLength(int originalUrlLength, string protocol,
                                                string originalHost)
        {
            if (isEmptyString(originalHost))
            {
                return false;
            }
            int originalHostLength = originalHost.Length;
            string host;
            try
            {
                // Use IDN for all host names, if the host is all ASCII, it returns unchanged.
                // It comes with an added benefit of checking host length to be between 1 and 63 characters.

                var idn = new IdnMapping
                {
                    AllowUnassigned = true
                };
                host = idn.GetAscii(originalHost);
                // toASCII can throw IndexOutOfBoundsException when the domain name is longer than
                // 256 characters, instead of the documented IllegalArgumentException.
            }
            catch (Exception ex) when (ex is ArgumentException || ex is IndexOutOfRangeException)
            {
                return false;
            }
            int punycodeEncodedHostLength = host.Length;
            if (punycodeEncodedHostLength == 0)
            {
                return false;
            }
            // The punycodeEncoded host length might be different now, offset that length from the URL.
            int urlLength = originalUrlLength + punycodeEncodedHostLength - originalHostLength;
            // Add the protocol to our length check, if there isn't one,
            // to ensure it doesn't go over the limit.
            int urlLengthWithProtocol =
                urlLength + (protocol == null ? URL_GROUP_PROTOCOL_LENGTH : 0);
            return urlLengthWithProtocol <= MAX_URL_LENGTH;
        }

        /**
         * Extract #hashtag references from Tweet text.
         *
         * @param text of the tweet from which to extract hashtags
         * @return List of hashtags referenced (without the leading # sign)
         */
        public List<string> extractHashtags(string text)
        {
            if (isEmptyString(text))
            {
                return new List<string>();
            }

            List<string> extracted = new List<string>();
            foreach (Entity entity in extractHashtagsWithIndices(text))
            {
                extracted.Add(entity.value);
            }

            return extracted;
        }

        /**
         * Extract #hashtag references from Tweet text.
         *
         * @param text of the tweet from which to extract hashtags
         * @return List of hashtags referenced (without the leading # sign)
         */
        public List<Entity> extractHashtagsWithIndices(string text)
        {
            return extractHashtagsWithIndices(text, true);
        }

        /**
         * Extract #hashtag references from Tweet text.
         *
         * @param text of the tweet from which to extract hashtags
         * @param checkUrlOverlap if true, check if extracted hashtags overlap URLs and
         * remove overlapping ones
         * @return List of hashtags referenced (without the leading # sign)
         */
        private List<Entity> extractHashtagsWithIndices(string text, bool checkUrlOverlap)
        {
            if (isEmptyString(text))
            {
                return new List<Entity>();
            }

            // Performance optimization.
            // If text doesn't contain #/＃ at all, text doesn't contain
            // hashtag, so we can simply return an empty list.
            bool found = false;
            foreach (char c in text)
            {
                if (c == '#' || c == '＃')
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                return new List<Entity>();
            }

            List<Entity> extracted = new List<Entity>();
            var matches = Regex.VALID_HASHTAG.Matches(text).ToList();

            foreach (var match in matches)
            {
                string after = text.Substring(match.Index + match.Length);
                if (!Regex.INVALID_HASHTAG_MATCH_END.IsMatch(after))
                {
                    extracted.Add(new Entity(match, Entity.Type.HASHTAG, Regex.VALID_HASHTAG_GROUP_TAG));
                }
            }

            if (checkUrlOverlap)
            {
                // extract URLs
                List<Entity> urls = extractURLsWithIndices(text);
                if (urls.Any())
                {
                    extracted.AddRange(urls);
                    // remove overlap
                    removeOverlappingEntities(extracted);
                    // remove URL entities
                    extracted.RemoveAll(x => x.getType() != Entity.Type.HASHTAG);
                }
            }

            return extracted;
        }

        /**
         * Extract $cashtag references from Tweet text.
         *
         * @param text of the tweet from which to extract cashtags
         * @return List of cashtags referenced (without the leading $ sign)
         */
        public List<string> extractCashtags(string text)
        {
            if (isEmptyString(text))
            {
                return new List<string>();
            }

            List<string> extracted = new List<string>();
            foreach (Entity entity in extractCashtagsWithIndices(text))
            {
                extracted.Add(entity.value);
            }

            return extracted;
        }

        /**
         * Extract $cashtag references from Tweet text.
         *
         * @param text of the tweet from which to extract cashtags
         * @return List of cashtags referenced (without the leading $ sign)
         */
        public List<Entity> extractCashtagsWithIndices(string text)
        {
            if (isEmptyString(text))
            {
                return new List<Entity>();
            }

            // Performance optimization.
            // If text doesn't contain $, text doesn't contain
            // cashtag, so we can simply return an empty list.
            if (text.IndexOf('$') == -1)
            {
                return new List<Entity>();
            }

            List<Entity> extracted = new List<Entity>();
            var matches = Regex.VALID_CASHTAG.Matches(text).ToList();

            foreach (var match in matches)
            {
                extracted.Add(new Entity(match, Entity.Type.CASHTAG, Regex.VALID_CASHTAG_GROUP_CASHTAG));
            }

            return extracted;
        }

        public void setExtractURLWithoutProtocol(bool extractURLWithoutProtocol)
        {
            this.extractURLWithoutProtocol = extractURLWithoutProtocol;
        }

        public bool isExtractURLWithoutProtocol()
        {
            return extractURLWithoutProtocol;
        }

        /**
         * Modify Unicode-based indices of the entities to UTF-16 based indices.
         * <p>
         * In UTF-16 based indices, Unicode supplementary characters are counted as two characters.
         * <p>
         * This method requires that the list of entities be in ascending order by start index.
         *
         * @param text original text
         * @param entities entities with Unicode based indices
         */
        public void modifyIndicesFromUnicodeToUTF16(string text, List<Entity> entities)
        {
            var convert = new IndexConverter(text);

            foreach (var entity in entities)
            {
                entity.start = convert.codePointsToCodeUnits(entity.start);
                entity.end = convert.codePointsToCodeUnits(entity.end);
            }
        }

        /**
         * Modify UTF-16-based indices of the entities to Unicode-based indices.
         * <p>
         * In Unicode-based indices, Unicode supplementary characters are counted as single characters.
         * <p>
         * This method requires that the list of entities be in ascending order by start index.
         *
         * @param text original text
         * @param entities entities with UTF-16 based indices
         */
        public void modifyIndicesFromUTF16ToUnicode(string text, List<Entity> entities)
        {
            var convert = new IndexConverter(text);

            foreach (var entity in entities)
            {
                entity.start = convert.codeUnitsToCodePoints(entity.start);
                entity.end = convert.codeUnitsToCodePoints(entity.end);
            }
        }

        private static bool isEmptyString(string str)
        {
            return str == null || str.Length == 0;
        }

        /**
         * An efficient converter of indices between code points and code units.
         */
        public class IndexConverter
        {
            protected string text;

            // Keep track of a single corresponding pair of code unit and code point
            // offsets so that we can re-use counting work if the next requested
            // entity is near the most recent entity.
            protected int codePointIndex = 0;
            protected int charIndex = 0;

            public IndexConverter(string text)
            {
                this.text = text;
            }

            /**
             * Converts code units to code points
             *
             * @param charIndex Index into the string measured in code units.
             * @return The code point index that corresponds to the specified character index.
             */
            public int codeUnitsToCodePoints(int charIndex)
            {
                if (charIndex < this.charIndex)
                {
                    this.codePointIndex -= CodePointCount(text, charIndex, this.charIndex);
                }
                else
                {
                    this.codePointIndex += CodePointCount(text, this.charIndex, charIndex);
                }
                this.charIndex = charIndex;

                // Make sure that charIndex never points to the second code unit of a
                // surrogate pair.
                if (charIndex > 0 && char.IsSurrogate(text, charIndex - 1))
                {
                    this.charIndex -= 1;
                }
                return this.codePointIndex;
            }

            /**
             * Converts code points to code units
             *
             * @param codePointIndex Index into the string measured in code points.
             * @return the code unit index that corresponds to the specified code point index.
             */
            public int codePointsToCodeUnits(int codePointIndex)
            {
                // Note that offsetByCodePoints accepts negative indices.
                this.charIndex =
                    OffsetByCodePoints(text, this.charIndex, codePointIndex - this.codePointIndex);
                this.codePointIndex = codePointIndex;
                return this.charIndex;
            }


            /*
             * gets the offset by code points
             * TODO: check this method, I don't know much about encoding and code points
             */
            private int OffsetByCodePoints(string str, int beginIndex, int endIndex)
            {
                int totalCodePoints = CodePointCount(str, 0, str.Length);
                int sectionCodePoints = CodePointCount(str, beginIndex, endIndex);
                return totalCodePoints - sectionCodePoints;
            }

            /*
             * counts code points
             * TODO: check this method, I don't know much about encoding and code points
             */
            private int CodePointCount(string str, int beginIndex, int endIndex)
            {
                var substring = str[beginIndex..endIndex];
                byte[] bytes = Encoding.UTF32.GetBytes(substring);

                int codePointCount = bytes.Length / 4;
                return codePointCount;
            }
        }
    }
}