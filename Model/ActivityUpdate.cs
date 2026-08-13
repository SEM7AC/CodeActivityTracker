namespace CodeActivityTracker.Model
    {
    public class ActivityUpdate
        {

        public int TypingSeconds { get; set; }
        public int IDESeconds { get; set; }
        public int DebugSeconds { get; set; }
        public int IdleSeconds { get; set; }
        public int TotalSeconds => TypingSeconds + IDESeconds + DebugSeconds + IdleSeconds;
        public string TypingFormatted { get; set; } = "";
        public string IDEFormatted { get; set; } = "";
        public string DebugFormatted { get; set; } = "";
        public string IdleFormatted { get; set; } = "";

        }
    }
