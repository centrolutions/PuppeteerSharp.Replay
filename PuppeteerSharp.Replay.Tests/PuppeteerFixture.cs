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
                DefaultViewport = new ViewPortOptions() { Width = 1280, Height = 810 },
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }, //todo: figure out why we need this in GitHub Actions runners
            };
            Browser = Task.Run(() => Puppeteer.LaunchAsync(options)).GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            //no-op
            Task.Run(() => Browser?.CloseAsync()).GetAwaiter().GetResult();
            Browser?.Dispose();
        }
    }

    [CollectionDefinition("Puppeteer")]
    public class PuppeteerCollection : ICollectionFixture<PuppeteerFixture>
    {
        //only purpose of this class is to apply the fixture to the collection
    }
}
