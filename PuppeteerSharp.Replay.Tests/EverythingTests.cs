using PuppeteerSharp.Replay.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuppeteerSharp.Replay.Tests
{
    [Collection("Puppeteer")]
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
        private readonly PuppeteerFixture _Fixture;

        public EverythingTests(PuppeteerFixture fixture)
        {
            _Flow = UserFlow.Parse(File.ReadAllText($"Data{Path.DirectorySeparatorChar}everything.json"));
            _Fixture = fixture;
        }

        [Fact]
        public async Task CanRunEverything()
        {
            using IPage page = await _Fixture.Browser.NewPageAsync();

            var sut = new RunnerExtension(_Fixture.Browser, page, 5 * 1000);
            var runner = await Runner.CreateRunner(_Flow, sut);
            await runner.Run();

            var logPre = await page.QuerySelectorAsync("#log");
            var logText = await logPre.EvaluateFunctionAsync<string>("e => e.innerText");
            var logLines = logText.ReplaceLineEndings().Split(Environment.NewLine);

            await page.CloseAsync();

            Assert.Equal(_ExpectedLog, logLines);
        }
    }
}
