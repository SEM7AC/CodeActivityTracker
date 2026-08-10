using CodeActivityTracker.Model;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CodeActivityTracker.Services;

public class ActivityTrackerService
    {

    private readonly TimerService _timer;

    private int idleCooldown = 0;
    private int typingCooldown = 0;
    private int ideCooldown = 0;
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
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT Point);

    [DllImport("user32.dll")]
    private static extern bool IsChild(IntPtr hWndParent, IntPtr hWnd);


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

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
        {
        public int X;
        public int Y;
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
    private bool DetectMouseInsideIDE()
        {
        POINT pt;
        GetCursorPos(out pt);

        IntPtr window = WindowFromPoint(pt);

        IntPtr ideWindow = GetForegroundWindow();
        uint pid;
        GetWindowThreadProcessId(ideWindow, out pid);

        var process = Process.GetProcessById((int)pid);

        if (!process.ProcessName.Equals("devenv", StringComparison.OrdinalIgnoreCase))
            return false;

        return window == ideWindow || IsChild(ideWindow, window);
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
            ForegroundProcess = GetForegroundProcessName(),
            IsMouseInsideIDE = DetectMouseInsideIDE()
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
        bool isIDEEngaged =
        (s.IsKeyboardActive && s.IsIDEActive) ||   // typing inside IDE
        (s.IsMouseActive && s.IsMouseInsideIDE);   // mouse movement inside IDE

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
                // idle should NOT tick during IDE engagement
                if (!isIDEEngaged)
                    idleSeconds++;
                
                }
            }


        // IDE BUFFER (5 seconds before IDESeconds starts)
        if (isIDEEngaged)
            {
            if (ideCooldown < 5)
                ideCooldown++;

            if (ideCooldown >= 5)
                ideSeconds++;
            }
        else
            {
            ideCooldown = 0;
            }

        // IDLE only when NOTHING is happening
        if (!isTyping && !s.IsMouseActive)
            {
            if (idleCooldown > 0)
                idleCooldown--;
            else
                idleSeconds++;
            }


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

