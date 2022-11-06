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
        UserFlow _Flow;

        public RunnerExtensionTests(PuppeteerFixture fixture)
        {
            _Flow = UserFlow.Parse(File.ReadAllText($"Data{Path.DirectorySeparatorChar}UserFlowExample.json"));
        }

        [Fact]
        public async Task CanRunFlow()
        {
            var options = new LaunchOptions()
            {
                Headless = true,
                DefaultViewport = new ViewPortOptions() { Width = 1280, Height = 810 }
            };
            using IBrowser browser = await Puppeteer.LaunchAsync(options);
            using IPage page = await browser.NewPageAsync();

            var sut = new RunnerExtension(browser, page, null);
            var runner = await Runner.CreateRunner(_Flow, sut);
            await runner.Run();

            Assert.True(true);
        }
    }
}
