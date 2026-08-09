using CodeActivityTracker.Model;

namespace CodeActivityTracker.Services;

public class ActivityTrackerService
    {

    private readonly TimerService _timer;

    private int typingSeconds = 0;
    private int idleSeconds = 0;
    private int debugSeconds = 0;
    private int ideSeconds = 0;

    public event Action<ActivityUpdate>? ActivityUpdated;

    public ActivityTrackerService(TimerService timer)
        {
        _timer = timer;
        _timer.Tick += OnTick;
        }

    private void OnTick()
        {
        typingSeconds++;

        var update = new ActivityUpdate
            {
            TypingFormatted = FormatTime(typingSeconds),
            IDEFormatted = FormatTime(ideSeconds),
            DebugFormatted = FormatTime(debugSeconds),
            IdleFormatted = FormatTime(idleSeconds),

            TypingWidth = typingSeconds * 0.5,
            IDEWidth = ideSeconds * 0.5,
            DebugWidth = debugSeconds * 0.5,
            IdleWidth = idleSeconds * 0.1
            };

        ActivityUpdated?.Invoke(update);
        }

    private string FormatTime(int seconds)
        {
        return TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");
        }

    }

