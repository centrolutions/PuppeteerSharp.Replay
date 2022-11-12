using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuppeteerSharp.Replay.Tests
{
    public class PuppeteerFixture : IDisposable
    {
        public IBrowser Browser { get; set; }
        public const string BaseUrl = "http://localhost:5000";

        public PuppeteerFixture()
        {
            var fetcher = new BrowserFetcher();
            try
            {
                fetcher.DownloadAsync().Wait();
            }
            finally { }

            var options = new LaunchOptions()
            {
                Headless = true,
                DefaultViewport = new ViewPortOptions() { Width = 1280, Height = 810 }
            };
            Browser = Puppeteer.LaunchAsync(options).Result;
        }

        public void Dispose()
        {
            //no-op
        }
    }

    [CollectionDefinition("Puppeteer")]
    public class PuppeteerCollection : ICollectionFixture<PuppeteerFixture>
    {
        //only purpose of this class is to apply the fixture to the collection
    }
}
