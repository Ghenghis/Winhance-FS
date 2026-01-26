using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Winhance.WPF.Features.FileManager.ViewModels;

namespace Winhance.WPF.Features.FileManager.Controls
{
    public class KeyboardShortcutManager
    {
        private readonly DualPaneBrowserViewModel _viewModel;
        private readonly Dictionary<KeyGesture, ICommand> _shortcuts;

        public KeyboardShortcutManager(DualPaneBrowserViewModel viewModel)
        {
            _viewModel = viewModel;
            _shortcuts = new Dictionary<KeyGesture, ICommand>();
            
            InitializeShortcuts();
        }

        private void InitializeShortcuts()
        {
            // File operations
            _shortcuts[new KeyGesture(Key.C, ModifierKeys.Control)] = _viewModel.CopyToClipboardCommand;
            _shortcuts[new KeyGesture(Key.X, ModifierKeys.Control)] = _viewModel.CutToClipboardCommand;
            _shortcuts[new KeyGesture(Key.V, ModifierKeys.Control)] = _viewModel.PasteFromClipboardCommand;
            _shortcuts[new KeyGesture(Key.Delete)] = _viewModel.DeleteCommand;
            _shortcuts[new KeyGesture(Key.F2)] = _viewModel.StartRenameCommand;
            _shortcuts[new KeyGesture(Key.F5)] = _viewModel.RefreshCommand;
            _shortcuts[new KeyGesture(Key.N, ModifierKeys.Control | ModifierKeys.Shift)] = _viewModel.NewFolderCommand;
            
            // Navigation
            _shortcuts[new KeyGesture(Key.Up, ModifierKeys.Alt)] = _viewModel.NavigateUpCommand;
            _shortcuts[new KeyGesture(Key.Left, ModifierKeys.Alt)] = _viewModel.GoBackCommand;
            _shortcuts[new KeyGesture(Key.Right, ModifierKeys.Alt)] = _viewModel.GoForwardCommand;
            _shortcuts[new KeyGesture(Key.Back)] = _viewModel.GoBackCommand;
            
            // Selection
            _shortcuts[new KeyGesture(Key.A, ModifierKeys.Control)] = _viewModel.SelectCommand;
            _shortcuts[new KeyGesture(Key.Escape)] = _viewModel.ClearSelectionCommand;
            _shortcuts[new KeyGesture(Key.I, ModifierKeys.Control)] = _viewModel.InvertSelectionCommand;
            
            // View modes
            _shortcuts[new KeyGesture(Key.D1, ModifierKeys.Control)] = _viewModel.SetDetailsViewCommand;
            _shortcuts[new KeyGesture(Key.D2, ModifierKeys.Control)] = _viewModel.SetIconsViewCommand;
            _shortcuts[new KeyGesture(Key.D3, ModifierKeys.Control)] = _viewModel.SetListViewCommand;
            _shortcuts[new KeyGesture(Key.D4, ModifierKeys.Control)] = _viewModel.SetTilesViewCommand;
            
            // Tabs
            _shortcuts[new KeyGesture(Key.T, ModifierKeys.Control)] = _viewModel.NewTabCommand;
            _shortcuts[new KeyGesture(Key.W, ModifierKeys.Control)] = _viewModel.CloseTabCommand;
            _shortcuts[new KeyGesture(Key.Tab, ModifierKeys.Control)] = _viewModel.NextTabCommand;
            _shortcuts[new KeyGesture(Key.Tab, ModifierKeys.Control | ModifierKeys.Shift)] = _viewModel.PreviousTabCommand;
            
            // Search
            _shortcuts[new KeyGesture(Key.F, ModifierKeys.Control)] = _viewModel.FocusSearchCommand;
            
            // Favorites
            _shortcuts[new KeyGesture(Key.B, ModifierKeys.Control)] = _viewModel.AddToFavoritesCommand;
            
            // Advanced operations
            _shortcuts[new KeyGesture(Key.F3)] = _viewModel.QuickViewCommand;
            _shortcuts[new KeyGesture(Key.F4)] = _viewModel.EditItemCommand;
            _shortcuts[new KeyGesture(Key.F6)] = _viewModel.MoveToOtherPaneCommand;
            _shortcuts[new KeyGesture(Key.F7)] = _viewModel.NewFolderCommand;
            
            // Quick access
            _shortcuts[new KeyGesture(Key.D, ModifierKeys.Control)] = _viewModel.ToggleQuickAccessCommand;
            
            // Command palette
            _shortcuts[new KeyGesture(Key.P, ModifierKeys.Control | ModifierKeys.Shift)] = _viewModel.ShowCommandPaletteCommand;
        }

        public bool HandleKey(KeyEventArgs e)
        {
            var key = e.Key;
            var modifiers = Keyboard.Modifiers;
            
            // Handle special cases for navigation keys
            if (modifiers == ModifierKeys.None)
            {
                switch (key)
                {
                    case Key.Up:
                        _viewModel.NavigateUpKeyCommand.Execute(null);
                        return true;
                    case Key.Down:
                        _viewModel.NavigateDownCommand.Execute(null);
                        return true;
                    case Key.Left:
                        _viewModel.NavigateLeftCommand.Execute(null);
                        return true;
                    case Key.Right:
                        _viewModel.NavigateRightCommand.Execute(null);
                        return true;
                    case Key.Home:
                        _viewModel.NavigateHomeCommand.Execute(null);
                        return true;
                    case Key.End:
                        _viewModel.NavigateEndCommand.Execute(null);
                        return true;
                    case Key.PageUp:
                        _viewModel.NavigatePageUpCommand.Execute(null);
                        return true;
                    case Key.PageDown:
                        _viewModel.NavigatePageDownCommand.Execute(null);
                        return true;
                    case Key.Insert:
                        ToggleMarkOnSelected();
                        return true;
                    case Key.Space:
                        QuickPreview();
                        return true;
                    case Key.Return:
                        OpenSelected();
                        return true;
                }
            }
            
            // Check for registered shortcuts
            var gesture = new KeyGesture(key, modifiers);
            if (_shortcuts.TryGetValue(gesture, out var command))
            {
                if (command.CanExecute(null))
                {
                    command.Execute(null);
                    return true;
                }
            }
            
            return false;
        }

