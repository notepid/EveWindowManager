﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using EveWindowManager.Extensions;
using EveWindowManager.Properties;
using EveWindowManager.Store;
using EveWindowManager.Ui.Models;
using EveWindowManager.Windows;
using Microsoft.Win32;
using NHotkey;
using NHotkey.Wpf;
using Timer = System.Timers.Timer;


namespace EveWindowManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string Version = "v0.0.10";
        public string TitleBar { get; } = $"ewm {Version} - Eve Window Manager";

        private readonly EveClientSettingsStore _clientSettingsStore = new EveClientSettingsStore();
        public readonly Timer CheckTimer = new Timer();

        private readonly List<ProcessListItem> _icEveClientsList = new List<ProcessListItem>();

        public MainWindow()
        {
            _clientSettingsStore.LoadFromFile(Settings.Default.ClientSettingsFile);
            InitializeComponent();
            DataContext = this;
            UpdateStatus($"Loaded settings from {Path.GetFileName(Settings.Default.ClientSettingsFile)}.");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CheckTimer.Interval = Settings.Default.AutoRefreshIntervalMs;
            CheckTimer.Elapsed += CheckTimerOnElapsed;
            CheckTimer.AutoReset = true;
            CheckTimer.Enabled = true;
            icEveClients.ItemsSource = _icEveClientsList;

            HotkeyManager.Current.AddOrReplace("RestoreAll", Key.End, ModifierKeys.Control | ModifierKeys.Alt, OnRestoreHotkey);
        }

        private void CheckTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            if (Settings.Default.AutoRefreshEnabled)
                Dispatcher?.BeginInvoke(new ThreadStart(RefreshClients));

            if (Settings.Default.AutoRestoreClients)
                Dispatcher?.BeginInvoke(new ThreadStart(RestoreAllClientPositionsOnce));
        }

        private void UpdateStatus(string message)
        {
            lbStatus.Content = message;
        }

        private void RefreshClients()
        {
            var processes = Process.GetProcessesByName("exefile");

            var anyUpdated = false;

            //Add new
            foreach (var process in processes)
            {
                //Skip any eve clients that are not logged in
                if (!process.MainWindowTitle.StartsWith(Settings.Default.EveTitlebarPrefix)) continue;

                //Skip any clients that already has been added to the list
                if (_icEveClientsList.Any(x => x.Process.MainWindowTitle.Equals(process.MainWindowTitle))) continue;

                var isSaved = _clientSettingsStore.IsSaved(process.MainWindowTitle);
                _icEveClientsList.Add(new ProcessListItem { Process = process, IsSaved = isSaved });

                anyUpdated = true;
            }

            //Remove those that have been closed
            foreach (var processListItem in _icEveClientsList.ToList())
            {
                if (processes.Any(x => x.MainWindowTitle.Equals(processListItem.Process.MainWindowTitle))) continue;

                _icEveClientsList.Remove(processListItem);
                anyUpdated = true;
            }

            if (anyUpdated)
                icEveClients.Items.Refresh();
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

            if (Settings.Default.UnMinimizeOnRestore)
                WindowHelper.ShowWindow(process.MainWindowHandle, (int)WindowHelper.CmdShow.SW_SHOWNORMAL);
        }

        private void OnRestoreHotkey(object sender, HotkeyEventArgs e)
        {
            RestoreAllClientPositions();
        }

        private void RestoreAllClientPositions()
        {
            foreach (var clientSetting in _clientSettingsStore.All())
            {
                var icItem = _icEveClientsList.FirstOrDefault(x => x.Process.MainWindowTitle.Equals(clientSetting.ProcessTitle));
                if (icItem == null) continue;

                RestoreClientPosition(icItem.Process);
            }
        }

        private void RestoreAllClientPositionsOnce()
        {
            var anyUpdated = false;

            foreach (var clientSetting in _clientSettingsStore.All())
            {
                var icItem = _icEveClientsList.FirstOrDefault(x => x.Process.MainWindowTitle.Equals(clientSetting.ProcessTitle));
                if (icItem == null) continue;

                if (icItem.HasBeenRestored) continue;

                RestoreClientPosition(icItem.Process);

                icItem.HasBeenRestored = true;
                anyUpdated = true;
                UpdateStatus($"Position of {icItem.Process.MainWindowTitle} auto restored.");
            }

            if (anyUpdated)
                icEveClients.Items.Refresh();
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
            RestoreAllClientPositions();
            UpdateStatus("All clients restored.");
        }

        private void Button_MinimizeAll(object sender, RoutedEventArgs e)
        {
            foreach (var clientSetting in _clientSettingsStore.All())
            {
                var icItem = _icEveClientsList.FirstOrDefault(x => x.Process.MainWindowTitle.Equals(clientSetting.ProcessTitle));
                if (icItem == null) continue;

                WindowHelper.ShowWindow(icItem.Process.MainWindowHandle, (int)WindowHelper.CmdShow.SW_MINIMIZE);
            }
            UpdateStatus("All clients minimizer.");
        }

        private void Button_SaveAll(object sender, RoutedEventArgs e)
        {
            foreach (var icItem in _icEveClientsList)
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
            var openFileDialog = new OpenFileDialog { Filter = "Json Configuration File (*.json)|*.json" };

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
            new About { Owner = this }.Show();
        }

        #endregion
    }
}
