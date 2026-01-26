using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Winhance.WPF.Features.FileManager.ViewModels;

namespace Winhance.WPF.Features.FileManager.Controls
{
    /// <summary>
    /// Interaction logic for FileListControl.xaml
    /// </summary>
    public partial class FileListControl : UserControl
    {
        public FileListControl()
        {
            InitializeComponent();
            Loaded += FileListControl_Loaded;
        }

        private void FileListControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Setup data context bindings
            if (DataContext is FileListViewModel viewModel)
            {
                // Bind SelectedItems property (two-way)
                var selectedItemsBinding = new Binding("SelectedItems")
                {
                    Source = viewModel,
                    Mode = BindingMode.TwoWay
                };
                FileListView.SetBinding(SelectionListView.SelectedItemsProperty, selectedItemsBinding);
            }
        }

        private void FileListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListView listView && listView.SelectedItem is FileItemViewModel item)
            {
                if (item.IsDirectory)
                {
                    // Navigate to directory
                    (DataContext as FileListViewModel)?.LoadPathAsync(item.FullPath);
                }
                else
                {
                    // Open file
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = item.FullPath,
                        UseShellExecute = true
                    });
                }
            }
        }

        private void FileListView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is ListView listView && DataContext is FileListViewModel viewModel)
            {
                switch (e.Key)
                {
                    case Key.Enter:
                        if (listView.SelectedItem is FileItemViewModel item)
                        {
                            if (item.IsDirectory)
                            {
                                viewModel.LoadPathAsync(item.FullPath);
                            }
                            else
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = item.FullPath,
                                    UseShellExecute = true
                                });
                            }
                        }
                        break;
                    case Key.A when e.KeyboardModifiers == ModifierKeys.Control:
                        viewModel.SelectAllCommand.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.Escape:
                        viewModel.ClearSelectionCommand.Execute(null);
                        e.Handled = true;
                        break;
                }
            }
        }

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (sender is GridViewColumnHeader header && header.Column.Header is string columnName)
            {
                (DataContext as FileListViewModel)?.SortByColumn(columnName);
            }
        }

        private void FileListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Handle drag initiation
            if (sender is ListView listView && e.OriginalSource is FrameworkElement element)
            {
                var item = FindAncestor<ListViewItem>(element);
                if (item != null && item.Content is FileItemViewModel fileItem)
                {
                    // Start drag operation
                    DragDrop.DoDragDrop(listView, fileItem, DragDropEffects.Copy | DragDropEffects.Move);
                }
            }
        }

        private void FileListView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // Handle drag preview
        }

        private void FileListView_Drop(object sender, DragEventArgs e)
        {
            // Handle drop
            if (e.Data.GetDataPresent(typeof(FileItemViewModel)))
            {
                var item = e.Data.GetData(typeof(FileItemViewModel)) as FileItemViewModel;
                // Process dropped item
            }
        }

        private void FileListView_DragOver(object sender, DragEventArgs e)
        {
            // Handle drag over
            if (e.Data.GetDataPresent(typeof(FileItemViewModel)))
            {
                e.Effects = DragDropEffects.Copy | DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }
    }
}
