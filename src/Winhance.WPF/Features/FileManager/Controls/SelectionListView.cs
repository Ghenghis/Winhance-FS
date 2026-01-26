using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Winhance.WPF.Features.FileManager.Controls
{
    public class SelectionListView : ListView
    {
        private Point _startPoint;
        private Rectangle _selectionRect;
        private Canvas _selectionCanvas;
        private bool _isSelecting;

        public SelectionListView()
        {
            this.Loaded += SelectionListView_Loaded;
        }

        private void SelectionListView_Loaded(object sender, RoutedEventArgs e)
        {
            // Add selection canvas for rubber band
            _selectionCanvas = new Canvas
            {
                IsHitTestVisible = false,
                Background = Brushes.Transparent
            };
            
            // Insert canvas as first child in scroll viewer
            if (this.Template != null)
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(this);
                if (scrollViewer != null)
                {
                    var grid = FindVisualChild<Grid>(scrollViewer);
                    if (grid != null)
                    {
                        grid.Children.Insert(0, _selectionCanvas);
                    }
                }
            }
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);
            
            if (e.Source == this || e.Source is Canvas)
            {
                _startPoint = e.GetPosition(this);
                _isSelecting = true;
                CaptureMouse();
                
                // Clear selection if clicking empty space
                if (Keyboard.Modifiers != ModifierKeys.Control && Keyboard.Modifiers != ModifierKeys.Shift)
                {
                    SelectedItems.Clear();
                }
            }
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);
            
            if (_isSelecting && IsMouseCaptured)
            {
                Point currentPoint = e.GetPosition(this);
                UpdateSelectionRect(_startPoint, currentPoint);
                
                // Select items within rectangle
                SelectItemsInRect(_startPoint, currentPoint);
            }
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);
            
            if (_isSelecting)
            {
                _isSelecting = false;
                ReleaseMouseCapture();
                ClearSelectionRect();
            }
        }

        private void UpdateSelectionRect(Point start, Point end)
        {
            if (_selectionRect == null)
            {
                _selectionRect = new Rectangle
                {
                    Fill = new SolidColorBrush(Color.FromArgb(50, 0, 120, 215)),
                    Stroke = new SolidColorBrush(Color.FromArgb(150, 0, 120, 215)),
                    StrokeDashArray = new DoubleCollection { 2, 2 }
                };
                _selectionCanvas.Children.Add(_selectionRect);
            }

            double x = Math.Min(start.X, end.X);
            double y = Math.Min(start.Y, end.Y);
            double width = Math.Abs(end.X - start.X);
            double height = Math.Abs(end.Y - start.Y);

            Canvas.SetLeft(_selectionRect, x);
            Canvas.SetTop(_selectionRect, y);
            _selectionRect.Width = width;
            _selectionRect.Height = height;
        }

        private void ClearSelectionRect()
        {
            if (_selectionRect != null)
            {
                _selectionCanvas.Children.Remove(_selectionRect);
                _selectionRect = null;
            }
        }

        private void SelectItemsInRect(Point start, Point end)
        {
            var rect = new Rect(
                Math.Min(start.X, end.X),
                Math.Min(start.Y, end.Y),
                Math.Abs(end.X - start.X),
                Math.Abs(end.Y - start.Y));

            foreach (var item in Items)
            {
                var container = ItemContainerGenerator.ContainerFromItem(item) as ListViewItem;
                if (container != null)
                {
                    var bounds = container.TransformToAncestor(this).TransformBounds(new Rect(0, 0, container.ActualWidth, container.ActualHeight));
                    
                    if (rect.IntersectsWith(bounds))
                    {
                        if (!SelectedItems.Contains(item))
                        {
                            if (Keyboard.Modifiers == ModifierKeys.Control)
                            {
                                SelectedItems.Add(item);
                            }
                            else if (Keyboard.Modifiers == ModifierKeys.Shift)
                            {
                                // Range selection logic
                                HandleRangeSelection(item);
                            }
                            else
                            {
                                SelectedItems.Add(item);
                            }
                        }
                    }
                }
            }
        }

        private void HandleRangeSelection(object toItem)
        {
            if (SelectedItems.Count > 0)
            {
                var lastSelected = SelectedItems[SelectedItems.Count - 1];
                int fromIndex = Items.IndexOf(lastSelected);
                int toIndex = Items.IndexOf(toItem);
                
                if (fromIndex >= 0 && toIndex >= 0)
                {
                    int min = Math.Min(fromIndex, toIndex);
                    int max = Math.Max(fromIndex, toIndex);
                    
                    for (int i = min; i <= max; i++)
                    {
                        if (!SelectedItems.Contains(Items[i]))
                        {
                            SelectedItems.Add(Items[i]);
                        }
                    }
                }
            }
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    return result;
                }
                
                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                {
                    return descendant;
                }
            }
            return null;
        }
    }
}
