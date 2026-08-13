namespace CodeActivityTracker.Services;

public class TierNameProvider
    {
    // One fixed index for the entire session
    private readonly int _fixedIndex;

    public TierNameProvider()
        {
        // Generate once per session
        _fixedIndex = new Random().Next(0, 10);
        }

    // ============================
    // TYPING TIER NAMES
    // ============================
    private readonly Dictionary<int, string[]> TypingNames = new()
    {
        { 1, new[] { "Stevie Wonder", "Finger Nukes", "Special Forces", "120wpm Demon" } },
        { 2, new[] { "Hard Click", "Cruising", "Uses Pinky" } },
        { 3, new[] { "Mid‑Wpm", "Average", "Nubbs" } },
        { 4, new[] { "Single Finger", "Where is Q", "No Numpad" } },
        { 5, new[] { "Word Per Hour", "zzzZZZZzz", "Unplugged" } }
    };

    // ============================
    // IDE TIER NAMES
    // ============================
    private readonly Dictionary<int, string[]> IdeNames = new()
    {
        { 1, new[] { "Project CDR", "Tab Master", "Clean Code" } },
        { 2, new[] { "ToolBox Open", "Designer" } },
        { 3, new[] { "Life Scroll", "Vacation" } },
        { 4, new[] { "Git Error", "Lost" } },
        { 5, new[] { "New Project", "Wrong Template" } }
    };

    // ============================
    // DEBUG TIER NAMES
    // ============================
    private readonly Dictionary<int, string[]> DebugNames = new()
    {
        { 1, new[] { "Stacktrace Boss", "Full Test" } },
        { 2, new[] { "Bug Hunter", "Breakpoint Here" } },
        { 3, new[] { "Rabbit Hole", "Exception Thrown" } },
        { 4, new[] { "Modem Screech", "Log Diver" } },
        { 5, new[] { "Ship Now", "Debug Never", "Perfect Code" } }
    };

    // ============================
    // IDLE TIER NAMES
    // ============================
    private readonly Dictionary<int, string[]> IdleNames = new()
    {
        { 1, new[] { "Locked In", "Focus Guru" } },
        { 2, new[] { "2nd Place", "Side Quest" } },
        { 3, new[] { "Brain Buffer", "Scrambled Eggs" } },
        { 4, new[] { "BreakTime Max", "Checked Out" } },
        { 5, new[] { "Blackout", "Expired", "Dead", "Boötes Void" } }
    };

    // ============================
    // OVERALL TIER NAMES
    // ============================
    private readonly Dictionary<int, string[]> OverallNames = new()
    {
        { 1, new[] { "Session Overlord", "StackOverflow", "Promotion", "Beast Mode" } },
        { 2, new[] { "Solid Session", "Clean Execution", "Respectable Output" } },
        { 3, new[] { "Mid Mode", "Average Operator", "Meets The Standard" } },
        { 4, new[] { "Productivity Leak", "Drifter", "Defrag Now" } },
        { 5, new[] { "Imposter", "404 Work Not Found", "TikToc Master", "Blackout" } }
    };

    // ============================
    // PUBLIC API
    // ============================
    public string GetTypingName(int tier) => Pick(TypingNames[tier]);
    public string GetIdeName(int tier) => Pick(IdeNames[tier]);
    public string GetDebugName(int tier) => Pick(DebugNames[tier]);
    public string GetIdleName(int tier) => Pick(IdleNames[tier]);
    public string GetOverallName(int tier) => Pick(OverallNames[tier]);

    // ============================
    // INTERNAL FIXED PICKER
    // ============================
    private string Pick(string[] arr)
        {
        return arr[_fixedIndex % arr.Length];
        }
    }
