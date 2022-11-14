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

            var result = await ExecuteFlow(page, _Flow);

            Assert.True(result);
        }

        [Fact]
        public async Task CanReplayScrollEvents()
        {
            using IPage page = await _Fixture.Browser.NewPageAsync();
            UserFlow flow = SetupScrollFlow();

            var result = await ExecuteFlow(page, flow);

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
            UserFlow flow = SetupReplayNavigateFlow(url);

            var result = await ExecuteFlow(page, flow);

            Assert.Equal(url, page.Url);
        }

        [Fact]
        public async Task CanReplayClickEvents()
        {
            using IPage page = await _Fixture.Browser.NewPageAsync();
            UserFlow flow = SetupReplayMouseClicksFlow();

            var runnerResult = await ExecuteFlow(page, flow);
            var result = await page.EvaluateFunctionAsync<bool>(GetWindowContextClicksScript);

            Assert.True(result);
        }

        [Fact]
        public async Task CanClickSvgPathElements()
        {
            using IPage page = await _Fixture.Browser.NewPageAsync();
            UserFlow flow = SetupClickSvgPathFlow();

            var result = await ExecuteFlow(page, flow);

            Assert.True(result);
        }

        [Fact]
        public async Task CanClickElementsInInvisibleParents()
        {
            using IPage page = await _Fixture.Browser.NewPageAsync();
            UserFlow flow = SetupClickElementsInInvisibleParentFlow();

            var result = await ExecuteFlow(page, flow);

            Assert.True(result);
        }
        
        [Fact]
        public async Task CanClickOnCheckboxes()
        {
            using IPage page = await _Fixture.Browser.NewPageAsync();
            UserFlow flow = SetupClickCheckboxFlow();

            var runnerResult = await ExecuteFlow(page, flow);
            var result = await page.EvaluateFunctionAsync<bool>("() => document.querySelector('input')?.checked");
            
            Assert.True(result);
        }

        [Fact]
        public async Task CanReplayKeyboardEvents()
        {
            var expectedLog = new string[] { "one:1", "two:2" };
            using IPage page = await _Fixture.Browser.NewPageAsync();
            UserFlow flow = SetupReplayKeyboardEventsFlow();

            var result = await ExecuteFlow(page, flow);

            var logText = await page.EvaluateFunctionAsync<string>("() => document.getElementById('log').innerText");
            var logLines = logText.Trim().ReplaceLineEndings().Split(Environment.NewLine);
            
            Assert.Equal(expectedLog, logLines);
        }

        [Fact]
        public async Task CanReplayEventsOnSelectElement()
        {
            using IPage page = await _Fixture.Browser.NewPageAsync();
            UserFlow flow = SetupReplaySelectEventsFlow();

            var result = await ExecuteFlow(page, flow);

            var selectValue = await page.EvaluateFunctionAsync<string>("() => document.getElementById('select').value");
            
            Assert.Equal("O2", selectValue);
        }

        [Fact]
        public async Task CloseSelectDropdownAfterTheClickAndChange()
        {
            using IPage page = await _Fixture.Browser.NewPageAsync();
            UserFlow flow = SetupCloseDropdownFlow();

            var result = await ExecuteFlow(page, flow);

            var selectValue = await page.EvaluateFunctionAsync<string>("() => document.getElementById('select').value");
            
            Assert.Equal("O2", selectValue);
        }

        [Fact]
        public async Task CanReplayChangesOnNonTextInputs()
        {
            using IPage page = await _Fixture.Browser.NewPageAsync();
            UserFlow flow = SetupReplayChangesOnNonTextInputsFlow();

            var result = await ExecuteFlow(page, flow);

            var selectedValue = await page.EvaluateFunctionAsync<string>("() => document.getElementById('color').value");

            Assert.Equal("#333333", selectedValue);
        }

        [Fact]
        public async Task CanChangeValueOfInputThatAlreadyHasValue()
        {
            var page = await _Fixture.Browser.NewPageAsync();
            UserFlow flow = SetupChangeExistingValueFlow();

            var result = await ExecuteFlow(page, flow);

            var selectedValue = await page.EvaluateFunctionAsync<string>("() => document.getElementById('prefilled').value");

            Assert.Equal("cba", selectedValue);
        }

        [Fact]
        public async Task CanChangeValueOfPartiallyFilledValue()
        {
            var page = await _Fixture.Browser.NewPageAsync();
            UserFlow flow = SetupChangePartialExistingValueFlow();

            var result = await ExecuteFlow(page, flow);

            var selectedValue = await page.EvaluateFunctionAsync<string>("() => document.getElementById('partially-prefilled').value");

            Assert.Equal("abcdef", selectedValue);
        }

        [Fact]
        public async Task CanReplayViewportChanges()
        {
            var page = await _Fixture.Browser.NewPageAsync();
            UserFlow flow = SetupChangeViewportFlow();

            var result = await ExecuteFlow(page, flow);

            var width = await page.EvaluateFunctionAsync<int>("() => window.visualViewport?.width");
            var height = await page.EvaluateFunctionAsync<int>("() => window.visualViewport?.height");

            Assert.Equal(800, width);
            Assert.Equal(600, height);
        }

        [Fact]
        public async Task CanScrollIntoViewWhenNeeded()
        {
            var page = await _Fixture.Browser.NewPageAsync();
            UserFlow flow = SetupScrollIntoViewFlow();

            var result = await ExecuteFlow(page, flow);

            var buttonText = await page.EvaluateFunctionAsync<string>("() => document.querySelector('button')?.innerText");

            Assert.Equal("clicked", buttonText);
        }

        [Fact]
        public async Task CanReplayAriaSelectorsOnInputs()
        {
            var page = await _Fixture.Browser.NewPageAsync();
            UserFlow flow = SetupAriaSelectorsOnInputsFlow();

            var result = await ExecuteFlow(page, flow);

            var activeElementId = await page.EvaluateFunctionAsync<string>("() => document.activeElement?.id");

            Assert.Equal("name", activeElementId);
        }

        [Fact(Skip = "PuppeteerSharp does not support yet.")]
        public async Task CanReplayTextSelectors()
        {
            var page = await _Fixture.Browser.NewPageAsync();
            UserFlow flow = SetupTextSelectorsFlow();

            var result = await ExecuteFlow(page, flow);

            var activeElementId = await page.EvaluateFunctionAsync<string>("() => document.activeElement?.id");

            Assert.Equal("input", activeElementId);
        }

        [Fact]
        public async Task CanWaitForElement()
        {
            var page = await _Fixture.Browser.NewPageAsync();
            UserFlow flow = SetupWaitForElementFlow();

            var result = await ExecuteFlow(page, flow);

            var buttonCount = await page.EvaluateFunctionAsync<int>("() => document.querySelectorAll('custom-element').length");

            Assert.Equal(2, buttonCount);
        }

        [Fact]
        public async Task CanWaitForExpression()
        {
            var page = await _Fixture.Browser.NewPageAsync();
            UserFlow flow = SetupWaitForExpressionFlow();

            var result = await ExecuteFlow(page, flow);

            var count = await page.EvaluateFunctionAsync<int>("() => document.querySelectorAll('custom-element').length");

            Assert.Equal(2, count);
        }

        [Fact(Skip = "temp")]
        public async Task CanReplayWithPopups()
        {
            var page = await _Fixture.Browser.NewPageAsync();
            UserFlow flow = SetupReplayWithPopupsFlow();

            var result = await ExecuteFlow(page, flow);

            Assert.True(result);
        }

        async Task<bool> ExecuteFlow(IPage page, UserFlow flow)
        {
            var runnerExtension = new RunnerExtension(_Fixture.Browser, page, 0);
            var runner = await Runner.CreateRunner(flow, runnerExtension);
            var result = await runner.Run();

            return result;
        }

        static UserFlow SetupReplayNavigateFlow(string url)
        {
            return new UserFlow()
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

        static UserFlow SetupReplayMouseClicksFlow()
        {
            return new UserFlow()
            {
                Title = "Replay Mouse Clicks",
                Steps = new Step[]
                {
                    new Step()
                    {
                        Type = StepType.Navigate,
                        Url = $"{PuppeteerFixture.BaseUrl}/main.html"
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
        }

        static UserFlow SetupClickSvgPathFlow()
        {
            return new UserFlow()
            {
                Title = "Click SVG Path Element",
                Steps = new Step[]
                {
                    new Step()
                    {
                        Type = StepType.Navigate,
                        Url = $"{PuppeteerFixture.BaseUrl}/svg.html"
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
        }

        static UserFlow SetupClickElementsInInvisibleParentFlow()
        {
            return new UserFlow()
            {
                Title = "Click Element in Invisible Parent",
                Steps = new Step[]
                {
                    new Step()
                    {
                        Type = StepType.Navigate,
                        Url = $"{PuppeteerFixture.BaseUrl}/invisible-parent.html"
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
        }

        static UserFlow SetupClickCheckboxFlow()
        {
            return new UserFlow()
            {
                Title = "Click Checkbox",
                Steps = new Step[]
                {
                    new Step()
                    {
                        Type = StepType.Navigate,
                        Url = $"{PuppeteerFixture.BaseUrl}/checkbox.html"
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
        }

        static UserFlow SetupReplayKeyboardEventsFlow()
        {
            return new UserFlow()
            {
                Title = "Replay Keyboard Events",
                Steps = new Step[]
                {
                    new Step()
                    {
                        Type = StepType.Navigate,
                        Url = $"{PuppeteerFixture.BaseUrl}/input.html"
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
        }

        static UserFlow SetupReplaySelectEventsFlow()
        {
            return new UserFlow()
            {
                Title = "Change Select Value",
                Steps = new Step[]
                {
                    new Step()
                    {
                        Type = StepType.Navigate,
                        Url = $"{PuppeteerFixture.BaseUrl}/select.html"
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
        }

        static UserFlow SetupCloseDropdownFlow()
        {
            return new UserFlow()
            {
                Title = "Close Dropdown After Click and Change",
                Steps = new Step[]
                {
                    new Step()
                    {
                        Type = StepType.Navigate,
                        Url = $"{PuppeteerFixture.BaseUrl}/select.html"
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
        }

        static UserFlow SetupReplayChangesOnNonTextInputsFlow()
        {
            return new UserFlow()
            {
                Title = "Change Non-Text Inputs",
                Steps = new Step[]
                {
                    new Step()
                    {
                        Type = StepType.Navigate,
                        Url = $"{PuppeteerFixture.BaseUrl}/input.html"
                    },
                    new Step()
                    {
                        Type = StepType.Change,
                        Target = "main",
                        Selectors = new string[][] { new string[] { "#color" } },
                        Value = "#333333"
                    }
                }
            };
        }

        static UserFlow SetupChangeExistingValueFlow()
        {
            return new UserFlow()
            {
                Title = "Change Existing Input Value",
                Steps = new Step[]
                {
                    new Step()
                    {
                        Type = StepType.Navigate,
                        Url = $"{PuppeteerFixture.BaseUrl}/input.html"
                    },
                    new Step()
                    {
                        Type = StepType.Change,
                        Target = "main",
                        Selectors = new string[][] { new string[] { "#prefilled" } },
                        Value = "cba"
                    }
                }
            };
        }

        static UserFlow SetupChangePartialExistingValueFlow()
        {
            return new UserFlow()
            {
                Title = "Change Existing Pre-Filled Input",
                Steps = new Step[]
                {
                    new Step()
                    {
                        Type = StepType.Navigate,
                        Url = $"{PuppeteerFixture.BaseUrl}/input.html"
                    },
                    new Step()
                    {
                        Type = StepType.Change,
                        Target = "main",
                        Selectors = new string[][] { new string[] { "#partially-prefilled" } },
                        Value = "abcdef"
                    }
                }
            };
        }

        static UserFlow SetupChangeViewportFlow()
        {
            return new UserFlow()
            {
                Title = "Change Viewport Size",
                Steps = new Step[]
                {
                    new Step()
                    {
                        Type = StepType.Navigate,
                        Url = $"{PuppeteerFixture.BaseUrl}/select.html"
                    },
                    new Step()
                    {
                        Type = StepType.SetViewport,
                        Width = 800,
                        Height = 600,
                        IsLandscape = false,
                        IsMobile = false,
                        DeviceScaleFactor = 1,
                        HasTouch = false,
                    }
                }
            };
        }

        static UserFlow SetupScrollIntoViewFlow()
        {
            return new UserFlow()
            {
                Title = "Scroll Into View When Needed",
                Steps = new Step[]
                {
                    new Step()
                    {
                        Type = StepType.SetViewport,
                        Width = 800,
                        Height = 600,
                        IsLandscape = false,
                        IsMobile = false,
                        DeviceScaleFactor = 1,
                        HasTouch = false,
                    },
                    new Step()
                    {
                        Type = StepType.Navigate,
                        Url = $"{PuppeteerFixture.BaseUrl}/scroll-into-view.html"
                    },
                    new Step()
                    {
                        Type = StepType.Click,
                        Selectors = new string[][] { new string[] { "button" } },
                        OffsetX = 1,
                        OffsetY = 1
                    }
                }
            };
        }

        static UserFlow SetupAriaSelectorsOnInputsFlow()
        {
            return new UserFlow()
            {
                Title = "Aria Selectors on Inputs",
                Steps = new Step[]
                {
                    new Step()
                    {
                        Type = StepType.Navigate,
                        Url = $"{PuppeteerFixture.BaseUrl}/form.html"
                    },
                    new Step()
                    {
                        Type = StepType.SetViewport,
                        Width = 800,
                        Height = 600,
                        IsLandscape = false,
                        IsMobile = false,
                        DeviceScaleFactor = 1,
                        HasTouch = false,
                    },
                    new Step()
                    {
                        Type = StepType.Click,
                        Target = "main",
                        Selectors = new string[][] { new string[] { "aria/Name:" } },
                        OffsetX = 1,
                        OffsetY = 1
                    }
                }
            };
        }

        static UserFlow SetupTextSelectorsFlow()
        {
            return new UserFlow()
            {
                Title = "Text Selectors",
                Steps = new Step[]
                {
                    new Step()
                    {
                        Type = StepType.Navigate,
                        Url = $"{PuppeteerFixture.BaseUrl}/main.html"
                    },
                    new Step()
                    {
                        Type = StepType.SetViewport,
                        Width = 800,
                        Height = 600,
                        IsLandscape = false,
                        IsMobile = false,
                        DeviceScaleFactor = 1,
                        HasTouch = false,
                    },
                    new Step()
                    {
                        Type = StepType.Click,
                        Target = "main",
                        Selectors = new string[][] { new string[] { "text/Inp" } },
                        OffsetX = 1,
                        OffsetY = 1
                    }
                }
            };
        }

        static UserFlow SetupWaitForElementFlow()
        {
            return new UserFlow()
            {
                Title = "Wait For Element",
                Steps = new Step[]
                {
                    new Step()
                    {
                        Type = StepType.Navigate,
                        Url = $"{PuppeteerFixture.BaseUrl}/shadow-dynamic.html"
                    },
                    new Step()
                    {
                        Type = StepType.WaitForElement,
                        Selectors = new string[][] { new string[] { "custom-element", "button" } }
                    },
                    new Step()
                    {
                        Type = StepType.Click,
                        Target = "main",
                        Selectors = new string[][] { new string[] { "custom-element", "button" } },
                        OffsetX = 1,
                        OffsetY = 1
                    },
                    new Step()
                    {
                        Type = StepType.WaitForElement,
                        Selectors = new string[][] { new string[] { "custom-element", "button" } },
                        Operator = ">=",
                        Count = 2
                    }
                }
            };
        }

        static UserFlow SetupWaitForExpressionFlow()
        {
            return new UserFlow()
            {
                Title = "Wait For Expression",
                Steps = new Step[]
                {
                    new Step()
                    {
                        Type = StepType.Navigate,
                        Url = $"{PuppeteerFixture.BaseUrl}/shadow-dynamic.html"
                    },
                    new Step()
                    {
                        Type = StepType.Click,
                        Target = "main",
                        Selectors = new string[][] { new string[] { "custom-element", "button" } },
                        OffsetX = 1,
                        OffsetY = 1
                    },
                    new Step()
                    {
                        Type = StepType.WaitForExpression,
                        Target = "main",
                        Expression = "document.querySelectorAll(\"custom-element\").length === 2"
                    }
                }
            };
        }

        static UserFlow SetupReplayWithPopupsFlow()
        {
            return new UserFlow()
            {
                Title = "Replay With Popups",
                Steps = new Step[]
                {
                    new Step()
                    {
                        Type = StepType.Navigate,
                        Url = $"{PuppeteerFixture.BaseUrl}/main.html",
                        AssertedEvents = new AssertedEvent[]
                        {
                            new AssertedEvent()
                            {
                                Title = String.Empty,
                                Type = AssertedEventType.Navigation,
                                Url = $"{PuppeteerFixture.BaseUrl}/main.html"
                            }
                        }
                    },
                    new Step()
                    {
                        Type = StepType.Click,
                        Selectors = new string[][] { new string[] { "aria/Open Popup" }, new string[] { "#popup" } },
                        Target = "main",
                        OffsetX = 1,
                        OffsetY = 1
                    },
                    new Step()
                    {
                        Type = StepType.Click,
                        Selectors = new string[][] { new string[] { "aria/Button in Popup" }, new string[] { "body > button" } },
                        Target = $"{PuppeteerFixture.BaseUrl}/popup.html",
                        OffsetX = 1,
                        OffsetY = 1
                    },
                    new Step()
                    {
                        Type = StepType.Close,
                        Target = $"{PuppeteerFixture.BaseUrl}/popup.html",
                    }
                }
            };
        }
    }
}
