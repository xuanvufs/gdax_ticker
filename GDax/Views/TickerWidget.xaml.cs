using GDax.Helpers;
using GDax.Views.Models;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace GDax.Views
{
    /// <summary>
    /// Interaction logic for TickerWidget.xaml
    /// </summary>
    public partial class TickerWidget : Window
    {
        private ITickerViewModel _model;

        public TickerWidget(ITickerViewModel model)
        {
            InitializeComponent();
            DataContext = _model = model;
            tickerCard.Background = _model.NonActiveBackground;
        }

        private void MoveTicker(object sender, MouseButtonEventArgs e)
        {
            Native.ReleaseCapture();
            Native.SendMessage(new WindowInteropHelper(this).Handle, Native.WM_NCLBUTTONDOWN, Native.HT_CAPTION, 0);

            _model.Settings.Top = Top;
            _model.Settings.Left = Left;
        }

        private void MouseOver(object sender, MouseEventArgs e)
        {
            tickerCard.Background = _model.Background;
        }

        private void MouseOut(object sender, MouseEventArgs e)
        {
            tickerCard.Background = _model.NonActiveBackground;
        }

        private void TickerLoaded(object sender, RoutedEventArgs e)
        {
            var wndHlp = new WindowInteropHelper(this);
            var style = (int)Native.GetWindowLong(wndHlp.Handle, (int)WindowLongFlags.GWL_EXSTYLE) | (int)WindowStylesEx.WS_EX_TOOLWINDOW;

            Native.SetWindowLong(wndHlp.Handle, (int)WindowLongFlags.GWL_EXSTYLE, (IntPtr)style);
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
        }
    }
}