using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Timers;
using System.Windows;
using EveWindowManager.Extensions;
using EveWindowManager.Properties;
using EveWindowManager.Store;
using EveWindowManager.Ui.Models;
using EveWindowManager.Windows;
using Microsoft.Win32;
using Timer = System.Timers.Timer;

namespace EveWindowManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string Version = "v0.0.5";
        public string TitleBar { get; } = $"ewm {Version} - Eve Window Manager";

        private readonly EveClientSettingsStore _clientSettingsStore = new EveClientSettingsStore();
        public readonly Timer RefreshTimer = new Timer();

        public MainWindow()
        {
            _clientSettingsStore.LoadFromFile(Settings.Default.ClientSettingsFile);
            InitializeComponent();
            DataContext = this;
            UpdateStatus($"Loaded settings from {Path.GetFileName(Settings.Default.ClientSettingsFile)}.");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshTimer.Interval = Settings.Default.AutoRefreshIntervalMs;
            RefreshTimer.Elapsed += RefreshTimerOnElapsed;
            RefreshTimer.AutoReset = true;
            RefreshTimer.Enabled = Settings.Default.AutoRefreshEnabled;
        }

        private void RefreshTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new ThreadStart(RefreshClients));
            Debug.WriteLine($"RefreshTimerOnElapsed {DateTime.Now.Millisecond}");
        }

        private void UpdateStatus(string message)
        {
            lbStatus.Content = message;
        }

        private void RefreshClients()
        {
            var processes = Process.GetProcessesByName("exefile");

            var items = new List<ProcessListItem>();

            foreach (var process in processes)
            {
                if (!process.MainWindowTitle.StartsWith(Settings.Default.EveTitlebarPrefix)) continue;

                var isSaved = _clientSettingsStore.IsSaved(process.MainWindowTitle); ;
                items.Add(new ProcessListItem { Process = process, IsSaved = isSaved });
            }

            icEveClients.ItemsSource = items;
        }

        private void RestoreClientPosition(Process process)
        {
            var settings = _clientSettingsStore.GetSettingByWindowTitle(process.MainWindowTitle);

            if (settings == null) return;

            if (!WindowHelper.SetWindowPos(process.MainWindowHandle, IntPtr.Zero, settings.PositionX, settings.PositionY, settings.Width, settings.Height))
                MessageBox.Show($"Unable to set position for {process.MainWindowTitle}");

            if (settings.IsMaximized)
                WindowHelper.ShowWindow(process.MainWindowHandle, (int)WindowHelper.CmdShow.SW_MAXIMIZE);

            if (Settings.Default.BringToForegroundOnRestore)
                WindowHelper.SetForegroundWindow(process.MainWindowHandle);
        }

        #region Item Container Methods

        private void ItemButtonRestoreClick(object sender, RoutedEventArgs e)
        {
            var icItem = ((FrameworkElement)sender).DataContext as ProcessListItem;

            RestoreClientPosition(icItem.Process);

            UpdateStatus($"Client {icItem.Process.MainWindowTitle} restored.");
        }

        private void ItemButtonSaveClick(object sender, RoutedEventArgs e)
        {
            var icItem = ((FrameworkElement)sender).DataContext as ProcessListItem;
            _clientSettingsStore.Upsert(icItem.Process.ToEveClientSetting());
            _clientSettingsStore.SaveToFile(Settings.Default.ClientSettingsFile);

            icItem.IsSaved = _clientSettingsStore.IsSaved(icItem.Process.MainWindowTitle);
            icEveClients.Items.Refresh();
            UpdateStatus($"Client {icItem.Process.MainWindowTitle} saved.");
        }

        private void ItemButtonDeleteClick(object sender, RoutedEventArgs e)
        {
            var icItem = ((FrameworkElement)sender).DataContext as ProcessListItem;
            _clientSettingsStore.DeleteByWindowTitle(icItem.Process.MainWindowTitle);
            _clientSettingsStore.SaveToFile(Settings.Default.ClientSettingsFile);

            icItem.IsSaved = _clientSettingsStore.IsSaved(icItem.Process.MainWindowTitle);
            icEveClients.Items.Refresh();
            UpdateStatus($"Settings for client {icItem.Process.MainWindowTitle} deleted.");
        }

        #endregion

        #region Button methods

        private void Button_Refresh(object sender, RoutedEventArgs e)
        {
            RefreshClients();
            UpdateStatus("Client list refreshed.");
        }
        
        private void Button_RestoreAll(object sender, RoutedEventArgs e)
        {
            foreach (ProcessListItem icItem in icEveClients.Items)
            {
                RestoreClientPosition(icItem.Process);

                if (Settings.Default.BringToForegroundOnRestore)
                    WindowHelper.SetForegroundWindow(icItem.Process.MainWindowHandle);
            }
            UpdateStatus("All clients restored.");
        }

        private void Button_SaveAll(object sender, RoutedEventArgs e)
        {
            foreach (ProcessListItem icItem in icEveClients.Items)
            {
                _clientSettingsStore.Upsert(icItem.Process.ToEveClientSetting());
                icItem.IsSaved = _clientSettingsStore.IsSaved(icItem.Process.MainWindowTitle);
            }
            _clientSettingsStore.SaveToFile(Settings.Default.ClientSettingsFile);
            icEveClients.Items.Refresh();
            UpdateStatus("All clients saved.");
        }

        #endregion

        #region Menu methods
        private void Menu_Load(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog {Filter = "Json Configuration File (*.json)|*.json" };

            if (openFileDialog.ShowDialog() != true) return;

            _clientSettingsStore.LoadFromFile(openFileDialog.FileName);
            Settings.Default.ClientSettingsFile = openFileDialog.FileName;
            Settings.Default.Save();

            UpdateStatus($"Client settings loaded from file {Path.GetFileName(Settings.Default.ClientSettingsFile)}.");
        }

        private void Menu_Save(object sender, RoutedEventArgs e)
        {
            _clientSettingsStore.SaveToFile(Settings.Default.ClientSettingsFile);
            UpdateStatus($"Settings saved to {Path.GetFileName(Settings.Default.ClientSettingsFile)}.");
        }

        private void Menu_SaveAs(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog { Filter = "Json Configuration File (*.json)|*.json" };
            if (saveFileDialog.ShowDialog() != true) return;

            Settings.Default.ClientSettingsFile = saveFileDialog.FileName;
            Settings.Default.Save();

            _clientSettingsStore.SaveToFile(Settings.Default.ClientSettingsFile);
            UpdateStatus($"Settings saved to {Path.GetFileName(Settings.Default.ClientSettingsFile)}.");
        }

        private void Menu_Exit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Settings.Default.Save();
        }

        private void Menu_About(object sender, RoutedEventArgs e)
        {
            new About {Owner = this}.Show();
        }

        private void MenuItem_AutoRefreshEnabled(object sender, RoutedEventArgs e)
        {
            RefreshTimer.Enabled = Settings.Default.AutoRefreshEnabled;
        }

        #endregion
    }
}
