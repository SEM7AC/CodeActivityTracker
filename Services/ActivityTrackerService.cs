using CodeActivityTracker.Model;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CodeActivityTracker.Services;

public class ActivityTrackerService
    {

    private readonly TimerService _timer;

    private int idleCooldown = 0;
    private int typingCooldown = 0;
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

    //DLL IMPORTS ---------------------------------------------

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
        {
        public uint cbSize;
        public uint dwTime;
        }


    //-----------------------------------------------------------


    // METHODS 
    private void OnTick()
        {
        var signals = CollectSignals();
        IncrementCounters(signals);
        ActivityUpdated?.Invoke(BuildUpdate());
        }


    private bool DetectKeyboard()
        {
        for (int key = 0x08; key <= 0xFE; key++)
            {
            if ((GetAsyncKeyState(key) & 0x8000) != 0)
                return true;
            }
        return false;
        }
    private bool DetectMouse()
        {
        return GetIdleTimeSeconds() == 0 && !DetectKeyboard();
        }
    private bool DetectIdle()
        {
        return GetIdleTimeSeconds() > 0;
        }
    private bool DetectIDE()
        {
        IntPtr hwnd = GetForegroundWindow();
        uint pid;
        GetWindowThreadProcessId(hwnd, out pid);

        var process = Process.GetProcessById((int)pid);

        return process.ProcessName.Equals("devenv", StringComparison.OrdinalIgnoreCase);
        }
    private bool DetectDebugger()
        {
        return Debugger.IsAttached;
        }
    private ActivitySignals CollectSignals()
        {
        return new ActivitySignals
            {
            IsKeyboardActive = DetectKeyboard(),
            IsMouseActive = DetectMouse(),
            IsIdle = DetectIdle(),
            IsIDEActive = DetectIDE(),
            IsDebuggerRunning = DetectDebugger(),
            ForegroundProcess = GetForegroundProcessName()
            };
        }
    private string GetForegroundProcessName()
        {
        IntPtr hwnd = GetForegroundWindow();
        uint pid;
        GetWindowThreadProcessId(hwnd, out pid);

        return Process.GetProcessById((int)pid).ProcessName;
        }
    private void IncrementCounters(ActivitySignals s)
        {
        
        bool isTyping = s.IsKeyboardActive || typingCooldown > 0;

        // KEYBOARD ACTIVE → typing
        if (s.IsKeyboardActive)
            {
            typingCooldown = 5;   // typing buffer
            idleCooldown = 5;     // idle buffer
            typingSeconds++;
            }
        else if (typingCooldown > 0)
            {
            typingCooldown--;
            idleCooldown = 5;     // still not idle yet
            typingSeconds++;
            }
        else
            {
            // typing buffer expired → now count down idle buffer
            if (idleCooldown > 0)
                {
                idleCooldown--;
                }
            else
                {
                idleSeconds++;
                }
            }

        // IDE
        if (s.IsIDEActive)
            ideSeconds++;

        // DEBUG
        if (s.IsDebuggerRunning)
            debugSeconds++;
        }
    private ActivityUpdate BuildUpdate()
        {
        return new ActivityUpdate
            {
            
            TypingSeconds = typingSeconds,
            IDESeconds = ideSeconds,
            DebugSeconds = debugSeconds,
            IdleSeconds = idleSeconds,


            TypingFormatted = FormatTime(typingSeconds),
            IDEFormatted = FormatTime(ideSeconds),
            DebugFormatted = FormatTime(debugSeconds),
            IdleFormatted = FormatTime(idleSeconds),

            TypingWidth = typingSeconds * 0.5,
            IDEWidth = ideSeconds * 0.5,
            DebugWidth = debugSeconds * 0.5,
            IdleWidth = idleSeconds * 0.1
            };
        }
    public string FormatTime(int seconds)
        {
        return TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");
        }
    private int GetIdleTimeSeconds()
        {
        LASTINPUTINFO info = new LASTINPUTINFO();
        info.cbSize = (uint)Marshal.SizeOf(info);

        if (!GetLastInputInfo(ref info))
            return 0;

        uint idleTicks = ((uint)Environment.TickCount - info.dwTime);
        return (int)(idleTicks / 1000);
        }




    }

