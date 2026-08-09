using System;
using System.Collections.Generic;
using System.Text;

namespace CodeActivityTracker.Model
    {
    public class ActivityUpdate
        {
        public string TypingFormatted { get; set; } = "";
        public string IDEFormatted { get; set; } = "";
        public string DebugFormatted { get; set; } = "";
        public string IdleFormatted { get; set; } = "";

        public double TypingWidth { get; set; }
        public double IDEWidth { get; set; }
        public double DebugWidth { get; set; }
        public double IdleWidth { get; set; }
        }
    }
