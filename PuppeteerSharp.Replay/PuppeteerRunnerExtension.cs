using Newtonsoft.Json;
using PuppeteerSharp.Replay.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PuppeteerSharp.Replay
{
    public class PuppeteerRunnerExtension : IRunnerExtension
    {
        private readonly IBrowser _Browser;
        private readonly IPage _Page;
        private readonly int? _TimeoutMilliseconds;

        public PuppeteerRunnerExtension(IBrowser browser, IPage page, int? timeoutMilliseconds = null)
        {
            _Browser = browser;
            _Page = page;
            _TimeoutMilliseconds = timeoutMilliseconds ?? 5000;
        }
        public Task AfterAllSteps(UserFlow flow)
        {
            throw new NotImplementedException();
        }

        public Task AfterEachStep(Step step, UserFlow flow)
        {
            throw new NotImplementedException();
        }

        public Task BeforeAllSteps(UserFlow flow)
        {
            throw new NotImplementedException();
        }

        public Task BeforeEachStep(Step step, UserFlow flow)
        {
            throw new NotImplementedException();
        }

        public async Task RunStep(Step step, UserFlow flow)
        {
            var timeout = GetTimeoutForStep(step, flow);
            var targetPage = await GetTargetPageForStep(_Browser, _Page, step, timeout);
            IFrame targetFrame = null;
            if (targetPage == null && !string.IsNullOrWhiteSpace(step.Target))
            {
                foreach (var frame in _Page.Frames)
                {
                    if (frame.IsOopFrame && frame.Url == step.Url)
                    {
                        targetFrame = frame;
                    }
                }

                if (targetFrame == null)
                {
                    targetFrame = await _Page.WaitForFrameAsync(step.Target, new WaitForOptions() { Timeout = timeout });
                }
            }

            if (targetPage == null && targetFrame == null)
                throw new Exception("Target is not found for step: " + JsonConvert.SerializeObject(step));

            await EnsureAutomationEmulatation(targetPage);
            var localFrame = GetFrame(targetPage, targetFrame, step);
            await RunStepInFrame(step, _Page, targetPage, targetFrame, localFrame, timeout);
        }

        private Task RunStepInFrame(Step step, IPage page, IPage targetPage, IFrame targetFrame, IFrame localFrame, int timeout)
        {
            var waitForVisible = true;
            Task assertedEvent = null;


            throw new NotImplementedException();
        }

        private IFrame GetFrame(IPage targetPage, IFrame targetFrame, Step step)
        {
            var frame = targetPage?.MainFrame ?? targetFrame;
            if (step.Frame != null && step.Frame.Length > 0)
            {
                foreach (var index in step.Frame)
                {
                    frame = targetFrame.ChildFrames[index];
                }
            }
            return frame;
        }

        int GetTimeoutForStep(Step step, UserFlow flow)
        {
            return ((step.Timeout ?? flow.Timeout) ?? _TimeoutMilliseconds) ?? 5000;
        }

        async Task<IPage> GetTargetPageForStep(IBrowser browser, IPage page, Step step, int timeout)
        {
            if (string.IsNullOrWhiteSpace(step.Target) || step.Target == "main")
            {
                return page;
            }

            var target = await browser.WaitForTargetAsync(x => x.Url == step.Target, new WaitForOptions() {  Timeout = timeout });
            var targetPage = await target.PageAsync();

            if (targetPage == null)
                return null;

            targetPage.DefaultTimeout = timeout;
            return targetPage;
        }

        async Task EnsureAutomationEmulatation(IPage page)
        {
            try
            {
                if (page != null)
                    await page.Client.SendAsync("Emulation.setAutomationOverride", new { enabled = true });
            }
            catch
            {
                //ignore errors as not all version support this command
            }
        }
    }
}
