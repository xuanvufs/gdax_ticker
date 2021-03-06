﻿using GDax.Controls;
using GDax.Helpers;
using GDax.IoC;
using GDax.Views;
using Hardcodet.Wpf.TaskbarNotification;
using StructureMap;
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

        //private TrayIconViewModel _model;
        private IContainer _container;

        private IEnumerable<TickerWidget> _widgets;

        public App()
        {
            _container = Bootstrapper.Init();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            if (e.Args.Length > 0 && e.Args[0] == "-debug")
            {
                Debugging.CreateDebugConsole();
            }

            _trayIcon = _container.GetInstance<TaskbarIcon>();
            _trayIcon.Icon = GDax.Properties.Resources.disconnected;
            _trayIcon.Visibility = Visibility.Visible;
            _trayIcon.ContextMenu = _container.GetInstance<TrayMenu>();
            _widgets = _container.GetAllInstances<TickerWidget>();
            _container.GetInstance<IFeed>().ConnectionStateChanged += ConnectionStateChanged;
        }

        private void ConnectionStateChanged(System.Net.WebSockets.WebSocketState state)
        {
            _trayIcon.Icon = state != System.Net.WebSockets.WebSocketState.Open ? GDax.Properties.Resources.disconnected : GDax.Properties.Resources.gdax;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            _container?.GetInstance<ISettingsFactory>().SaveSettings();
            _container?.GetInstance<IFeed>().Stop();
            _container?.Dispose();
            _trayIcon?.Dispose();
        }
    }
}