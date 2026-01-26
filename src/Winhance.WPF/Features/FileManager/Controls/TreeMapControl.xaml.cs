using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Winhance.WPF.Features.FileManager.Controls
{
    public partial class TreeMapControl : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable<TreeMapItem>),
                typeof(TreeMapControl), new PropertyMetadata(null, OnItemsSourceChanged));

        public IEnumerable<TreeMapItem>? ItemsSource
        {
            get => (IEnumerable<TreeMapItem>?)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public event EventHandler<TreeMapItem>? ItemClicked;
        public event EventHandler<TreeMapItem>? ItemDoubleClicked;

        private static readonly Color[] CategoryColors = new[]
        {
            Color.FromRgb(66, 133, 244),   // Blue - Documents
            Color.FromRgb(52, 168, 83),    // Green - Images
            Color.FromRgb(251, 188, 5),    // Yellow - Videos
            Color.FromRgb(234, 67, 53),    // Red - Audio
            Color.FromRgb(155, 89, 182),   // Purple - Archives
            Color.FromRgb(26, 188, 156),   // Teal - Code
            Color.FromRgb(241, 196, 15),   // Orange - Data
            Color.FromRgb(149, 165, 166),  // Gray - Other
        };

        public TreeMapControl()
        {
            InitializeComponent();
            SizeChanged += OnSizeChanged;
            _zoomLevel = 1.0;
        }

        private double _zoomLevel = 1.0;
        private const double MinZoom = 0.5;
        private const double MaxZoom = 3.0;
        private const double ZoomStep = 0.2;

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            if (_zoomLevel < MaxZoom)
            {
                _zoomLevel = Math.Min(MaxZoom, _zoomLevel + ZoomStep);
                UpdateZoom();
            }
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            if (_zoomLevel > MinZoom)
            {
                _zoomLevel = Math.Max(MinZoom, _zoomLevel - ZoomStep);
                UpdateZoom();
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _zoomLevel = 1.0;
            UpdateZoom();
        }

        private void UpdateZoom()
        {
            ZoomTransform.ScaleX = _zoomLevel;
            ZoomTransform.ScaleY = _zoomLevel;
        }

        private void TreeMapCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Handle item selection
        }

        private void TreeMapCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                ZoomInButton_Click(this, new RoutedEventArgs());
            }
            else
            {
                ZoomOutButton_Click(this, new RoutedEventArgs());
            }
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TreeMapControl control)
            {
                control.RenderTreeMap();
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            RenderTreeMap();
        }

        public void RenderTreeMap()
        {
            TreeMapCanvas.Children.Clear();

            var items = ItemsSource?.Where(i => i.Size > 0).ToList();
            if (items == null || items.Count == 0)
            {
                EmptyMessage.Visibility = Visibility.Visible;
                return;
            }

            EmptyMessage.Visibility = Visibility.Collapsed;

            var totalSize = items.Sum(i => i.Size);
            var sortedItems = items.OrderByDescending(i => i.Size).ToList();

            var rect = new Rect(0, 0, ActualWidth, ActualHeight);
            LayoutSquarified(sortedItems, rect, totalSize);
        }

        private void LayoutSquarified(List<TreeMapItem> items, Rect bounds, long totalSize)
        {
            if (items.Count == 0 || bounds.Width < 2 || bounds.Height < 2)
                return;

            if (items.Count == 1)
            {
                AddRectangle(items[0], bounds, 0);
                return;
            }

            var currentRow = new List<TreeMapItem>();
            var remaining = new List<TreeMapItem>(items);
            var currentBounds = bounds;
            int colorIndex = 0;

            while (remaining.Count > 0)
            {
                var item = remaining[0];
                remaining.RemoveAt(0);

                var testRow = new List<TreeMapItem>(currentRow) { item };
                var rowSize = testRow.Sum(i => i.Size);
                var rowRatio = (double)rowSize / totalSize;

                if (currentRow.Count == 0 || GetWorstRatio(testRow, currentBounds, rowRatio) <= GetWorstRatio(currentRow, currentBounds, (double)currentRow.Sum(i => i.Size) / totalSize))
                {
                    currentRow.Add(item);
                }
                else
                {
                    var rowBounds = LayoutRow(currentRow, currentBounds, (double)currentRow.Sum(i => i.Size) / totalSize, ref colorIndex);
                    currentBounds = GetRemainingBounds(currentBounds, rowBounds);
                    currentRow.Clear();
                    currentRow.Add(item);
                }
            }

            if (currentRow.Count > 0)
            {
                LayoutRow(currentRow, currentBounds, (double)currentRow.Sum(i => i.Size) / totalSize, ref colorIndex);
            }
        }

        private double GetWorstRatio(List<TreeMapItem> row, Rect bounds, double rowRatio)
        {
            if (row.Count == 0) return double.MaxValue;

            var isHorizontal = bounds.Width >= bounds.Height;
            var rowLength = isHorizontal ? bounds.Height * rowRatio : bounds.Width * rowRatio;
            var crossLength = isHorizontal ? bounds.Width : bounds.Height;

            var rowTotal = row.Sum(i => i.Size);
            var worstRatio = 0.0;

            foreach (var item in row)
            {
                var itemRatio = (double)item.Size / rowTotal;
                var itemLength = crossLength * itemRatio;
                var ratio = Math.Max(rowLength / itemLength, itemLength / rowLength);
                worstRatio = Math.Max(worstRatio, ratio);
            }

            return worstRatio;
        }

        private Rect LayoutRow(List<TreeMapItem> row, Rect bounds, double rowRatio, ref int colorIndex)
        {
            var isHorizontal = bounds.Width >= bounds.Height;
            var rowTotal = row.Sum(i => i.Size);

            double x = bounds.X, y = bounds.Y;
            double rowWidth, rowHeight;

            if (isHorizontal)
            {
                rowWidth = bounds.Width;
                rowHeight = bounds.Height * rowRatio;

                foreach (var item in row)
                {
                    var itemRatio = (double)item.Size / rowTotal;
                    var itemWidth = rowWidth * itemRatio;
                    var itemRect = new Rect(x, y, Math.Max(1, itemWidth - 2), Math.Max(1, rowHeight - 2));
                    AddRectangle(item, itemRect, colorIndex++);
                    x += itemWidth;
                }

                return new Rect(bounds.X, bounds.Y, rowWidth, rowHeight);
            }
            else
            {
                rowWidth = bounds.Width * rowRatio;
                rowHeight = bounds.Height;

                foreach (var item in row)
                {
                    var itemRatio = (double)item.Size / rowTotal;
                    var itemHeight = rowHeight * itemRatio;
                    var itemRect = new Rect(x, y, Math.Max(1, rowWidth - 2), Math.Max(1, itemHeight - 2));
                    AddRectangle(item, itemRect, colorIndex++);
                    y += itemHeight;
                }

                return new Rect(bounds.X, bounds.Y, rowWidth, rowHeight);
            }
        }

        private Rect GetRemainingBounds(Rect original, Rect used)
        {
            if (original.Width >= original.Height)
            {
                return new Rect(original.X, original.Y + used.Height, original.Width, original.Height - used.Height);
            }
            else
            {
                return new Rect(original.X + used.Width, original.Y, original.Width - used.Width, original.Height);
            }
        }

        private void AddRectangle(TreeMapItem item, Rect bounds, int colorIndex)
        {
            if (bounds.Width < 3 || bounds.Height < 3) return;

            var color = CategoryColors[colorIndex % CategoryColors.Length];
            var brush = new SolidColorBrush(color);

            var border = new Border
            {
                Width = bounds.Width,
                Height = bounds.Height,
                Background = brush,
                BorderBrush = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Cursor = Cursors.Hand,
                ToolTip = $"{item.Name}\n{FormatSize(item.Size)}\n{item.ItemCount} items",
                Tag = item,
            };

            if (bounds.Width > 60 && bounds.Height > 30)
            {
                var stack = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(4),
                };

                stack.Children.Add(new TextBlock
                {
                    Text = item.Name,
                    FontSize = Math.Min(14, Math.Max(10, bounds.Width / 10)),
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    HorizontalAlignment = HorizontalAlignment.Center,
                });

                if (bounds.Height > 50)
                {
                    stack.Children.Add(new TextBlock
                    {
                        Text = FormatSize(item.Size),
                        FontSize = Math.Min(12, Math.Max(9, bounds.Width / 12)),
                        Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                    });
                }

                border.Child = stack;
            }

            border.MouseLeftButtonUp += (s, e) => ItemClicked?.Invoke(this, item);
            border.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                    ItemDoubleClicked?.Invoke(this, item);
            };

            Canvas.SetLeft(border, bounds.X);
            Canvas.SetTop(border, bounds.Y);
            TreeMapCanvas.Children.Add(border);
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

    public class TreeMapItem
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public long Size { get; set; }
        public int ItemCount { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
    }
}
