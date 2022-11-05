using PuppeteerSharp.Input;
using PuppeteerSharp.Replay.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuppeteerSharp.Replay
{
    public class RunnerExtension : IRunnerExtension
    {
        readonly IBrowser _Browser;
        readonly IPage _Page;
        readonly int? _Timeout;
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

        public RunnerExtension(IBrowser browser, IPage page, int? timeout)
        {
            _Browser = browser;
            _Page = page;
            _Timeout = timeout;
        }

        public Task AfterAllSteps(UserFlow flow)
        {
            return Task.CompletedTask;
        }

        public Task AfterEachStep(Step step, UserFlow flow)
        {
            return Task.CompletedTask;
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
            var eventTasks = WaitForEvents(step, timeout);
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
            }
        }

        async Task KeyDown(Step step)
        {
            await _Page.Keyboard.DownAsync(step.Key);
            await _Page.WaitForTimeoutAsync(100);
        }

        async Task KeyUp(Step step)
        {
            await _Page.Keyboard.UpAsync(step.Key);
            await _Page.WaitForTimeoutAsync(100);
        }

        async Task Change(Step step, int timeout)
        {
            var element = await WaitForSelectors(step.Selectors, timeout, true);
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
            await element.TypeAsync(step.Value);
        }

        async Task ChangeSelectElement(Step step, IElementHandle element)
        {
            await element.SelectAsync(step.Value);
            await element.EvaluateFunctionAsync("(e) => { e.blur(); e.focus(); }");
        }

        async Task Click(Step step, int timeout, int clickCount)
        {
            var element = await WaitForSelectors(step.Selectors, timeout, false);
            var options = new ClickOptions()
            {
                OffSet = new Offset(step.OffsetX, step.OffsetY),
                ClickCount = clickCount,
                Button = _MouseButtonMap[step.Button ?? string.Empty]
            };
            if (clickCount == 1)
                options.Delay = step.Duration ?? 0;

            await element.ClickAsync(options);
        }

        async Task Navigate(Step step, int timeout)
        {
            var options = new NavigationOptions()
            {
                Timeout = timeout,
            };
            await _Page.GoToAsync(step.Url, options);
        }

        async Task SetViewport(Step step)
        {
            var options = new ViewPortOptions()
            {
                DeviceScaleFactor = step.DeviceScaleFactor,
                HasTouch = step.HasTouch,
                Height = step.Height,
                IsLandscape = step.IsLandscape,
                IsMobile = step.IsMobile,
                Width = step.Width,
            };
            await _Page.SetViewportAsync(options);
        }

        int GetTimeoutForStep(Step step, UserFlow flow)
        {
            return step.Timeout ?? flow.Timeout ?? _Timeout ?? 0;
        }

        IEnumerable<Task> WaitForEvents(Step step, int timeout)
        {
            if (step.AssertedEvents == null || step.AssertedEvents.Length <= 0) return new Task[] { };

            var events = new List<Task>();
            foreach (var ev in step.AssertedEvents)
            {
                switch (ev.Type)
                {
                    case AssertedEventType.Navigation:
                        events.Add(_Page.WaitForNavigationAsync(new NavigationOptions() { Timeout = timeout }));
                        break;
                    default:
                        throw new Exception($"Event type {ev.Type} is not supported.");
                }
            }

            return events;
        }

        async Task<IElementHandle> WaitForSelectors(string[][] selectors, int timeout, bool visible)
        {
            foreach (var selector in selectors)
            {
                var result = await WaitForSelector(selector, timeout, visible);
                if (result != null)
                    return result;
            }
            throw new Exception("Could not find element for selectors: " + string.Join(";", selectors.Select(x => string.Join(">>", x))));
        }

        async Task<IElementHandle> WaitForSelector(string[] selectors, int timeout, bool visible)
        {
            foreach (var selector in selectors)
            {
                return await _Page.WaitForSelectorAsync(selector, new WaitForSelectorOptions() { Timeout = timeout, Visible = visible });
            }

            return null;
        }
    }
}
