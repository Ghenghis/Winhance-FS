using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Winhance.Core.Features.FileManager.Interfaces;

namespace Winhance.WPF.Features.FileManager.ViewModels
{
    /// <summary>
    /// ViewModel for displaying and managing search results.
    /// Supports both service-based and fallback file system search.
    /// </summary>
    public partial class SearchResultsViewModel : ObservableObject
    {
        private readonly ISearchService? _searchService;
        private CancellationTokenSource? _searchCts;
        private CancellationTokenSource? _debounceCts;

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private ObservableCollection<SearchResultItem> _results = new();

        [ObservableProperty]
        private bool _isSearching;

        [ObservableProperty]
        private string _statusMessage = "Enter a search term to begin";

        [ObservableProperty]
        private int _totalResults;

        [ObservableProperty]
        private long _totalSize;

        [ObservableProperty]
        private bool _hasResults;

        [ObservableProperty]
        private SearchResultItem? _selectedResult;

        [ObservableProperty]
        private string _searchPath = string.Empty;

        [ObservableProperty]
        private bool _searchRecursively = true;

        [ObservableProperty]
        private bool _caseSensitive;

        [ObservableProperty]
        private bool _useRegex;

        [ObservableProperty]
        private string _extensionFilter = string.Empty;

        [ObservableProperty]
        private long? _minSize;

        [ObservableProperty]
        private long? _maxSize;

        [ObservableProperty]
        private DateTime? _modifiedAfter;

        [ObservableProperty]
        private DateTime? _modifiedBefore;

        [ObservableProperty]
        private int _maxResults = 1000;

        public event EventHandler<string>? NavigateToPathRequested;
        public event EventHandler<string>? OpenFileRequested;

        public SearchResultsViewModel(ISearchService? searchService = null)
        {
            _searchService = searchService;
            SearchPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        [RelayCommand]
        public async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                StatusMessage = "Enter a search term to begin";
                return;
            }

            // Cancel any pending debounced search and any ongoing search
            _debounceCts?.Cancel();
            _searchCts?.Cancel();
            var cts = new CancellationTokenSource();
            _searchCts = cts;
            var token = cts.Token;

            IsSearching = true;
            StatusMessage = $"Searching for '{SearchQuery}'...";
            Results.Clear();
            TotalResults = 0;
            TotalSize = 0;

            try
            {
                if (_searchService != null)
                {
                    await SearchWithServiceAsync(token);
                }
                else
                {
                    await SearchFallbackAsync(token);
                }

                HasResults = Results.Count > 0;
                TotalResults = Results.Count;
                TotalSize = Results.Sum(r => r.Size);

                StatusMessage = HasResults
                    ? $"Found {TotalResults:N0} results ({FormatSize(TotalSize)})"
                    : "No results found";
            }
            catch (OperationCanceledException)
            {
                // A newer search owns the status when this one was superseded.
                if (ReferenceEquals(cts, _searchCts))
                {
                    StatusMessage = "Search cancelled";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Search error: {ex.Message}";
            }
            finally
            {
                if (ReferenceEquals(cts, _searchCts))
                {
                    IsSearching = false;
                }
            }
        }

        private async Task SearchWithServiceAsync(CancellationToken token)
        {
            if (_searchService == null)
            {
                return;
            }

            var extensions = string.IsNullOrWhiteSpace(ExtensionFilter)
                ? null
                : ExtensionFilter.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim().TrimStart('*'))
                    .Where(e => e.Length > 0 && e != ".")
                    .Select(e => e.StartsWith(".") ? e : "." + e)
                    .ToArray();

            var searchRoot = string.IsNullOrWhiteSpace(SearchPath)
                ? null
                : Path.GetFullPath(SearchPath);

            var options = new Winhance.Core.Features.FileManager.Models.SearchOptions
            {
                Query = SearchQuery,
                UseRegex = UseRegex,
                CaseSensitive = CaseSensitive,
                IncludeHidden = false,
                Extensions = extensions ?? Array.Empty<string>(),
                FileExtensions = extensions ?? Array.Empty<string>(),
                // null lets the Core service fall back to its default search roots.
                SearchPaths = searchRoot == null ? null! : new[] { searchRoot },
                Recursive = SearchRecursively,
                IncludeSubdirectories = SearchRecursively,
                MinSize = MinSize,
                MaxSize = MaxSize,
                ModifiedAfter = ModifiedAfter,
                ModifiedBefore = ModifiedBefore,
                MaxResults = MaxResults,
                // Relevance sorts by Score; descending puts the best matches first.
                SortBy = Winhance.Core.Features.FileManager.Models.SearchSortMode.Relevance,
                SortDescending = true,
            };