        private void ToggleMarkOnSelected()
        {
            var selectedItems = _viewModel.IsLeftPaneActive ? _viewModel.SelectedLeftItems : _viewModel.SelectedRightItems;
            if (selectedItems != null && selectedItems.Count > 0)
            {
                foreach (var item in selectedItems)
                {
                    item.IsMarked = !item.IsMarked;
                }
                _viewModel.StatusMessage = $"Mark toggled on {selectedItems.Count} item(s)";
            }
            else
            {
                _viewModel.StatusMessage = "No items selected to mark";
            }
        }

        private void QuickPreview()
        {
            var selectedItem = _viewModel.IsLeftPaneActive ? _viewModel.SelectedLeftItem : _viewModel.SelectedRightItem;
            if (selectedItem != null)
            {
                _viewModel.StatusMessage = $"Previewing: {selectedItem.Name}";
                _viewModel.QuickViewCommand.Execute(selectedItem);
            }
            else
            {
                _viewModel.StatusMessage = "No item selected for preview";
            }
        }

        private void OpenSelected()
        {
            var item = _viewModel.IsLeftPaneActive ? _viewModel.SelectedLeftItem : _viewModel.SelectedRightItem;
            if (item != null)
            {
                _viewModel.OpenItemCommand.Execute(item);
            }
        }

        public static KeyboardShortcutManager Attach(UIElement element, DualPaneBrowserViewModel viewModel)
        {
            var manager = new KeyboardShortcutManager(viewModel);
            
            element.KeyDown += (s, e) =>
            {
                if (manager.HandleKey(e))
                {
                    e.Handled = true;
                }
            };
            
            return manager;
        }
    }

    public class CommandPaletteViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<CommandItem> _commands = new();

        [ObservableProperty]
        private ObservableCollection<CommandItem> _filteredCommands = new();

        [ObservableProperty]
        private CommandItem? _selectedCommand;

        public CommandPaletteViewModel()
        {
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            Commands.Clear();
            
            // File operations
            Commands.Add(new CommandItem { Name = "Copy", Shortcut = "Ctrl+C", Description = "Copy selected files" });
            Commands.Add(new CommandItem { Name = "Cut", Shortcut = "Ctrl+X", Description = "Cut selected files" });
            Commands.Add(new CommandItem { Name = "Paste", Shortcut = "Ctrl+V", Description = "Paste files" });
            Commands.Add(new CommandItem { Name = "Delete", Shortcut = "Del", Description = "Delete selected files" });
            Commands.Add(new CommandItem { Name = "Rename", Shortcut = "F2", Description = "Rename selected item" });
            
            // Navigation
            Commands.Add(new CommandItem { Name = "Back", Shortcut = "Alt+Left", Description = "Navigate back" });
            Commands.Add(new CommandItem { Name = "Forward", Shortcut = "Alt+Right", Description = "Navigate forward" });
            Commands.Add(new CommandItem { Name = "Up", Shortcut = "Alt+Up", Description = "Navigate to parent" });
            Commands.Add(new CommandItem { Name = "Refresh", Shortcut = "F5", Description = "Refresh current view" });
            
            // Selection
            Commands.Add(new CommandItem { Name = "Select All", Shortcut = "Ctrl+A", Description = "Select all items" });
            Commands.Add(new CommandItem { Name = "Invert Selection", Shortcut = "Ctrl+I", Description = "Invert selection" });
            Commands.Add(new CommandItem { Name = "Toggle Mark", Shortcut = "Ctrl+M", Description = "Mark/unmark selected items" });
            
            // Special
            Commands.Add(new CommandItem { Name = "Quick Preview", Shortcut = "Space", Description = "Quick preview selected item" });
            Commands.Add(new CommandItem { Name = "Properties", Shortcut = "Alt+Enter", Description = "Show properties" });
            Commands.Add(new CommandItem { Name = "Command Palette", Shortcut = "Ctrl+Shift+P", Description = "Show command palette" });
            Commands.Add(new CommandItem { Name = "Delete", Shortcut = "Del", Description = "Delete selected files" });
            Commands.Add(new CommandItem { Name = "Rename", Shortcut = "F2", Description = "Rename selected file" });
            Commands.Add(new CommandItem { Name = "New Folder", Shortcut = "Ctrl+Shift+N", Description = "Create new folder" });
            // Add more commands...
            
            FilteredCommands = new ObservableCollection<CommandItem>(Commands);
        }

        partial void OnSearchTextChanged(string value)
        {
            FilteredCommands.Clear();
            
            if (string.IsNullOrWhiteSpace(value))
            {
                foreach (var cmd in Commands)
                {
                    FilteredCommands.Add(cmd);
                }
            }
            else
            {
                var search = value.ToLowerInvariant();
                foreach (var cmd in Commands.Where(c => 
                    c.Name.ToLowerInvariant().Contains(search) || 
                    c.Description.ToLowerInvariant().Contains(search)))
                {
                    FilteredCommands.Add(cmd);
                }
            }
            
            SelectedCommand = FilteredCommands.FirstOrDefault();
        }
    }

    public class CommandItem
    {
        public string Name { get; set; } = string.Empty;
        public string Shortcut { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ICommand? Command { get; set; }
    }
}
