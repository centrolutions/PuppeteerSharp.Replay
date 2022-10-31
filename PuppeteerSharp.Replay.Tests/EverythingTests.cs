using PuppeteerSharp.Replay.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuppeteerSharp.Replay.Tests
{
    public class EverythingTests
    {
        UserFlow _Flow;
        const string _ExpectedLog = "window dimensions 1280x810\r\nclick targetId=button button=0 value=\r\nclick targetId=button button=0 value=\r\ndblclick targetId=button button=0 value=\r\nchange targetId=input button=undefined value=test\r\ncontextmenu targetId=input button=2 value=test\r\nmouseenter targetId=hover button=0 value=";

        public EverythingTests()
        {
            _Flow = UserFlow.Parse(File.ReadAllText($"Data{Path.DirectorySeparatorChar}everything.json"));
        }

        [Fact]
        public async Task CanRunEverything()
        {
            var fetcher = new BrowserFetcher();
            await fetcher.DownloadAsync();
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

            var logPre = await page.QuerySelectorAsync("#log");
            var logText = await logPre.EvaluateFunctionAsync<string>("e => e.innerText");

            Assert.Equal(_ExpectedLog, logText);
        }
    }
}
