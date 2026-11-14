using System;
using System.Collections.Generic;

namespace Winhance.Core.Features.FileManager.Models
{
    /// <summary>
    /// Configuration for an action button in the center panel.
    /// </summary>
    public class ActionButtonConfig
    {
        /// <summary>
        /// Unique identifier.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Display name for the button.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Icon key or path.
        /// </summary>
        public string Icon { get; set; } = "📋";

        /// <summary>
        /// The type of action to perform.
        /// </summary>
        public CenterPanelActionType ActionType { get; set; }

        /// <summary>
        /// Direction of the action (left to right, right to left, etc.).
        /// </summary>
        public ActionDirection Direction { get; set; }

        /// <summary>
        /// Custom command for ActionType.Custom.
        /// </summary>
        public string? CustomCommand { get; set; }

        /// <summary>
        /// Sort order in the panel.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Whether the button is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Tooltip text.
        /// </summary>
        public string Tooltip { get; set; } = string.Empty;

        /// <summary>
        /// Keyboard shortcut (e.g., "Ctrl+C").
        /// </summary>
        public string? Shortcut { get; set; }

        /// <summary>
        /// For drive actions, the drive letter.
        /// </summary>
        public string? DriveLetter { get; set; }

        /// <summary>
        /// Whether to show the button label.
        /// </summary>
        public bool ShowLabel { get; set; } = true;

        /// <summary>
        /// Button size (Small, Medium, Large).
        /// </summary>
        public ButtonSize Size { get; set; } = ButtonSize.Medium;

        /// <summary>
        /// Custom background color (hex).
        /// </summary>
        public string? BackgroundColor { get; set; }

        /// <summary>
        /// Custom foreground color (hex).
        /// </summary>
        public string? ForegroundColor { get; set; }
    }

    /// <summary>
    /// Configuration for the center panel.
    /// </summary>
    public class CenterPanelConfig
    {
        /// <summary>
        /// Width of the center panel in pixels.
        /// </summary>
        public double Width { get; set; } = 120;

        /// <summary>
        /// Minimum width of the center panel.
        /// </summary>
        public double MinWidth { get; set; } = 80;

        /// <summary>
        /// Maximum width of the center panel.
        /// </summary>
        public double MaxWidth { get; set; } = 300;

        /// <summary>
        /// Whether the panel is visible.
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Whether to auto-hide when empty.
        /// </summary>
        public bool AutoHideWhenEmpty { get; set; }

        /// <summary>
        /// Number of columns for buttons.
        /// </summary>
        public int Columns { get; set; } = 1;

        /// <summary>
        /// Button spacing in pixels.
        /// </summary>
        public double ButtonSpacing { get; set; } = 4;

        /// <summary>
        /// Panel padding in pixels.
        /// </summary>
        public double Padding { get; set; } = 8;

        /// <summary>
        /// Background color (hex).
        /// </summary>
        public string BackgroundColor { get; set; } = "#F0F0F0";

        /// <summary>
        /// Whether to show the resize grip.
        /// </summary>
        public bool ShowResizeGrip { get; set; } = true;

        /// <summary>
        /// List of action buttons.
        /// </summary>
        public List<ActionButtonConfig> Buttons { get; set; } = new();
    }

    /// <summary>
    /// Type of action for center panel buttons.
    /// </summary>
    public enum CenterPanelActionType
    {
        /// <summary>Copy files/folders.</summary>
        Copy,
        /// <summary>Move files/folders.</summary>
        Move,
        /// <summary>Delete files/folders.</summary>
        Delete,
        /// <summary>Rename files/folders.</summary>
        Rename,
        /// <summary>Compare files/folders.</summary>
        Compare,
        /// <summary>Synchronize folders.</summary>
        Sync,
        /// <summary>Open a drive.</summary>
        OpenDrive,
        /// <summary>Open a specific folder.</summary>
        OpenFolder,
        /// <summary>Create an archive.</summary>
        CreateArchive,
        /// <summary>Extract an archive.</summary>
        ExtractArchive,
        /// <summary>Search in current location.</summary>
        Search,
        /// <summary>Create new folder.</summary>
        NewFolder,
        /// <summary>Create new file.</summary>
        NewFile,
        /// <summary>View properties.</summary>
        Properties,
        /// <summary>Open in terminal.</summary>
        OpenTerminal,
        /// <summary>Open in VS Code.</summary>
        OpenVSCode,
        /// <summary>Custom command.</summary>
        Custom,
        /// <summary>Separator (visual only).</summary>
        Separator
    }

