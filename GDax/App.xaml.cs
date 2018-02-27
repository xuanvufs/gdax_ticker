using Hardcodet.Wpf.TaskbarNotification;
using System.Collections.Generic;
using System.Windows;

namespace GDax
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private TaskbarIcon _trayIcon;
        private TrayIconViewModel _model;
        private Dictionary<CoinKind, TickerWidget> _widgets;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Length > 0 && e.Args[0] == "-debug")
            {
                Helper.CreateDebugConsole();
            }

            _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
            _widgets = new Dictionary<CoinKind, TickerWidget>();
            _model = _trayIcon.DataContext as TrayIconViewModel;

            var coins = _model?.Coins;
            if (coins != null)
            {
                foreach (var coin in coins)
                {
                    _widgets.Add(coin.Kind, new TickerWidget(coin));
                }
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayIcon?.Dispose();
            _model?.Dispose();
            base.OnExit(e);
        }
    }
}
