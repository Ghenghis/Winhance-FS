using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Winhance.Core.Features.Common.Interfaces;
using Winhance.Core.Features.Common.Models;
using Winhance.WPF.Features.Common.ViewModels;

namespace Winhance.WPF.Features.Common.Views
{
    public partial class VersionHistoryDialog : Window
    {
        private readonly IVersionService _versionService;
        private List<VersionHistoryEntry> _versions = new();

        public VersionHistoryDialog(IVersionService versionService)
        {
            InitializeComponent();
            _versionService = versionService;
            
            Loaded += async (sender, e) =>
            {
                if (Application.Current.MainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    mainViewModel.IsDialogOverlayVisible = true;
                }
                
                await LoadVersionHistoryAsync();
            };

            Closed += (s, e) =>
            {
                if (Application.Current.MainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    mainViewModel.IsDialogOverlayVisible = false;
                }
            };
        }

        private async Task LoadVersionHistoryAsync()
        {
            try
            {
                LoadingPanel.Visibility = Visibility.Visible;
                ErrorText.Visibility = Visibility.Collapsed;
                VersionList.Visibility = Visibility.Collapsed;

                var currentVersion = _versionService.GetCurrentVersion();
                CurrentVersionText.Text = currentVersion.Version;

                var history = await _versionService.GetVersionHistoryAsync(10);
                _versions = history.Entries.OrderByDescending(e => e.ReleaseDate).ToList();

                VersionList.ItemsSource = _versions;
                VersionList.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ErrorText.Text = $"Failed to load version history: {ex.Message}";
                ErrorText.Visibility = Visibility.Visible;
            }
            finally
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CheckUpdatesButton.IsEnabled = false;
                CheckUpdatesButton.Content = "Checking...";

                var latestVersion = await _versionService.CheckForUpdateAsync();
                
                if (latestVersion.IsUpdateAvailable)
                {
                    var result = MessageBox.Show(
                        $"Update available: {latestVersion.Version}\n\nWould you like to download and install it now?",
                        "Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        await _versionService.DownloadAndInstallUpdateAsync();
                        Application.Current.Shutdown();
                    }
                }
                else
                {
                    MessageBox.Show(
                        "You are running the latest version.",
                        "No Updates",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error checking for updates: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                CheckUpdatesButton.IsEnabled = true;
                CheckUpdatesButton.Content = "Check for Updates";
            }
        }

        private async void RollbackButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string version)
            {
                var entry = _versions.FirstOrDefault(v => v.Version == version);
                if (entry == null) return;

                var result = MessageBox.Show(
                    $"Are you sure you want to rollback to {version}?\n\n" +
                    "The current version will be closed and the installer for the selected version will be launched.\n\n" +
                    "Note: Your settings and data will be preserved.",
                    "Confirm Rollback",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        button.IsEnabled = false;
                        button.Content = "Downloading...";

                        await _versionService.DownloadAndInstallVersionAsync(version);
                        
                        MessageBox.Show(
                            $"Rollback to {version} has been initiated.\n\nThe installer will now launch. Please follow the installation steps.",
                            "Rollback Started",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        Application.Current.Shutdown();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error during rollback: {ex.Message}",
                            "Rollback Failed",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        
                        button.IsEnabled = true;
                        button.Content = "Rollback";
                    }
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public static void Show(IVersionService versionService)
        {
            var dialog = new VersionHistoryDialog(versionService)
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            dialog.ShowDialog();
        }
    }
}