            var results = await _searchService.SearchAsync(SearchQuery, options, token);
            token.ThrowIfCancellationRequested();

            foreach (var result in results)
            {
                token.ThrowIfCancellationRequested();

                // Indexed search is not scoped to SearchPaths and the file-system search always
                // recurses, so scope results to the requested folder here.
                if (searchRoot != null && !SearchResultScope.IsWithinScope(result.FullPath, searchRoot, SearchRecursively))
                {
                    continue;
                }

                var item = new SearchResultItem
                {
                    FullPath = result.FullPath,
                    Name = result.Name,
                    Directory = result.Directory,
                    IsDirectory = result.IsDirectory,
                    Size = result.Size,
                    DateModified = result.DateModified,
                    DateCreated = result.DateCreated,
                    Extension = result.Extension,
                    Score = result.Score,
                    MatchHighlight = result.MatchHighlight,
                };

                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    Results.Add(item);
                });
            }
        }

        private async Task SearchFallbackAsync(CancellationToken token)
        {
            var searchPath = string.IsNullOrEmpty(SearchPath)
                ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                : SearchPath;

            if (!Directory.Exists(searchPath))
            {
                StatusMessage = "Search path does not exist";
                return;
            }

            var extensions = string.IsNullOrWhiteSpace(ExtensionFilter)
                ? null
                : ExtensionFilter.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim().TrimStart('*').ToLowerInvariant())
                    .Where(e => e.Length > 0 && e != ".")
                    .Select(e => e.StartsWith(".") ? e : "." + e)
                    .ToHashSet();

            if (extensions != null && extensions.Count == 0)
            {
                extensions = null;
            }

            // Skip inaccessible folders instead of aborting the whole search on the first one.
            var enumerationOptions = new EnumerationOptions
            {
                RecurseSubdirectories = SearchRecursively,
                IgnoreInaccessible = true,
                AttributesToSkip = 0
            };

            var query = CaseSensitive ? SearchQuery : SearchQuery.ToLowerInvariant();
            System.Text.RegularExpressions.Regex? regex = null;
            if (UseRegex)
            {
                try
                {
                    // Build from the original pattern: lower-casing a regex changes its meaning (e.g. \D vs \d).
                    regex = new System.Text.RegularExpressions.Regex(
                        SearchQuery,
                        CaseSensitive
                            ? System.Text.RegularExpressions.RegexOptions.None
                            : System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }
                catch (ArgumentException ex)
                {
                    StatusMessage = $"Invalid regular expression: {ex.Message}";
                    return;
                }
            }

            var count = 0;

            await Task.Run(() =>
            {
                try
                {
                    var entries = Directory.EnumerateFileSystemEntries(searchPath, "*", enumerationOptions);

                    foreach (var entry in entries)
                    {
                        token.ThrowIfCancellationRequested();

                        if (count >= MaxResults)
                        {
                            break;
                        }

                        try
                        {
                            var name = Path.GetFileName(entry);
                            var compareName = CaseSensitive ? name : name.ToLowerInvariant();

                            // Name match
                            var matches = regex != null
                                ? regex.IsMatch(name)
                                : compareName.Contains(query);

                            if (!matches)
                            {
                                continue;
                            }

                            var isDirectory = Directory.Exists(entry);
                            var fileInfo = isDirectory ? null : new FileInfo(entry);

                            // Extension filter (files only)
                            if (!isDirectory && extensions != null)
                            {
                                var ext = Path.GetExtension(entry).ToLowerInvariant();
                                if (!extensions.Contains(ext))
                                {
                                    continue;
                                }
                            }

                            // Size filter (files only)
                            if (!isDirectory && fileInfo != null)
                            {
                                if (MinSize.HasValue && fileInfo.Length < MinSize.Value)
                                {
                                    continue;
                                }

                                if (MaxSize.HasValue && fileInfo.Length > MaxSize.Value)
                                {
                                    continue;
                                }
                            }

                            // Date filter
                            var modified = isDirectory
                                ? Directory.GetLastWriteTime(entry)
                                : fileInfo!.LastWriteTime;

                            if (ModifiedAfter.HasValue && modified < ModifiedAfter.Value)
                            {
                                continue;
                            }

                            if (ModifiedBefore.HasValue && modified > ModifiedBefore.Value)
                            {
                                continue;
                            }

                            var item = new SearchResultItem
                            {
                                FullPath = entry,
                                Name = name,
                                Directory = Path.GetDirectoryName(entry) ?? string.Empty,
                                IsDirectory = isDirectory,
                                Size = isDirectory ? 0 : fileInfo!.Length,
                                DateModified = modified,
                                DateCreated = isDirectory
                                    ? Directory.GetCreationTime(entry)
                                    : fileInfo!.CreationTime,
                                Extension = isDirectory ? string.Empty : Path.GetExtension(entry),
                            };

                            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                            {
                                Results.Add(item);
                            });

                            count++;

                            // Update status periodically
                            if (count % 100 == 0)
                            {
                                var currentCount = count;
                                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                                {
                                    StatusMessage = $"Found {currentCount:N0} results...";
                                });
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // Skip inaccessible items
                        }
                        catch (IOException)
                        {
                            // Skip items with IO errors
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        StatusMessage = "Access denied to some directories";
                    });
                }
            }, token);
        }

        [RelayCommand]
        public void CancelSearch()
        {
            _searchCts?.Cancel();
        }

        [RelayCommand]
        public void ClearResults()
        {
            Results.Clear();
            TotalResults = 0;
            TotalSize = 0;
            HasResults = false;
            StatusMessage = "Results cleared";
        }

        [RelayCommand]
        public void OpenResult(SearchResultItem? item)
        {
            if (item == null)
            {
                return;
            }

            if (item.IsDirectory)
            {
                NavigateToPathRequested?.Invoke(this, item.FullPath);
            }
            else
            {
                OpenFileRequested?.Invoke(this, item.FullPath);
            }
        }

        [RelayCommand]
        public void NavigateToResult(SearchResultItem? item)
        {
            if (item == null)
            {
                return;
            }

            var path = item.IsDirectory ? item.FullPath : item.Directory;
            NavigateToPathRequested?.Invoke(this, path);
        }

        [RelayCommand]
        public void CopyPath(SearchResultItem? item)
        {
            if (item == null)
            {
                return;
            }

            try
            {
                System.Windows.Clipboard.SetText(item.FullPath);
                StatusMessage = "Path copied to clipboard";
            }
            catch
            {
                StatusMessage = "Failed to copy path";
            }
        }

        [RelayCommand]
        public void SelectAll()
        {
            foreach (var item in Results)
            {
                item.IsSelected = true;
            }
        }

        [RelayCommand]
        public void SelectNone()
        {
            foreach (var item in Results)
            {
                item.IsSelected = false;
            }
        }

        [RelayCommand]
        public void InvertSelection()
        {
            foreach (var item in Results)
            {
                item.IsSelected = !item.IsSelected;
            }
        }

        public IEnumerable<SearchResultItem> GetSelectedItems()
        {
            return Results.Where(r => r.IsSelected);
        }

        partial void OnSearchQueryChanged(string value)
        {
            // Auto-search after short delay (debounce) so each keystroke does not start a full search
            _debounceCts?.Cancel();
            if (value.Length < 2)
            {
                return;
            }

            var cts = new CancellationTokenSource();
            _debounceCts = cts;
            _ = DebouncedSearchAsync(cts.Token);
        }

        private async Task DebouncedSearchAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(400, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (!token.IsCancellationRequested)
            {
                await SearchAsync();
            }
        }

        private static string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }

    /// <summary>
    /// Represents a single search result item.
    /// </summary>
    public partial class SearchResultItem : ObservableObject
    {
        [ObservableProperty]
        private string _fullPath = string.Empty;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _directory = string.Empty;

        [ObservableProperty]
        private bool _isDirectory;

        [ObservableProperty]
        private long _size;

        [ObservableProperty]
        private DateTime _dateModified;

        [ObservableProperty]
        private DateTime _dateCreated;

        [ObservableProperty]
        private string _extension = string.Empty;

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private double _score;

        [ObservableProperty]
        private string _matchHighlight = string.Empty;

        public string SizeDisplay => IsDirectory ? "--" : FormatSize(Size);

        public string TypeDisplay => IsDirectory ? "Folder" : (string.IsNullOrEmpty(Extension) ? "File" : Extension.TrimStart('.').ToUpperInvariant() + " File");

        private static string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }
}
