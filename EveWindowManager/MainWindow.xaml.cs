using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using EveWindowManager.Extensions;
using EveWindowManager.Store;
using EveWindowManager.Windows;

namespace EveWindowManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private EveClientSettingsStore _clientSettingsStore = new EveClientSettingsStore();

        public MainWindow()
        {
            InitializeComponent();
            RefreshClients();
        }

        private void UpdateStatus(string message)
        {
            lbStatus.Content = message;
        }

        /* Refresh client list */

        private void RefreshButtonClick(object sender, RoutedEventArgs e)
        {
            RefreshClients();
        }

        private void RefreshClients()
        {
            var processes = Process.GetProcessesByName("exefile");

            var items = new List<Process>();

            foreach (var process in processes)
            {
                if (!process.MainWindowTitle.StartsWith("EVE - ")) continue;
                items.Add(process);
            }

            icEveClients.ItemsSource = items;

            UpdateStatus("Clients refreshed.");
        }

        /* Restore client positions */

        private void RestoreAllButtonClick(object sender, RoutedEventArgs e)
        {
            foreach (Process process in icEveClients.Items)
            {
                RestoreClientPosition(process);
            }
            UpdateStatus("All clients restored.");
        }

        private void RestoreClientPosition(Process process)
        {
            var settings = _clientSettingsStore.GetSettingByWindowTitle(process.MainWindowTitle);

            if (settings == null) return;

            if (!WindowHelper.SetWindowPos(process.MainWindowHandle, IntPtr.Zero, settings.PositionX, settings.PositionY, settings.Width, settings.Height))
                MessageBox.Show($"Unable to set position for {process.MainWindowTitle}");
        }

        private void ItemButtonRestoreClick(object sender, RoutedEventArgs e)
        {
            var process = ((FrameworkElement)sender).DataContext as Process;

            RestoreClientPosition(process);

            UpdateStatus($"Client {process.MainWindowTitle} restored.");
        }

        /* Save client positions */

        private void SaveAllButtonClick(object sender, RoutedEventArgs e)
        {
            foreach (Process process in icEveClients.Items)
            {
                _clientSettingsStore.Upsert(process.ToEveClientSetting());
            }
            _clientSettingsStore.SaveToFile();
            UpdateStatus("All clients saved.");
        }

        private void ItemButtonSaveClick(object sender, RoutedEventArgs e)
        {
            var process = ((FrameworkElement)sender).DataContext as Process;
            _clientSettingsStore.Upsert(process.ToEveClientSetting());
            _clientSettingsStore.SaveToFile();
            UpdateStatus($"Client {process.MainWindowTitle} saved.");
        }

        private void ReloadFromFileClick(object sender, RoutedEventArgs e)
        {
            RefreshClients();
            _clientSettingsStore.LoadFromFile();
            UpdateStatus("Settings loaded from file.");
        }

        private void SaveSettingsToFileClick(object sender, RoutedEventArgs e)
        {
            _clientSettingsStore.SaveToFile();
            UpdateStatus("Settings saved to file.");
        }

        private void ExitClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();
        }
    }
}
