using System;
using System.Collections.Generic;
using System.Text;

namespace PuppeteerSharp.Replay
{
    public class UserFlow
    {
        public string Title { get; set; }
        public Step[] Steps { get; set; }
    }

    public class Step
    {
        public string Type { get; set; }
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
        public float OffsetY { get; set; }
        public int OffsetX { get; set; }

        //change
        public string Value { get; set; }

        //keyDown
        public string Key { get; set; }
    }

    public class AssertedEvent
    {
        public string Type { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
    }

}
