# PuppeteerSharp.Replay
![Build and Publish](https://github.com/centrolutions/PuppeteerSharp.Replay/actions/workflows/build-and-publish.yml/badge.svg)

PuppeteerSharp.Replay is a .NET port of [https://github.com/puppeteer/replay]

# Installation
```
dotnet add package PuppeteerSharp.Replay
```

# Getting Started
You can use PuppeteerSharp.Replay to replay user flows recorded by [Chrome's developer tools](https://developer.chrome.com/docs/devtools/recorder/). The Recorder allows you to export your recordings as JSON files. You can easily play back those files like:
```
var flow = UserFlow.Parse(File.ReadAllText("everything.json"));
var runnerExt = new RunnerExtension(puppeteerBrowser, puppeteerPage);
var runner = await Runner.CreateRunner(flow, runnerExt);
await runner.Run();
```

Optionally, you can create user flows in code like:
```
var flow = new UserFlow()
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
```

# Using PuppeteerSharp.Replay in Automated Tests
The current version of the user flow document spec does not have an "assert" action. It is recommended you create your Puppeteer browser and page objects in a way that allows you to re-use them to assert in your test. An example of using PuppeteerSharp.Replay with NUnit can be found in [this repo's codebase](https://github.com/centrolutions/PuppeteerSharp.Replay/tree/main/PuppeteerSharp.Replay.Tests).
 