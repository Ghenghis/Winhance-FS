using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Winhance.Core.Features.FileManager.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Winhance.WPF.Features.FileManager.ViewModels
{
    /// <summary>
    /// Main ViewModel for the File Manager dashboard.
    /// Integrates with Rust nexus_core for ultra-fast file operations.
    /// </summary>
    public partial class FileManagerViewModel : ObservableObject
    {
        private readonly IFileManagerService? _fileManagerService;
        private readonly IBatchRenameService? _batchRenameService;
        private readonly IOrganizerService? _organizerService;
        private readonly INexusIndexerService? _nexusIndexer;
        private readonly IServiceProvider? _serviceProvider;

        [ObservableProperty]
        private bool _isBrowserTabSelected = true;

        [ObservableProperty]
        private bool _isBatchRenameTabSelected;

        [ObservableProperty]
        private bool _isOrganizerTabSelected;

        [ObservableProperty]
        private bool _isSpaceRecoveryTabSelected;

        [ObservableProperty]
        private bool _isEnvironmentTabSelected;

        [ObservableProperty]
        private bool _isDuplicatesTabSelected;

        [ObservableProperty]
        private bool _isForensicsTabSelected;

        [ObservableProperty]
        private bool _isSearchTabSelected;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private object? _currentView;

        public DualPaneBrowserViewModel? BrowserViewModel { get; private set; }

        public BatchRenameViewModel? BatchRenameViewModel { get; private set; }

        public OrganizerViewModel? OrganizerViewModel { get; private set; }

        public SpaceRecoveryViewModel? SpaceRecoveryViewModel { get; private set; }

        public EnvironmentDoctorViewModel? EnvironmentDoctorViewModel { get; private set; }

        public DuplicateFinderViewModel? DuplicateFinderViewModel { get; private set; }

        public ForensicsViewModel? ForensicsViewModel { get; private set; }

        public FastSearchViewModel? FastSearchViewModel { get; private set; }

        public TabContainerViewModel? TabContainerViewModel { get; private set; }

        public SearchResultsViewModel? SearchResultsViewModel { get; private set; }

        public FileManagerViewModel()
        {
            // Design-time constructor
            InitializeViewModels();
        }

        public FileManagerViewModel(
            IFileManagerService fileManagerService,
            IBatchRenameService batchRenameService,
            IOrganizerService organizerService,
            INexusIndexerService nexusIndexer,
            IServiceProvider serviceProvider)
        {
            _fileManagerService = fileManagerService;
            _batchRenameService = batchRenameService;
            _organizerService = organizerService;
            _nexusIndexer = nexusIndexer;
            _serviceProvider = serviceProvider;

            InitializeViewModels();
        }

        private void InitializeViewModels()
        {
            BrowserViewModel = new DualPaneBrowserViewModel(_fileManagerService, _serviceProvider);
            BatchRenameViewModel = new BatchRenameViewModel(_batchRenameService);
            OrganizerViewModel = new OrganizerViewModel(_organizerService);
            SpaceRecoveryViewModel = new SpaceRecoveryViewModel(_organizerService);
            EnvironmentDoctorViewModel = new EnvironmentDoctorViewModel(_serviceProvider?.GetService<IEnvironmentDoctorService>());
            DuplicateFinderViewModel = new DuplicateFinderViewModel(_serviceProvider?.GetService<IDuplicateFinderService>());
            ForensicsViewModel = new ForensicsViewModel(_serviceProvider?.GetService<IForensicsService>());
            FastSearchViewModel = new FastSearchViewModel(_nexusIndexer);
            TabContainerViewModel = _serviceProvider?.GetRequiredService<TabContainerViewModel>() ?? new TabContainerViewModel();
            SearchResultsViewModel = _serviceProvider?.GetRequiredService<SearchResultsViewModel>() ?? new SearchResultsViewModel();

            // Wire up tab navigation to browser
            TabContainerViewModel.TabNavigationRequested += OnTabNavigationRequested;

            CurrentView = BrowserViewModel;
        }

        private void OnTabNavigationRequested(object? sender, string path)
        {
            if (BrowserViewModel != null)
            {
                BrowserViewModel.LeftPanePath = path;
                _ = BrowserViewModel.RefreshAsync();
            }
        }

        /// <summary>Show one tab: exactly one flag is true and <see cref="CurrentView"/> is its view model.</summary>
        private void ShowTab(object? view)
        {
            CurrentView = view;
            IsBrowserTabSelected = ReferenceEquals(view, BrowserViewModel);
            IsBatchRenameTabSelected = ReferenceEquals(view, BatchRenameViewModel);
            IsOrganizerTabSelected = ReferenceEquals(view, OrganizerViewModel);
            IsSpaceRecoveryTabSelected = ReferenceEquals(view, SpaceRecoveryViewModel);
            IsEnvironmentTabSelected = ReferenceEquals(view, EnvironmentDoctorViewModel);
            IsDuplicatesTabSelected = ReferenceEquals(view, DuplicateFinderViewModel);
            IsForensicsTabSelected = ReferenceEquals(view, ForensicsViewModel);
            IsSearchTabSelected = ReferenceEquals(view, FastSearchViewModel);
        }

        [RelayCommand]
        private void SelectBrowserTab() => ShowTab(BrowserViewModel);

        [RelayCommand]
        private void SelectBatchRenameTab() => ShowTab(BatchRenameViewModel);

        [RelayCommand]
        private void SelectOrganizerTab() => ShowTab(OrganizerViewModel);

        [RelayCommand]
        private void SelectSpaceRecoveryTab() => ShowTab(SpaceRecoveryViewModel);

        [RelayCommand]
        private void SelectEnvironmentTab() => ShowTab(EnvironmentDoctorViewModel);

        [RelayCommand]
        private void SelectDuplicatesTab() => ShowTab(DuplicateFinderViewModel);

        [RelayCommand]
        private void SelectForensicsTab() => ShowTab(ForensicsViewModel);

        [RelayCommand]
        private void SelectSearchTab() => ShowTab(FastSearchViewModel);

        // The tab headers are RadioButtons bound TwoWay to these flags, so selecting a tab by keyboard,
        // screen reader or UI Automation (which sets IsChecked without a click) also switches the view.
        partial void OnIsBrowserTabSelectedChanged(bool value) => ShowIfSelected(value, BrowserViewModel);

        partial void OnIsBatchRenameTabSelectedChanged(bool value) => ShowIfSelected(value, BatchRenameViewModel);

        partial void OnIsOrganizerTabSelectedChanged(bool value) => ShowIfSelected(value, OrganizerViewModel);

        partial void OnIsSpaceRecoveryTabSelectedChanged(bool value) => ShowIfSelected(value, SpaceRecoveryViewModel);

        partial void OnIsEnvironmentTabSelectedChanged(bool value) => ShowIfSelected(value, EnvironmentDoctorViewModel);

        partial void OnIsDuplicatesTabSelectedChanged(bool value) => ShowIfSelected(value, DuplicateFinderViewModel);

        partial void OnIsForensicsTabSelectedChanged(bool value) => ShowIfSelected(value, ForensicsViewModel);

        partial void OnIsSearchTabSelectedChanged(bool value) => ShowIfSelected(value, FastSearchViewModel);

        private void ShowIfSelected(bool selected, object? view)
        {
            if (selected && view != null && !ReferenceEquals(CurrentView, view))
            {
                ShowTab(view);
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            IsLoading = true;
            StatusMessage = "Refreshing...";

            try
            {
                if (IsBrowserTabSelected && BrowserViewModel != null)
                {
                    await BrowserViewModel.RefreshAsync();
                }
                else if (IsBatchRenameTabSelected && BatchRenameViewModel != null)
                {
                    await BatchRenameViewModel.RefreshPreviewAsync();
                }
                else if (IsOrganizerTabSelected && OrganizerViewModel != null)
                {
                    await OrganizerViewModel.AnalyzeAsync();
                }
                else if (IsSpaceRecoveryTabSelected && SpaceRecoveryViewModel != null)
                {
                    await SpaceRecoveryViewModel.AnalyzeCommand.ExecuteAsync(null);
                }
                else if (IsEnvironmentTabSelected && EnvironmentDoctorViewModel != null)
                {
                    await EnvironmentDoctorViewModel.ScanCommand.ExecuteAsync(null);
                }
                else if (IsDuplicatesTabSelected && DuplicateFinderViewModel?.Folders.Count > 0)
                {
                    await DuplicateFinderViewModel.ScanCommand.ExecuteAsync(null);
                }
                else if (IsForensicsTabSelected && ForensicsViewModel?.IsRecycleSection == true)
                {
                    await ForensicsViewModel.LoadRecycleBinCommand.ExecuteAsync(null);
                }
                else if (IsSearchTabSelected && FastSearchViewModel != null)
                {
                    await FastSearchViewModel.SearchNowCommand.ExecuteAsync(null);
                }

                StatusMessage = "Ready";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ShowHelp()
        {
            var helpText = "Winhance-FS File Manager Help\n\n" +
                          "Keyboard Shortcuts:\n" +
                          "F2 - Rename\n" +
                          "F3 - Quick View\n" +
                          "F4 - Edit\n" +
                          "F5 - Copy\n" +
                          "F6 - Move\n" +
                          "F7 - New Folder\n" +
                          "Del - Delete\n" +
                          "Ctrl+A - Select All\n" +
                          "Ctrl+F - Search\n" +
                          "Ctrl+T - New Tab\n" +
                          "Ctrl+W - Close Tab\n" +
                          "Tab - Switch Panel";

            System.Windows.MessageBox.Show(
                helpText,
                "Help",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        partial void OnSearchTextChanged(string value)
        {
            // Filter current view based on search text
            if (IsBrowserTabSelected && BrowserViewModel != null)
            {
                BrowserViewModel.FilterText = value;
            }
        }
    }
}
