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
        readonly string[] _ExpectedLog = new string[7]
        {
            "window dimensions 900x700",
            "click targetId=button button=0 value=",
            "click targetId=button button=0 value=",
            "dblclick targetId=button button=0 value=",
            "change targetId=input button=undefined value=test",
            "contextmenu targetId=input button=2 value=test",
            "mouseenter targetId=hover button=0 value="
        };

        public EverythingTests()
        {
            _Flow = UserFlow.Parse(File.ReadAllText($"Data{Path.DirectorySeparatorChar}everything.json"));
        }

        [Fact]
        public async Task CanRunEverything()
        {
            //var fetcher = new BrowserFetcher();
            //if (!fetcher.LocalRevisions().Any())
            //    await fetcher.DownloadAsync();

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
            var logLines = logText.ReplaceLineEndings().Split(Environment.NewLine);

            Assert.Equal(_ExpectedLog, logLines);
        }
    }
}
