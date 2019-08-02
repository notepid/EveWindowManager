using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using EveWindowManager.Extensions;
using EveWindowManager.Models;
using EveWindowManager.Store;
using EveWindowManager.Windows;

namespace EveWindowManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly EveClientSettingsStore _clientSettingsStore = new EveClientSettingsStore();

        public MainWindow()
        {
            InitializeComponent();
            RefreshClients();
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
                if (!process.MainWindowTitle.StartsWith("EVE - ")) continue;

                var isSaved = _clientSettingsStore.IsSaved(process.MainWindowTitle); ;
                items.Add(new ProcessListItem { Process = process, IsSaved = isSaved });
            }

            icEveClients.ItemsSource = items;

            UpdateStatus("Clients refreshed.");
        }

        private void RestoreClientPosition(Process process)
        {
            var settings = _clientSettingsStore.GetSettingByWindowTitle(process.MainWindowTitle);

            if (settings == null) return;

            if (!WindowHelper.SetWindowPos(process.MainWindowHandle, IntPtr.Zero, settings.PositionX, settings.PositionY, settings.Width, settings.Height))
                MessageBox.Show($"Unable to set position for {process.MainWindowTitle}");

            if (settings.IsMaximized)
                WindowHelper.ShowWindow(process.MainWindowHandle, (int)WindowHelper.CmdShow.SW_MAXIMIZE);
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
            _clientSettingsStore.SaveToFile();
            
            icItem.IsSaved = _clientSettingsStore.IsSaved(icItem.Process.MainWindowTitle);
            icEveClients.Items.Refresh();
            UpdateStatus($"Client {icItem.Process.MainWindowTitle} saved.");
        }

        private void ItemButtonDeleteClick(object sender, RoutedEventArgs e)
        {
            var icItem = ((FrameworkElement)sender).DataContext as ProcessListItem;
            _clientSettingsStore.DeleteByWindowTitle(icItem.Process.MainWindowTitle);
            _clientSettingsStore.SaveToFile();

            icItem.IsSaved = _clientSettingsStore.IsSaved(icItem.Process.MainWindowTitle);
            icEveClients.Items.Refresh();
            UpdateStatus($"Settings for client {icItem.Process.MainWindowTitle} deleted.");
        }

        #endregion

        #region Button methods

        private void Button_Refresh(object sender, RoutedEventArgs e)
        {
            RefreshClients();
        }
        
        private void Button_RestoreAll(object sender, RoutedEventArgs e)
        {
            foreach (ProcessListItem icItem in icEveClients.Items)
            {
                RestoreClientPosition(icItem.Process);
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
            _clientSettingsStore.SaveToFile();
            icEveClients.Items.Refresh();
            UpdateStatus("All clients saved.");
        }

        #endregion

        #region Menu methods
        private void Menu_ReloadFromFileClick(object sender, RoutedEventArgs e)
        {
            RefreshClients();
            _clientSettingsStore.LoadFromFile();
            UpdateStatus("Settings loaded from file.");
        }

        private void Menu_SaveSettingsToFileClick(object sender, RoutedEventArgs e)
        {
            _clientSettingsStore.SaveToFile();
            UpdateStatus("Settings saved to file.");
        }

        private void Menu_Exit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void Menu_About(object sender, RoutedEventArgs e)
        {
            new About().Show();
        }
        #endregion
    }
}
