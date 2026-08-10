namespace CodeActivityTracker.Model;

public class ActivitySignals
    {
    public bool IsKeyboardActive { get; set; }
    public bool IsMouseActive { get; set; }
    public bool IsIdle { get; set; }
    public bool IsIDEActive { get; set; }
    public bool IsDebuggerRunning { get; set; }
    public string ForegroundProcess { get; set; } = string.Empty;
    }
