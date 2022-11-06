using PuppeteerSharp.Replay.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuppeteerSharp.Replay.Tests
{
    [Collection("Puppeteer")]
    public class RunnerExtensionTests
    {
        private readonly PuppeteerFixture _Fixture;
        UserFlow _Flow;

        public RunnerExtensionTests(PuppeteerFixture fixture)
        {
            _Flow = UserFlow.Parse(File.ReadAllText($"Data{Path.DirectorySeparatorChar}UserFlowExample.json"));
            _Fixture = fixture;
        }

        [Fact]
        public async Task CanRunFlow()
        {
            using IPage page = await _Fixture.Browser.NewPageAsync();

            var sut = new RunnerExtension(_Fixture.Browser, page, null);
            var runner = await Runner.CreateRunner(_Flow, sut);
            await runner.Run();

            Assert.True(true);
        }
    }
}
