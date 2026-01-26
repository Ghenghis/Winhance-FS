using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Winhance.WPF.Features.FileManager.ViewModels;

namespace Winhance.WPF.Features.FileManager.Controls
{
    /// <summary>
    /// Interaction logic for BreadcrumbBar.xaml
    /// </summary>
    public partial class BreadcrumbBar : UserControl
    {
        public BreadcrumbBar()
        {
            InitializeComponent();
            Loaded += BreadcrumbBar_Loaded;
        }

        private void BreadcrumbBar_Loaded(object sender, RoutedEventArgs e)
        {
            // Focus handling
            this.IsVisibleChanged += BreadcrumbBar_IsVisibleChanged;
        }

        private void BreadcrumbBar_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue && DataContext is BreadcrumbBarViewModel viewModel)
            {
                // Update path when control becomes visible
                if (!string.IsNullOrEmpty(viewModel.CurrentPath))
                {
                    _ = viewModel.SetPathAsync(viewModel.CurrentPath);
                }
            }
        }

        private void PathTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is TextBox textBox && DataContext is BreadcrumbBarViewModel viewModel)
            {
                switch (e.Key)
                {
                    case Key.Enter:
                        viewModel.AcceptEditCommand.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.Escape:
                        viewModel.CancelEditCommand.Execute(null);
                        e.Handled = true;
                        break;
                }
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            // Handle Ctrl+L to focus address bar
            if (e.Key == Key.L && e.KeyboardModifiers == ModifierKeys.Control)
            {
                if (DataContext is BreadcrumbBarViewModel viewModel)
                {
                    viewModel.StartEditCommand.Execute(null);
                    e.Handled = true;
                }
            }
            
            base.OnPreviewKeyDown(e);
        }
    }
}
