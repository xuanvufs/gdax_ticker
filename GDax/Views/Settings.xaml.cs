using GDax.Helpers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace GDax.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void MoveWindow(object sender, MouseButtonEventArgs e)
        {
            Native.ReleaseCapture();
            Native.SendMessage(new WindowInteropHelper(this).Handle, Native.WM_NCLBUTTONDOWN, Native.HT_CAPTION, 0);
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void MaximizeWindow(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized) SystemCommands.RestoreWindow(this);
            else SystemCommands.MaximizeWindow(this);
        }

        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }
    }
}
