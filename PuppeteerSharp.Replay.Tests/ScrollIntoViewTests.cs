using PuppeteerSharp.Replay.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuppeteerSharp.Replay.Tests
{
    [Collection("Puppeteer")]
    public class ScrollIntoViewTests
    {
        private UserFlow _Flow;
        private readonly PuppeteerFixture _Fixture;

        public ScrollIntoViewTests(PuppeteerFixture fixture)
        {
            _Flow = UserFlow.Parse(File.ReadAllText($"Data{Path.DirectorySeparatorChar}scroll-into-view.json"));
            _Fixture = fixture;
        }

        [Fact]
        public async Task CanRunScrollIntoView()
        {
            using IPage page = await _Fixture.Browser.NewPageAsync();

            var sut = new RunnerExtension(_Fixture.Browser, page, 5 * 1000);
            var runner = new Runner(_Flow, sut);
            await runner.Run();

            var button = await page.QuerySelectorAsync("button");
            var buttonText = await page.EvaluateFunctionAsync<string>("e => e.value");

            Assert.Equal("Clicked", buttonText);
        }
    }
}
