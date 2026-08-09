using System.Windows;
using System.Windows.Input;

namespace CodeActivityTracker
    {
    public partial class MainWindow : Window
        {
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


        }
    }
