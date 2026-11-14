using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Winhance.WPF.Features.FileManager.ViewModels;

namespace Winhance.WPF.Features.FileManager.Views
{
    public partial class DualPaneBrowserView : UserControl
    {
        public DualPaneBrowserView()
        {
            InitializeComponent();
        }

        private DualPaneBrowserViewModel? ViewModel => DataContext as DualPaneBrowserViewModel;

        private bool IsLeft(object sender) => ReferenceEquals(sender, LeftPaneList);

        private void ActivatePane(object sender)
        {
            if (ViewModel != null)
            {
                ViewModel.IsLeftPaneActive = IsLeft(sender);
            }
        }

        private void Pane_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => ActivatePane(sender);

        // Right-click must activate the pane before the context menu command runs.
        private void Pane_PreviewMouseDown(object sender, MouseButtonEventArgs e) => ActivatePane(sender);

        /// <summary>
        /// ListView.SelectedItems is read-only, so the full multi-selection is mirrored into the
        /// view model's SelectedLeftItems/SelectedRightItems here (commands read those collections).
        /// </summary>
        private void Pane_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null || sender is not ListView list)
            {
                return;
            }

            var target = IsLeft(sender) ? ViewModel.SelectedLeftItems : ViewModel.SelectedRightItems;
            var selected = list.SelectedItems.OfType<FileItemViewModel>().Where(i => !i.IsParentDirectory).ToList();
            target.Clear();
            foreach (var item in selected)
            {
                target.Add(item);
            }
        }

        private void Pane_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2 && Keyboard.Modifiers == ModifierKeys.None)
            {
                ActivatePane(sender);
                PromptRename();
                e.Handled = true;
            }
        }

        private void Rename_Click(object sender, RoutedEventArgs e) => PromptRename();

        private void PromptRename()
        {
            var vm = ViewModel;
            if (vm == null)
            {
                return;
            }

            vm.StartRenameCommand.Execute(null);
            var item = vm.RenamingItem;
            if (item == null)
            {
                return;
            }

            var newName = ShowRenameDialog(item.Name, item.IsDirectory);
            if (newName == null)
            {
                vm.RenamingItem = null;
                return;
            }

            vm.CompleteRenameCommand.Execute(newName);
        }

        /// <summary>Shows a modal rename prompt. Returns the new name, or null if cancelled.</summary>
        private string? ShowRenameDialog(string currentName, bool isDirectory)
        {
            var textBox = new TextBox
            {
                Text = currentName,
                MinWidth = 320,
                Margin = new Thickness(0, 6, 0, 10),
            };
            var error = new TextBlock
            {
                Foreground = System.Windows.Media.Brushes.IndianRed,
                Visibility = Visibility.Collapsed,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8),
            };
            var ok = new Button { Content = "Rename", IsDefault = true, MinWidth = 80, Margin = new Thickness(0, 0, 8, 0) };
            var cancel = new Button { Content = "Cancel", IsCancel = true, MinWidth = 80 };
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);

            var panel = new StackPanel { Margin = new Thickness(16) };
            panel.Children.Add(new TextBlock { Text = isDirectory ? "New folder name:" : "New file name:" });
            panel.Children.Add(textBox);
            panel.Children.Add(error);
            panel.Children.Add(buttons);

            var dialog = new Window
            {
                Title = "Rename",
                Content = panel,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ShowInTaskbar = false,
                Owner = Window.GetWindow(this),
            };

            ok.Click += (_, _) =>
            {
                var name = textBox.Text.Trim();
                string? problem = null;
                if (name.Length == 0)
                {
                    problem = "Name cannot be empty.";
                }
                else if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    problem = "Name contains characters that are not allowed: \\ / : * ? \" < > |";
                }
                else if (name is "." or "..")
                {
                    problem = "That name is reserved.";
                }

                if (problem != null)
                {
                    error.Text = problem;
                    error.Visibility = Visibility.Visible;
                    return;
                }

                dialog.DialogResult = true;
            };

            dialog.Loaded += (_, _) =>
            {
                textBox.Focus();
                // Select the base name so typing replaces it but keeps the extension.
                var dot = isDirectory ? -1 : currentName.LastIndexOf('.');
                textBox.Select(0, dot > 0 ? dot : currentName.Length);
            };

            return dialog.ShowDialog() == true ? textBox.Text.Trim() : null;
        }
    }
}
