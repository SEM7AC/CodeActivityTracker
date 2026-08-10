using CodeActivityTracker.Model;
using CodeActivityTracker.Services;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace CodeActivityTracker
    {
    public partial class MainWindow : Window
        {
        private DateTime sessionStartTime;
        private readonly TimerService _timer;
        private readonly LoggerService _logger;
        private readonly ActivityTrackerService _tracker;

        private static readonly Random _rng = new();

        private int totalSeconds;
        private int typingSeconds;
        private int ideSeconds;
        private int debugSeconds;
        private int idleSeconds;


        private string GetTierLabel(int tier)
            {
            return tier switch
                {
                    1 => "BEAST MODE",

                    2 => _rng.Next(2) == 0
                        ? "RESPECT EARNED"
                        : "UNEXPECTED COMPETENCE",

                    3 => _rng.Next(2) == 0
                        ? "CHECK THE BLOCK"
                        : "TIME CLOCK WATCHER",

                    4 => _rng.Next(2) == 0
                        ? "MODEM SCREECH"
                        : "PENTIUM",

                    5 => _rng.Next(2) == 0
                        ? "DEPRECATED"
                        : "404 — WORK NOT FOUND",

                    _ => "UNKNOWN TIER"
                    };
            }

        public MainWindow()
            {
            InitializeComponent();

            _timer = new TimerService(1000);
            _logger = new LoggerService();
            _tracker = new ActivityTrackerService(_timer);

            _tracker.ActivityUpdated += OnActivityUpdated;
            _timer.Start();

            sessionStartTime = DateTime.Now;

            Closing += MainWindow_Closing;



            }

        // WINDOW BAR AND DRAG METHODS
        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
            {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
            }

        private void Close_Click(object sender, RoutedEventArgs e)
            {
            Close();
            }

        private void Minimize_Click(object sender, RoutedEventArgs e)
            {
            WindowState = WindowState.Minimized;
            }

        //Update the UI
        private void OnActivityUpdated(ActivityUpdate update)
            {

            // Globals
            typingSeconds = update.TypingSeconds;
            ideSeconds = update.IDESeconds;
            debugSeconds = update.DebugSeconds;
            idleSeconds = update.IdleSeconds;
            totalSeconds = update.TotalSeconds;

            // Time
            TypeTime.Text = update.TypingFormatted;
            IDETime.Text = update.IDEFormatted;
            DebugTime.Text = update.DebugFormatted;
            IdleTime.Text = update.IdleFormatted;

            // Bars
            TypingFill.Width = update.TypingWidth;
            IDEFill.Width = update.IDEWidth;
            DebugFill.Width = update.DebugWidth;
            IdleFill.Width = update.IdleWidth;
            }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
            {
            WriteSessionLog();
            }

        private void WriteSessionLog()
            {
            // REAL elapsed time (correct denominator)
            int realElapsedSeconds = (int)(DateTime.Now - sessionStartTime).TotalSeconds;

            // Tier must use REAL time, not inflated activity ticks
            int tier = CalculateTier(
                realElapsedSeconds,
                typingSeconds,
                ideSeconds,
                debugSeconds,
                idleSeconds);

            string tierLabel = GetTierLabel(tier);

            // KPI string must also use REAL time
            string line =
                 $"{sessionStartTime:yyyy-MM-dd HH:mm:ss} | " +   // START TIME
                 $"Total: {_tracker.FormatTime(realElapsedSeconds)} | " +
                 $"Typing: {Percent(typingSeconds, realElapsedSeconds)} | " +
                 $"IDE: {Percent(ideSeconds, realElapsedSeconds)} | " +
                 $"Debug: {Percent(debugSeconds, realElapsedSeconds)} | " +
                 $"Idle: {Percent(idleSeconds, realElapsedSeconds)} | " +
                 $"Tier: {tierLabel}";


            _logger.Log(line);
            }

        private string Percent(int part, int total)
            {
            if (total == 0) return "0%";
            double pct = (double)part / total * 100;
            return $"{pct:0}%";
            }

        private int CalculateTier(int realTotal, int typing, int ide, int debug, int idle)
            {
            if (realTotal <= 0)
                return 5; // DEPRECATED / 404 — WORK NOT FOUND

            double typingPct = (double)typing / realTotal * 100;
            double idePct = (double)ide / realTotal * 100;
            double debugPct = (double)debug / realTotal * 100;
            double idlePct = (double)idle / realTotal * 100;

            // Engagement = actual work
            double engagement = typingPct + idePct + debugPct;

            // -----------------------------
            // DEBUG OVERRIDE (Tier 3)
            // -----------------------------
            if (debugPct >= 40)
                return 3; // CHECK THE BLOCK / TIME CLOCK WATCHER

            // -----------------------------
            // TIER 1 — BEAST MODE
            // -----------------------------
            if (engagement >= 70)
                return 1;

            // -----------------------------
            // TIER 2 — RESPECT EARNED / UNEXPECTED COMPETENCE
            // -----------------------------
            if (engagement >= 50)
                return 2;

            // -----------------------------
            // TIER 3 — CHECK THE BLOCK / TIME CLOCK WATCHER
            // -----------------------------
            if (engagement >= 30)
                return 3;

            // -----------------------------
            // TIER 4 — MODEM SCREECH / PENTIUM
            // -----------------------------
            if (engagement >= 10)
                return 4;

            // -----------------------------
            // TIER 5 — DEPRECATED / 404 — WORK NOT FOUND
            // -----------------------------
            return 5;
            }




        }
    }
