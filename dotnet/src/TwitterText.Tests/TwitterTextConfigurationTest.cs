using Xunit;

namespace TwitterText.Tests
{
    public class TwitterTextConfigurationTest
    {
        [Fact]
        public void TestDefaultVersion()
        {
            Assert.Equal(TwitterTextConfiguration.configurationFromJson("", false),
                TwitterTextConfiguration.configurationFromJson("v2.json", true));
        }

        [Fact]
        public void TestInvalidJsonPathDoesntCrash()
        {
            Assert.Equal(TwitterTextConfiguration.configurationFromJson("unknown", true),
                TwitterTextConfiguration.configurationFromJson("v2.json", true));
        }

        [Fact]
        public void TestJsonAsString()
        {
            var configuration =
                TwitterTextConfiguration.configurationFromJson("{\"version\": 1, " +
                    "\"maxWeightedTweetLength\": 30, \"scale\": 1, \"defaultWeight\": 1, " +
                    "\"transformedURLLength\": 14, \"ranges\": [{\"start\": 0, \"end\": 4351, " +
                    "\"weight\": 2}]}", false);
            Assert.Equal(1, configuration.getVersion());
            Assert.Equal(30, configuration.getMaxWeightedTweetLength());
            Assert.Equal(1, configuration.getScale());
            Assert.Equal(1, configuration.getDefaultWeight());
            Assert.Equal(14, configuration.getTransformedURLLength());
            var ranges =
                configuration.getRanges();
            Assert.NotNull(ranges);
            Assert.Single(ranges);
            var latinCharRange = ranges[0];
            Assert.Equal(new Range(0, 4351), latinCharRange.getRange());
            Assert.Equal(2, latinCharRange.getWeight());
        }

        [Fact]
        public void TestVersion1()
        {
            var configuration =
                TwitterTextConfiguration.configurationFromJson("v1.json", true);
            Assert.Equal(1, configuration.getVersion());
            Assert.Equal(140, configuration.getMaxWeightedTweetLength());
            Assert.Equal(1, configuration.getScale());
            Assert.Equal(1, configuration.getDefaultWeight());
            Assert.Equal(23, configuration.getTransformedURLLength());
            Assert.Empty(configuration.getRanges());
        }

        [Fact]
        public void TestVersion2()
        {
            var configuration =
                TwitterTextConfiguration.configurationFromJson("v2.json", true);
            Assert.Equal(2, configuration.getVersion());
            Assert.Equal(280, configuration.getMaxWeightedTweetLength());
            Assert.Equal(100, configuration.getScale());
            Assert.Equal(200, configuration.getDefaultWeight());
            Assert.Equal(23, configuration.getTransformedURLLength());
            var ranges =
                configuration.getRanges();
            Assert.NotNull(ranges);
            Assert.Equal(4, ranges.Count);
            var latinCharRange = ranges[0];
            Assert.Equal(new Range(0, 4351), latinCharRange.getRange());
            Assert.Equal(100, latinCharRange.getWeight());
            var spacesGeneralPunctuationBlock =
                ranges[1];
            Assert.Equal(new Range(8192, 8205), spacesGeneralPunctuationBlock.getRange());
            Assert.Equal(100, spacesGeneralPunctuationBlock.getWeight());
            var visibleGeneralPunctuationBlock1 =
                ranges[2];
            Assert.Equal(new Range(8208, 8223), visibleGeneralPunctuationBlock1.getRange());
            Assert.Equal(100, visibleGeneralPunctuationBlock1.getWeight());
            var visibleGeneralPunctuationBlock2 =
                ranges[3];
            Assert.Equal(new Range(8242, 8247), visibleGeneralPunctuationBlock2.getRange());
            Assert.Equal(100, visibleGeneralPunctuationBlock2.getWeight());
        }
    }
}
