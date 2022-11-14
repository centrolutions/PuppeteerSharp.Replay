using PuppeteerSharp.Input;
using PuppeteerSharp.Replay.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PuppeteerSharp.Replay
{
    public class RunnerExtension : IRunnerExtension
    {
        readonly IBrowser _Browser;
        readonly IPage _Page;
        readonly int? _Timeout;
        private readonly bool _ScreenshotAfterEachStep;
        readonly string[] _TypeableInputTypes = new string[]
        {
            "textarea",
            "text",
            "url",
            "tel",
            "search",
            "password",
            "number",
            "email",
        };
        readonly Dictionary<string, MouseButton> _MouseButtonMap = new Dictionary<string, MouseButton>()
        {
            { string.Empty, MouseButton.Left },
            { "primary", MouseButton.Left },
            { "secondary", MouseButton.Right },
            { "auxiliary", MouseButton.Middle },
            //{ "back",  }
            //{ "forward",  }
        };
        readonly Dictionary<string, Func<int, int, bool>> _ComparisonFunctions = new Dictionary<string, Func<int, int, bool>>()
        {
            { ">=", (a, b) => { return a >= b; } },
            { "==", (a, b) => { return a == b; } },
            { "<=", (a, b) => { return a <= b; } }
        };
        const string ScrollIntoViewJSFunction = @"async (e) => {
            e.scrollIntoView({behavior:'smooth', block:'nearest', inline:'nearest'});
            currPageXOffset = window.pageXOffset;
            currPageYOffset = window.pageYOffset;
            var isDoneScrolling = false;
            var scrollDone = setInterval(function () {
                if ( currPageXOffset == window.pageXOffset && currPageYOffset == window.pageYOffset ) {
                    clearInterval(scrollDone);
                    console.log('I have finished scrolling');
                    isDoneScrolling = true;
                }
                currPageXOffset = window.pageXOffset;
                currPageYOffset = window.pageYOffset;
            },50);

            while (!isDoneScrolling) {
                await new Promise(resolve => setTimeout(resolve, 50));
            }
            return true;
         }";

        public RunnerExtension(IBrowser browser, IPage page, int? timeout, bool screenshotAfterEachStep = true)
        {
            _Browser = browser;
            _Page = page;
            _Timeout = timeout;
            _ScreenshotAfterEachStep = screenshotAfterEachStep;
        }

        public Task AfterAllSteps(UserFlow flow)
        {
            return Task.CompletedTask;
        }

        public async Task AfterEachStep(Step step, UserFlow flow)
        {
            if (!_ScreenshotAfterEachStep)
                return;

            string filePath = CreateScreenshotFilePath(step, flow);
            var page = await GetTargetPageForStep(step, 0);
            if (page != null)
                await page.ScreenshotAsync(filePath);
        }

        public Task BeforeAllSteps(UserFlow flow)
        {
            return Task.CompletedTask;
        }

        public Task BeforeEachStep(Step step, UserFlow flow)
        {
            return Task.CompletedTask;
        }

        public async Task RunStep(Step step, UserFlow flow)
        {
            int timeout = GetTimeoutForStep(step, flow);
            var eventTasks = await WaitForEvents(step, timeout);
            switch (step.Type)
            {
                case StepType.SetViewport:
                    await Task.WhenAll(eventTasks.Append(SetViewport(step)));
                    break;
                case StepType.Navigate:
                    await Task.WhenAll(eventTasks.Append(Navigate(step, timeout)));
                    break;
                case StepType.Click:
                    await Task.WhenAll(eventTasks.Append(Click(step, timeout, 1)));
                    break;
                case StepType.DoubleClick:
                    await Task.WhenAll(eventTasks.Append(Click(step, timeout, 2)));
                    break;
                case StepType.Change:
                    await Task.WhenAll(eventTasks.Append(Change(step, timeout)));
                    break;
                case StepType.KeyDown:
                    await Task.WhenAll(eventTasks.Append(KeyDown(step)));
                    break;
                case StepType.KeyUp:
                    await Task.WhenAll(eventTasks.Append(KeyUp(step)));
                    break;
                case StepType.Hover:
                    await Task.WhenAll(eventTasks.Append(Hover(step, timeout)));
                    break;
                case StepType.WaitForExpression:
                    await Task.WhenAll(eventTasks.Append(WaitForExpression(step, timeout)));
                    break;
                case StepType.WaitForElement:
                    await Task.WhenAll(eventTasks.Append(WaitForElement(step, timeout)));
                    break;
                case StepType.Scroll:
                    await Task.WhenAll(eventTasks.Append(Scroll(step, timeout)));
                    break;
                case StepType.Close:
                    await Task.WhenAll(eventTasks.Append(Close(step, timeout)));
                    break;
            }
        }

        async Task Close(Step step, int timeout)
        {
            var page = await GetTargetPageForStep(step, timeout);
            await page?.CloseAsync();
        }

        async Task<IPage> GetTargetPageForStep(Step step, int timeout)
        {
            if (string.IsNullOrWhiteSpace(step.Target) || step.Target == "main")
                return _Page;

            var waitOptions = new WaitForOptions() { Timeout = 5 * 1000 };
            ITarget target = null;
            try
            {
                target = await _Browser.WaitForTargetAsync(t => t.Url == step.Target, waitOptions);
            }
            catch (TimeoutException)
            {
                return null;
            }
            var targetPage = await target.PageAsync();

            if (targetPage == null)
                return null;

            targetPage.DefaultTimeout = timeout;
            return targetPage;
        }

        async Task Scroll(Step step, int timeout)
        {
            var page = await GetTargetPageForStep(step, timeout);
            if (step.Selectors != null && step.Selectors.Length > 0)
            {
                await ScrollIntoViewIfNeeded(page, step.Selectors, timeout);
                var element = await WaitForSelectors(page, step.Selectors, timeout, true);
                await page.WaitForFunctionAsync("(e, x, y) => { e.scrollTop = y; e.scrollLeft = x; return true; }", element, step.OffsetX, step.OffsetY);
            }
            else
            {
                await page.WaitForFunctionAsync("(x, y) => { window.scroll(x, y); return true; }", step.OffsetX, step.OffsetY);
            }
        }

        async Task ScrollIntoViewIfNeeded(IPage page, string[][] selectors, int timeout)
        {
            var element = await WaitForSelectors(page, selectors, timeout, false);
            if (element == null)
                throw new Exception("The element could not be found.");

            await ScrollIntoViewIfNeeded(page, element, timeout);
        }

        async Task ScrollIntoViewIfNeeded(IPage page, IElementHandle element, int timeout)
        {
            var isInViewport = await element.IsIntersectingViewportAsync();
            if (isInViewport)
                return;

            await ScrollIntoView(page, element);
            await element.IsIntersectingViewportAsync();
        }

        async Task ScrollIntoView(IPage page, IElementHandle element)
        {
            await page.WaitForFunctionAsync(ScrollIntoViewJSFunction, element);
            Debug.WriteLine($"ScrollIntoView: EvaluateFunction done.");
        }

        async Task WaitForElement(Step step, int timeout)
        {
            var page = await GetTargetPageForStep(step, timeout);
            var timeoutTask = Task.Delay(timeout);
            var result = false;
            var op = step.Operator ?? ">=";
            var searchTask = Task.Run(async () =>
            {
                while (!result)
                {
                    var elements = await WaitForAllSelectors(page, step.Selectors, timeout, true);
                    var comparison = _ComparisonFunctions[op];
                    result = comparison(elements.Count(), step.Count);
                }
            });

            if (timeout > 0)
            {
                var completedTaskIndex = await Task.WhenAny(timeoutTask, searchTask);
            }
            else
            {
                await searchTask;
            }

            if (!result)
                throw new Exception($"Could not find {op}{step.Count} elements for selectors: " + string.Join(";", step.Selectors.Select(x => string.Join(">>", x))));
        }

        async Task WaitForExpression(Step step, int timeout)
        {
            var page = await GetTargetPageForStep(step, timeout);
            await page.WaitForExpressionAsync(step.Expression, new WaitForFunctionOptions() { Timeout = timeout });
        }

        async Task Hover(Step step, int timeout)
        {
            var page = await GetTargetPageForStep(step, timeout);
            await ScrollIntoViewIfNeeded(page, step.Selectors, timeout);
            var element = await WaitForSelectors(page, step.Selectors, timeout, true);
            await element.HoverAsync();
        }

        async Task KeyDown(Step step)
        {
            var page = await GetTargetPageForStep(step, 0);
            await page.Keyboard.DownAsync(step.Key);
            await page.WaitForTimeoutAsync(100);
        }

        async Task KeyUp(Step step)
        {
            var page = await GetTargetPageForStep(step, 0);
            await page.Keyboard.UpAsync(step.Key);
            await page.WaitForTimeoutAsync(100);
        }

        async Task Change(Step step, int timeout)
        {
            var page = await GetTargetPageForStep(step, timeout);
            await ScrollIntoViewIfNeeded(page, step.Selectors, timeout);
            var element = await WaitForSelectors(page, step.Selectors, timeout, true);
            var inputType = await element.EvaluateFunctionAsync<string>("(el) => el.type");
            if (inputType == "select-one")
            {
                await ChangeSelectElement(step, element);
            }
            else if (_TypeableInputTypes.Contains(inputType))
            {
                await TypeIntoElement(step, element);
            }
            else
            {
                await ChangeElementValue(step, element);
            }
        }

        static string CreateScreenshotFilePath(Step step, UserFlow flow)
        {
            string basePath = "screenshots";
            var folder = $"{basePath}{Path.DirectorySeparatorChar}{flow.Title.Replace(" ", "_")}";
            var filename = $"{Array.IndexOf(flow.Steps, step)}.jpg";
            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var filePath = Path.Combine(folder, filename);
            return filePath;
        }

        async Task ChangeElementValue(Step step, IElementHandle element)
        {
            await element.FocusAsync();
            await element.EvaluateFunctionAsync("(input, value) => {" +
                "input.value = value;" +
                "input.dispatchEvent(new Event('input', { bubbles: true }));" +
                "input.dispatchEvent(new Event('change', { bubbles: true }));" +
                "}", step.Value);
        }

        async Task TypeIntoElement(Step step, IElementHandle element)
        {
            var textToType = await element.EvaluateFunctionAsync<string>(@"(input, newValue) => {
              if (
                newValue.length <= input.value.length ||
                !newValue.startsWith(input.value)
              ) {
                input.value = '';
                return newValue;
              }
              const originalValue = input.value;
              // Move cursor to the end of the common prefix.
              input.value = '';
              input.value = originalValue;
              return newValue.substring(originalValue.length);
            }", step.Value);
            await element.TypeAsync(textToType);
        }

        async Task ChangeSelectElement(Step step, IElementHandle element)
        {
            await element.SelectAsync(step.Value);
            await element.EvaluateFunctionAsync("(e) => { e.blur(); e.focus(); }");
        }

        async Task Click(Step step, int timeout, int clickCount)
        {
            var page = await GetTargetPageForStep(step, timeout);
            await ScrollIntoViewIfNeeded(page, step.Selectors, timeout);
            Debug.WriteLine("Click: ScrollIntoViewIfNeeded done.");
            var element = await WaitForSelectors(page, step.Selectors, timeout, false);
            Debug.WriteLine($"Click: WaitForSelectors done. element == null is {element == null}");
            var options = new ClickOptions()
            {
                OffSet = new Offset(step.OffsetX, step.OffsetY),
                ClickCount = clickCount,
                Button = _MouseButtonMap[step.Button ?? string.Empty]
            };
            if (clickCount == 1)
                options.Delay = step.Duration ?? 0;

            try
            {
                await element.ClickAsync(options);
            }
            catch
            {
                await element.EvaluateFunctionAsync("(e) => e.click()");
            }
            Debug.WriteLine("Click: Click done.");
        }

        async Task Navigate(Step step, int timeout)
        {
            var page = await GetTargetPageForStep(step, timeout);
            var options = new NavigationOptions()
            {
                Timeout = timeout,
            };
            await page.GoToAsync(step.Url, options);
            Debug.WriteLine("Navigate done.");
        }

        async Task SetViewport(Step step)
        {
            var page = await GetTargetPageForStep(step, 0);
            var options = new ViewPortOptions()
            {
                DeviceScaleFactor = step.DeviceScaleFactor,
                HasTouch = step.HasTouch,
                Height = step.Height,
                IsLandscape = step.IsLandscape,
                IsMobile = step.IsMobile,
                Width = step.Width,
            };
            await page.SetViewportAsync(options);
        }

        int GetTimeoutForStep(Step step, UserFlow flow)
        {
            return step.Timeout ?? flow.Timeout ?? _Timeout ?? 5000;
        }

        async Task<IEnumerable<Task>> WaitForEvents(Step step, int timeout)
        {
            if (step.AssertedEvents == null || step.AssertedEvents.Length <= 0) return new Task[] { };

            var page = await GetTargetPageForStep(step, timeout);
            var events = new List<Task>();
            foreach (var ev in step.AssertedEvents)
            {
                switch (ev.Type)
                {
                    case AssertedEventType.Navigation:
                        events.Add(WaitForNavigation(page, timeout));
                        break;
                    default:
                        throw new Exception($"Event type {ev.Type} is not supported.");
                }
            }

            return events;
        }

        async Task WaitForNavigation(IPage page, int timeout)
        {
            var waitNavOptions = new NavigationOptions()
            {
                Timeout = timeout,
                WaitUntil = new WaitUntilNavigation[] { WaitUntilNavigation.Load },
            };
            await page.WaitForNavigationAsync(waitNavOptions);
            Debug.WriteLine("Wait for navigation done.");
        }

        async Task<IEnumerable<IElementHandle>> WaitForAllSelectors(IPage page, string[][] selectors, int timeout, bool visible)
        {
            var elements = new List<IElementHandle>();
            foreach (var selector in selectors)
            {
                var result = await WaitForAllSelector(page, selector, timeout, visible);
                if (result != null && result.Any())
                    elements.AddRange(result);
            }

            return elements;
        }

        async Task<IElementHandle> WaitForSelectors(IPage page, string[][] selectors, int timeout, bool visible)
        {
            foreach (var selector in selectors)
            {
                var result = await WaitForSelector(page, selector, timeout, visible);
                if (result != null)
                    return result;
            }
            throw new Exception("Could not find element for selectors: " + string.Join(";", selectors.Select(x => string.Join(">>", x))));
        }

        async Task<IEnumerable<IElementHandle>> WaitForAllSelector(IPage page, string[] selectors, int timeout, bool visible)
        {
            var results = new List<IElementHandle>();
            foreach (var selector in selectors)
            {
                var elements = await page.QuerySelectorAllAsync(selector);
                if (elements != null && elements.Any())
                    results.AddRange(elements);
            }
            return results;
        }

        async Task<IElementHandle> WaitForSelector(IPage page, string[] selectors, int timeout, bool visible)
        {
            var selectorTasks = new List<Task<IElementHandle>>();
            var selectorOptions = new WaitForSelectorOptions()
            {
                Timeout = timeout,
                Visible = visible,
            };
            foreach (var selector in selectors)
            {
                selectorTasks.Add(page.WaitForSelectorAsync(selector, selectorOptions));
            }

            var finishedTask = await Task.WhenAny(selectorTasks);
            return finishedTask.Result;
        }
    }
}
