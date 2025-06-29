using PuppeteerSharp.Replay.Contracts;

namespace PuppeteerSharp.Replay.Tests
{
    public class UserFlowTests
    {
        [Fact]
        public void Parse_DeserializesCorrectly()
        {
            var jsonText = File.ReadAllText($"Data{Path.DirectorySeparatorChar}UserFlowExample.json");

            var sut = UserFlow.Parse(jsonText);

            Assert.NotNull(sut);
            Assert.Equal("Select All Menu Items on Centrolutions", sut.Title);
            Assert.Equal(8, sut.Steps.Length);
        }
    }
}
