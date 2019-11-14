using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace TwitterText.Tests
{
    public class TwitterTextEmojiRegexTest
    {
        [Fact]
        public void TestEmojiUnicode10()
        {
            var matches = TwitterTextEmojiRegex.VALID_EMOJI_PATTERN
                .Matches("Unicode 10.0; grinning face with one large and one small eye: 🤪;" +
                    " woman with headscarf: 🧕;" +
                    " (fitzpatrick) woman with headscarf + medium-dark skin tone: 🧕🏾;" +
                    " flag (England): 🏴󠁧󠁢󠁥󠁮󠁧󠁿").ToList();
            var expected = new List<string> { "🤪", "🧕", "🧕🏾", "🏴󠁧󠁢󠁥󠁮󠁧󠁿" };

            Debug.WriteLine(string.Join(',', matches.Select(x => x.Value)));

            for (var i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                Assert.Equal(expected[i], match.Groups[0].Value);
            }
            Assert.Equal(expected.Count, matches.Count);
        }

        [Fact]
        public void TestEmojiUnicode9()
        {
            var matches = TwitterTextEmojiRegex.VALID_EMOJI_PATTERN
                .Matches("Unicode 9.0; face with cowboy hat: 🤠;" +
                    "woman dancing: 💃, woman dancing + medium-dark skin tone: 💃🏾").ToList();
            var expected = new List<string> { "🤠", "💃", "💃🏾" };

            for (var i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                Assert.Equal(expected[i], match.Groups[0].Value);
            }
            Assert.Equal(expected.Count, matches.Count);
        }
    }
}
