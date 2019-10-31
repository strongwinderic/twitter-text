using Xunit;

namespace TwitterText.Tests
{
    public class TwitterTextParserTest
    {
        [Fact]
        public void TestParseTweetWithoutUrlExtraction()
        {
            Assert.True(0 == TwitterTextParser.parseTweetWithoutUrlExtraction(null).weightedLength, "Handle null input");
            Assert.True(0 == TwitterTextParser.parseTweetWithoutUrlExtraction("").weightedLength, "Handle empty input");
            Assert.True(11 ==TwitterTextParser.parseTweetWithoutUrlExtraction("Normal Text").weightedLength, "Count Latin chars normally");
            Assert.True(38 ==TwitterTextParser.parseTweetWithoutUrlExtraction("Text with #hashtag, @mention and $CASH").weightedLength, "Count hashtags, @mentions and cashtags normally");
            Assert.True(94 == TwitterTextParser.parseTweetWithoutUrlExtraction("CJK Weighted chars: " + "あいうえおかきくけこあいうえおかきくけこあいうえおかきくけこあいうえおかき").weightedLength,
                "Count CJK chars with their appropriate weights");
            Assert.True(69 == TwitterTextParser.parseTweetWithoutUrlExtraction("Text with url: " + "a.com http://abc.com https://longurllongurllongurl.com").weightedLength,
                "URLs should be counted without transformation");
            Assert.True(39 == TwitterTextParser.parseTweetWithoutUrlExtraction("Text with t.co url: https://t.co/foobar").weightedLength, "t.co URLs should be counted without transformation");
        }
    }
}
