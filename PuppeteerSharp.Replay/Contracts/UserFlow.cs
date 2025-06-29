using System.Text.Json;

namespace PuppeteerSharp.Replay.Contracts
{
    public class UserFlow
    {
        public string Title { get; set; }
        public int? Timeout { get; set; }
        public Step[] Steps { get; set; }

        public UserFlow()
        {
            Steps = new Step[] { };
        }

        public static UserFlow Parse(string json)
        {
            var result = JsonSerializer.Deserialize<UserFlow>(json);
            return result;
        }
    }

    public class Step
    {
        public string Type { get; set; }
        public int? Timeout { get; set; }
        public int[] Frame { get; set; }

        //setViewport
        public int Width { get; set; }
        public int Height { get; set; }
        public int DeviceScaleFactor { get; set; }
        public bool IsMobile { get; set; }
        public bool HasTouch { get; set; }
        public bool IsLandscape { get; set; }

        //navigate
        public string Url { get; set; }
        public AssertedEvent[] AssertedEvents { get; set; }

        //click
        public string Target { get; set; }
        public string[][] Selectors { get; set; }
        public decimal OffsetY { get; set; }
        public decimal OffsetX { get; set; }
        public int? Duration { get; set; }
        public string Button { get; set; }

        //change
        public string Value { get; set; }

        //keyDown
        public string Key { get; set; }

        public string Expression { get; set; }

        //waitForElement
        public int Count { get; set; }
        public string Operator { get; set; }
    }

    public class AssertedEvent
    {
        public string Type { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
    }

    public static class StepType
    {
        public const string Change = "change";
        public const string Click = "click";
        public const string Close = "close";
        public const string CustomStep = "customStep";
        public const string DoubleClick = "doubleClick";
        public const string EmulateNetworkConditions = "emulateNetworkConditions";
        public const string Hover = "hover";
        public const string KeyDown = "keyDown";
        public const string KeyUp = "keyUp";
        public const string Navigate = "navigate";
        public const string Scroll = "scroll";
        public const string SetViewport = "setViewport";
        public const string WaitForElement = "waitForElement";
        public const string WaitForExpression = "waitForExpression";
    }

    public static class AssertedEventType
    {
        public const string Navigation = "navigation";
    }
}
