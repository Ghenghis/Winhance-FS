using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Winhance.WPF.Features.FileManager.ViewModels;

namespace Winhance.WPF.Features.FileManager.Views
{
    /// <summary>
    /// Search tab view.
    /// </summary>
    public partial class FastSearchView : UserControl
    {
        public FastSearchView()
        {
            InitializeComponent();
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            QueryBox.Focus();
            if (DataContext is FastSearchViewModel vm)
            {
                await vm.EnsureLoadedAsync();
            }
        }

        private void OnResultDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is FastSearchViewModel vm && ResultsList.SelectedItem is FastSearchRow row)
            {
                vm.OpenItemCommand.Execute(row);
            }
        }
    }
}
