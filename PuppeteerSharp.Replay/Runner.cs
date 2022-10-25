using System;
using System.Threading.Tasks;
using PuppeteerSharp.Replay.Contracts;

namespace PuppeteerSharp.Replay
{
    public class Runner
    {
        public UserFlow Flow { get; }
        public IRunnerExtension Extension { get; }

        private bool _Aborted;

        public Runner(UserFlow flow, IRunnerExtension extension)
        {
            Flow = flow;
            Extension = extension;
        }

        public async Task<bool> Run()
        {
            _Aborted = false;
            await Extension.BeforeAllSteps(Flow);

            foreach (var step in Flow.Steps)
            {
                await Extension.BeforeEachStep(step, Flow);
                await Extension.RunStep(step, Flow);
                await Extension.AfterEachStep(step, Flow);
            }

            await Extension.AfterAllSteps(Flow);
            return false;
        }

        public static async Task<Runner> CreateRunner(UserFlow flow, IRunnerExtension extension = null)
        {
            if (extension == null)
            {
                var fetcher = new BrowserFetcher();
                await fetcher.DownloadAsync();
                var options = new LaunchOptions()
                {
                    Headless = true,
                    DefaultViewport = new ViewPortOptions() {  Width = 1285, Height = 810 }
                };
                IBrowser browser = await Puppeteer.LaunchAsync(options);
                IPage page = await browser.NewPageAsync();
                extension = new RunnerExtension(browser, page, null);
            }
            return new Runner(flow, extension);
        }
    }
}
