using System;
using System.Text;
using Xunit;

namespace TwitterText.Tests
{
    public class ValidatorTest
    {
        protected Validator validator = new Validator();

        [Fact]
        public void TestBOMCharacter()
        {
            Assert.False(validator.isValidTweet("test \uFFFE"));
            Assert.False(validator.isValidTweet("test \uFEFF"));
        }

        [Fact]
        public void TestInvalidCharacter()
        {
            Assert.False(validator.isValidTweet("test \uFFFF"));
            Assert.False(validator.isValidTweet("test \uFEFF"));
        }

        [Fact]
        public void TestDirectionChangeCharacters()
        {
            Assert.True(validator.isValidTweet("test \u202A test"));
            Assert.True(validator.isValidTweet("test \u202B test"));
            Assert.True(validator.isValidTweet("test \u202C test"));
            Assert.True(validator.isValidTweet("test \u202D test"));
            Assert.True(validator.isValidTweet("test \u202E test"));
            Assert.True(validator.isValidTweet("test \u202F test"));
        }

        [Fact]
        public void TestAccentCharacters()
        {
            String c = "\u0065\u0301";
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < 279; i++)
            {
                builder.Append(c);
            }
            Assert.True(validator.isValidTweet(builder.ToString()));
            Assert.True(validator.isValidTweet(builder.Append(c).ToString()));
            Assert.False(validator.isValidTweet(builder.Append(c).ToString()));
        }

        [Fact]
        public void TestMutiByteCharacters()
        {
            String c = "\ud83d\ude02";
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < 139; i++)
            {
                builder.Append(c);
            }
            Assert.True(validator.isValidTweet(builder.ToString()));
            Assert.True(validator.isValidTweet(builder.Append(c).ToString()));
            Assert.False(validator.isValidTweet(builder.Append(c).ToString()));
        }
    }
}
