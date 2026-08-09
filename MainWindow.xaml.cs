using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Runtime.InteropServices;

namespace CodeActivityTracker
    {
    public partial class MainWindow : Window
        {
        

        // Win32API for capturing Idle time
        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
            {
            public uint cbSize;
            public uint dwTime;
            }

        public MainWindow()
            {
            InitializeComponent();
            
            
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

        private string FormatTime(int seconds)
            {
            return TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");
            }
        
        private int GetIdleTimeSeconds()
            {
            LASTINPUTINFO info = new LASTINPUTINFO();
            info.cbSize = (uint)Marshal.SizeOf(info);

            if (!GetLastInputInfo(ref info))
                return 0;
            
            uint idleTime = ((uint)Environment.TickCount - info.dwTime);
            return (int)(idleTime / 1000);
            }




        }
    }