    /// <summary>
    /// Direction for file operations.
    /// </summary>
    public enum ActionDirection
    {
        /// <summary>From left panel to right panel.</summary>
        LeftToRight,
        /// <summary>From right panel to left panel.</summary>
        RightToLeft,
        /// <summary>Bidirectional (swap or sync).</summary>
        Bidirectional,
        /// <summary>Based on current selection context.</summary>
        ContextBased,
        /// <summary>Target left panel.</summary>
        TargetLeft,
        /// <summary>Target right panel.</summary>
        TargetRight
    }

    /// <summary>
    /// Button size options.
    /// </summary>
    public enum ButtonSize
    {
        /// <summary>Small button (icon only, compact).</summary>
        Small,
        /// <summary>Medium button (default).</summary>
        Medium,
        /// <summary>Large button (with label).</summary>
        Large
    }

    /// <summary>
    /// Preset button configurations.
    /// </summary>
    public static class CenterPanelPresets
    {
        /// <summary>
        /// Gets the default button configuration.
        /// </summary>
        public static List<ActionButtonConfig> GetDefaultButtons()
        {
            return new List<ActionButtonConfig>
            {
                new() { DisplayName = "Copy →", Icon = "📋", ActionType = CenterPanelActionType.Copy, Direction = ActionDirection.LeftToRight, Order = 1, Tooltip = "Copy from left to right" },
                new() { DisplayName = "← Copy", Icon = "📋", ActionType = CenterPanelActionType.Copy, Direction = ActionDirection.RightToLeft, Order = 2, Tooltip = "Copy from right to left" },
                new() { DisplayName = "Move →", Icon = "📦", ActionType = CenterPanelActionType.Move, Direction = ActionDirection.LeftToRight, Order = 3, Tooltip = "Move from left to right" },
                new() { DisplayName = "← Move", Icon = "📦", ActionType = CenterPanelActionType.Move, Direction = ActionDirection.RightToLeft, Order = 4, Tooltip = "Move from right to left" },
                new() { DisplayName = "Sync ⟷", Icon = "🔄", ActionType = CenterPanelActionType.Sync, Direction = ActionDirection.Bidirectional, Order = 5, Tooltip = "Synchronize folders" },
                new() { DisplayName = "Compare", Icon = "⚖️", ActionType = CenterPanelActionType.Compare, Direction = ActionDirection.Bidirectional, Order = 6, Tooltip = "Compare folders" },
                new() { DisplayName = "Delete", Icon = "🗑️", ActionType = CenterPanelActionType.Delete, Direction = ActionDirection.ContextBased, Order = 7, Tooltip = "Delete selected items" },
                new() { DisplayName = "New Folder", Icon = "📁", ActionType = CenterPanelActionType.NewFolder, Direction = ActionDirection.ContextBased, Order = 8, Tooltip = "Create new folder" }
            };
        }

        /// <summary>
        /// Gets drive shortcut buttons for available drives.
        /// </summary>
        public static List<ActionButtonConfig> GetDriveButtons()
        {
            var buttons = new List<ActionButtonConfig>();
            var drives = System.IO.DriveInfo.GetDrives();
            int order = 100;

            foreach (var drive in drives)
            {
                if (drive.IsReady)
                {
                    buttons.Add(new ActionButtonConfig
                    {
                        DisplayName = $"{drive.Name.TrimEnd('\\')}",
                        Icon = drive.DriveType == System.IO.DriveType.Removable ? "💾" : "💿",
                        ActionType = CenterPanelActionType.OpenDrive,
                        DriveLetter = drive.Name,
                        Direction = ActionDirection.ContextBased,
                        Order = order++,
                        Tooltip = $"Open {drive.VolumeLabel} ({drive.Name})"
                    });
                }
            }

            return buttons;
        }
    }
}
