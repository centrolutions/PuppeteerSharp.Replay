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

            var sut = new RunnerExtension(_Fixture.Browser, page, 0);
            var runner = await Runner.CreateRunner(_Flow, sut);
            await runner.Run();

            Assert.True(true);
        }

        [Fact]
        public async Task CanReplayScrollEvents()
        {
            using IPage page = await _Fixture.Browser.NewPageAsync();

            var sut = new RunnerExtension(_Fixture.Browser, page, 0);
            UserFlow flow = SetupScrollFlow();

            var runner = await Runner.CreateRunner(flow, sut);
            await runner.Run();

            var x = await page.EvaluateFunctionAsync<int>("() => window.pageXOffset");
            var y = await page.EvaluateFunctionAsync<int>("() => window.pageYOffset");
            var top = await page.EvaluateFunctionAsync<int>("() => document.querySelector('#overflow')?.scrollTop");
            var left = await page.EvaluateFunctionAsync<int>("() => document.querySelector('#overflow')?.scrollLeft");

            Assert.Equal(40, x);
            Assert.Equal(40, y);
            Assert.Equal(40, top);
            Assert.Equal(0, left);
        }

        [Fact]
        public async Task CanReplayNavigateEvents()
        {
            using IPage page = await _Fixture.Browser.NewPageAsync();

            var url = $"{PuppeteerFixture.BaseUrl}/empty.html";
            var sut = new RunnerExtension(_Fixture.Browser, page, 0);
            var flow = new UserFlow()
            {
                Title = "Replay Navigate Events",
                Steps = new Step[]
                {
                    new Step()
                    {
                        Type = StepType.Navigate,
                        Url = url
                    }
                }
            };

            var runner = await Runner.CreateRunner(flow, sut);
            await runner.Run();

            Assert.Equal(url, page.Url);
        }

        static UserFlow SetupScrollFlow()
        {
            var steps = new List<Step>()
            {
                new Step()
                {
                    Type = StepType.Navigate,
                    Url = $"{PuppeteerFixture.BaseUrl}/scroll.html"
                },
                new Step()
                {
                    Type = StepType.SetViewport,
                    Width = 800,
                    Height = 600,
                    IsLandscape = false,
                    IsMobile = false,
                    DeviceScaleFactor = 1,
                    HasTouch = false
                },
                new Step()
                {
                    Type = StepType.Scroll,
                    Target = "main",
                    Selectors = new string[][] { new string[] { "body > div:nth-child(1)" } },
                    OffsetX = 0,
                    OffsetY = 40,
                },
                new Step()
                {
                    Type = StepType.Scroll,
                    Target = "main",
                    OffsetX = 40,
                    OffsetY = 40,
                }
            };
            var flow = new UserFlow() { Title = "Scroll" };
            flow.Steps = steps.ToArray();
            return flow;
        }
    }
}
