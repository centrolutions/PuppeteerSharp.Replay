using PuppeteerSharp.Replay.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PuppeteerSharp.Replay.Tests
{
    [Collection("Puppeteer")]
    public class RunnerExtensionTests
    {
        private readonly PuppeteerFixture _Fixture;
        UserFlow _Flow;
        const string GetWindowContextClicksScript = @"() => {
        const context = window;
        return (
          context.buttonClicks[0] === 0 &&
          context.buttonClicks[1] === 1 &&
          context.buttonClicks[2] === 2
        );
      }";

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

        [Fact]
        public async Task CanReplayClickEvents()
        {
            using IPage page = await _Fixture.Browser.NewPageAsync();

            var url = $"{PuppeteerFixture.BaseUrl}/main.html";
            var sut = new RunnerExtension(_Fixture.Browser, page, 0);
            var flow = new UserFlow()
            {
                Title = "Replay Mouse Clicks",
                Steps = new Step[]
                {
                    new Step()
                    {
                        Type = StepType.Navigate,
                        Url = url
                    },
                    new Step()
                    {
                        Type = StepType.Click,
                        Button = "primary",
                        Selectors = new string[][] { new string[] { "#test" } },
                        OffsetX = 1,
                        OffsetY = 1,
                    },
                    new Step()
                    {
                        Type = StepType.Click,
                        Button = "auxiliary",
                        Selectors = new string[][] { new string[] { "#test" } },
                        OffsetX = 1,
                        OffsetY = 1,
                    },
                    new Step()
                    {
                        Type = StepType.Click,
                        Button = "secondary",
                        Selectors = new string[][] { new string[] { "#test" } },
                        OffsetX = 1,
                        OffsetY = 1,
                    }
                }
            };

            var runner = await Runner.CreateRunner(flow, sut);
            await runner.Run();

            var result = await page.EvaluateFunctionAsync<bool>(GetWindowContextClicksScript);
            Assert.True(result);
        }

        [Fact]
        public async Task CanClickSvgPathElements()
        {
            using IPage page = await _Fixture.Browser.NewPageAsync();

            var url = $"{PuppeteerFixture.BaseUrl}/svg.html";
            var sut = new RunnerExtension(_Fixture.Browser, page, 0);
            var flow = new UserFlow()
            {
                Title = "Click SVG Path Element",
                Steps = new Step[]
                {
                    new Step()
                    {
                        Type = StepType.Navigate,
                        Url = url
                    },
                    new Step()
                    {
                        Type = StepType.Click,
                        Selectors = new string[][] { new string[] { "svg > path" } },
                        OffsetX = 1,
                        OffsetY = 1,
                    },
                }
            };

            var runner = await Runner.CreateRunner(flow, sut);
            var result = await runner.Run();

            Assert.True(result);
        }

        [Fact]
        public async Task CanClickElementsInInvisibleParents()
        {
            using IPage page = await _Fixture.Browser.NewPageAsync();

            var url = $"{PuppeteerFixture.BaseUrl}/invisible-parent.html";
            var sut = new RunnerExtension(_Fixture.Browser, page, 0);
            var flow = new UserFlow()
            {
                Title = "Click element in invisible parent",
                Steps = new Step[]
                {
                    new Step()
                    {
                        Type = StepType.Navigate,
                        Url = url
                    },
                    new Step()
                    {
                        Type = StepType.Click,
                        Selectors = new string[][] { new string[] { ".parent", ".child" } },
                        OffsetX = 1,
                        OffsetY = 1,
                    }
                }
            };

            var runner = await Runner.CreateRunner(flow, sut);
            var result = await runner.Run();

            Assert.True(result);
        }

        [Fact]
        public async Task CanClickOnCheckboxes()
        {
            using IPage page = await _Fixture.Browser.NewPageAsync();

            var url = $"{PuppeteerFixture.BaseUrl}/checkbox.html";
            var sut = new RunnerExtension(_Fixture.Browser, page, 0);
            var flow = new UserFlow()
            {
                Title = "Click Checkbox",
                Steps = new Step[]
                {
                    new Step()
                    {
                        Type = StepType.Navigate,
                        Url = url
                    },
                    new Step()
                    {
                        Type = StepType.Click,
                        Selectors = new string[][] { new string[] { "input" } },
                        OffsetX = 1,
                        OffsetY = 1,
                    }
                }
            };

            var runner = await Runner.CreateRunner(flow, sut);
            await runner.Run();

            var result = await page.EvaluateFunctionAsync<bool>("() => document.querySelector('input')?.checked");
            Assert.True(result);
        }

        [Fact]
        public async Task CanReplayKeyboardEvents()
        {
            using IPage page = await _Fixture.Browser.NewPageAsync();

            var url = $"{PuppeteerFixture.BaseUrl}/input.html";
            var sut = new RunnerExtension(_Fixture.Browser, page, 0);
            var flow = new UserFlow()
            {
                Title = "Replay Keyboard Events",
                Steps = new Step[]
                {
                    new Step()
                    {
                        Type = StepType.Navigate,
                        Url = url
                    },
                    new Step()
                    {
                        Type = StepType.KeyDown,
                        Target = "main",
                        Key = "Tab"
                    },
                    new Step()
                    {
                        Type = StepType.KeyUp,
                        Target = "main",
                        Key = "Tab",
                    },
                    new Step()
                    {
                        Type = StepType.KeyDown,
                        Target = "main",
                        Key = "1"
                    },
                    new Step()
                    {
                        Type = StepType.KeyUp,
                        Target = "main",
                        Key = "1"
                    },
                    new Step()
                    {
                        Type = StepType.KeyDown,
                        Target = "main",
                        Key = "Tab"
                    },
                    new Step()
                    {
                        Type = StepType.KeyUp,
                        Target = "main",
                        Key = "Tab",
                    },
                    new Step()
                    {
                        Type = StepType.KeyDown,
                        Target = "main",
                        Key = "2"
                    },
                    new Step()
                    {
                        Type = StepType.KeyUp,
                        Target = "main",
                        Key = "2"
                    },
                }
            };

            var runner = await Runner.CreateRunner(flow, sut);
            await runner.Run();

            var logText = await page.EvaluateFunctionAsync<string>("() => document.getElementById('log').innerText");
            var logLines = logText.Trim().ReplaceLineEndings().Split(Environment.NewLine);

            var expectedLog = new string[] { "one:1", "two:2" };
            Assert.Equal(expectedLog, logLines);
        }

        [Fact]
        public async Task CanReplayEventsOnSelectElement()
        {
            using IPage page = await _Fixture.Browser.NewPageAsync();

            var url = $"{PuppeteerFixture.BaseUrl}/select.html";
            var sut = new RunnerExtension(_Fixture.Browser, page, 0);
            var flow = new UserFlow()
            {
                Title = "Change Select Value",
                Steps = new Step[]
                {
                    new Step()
                    {
                        Type = StepType.Navigate,
                        Url = url
                    },
                    new Step()
                    {
                        Type = StepType.Change,
                        Target = "main",
                        Selectors = new string[][] { new string[] { "aria/Select" } },
                        Value = "O2"
                    }
                }
            };

            var runner = await Runner.CreateRunner(flow, sut);
            await runner.Run();

            var selectValue = await page.EvaluateFunctionAsync<string>("() => document.getElementById('select').value");
            Assert.Equal("O2", selectValue);
        }

        [Fact]
        public async Task CloseSelectDropdownAfterTheClickAndChange()
        {
            using IPage page = await _Fixture.Browser.NewPageAsync();

            var url = $"{PuppeteerFixture.BaseUrl}/select.html";
            var sut = new RunnerExtension(_Fixture.Browser, page, 0);
            var flow = new UserFlow()
            {
                Title = "Close Dropdown After Click and Change",
                Steps = new Step[]
                {
                    new Step()
                    {
                        Type = StepType.Navigate,
                        Url = url
                    },
                    new Step()
                    {
                        Type = StepType.Click,
                        Selectors = new string[][] { new string[] { "aria/Select" } },
                        OffsetX = 1,
                        OffsetY = 1,
                    },
                    new Step()
                    {
                        Type = StepType.Change,
                        Target = "main",
                        Selectors = new string[][] { new string[] { "aria/Select" } },
                        Value = "O2"
                    }
                }
            };

            var runner = await Runner.CreateRunner(flow, sut);
            await runner.Run();

            var selectValue = await page.EvaluateFunctionAsync<string>("() => document.getElementById('select').value");
            Assert.Equal("O2", selectValue);

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
