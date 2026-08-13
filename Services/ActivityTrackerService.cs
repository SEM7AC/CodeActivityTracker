using CodeActivityTracker.Model;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace CodeActivityTracker.Services;

public class ActivityTrackerService
    {

    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    private readonly TimerService _timer;
    public MainWindow? MainWindowRef { get; set; }


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

    private static readonly HashSet<string> KnownVSChildren = new(StringComparer.OrdinalIgnoreCase)
{
    "servicehub.host",
    "servicehub.datawarehouse",
    "servicehub.indexingservice",
    "perfwatson2",
    "vbcsccompiler",
    "msbuild",
    "xdesproc",
    "dllhost",
    "conhost",
    "vsdebugadapter",
    "vsdebugenghost",
    "vsdiagnostics",
    "vsls-agent",
    "vsls-bootstrapper",
    "vsls-proxy"
};



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

    [DllImport("ntdll.dll")]
    private static extern int NtQueryInformationProcess(
    IntPtr processHandle,
    int processInformationClass,
    ref PROCESS_BASIC_INFORMATION processInformation,
    int processInformationLength,
    out int returnLength);

    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_BASIC_INFORMATION
        {
        public IntPtr Reserved1;
        public IntPtr PebBaseAddress;
        public IntPtr Reserved2_0;
        public IntPtr Reserved2_1;
        public IntPtr UniqueProcessId;
        public IntPtr InheritedFromUniqueProcessId;
        }

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

    // METHODS ------------------------------------------------
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
        // Ignore debugging of CodeActivityTracker itself
        if (Debugger.IsAttached)
            return false;

        var vs = Process.GetProcessesByName("devenv").FirstOrDefault();
        if (vs == null)
            return false;

        // Known non-debug children of VS
        string[] knownChildren =
        {
        "devhub",
        "servicehub.intellicodemodelservice",
        "servicehub.host.extensibility.x64",
        "msbuild",
        "msedgewebview2",
        "livepreviewsurface"
    };

        // Find all children of devenv.exe
        var children = Process.GetProcesses()
            .Where(p =>
            {
                try
                    {
                    var parent = GetParentProcessIdFast(p);
                    return parent == vs.Id;
                    }
                catch { return false; }
            });

        foreach (var child in children)
            {
            string name = child.ProcessName.ToLowerInvariant();

            // If it's NOT a known background process → it's the debug target
            if (!knownChildren.Contains(name))
                return true;
            }

        return false;
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
    private int GetParentProcessIdFast(Process process)
        {
        PROCESS_BASIC_INFORMATION pbi = new PROCESS_BASIC_INFORMATION();
        int status = NtQueryInformationProcess(
            process.Handle,
            0,
            ref pbi,
            Marshal.SizeOf(pbi),
            out _);

        return status == 0 ? pbi.InheritedFromUniqueProcessId.ToInt32() : -1;
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

    //THE LOGIC MONSTER
    private void IncrementCounters(ActivitySignals s)
        {
        bool isTyping = s.IsKeyboardActive || typingCooldown > 0;
        bool isIDEEngaged =
            (s.IsKeyboardActive && s.IsIDEActive) ||   // typing inside IDE
            (s.IsMouseActive && s.IsMouseInsideIDE);   // mouse movement inside IDE

        // DEBUG ALWAYS OVERRIDES EVERYTHING
        if (s.IsDebuggerRunning)
            {
            debugSeconds++;
            // prevent idle from ticking during debugging
            idleCooldown = 5;
            }

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
            if (idleCooldown > 0)
                idleCooldown--;
            }

        // IDE LOGIC --------------------------------------
        if (isIDEEngaged)
            {
            ideCooldown = 5;      // reset sticky window
            ideSeconds++;         // IDE starts immediately
            }
        else
            {
            if (ideCooldown > 0)
                {
                ideCooldown--;    // sticky window counts down
                ideSeconds++;     // IDE continues during cooldown
                }
            // else IDE stops
            }

        // IDLE only when NOTHING is happening AND NOT DEBUGGING
        if (!s.IsDebuggerRunning && !isTyping && !s.IsMouseActive && !isIDEEngaged)
            {
            if (idleCooldown > 0)
                idleCooldown--;
            else
                idleSeconds++;
            }
        }
    private ActivityUpdate BuildUpdate()
        {

        // Build update object
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

