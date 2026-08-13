using CodeActivityTimer.Services;
using CodeActivityTracker.Model;
using CodeActivityTracker.Services;
using System.ComponentModel;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CodeActivityTracker
    {
    public partial class MainWindow : Window
        {
        private DateTime sessionStartTime;
        private readonly TimerService _timer;
        private readonly LoggerService _logger;
        private readonly ActivityTrackerService _tracker;
        private readonly TierCalculator _tierCalculator = new();
        private readonly TierNameProvider _tierNameProvider = new();
        private readonly int _tierNameIndex;




        private int totalSeconds;
        private int typingSeconds;
        private int ideSeconds;
        private int debugSeconds;
        private int idleSeconds;

        private const int FLIP_THRESHOLD = 600; // 10 minutes

        private int typingFlips = 0;
        private int ideFlips = 0;
        private int debugFlips = 0;
        private int idleFlips = 0;

        private int typingBarSeconds = 0;
        private int ideBarSeconds = 0;
        private int debugBarSeconds = 0;
        private int idleBarSeconds = 0;
     
        public MainWindow()
            {
            InitializeComponent();

            _timer = new TimerService(1000);
            _logger = new LoggerService();
            _tracker = new ActivityTrackerService(_timer);
            _tierNameIndex = new Random().Next(0, 4); // max array length


            // *** IMPORTANT: give tracker access to this window ***
            _tracker.MainWindowRef = this;

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
            // TOTALS from tracker
            typingSeconds = update.TypingSeconds;
            ideSeconds = update.IDESeconds;
            debugSeconds = update.DebugSeconds;
            idleSeconds = update.IdleSeconds;
            totalSeconds = update.TotalSeconds;

            // BAR increments (1 per tick if active)
            typingBarSeconds = typingSeconds - (typingFlips * FLIP_THRESHOLD);
            ideBarSeconds = ideSeconds - (ideFlips * FLIP_THRESHOLD);
            debugBarSeconds = debugSeconds - (debugFlips * FLIP_THRESHOLD);
            idleBarSeconds = idleSeconds - (idleFlips * FLIP_THRESHOLD);

            // --- FLIP CHECKS ---
            FlipBar(ref typingBarSeconds, ref typingFlips, TypingBar, TypingFlipsLabel);
            FlipBar(ref ideBarSeconds, ref ideFlips, IDEBar, IDEFlipsLabel);
            FlipBar(ref debugBarSeconds, ref debugFlips, DebugBar, DebugFlipsLabel);
            FlipBar(ref idleBarSeconds, ref idleFlips, IdleBar, IdleFlipsLabel);

            // --- UPDATE TIME LABELS (now using accumulated flips) ---
            TypeTime.Text = _tracker.FormatTime(typingBarSeconds + (typingFlips * FLIP_THRESHOLD));
            IDETime.Text = _tracker.FormatTime(ideBarSeconds + (ideFlips * FLIP_THRESHOLD));
            DebugTime.Text = _tracker.FormatTime(debugBarSeconds + (debugFlips * FLIP_THRESHOLD));
            IdleTime.Text = _tracker.FormatTime(idleBarSeconds + (idleFlips * FLIP_THRESHOLD));

            // --- UPDATE SLIDERS (value = seconds in current bar) ---
            TypingBar.Value = typingBarSeconds;
            IDEBar.Value = ideBarSeconds;
            DebugBar.Value = debugBarSeconds;
            IdleBar.Value = idleBarSeconds;

            // --- CALCULATE PERCENTAGES FOR TIER SYSTEM ---
            double typingPct = totalSeconds > 0 ? (double)typingSeconds / totalSeconds * 100 : 0;
            double idePct = totalSeconds > 0 ? (double)ideSeconds / totalSeconds * 100 : 0;
            double debugPct = totalSeconds > 0 ? (double)debugSeconds / totalSeconds * 100 : 0;
            double idlePct = totalSeconds > 0 ? (double)idleSeconds / totalSeconds * 100 : 0;

            // --- GET TIER OBJECT ---
            var tier = _tierCalculator.Calculate(typingPct, idePct, debugPct, idlePct);

            // --- UPDATE TIER TEXT ELEMENTS ----------------------------
            TypingTierLabel.Text = $"Typing: {tier.TypingLabel}";
            IdeTierLabel.Text = $"IDE: {tier.IdeLabel}";
            DebugTierLabel.Text = $"Debug: {tier.DebugLabel}";
            IdleTierLabel.Text = $"Idle: {tier.IdleLabel}";

            
            }


        private void MainWindow_Closing(object? sender, CancelEventArgs e)
            {
            _timer.Stop();
            WriteSessionLog();
            }
        private void WriteSessionLog()
            {
            // REAL elapsed time (correct denominator)
            int realElapsedSeconds = (int)(DateTime.Now - sessionStartTime).TotalSeconds;

            // --- REAL PERCENTAGES FOR TIER SYSTEM ---
            double typingPct = realElapsedSeconds > 0 ? (double)typingSeconds / realElapsedSeconds * 100 : 0;
            double idePct = realElapsedSeconds > 0 ? (double)ideSeconds / realElapsedSeconds * 100 : 0;
            double debugPct = realElapsedSeconds > 0 ? (double)debugSeconds / realElapsedSeconds * 100 : 0;
            double idlePct = realElapsedSeconds > 0 ? (double)idleSeconds / realElapsedSeconds * 100 : 0;

            // --- GET TIER OBJECT ---
            var tier = _tierCalculator.Calculate(typingPct, idePct, debugPct, idlePct);

            // --- AVERAGE TIER (OVERALL) ---
            int overallTier = (tier.TypingTier + tier.IdeTier + tier.DebugTier + tier.IdleTier) / 4;

            // --- GET OVERALL TIER NAME ---
            string tierLabel = _tierNameProvider.GetOverallName(overallTier);


            // KPI string must also use REAL time
            string line =
                 $"{sessionStartTime:yyyy-MM-dd HH:mm:ss} | " +
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
       
        private void FlipBar(ref int seconds, ref int flips, Slider bar, TextBlock label)
            {
            if (seconds >= FLIP_THRESHOLD)
                {
                seconds = 0;          // reset logic seconds
                bar.Value = 0;        // reset slider
                flips++;              // increment flip count
                label.Text = $"x{flips}";
                }
            }
        

        }
    }
