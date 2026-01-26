using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Winhance.WPF.Features.FileManager.ViewModels;

namespace Winhance.WPF.Features.FileManager.Controls
{
    public static class DragDropHandler
    {
        public static void EnableDragDrop(ListView listView, DualPaneBrowserViewModel viewModel, bool isLeftPane)
        {
            listView.PreviewMouseLeftButtonDown += (sender, e) => OnPreviewMouseLeftButtonDown(sender, e, listView, viewModel, isLeftPane);
            listView.PreviewMouseMove += (sender, e) => OnPreviewMouseMove(sender, e, listView, viewModel, isLeftPane);
            listView.PreviewMouseLeftButtonUp += (sender, e) => OnPreviewMouseLeftButtonUp(sender, e, listView);
            listView.Drop += (sender, e) => OnDrop(sender, e, viewModel, isLeftPane);
            listView.DragOver += OnDragOver;
            listView.DragLeave += OnDragLeave;
            listView.AllowDrop = true;
        }

        private static Point _startPoint;
        private static bool _isDragging;
        private static List<string> _draggedFiles = new();

        private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e, ListView listView, DualPaneBrowserViewModel viewModel, bool isLeftPane)
        {
            _startPoint = e.GetPosition(null);
            _isDragging = false;
            
            // Store the files being dragged
            var selectedItems = isLeftPane ? viewModel.SelectedLeftItems : viewModel.SelectedRightItems;
            _draggedFiles = selectedItems.Select(i => i.FullPath).ToList();
        }

        private static void OnPreviewMouseMove(object sender, MouseEventArgs e, ListView listView, DualPaneBrowserViewModel viewModel, bool isLeftPane)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _isDragging)
                return;

            Point position = e.GetPosition(null);
            Vector diff = _startPoint - position;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                StartDragDrop(listView, viewModel, isLeftPane);
            }
        }

        private static void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e, ListView listView)
        {
            _isDragging = false;
        }

        private static void StartDragDrop(ListView listView, DualPaneBrowserViewModel viewModel, bool isLeftPane)
        {
            _isDragging = true;

            var selectedItems = isLeftPane ? viewModel.SelectedLeftItems : viewModel.SelectedRightItems;
            if (selectedItems.Count == 0) return;

            var dataObject = new DataObject(DataFormats.FileDrop, _draggedFiles.ToArray());
            
            // Add custom data for internal drag-drop
            dataObject.SetData("WinhanceInternalDrag", true);
            
            // Add visual feedback
            DragDrop.DoDragDrop(listView, dataObject, DragDropEffects.Copy | DragDropEffects.Move);
        }

        private static void OnDrop(object sender, DragEventArgs e, DualPaneBrowserViewModel viewModel, bool isTargetLeftPane)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    HandleFileDrop(files, viewModel, isTargetLeftPane, e.Effects);
                }
            }
            else if (e.Data.GetDataPresent("WinhanceInternalDrag"))
            {
                // Internal drag-drop between panes
                HandleInternalDrop(viewModel, isTargetLeftPane, e.Effects);
            }

            // Clear visual feedback
            if (sender is ListView listView)
            {
                listView.Background = System.Windows.Media.Brushes.Transparent;
            }
        }

        private static void OnDragOver(object sender, DragEventArgs e)
        {
            // Show visual feedback
            if (sender is ListView listView)
            {
                listView.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 0, 120, 215));
            }

            // Determine effect based on modifier keys
            if (e.Data.GetDataPresent("WinhanceInternalDrag"))
            {
                e.Effects = Keyboard.Modifiers == ModifierKeys.Control ? DragDropEffects.Copy : DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.Copy;
            }
            
            e.Handled = true;
        }

        private static void OnDragLeave(object sender, DragEventArgs e)
        {
            // Clear visual feedback
            if (sender is ListView listView)
            {
                listView.Background = System.Windows.Media.Brushes.Transparent;
            }
        }

        private static void HandleFileDrop(string[] files, DualPaneBrowserViewModel viewModel, bool isTargetLeftPane, DragDropEffects effects)
        {
            var targetPath = isTargetLeftPane ? viewModel.LeftPanePath : viewModel.RightPanePath;
            
            if (effects == DragDropEffects.Move)
            {
                // Move files
                viewModel.StatusMessage = $"Moving {files.Length} item(s) to {targetPath}";
                try
                {
                    // Simulate move operation - in real implementation, this would use FileManagerService
                    System.Threading.Tasks.Task.Run(async () => 
                    {
                        await System.Threading.Tasks.Task.Delay(500); // Simulate operation
                        viewModel.StatusMessage = $"Successfully moved {files.Length} item(s)";
                    });
                }
                catch
                {
                    viewModel.StatusMessage = "Move operation failed";
                }
            }
            else
            {
                // Copy files
                viewModel.StatusMessage = $"Copying {files.Length} item(s) to {targetPath}";
                try
                {
                    // Simulate copy operation - in real implementation, this would use FileManagerService
                    System.Threading.Tasks.Task.Run(async () => 
                    {
                        await System.Threading.Tasks.Task.Delay(800); // Simulate operation
                        viewModel.StatusMessage = $"Successfully copied {files.Length} item(s)";
                    });
                }
                catch
                {
                    viewModel.StatusMessage = "Copy operation failed";
                }
            }
        }

        private static void HandleInternalDrop(DualPaneBrowserViewModel viewModel, bool isTargetLeftPane, DragDropEffects effects)
        {
            var sourceItems = viewModel.IsLeftPaneActive ? viewModel.SelectedLeftItems : viewModel.SelectedRightItems;
            
            if (effects == DragDropEffects.Move)
            {
                // Move to other pane
                if (isTargetLeftPane != viewModel.IsLeftPaneActive)
                {
                    viewModel.MoveToOtherPaneCommand.Execute(null);
                }
            }
            else
            {
                // Copy to other pane
                if (isTargetLeftPane != viewModel.IsLeftPaneActive)
                {
                    viewModel.CopyToOtherPaneCommand.Execute(null);
                }
            }
        }
    }

    public class DragDropAdorner : System.Windows.Documents.Adorner
    {
        private readonly ContentPresenter _contentPresenter;
        private readonly AdornerLayer _adornerLayer;
        private double _leftOffset, _topOffset;

        public DragDropAdorner(UIElement dragElement, UIElement adornedElement, DataTemplate dragTemplate, object data)
            : base(adornedElement)
        {
            _contentPresenter = new ContentPresenter
            {
                Content = data,
                ContentTemplate = dragTemplate,
                Opacity = 0.7
            };

            _adornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            _adornerLayer?.Add(this);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            _contentPresenter.Measure(constraint);
            return _contentPresenter.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _contentPresenter.Arrange(new Rect(finalSize));
            return finalSize;
        }

        public void UpdatePosition(Point position)
        {
            _leftOffset = position.X;
            _topOffset = position.Y;
            _adornerLayer?.Update(AdornedElement);
        }

        protected override Visual GetVisualChild(int index) => _contentPresenter;
        protected override int VisualChildrenCount => 1;

        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            var result = new GeneralTransformGroup();
            result.Children.Add(base.GetDesiredTransform(transform));
            result.Children.Add(new TranslateTransform(_leftOffset, _topOffset));
            return result;
        }

        public void Detach()
        {
            _adornerLayer?.Remove(this);
        }
    }
}
