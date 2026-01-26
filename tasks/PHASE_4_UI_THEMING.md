# Phase 4: UI & Borg Theme Studio

> **[Back to Roadmap](PROJECT_ROADMAP.md)** | **Previous:** [Phase 3](PHASE_3_CSHARP_INTEGRATION.md) | **Next:** [Phase 5](PHASE_5_AI_AGENTS.md)

---

## Phase Overview

| Attribute | Value |
|-----------|-------|
| **Status** | `[ ]` Not Started |
| **Priority** | High |
| **Estimated Tasks** | 22 |
| **Dependencies** | [Phase 3](PHASE_3_CSHARP_INTEGRATION.md) Complete |

---

## Objectives

1. Create Storage Intelligence WPF views following Winhance patterns
2. Implement Borg Theme Studio with 1-5 color customization
3. Create click-to-change color interface
4. Build 8 predefined Borg palettes
5. Enable theme import/export functionality

---

## Borg Theme Studio Concept

```
+==============================================================================+
|                         BORG THEME STUDIO                                     |
+==============================================================================+
|                                                                               |
|  Color Slots (Click any to customize):                                        |
|                                                                               |
|  +--------+  +--------+  +--------+  +--------+  +--------+                  |
|  |   1    |  |   2    |  |   3    |  |   4    |  |   5    |                  |
|  | PRIMARY|  |SECONDARY| |  ACCENT |  |  TEXT  |  |  BG    |                  |
|  +--------+  +--------+  +--------+  +--------+  +--------+                  |
|                                                                               |
|  Preset Palettes:                                                             |
|  [Borg Green] [Borg Red] [Borg Blue] [Borg Purple]                           |
|  [Borg Gold] [Borg Orange] [Borg Pink] [Borg Neon]                           |
|                                                                               |
|  [Import Theme]  [Export Theme]  [Reset to Default]                          |
|                                                                               |
+==============================================================================+
```

---

## Task List

### 4.1 Storage Intelligence Views

#### Task 4.1.1: Create StorageIntelligenceView

**Status:** `[ ]` Not Started

**Description:**
Create the main Storage Intelligence dashboard view.

**Acceptance Criteria:**
- [ ] `StorageIntelligenceView.xaml` follows Winhance XAML patterns
- [ ] Shows drive usage overview
- [ ] Displays space breakdown chart
- [ ] Lists recommendations

**Files to Create:**
```
src/Winhance.WPF/Features/Storage/Views/StorageIntelligenceView.xaml
src/Winhance.WPF/Features/Storage/Views/StorageIntelligenceView.xaml.cs
```

**Implementation:**
```xml
<!-- StorageIntelligenceView.xaml -->
<UserControl x:Class="Winhance.WPF.Features.Storage.Views.StorageIntelligenceView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:controls="clr-namespace:Winhance.WPF.Features.Storage.Controls"
             xmlns:vm="clr-namespace:Winhance.WPF.Features.Storage.ViewModels">

    <UserControl.DataContext>
        <vm:StorageIntelligenceViewModel />
    </UserControl.DataContext>

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>

        <!-- Header -->
        <Border Grid.Row="0"
                Background="{DynamicResource CardBackgroundColor}"
                Padding="20">
            <StackPanel>
                <TextBlock Text="Storage Intelligence"
                           Style="{DynamicResource HeadingStyle}"
                           Foreground="{DynamicResource PrimaryTextColor}" />
                <TextBlock Text="Analyze and optimize your storage"
                           Style="{DynamicResource SubheadingStyle}"
                           Foreground="{DynamicResource SecondaryTextColor}" />
            </StackPanel>
        </Border>

        <!-- Main Content -->
        <ScrollViewer Grid.Row="1" VerticalScrollBarVisibility="Auto">
            <StackPanel Margin="20">

                <!-- Drive Selection -->
                <Border Background="{DynamicResource CardBackgroundColor}"
                        CornerRadius="8"
                        Padding="16"
                        Margin="0,0,0,16">
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*" />
                            <ColumnDefinition Width="Auto" />
                        </Grid.ColumnDefinitions>

                        <ComboBox Grid.Column="0"
                                  ItemsSource="{Binding Drives}"
                                  SelectedItem="{Binding SelectedDrive}"
                                  DisplayMemberPath="Name"
                                  Style="{DynamicResource ComboBoxStyle}" />

                        <Button Grid.Column="1"
                                Content="Scan"
                                Command="{Binding ScanCommand}"
                                Style="{DynamicResource PrimaryButtonStyle}"
                                Margin="16,0,0,0" />
                    </Grid>
                </Border>

                <!-- Space Breakdown -->
                <Border Background="{DynamicResource CardBackgroundColor}"
                        CornerRadius="8"
                        Padding="16"
                        Margin="0,0,0,16">
                    <StackPanel>
                        <TextBlock Text="Space Breakdown"
                                   Style="{DynamicResource CardTitleStyle}" />
                        <controls:DriveUsageChart
                            Data="{Binding SpaceBreakdown}"
                            Height="200"
                            Margin="0,16,0,0" />
                    </StackPanel>
                </Border>

                <!-- Recommendations -->
                <Border Background="{DynamicResource CardBackgroundColor}"
                        CornerRadius="8"
                        Padding="16">
                    <StackPanel>
                        <TextBlock Text="Recommendations"
                                   Style="{DynamicResource CardTitleStyle}" />
                        <ItemsControl ItemsSource="{Binding Recommendations}"
                                      Margin="0,16,0,0">
                            <ItemsControl.ItemTemplate>
                                <DataTemplate>
                                    <controls:RecommendationCard
                                        Recommendation="{Binding}"
                                        ApplyCommand="{Binding DataContext.ApplyRecommendationCommand,
                                            RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        Margin="0,0,0,8" />
                                </DataTemplate>
                            </ItemsControl.ItemTemplate>
                        </ItemsControl>
                    </StackPanel>
                </Border>

            </StackPanel>
        </ScrollViewer>

        <!-- Status Bar -->
        <Border Grid.Row="2"
                Background="{DynamicResource SurfaceColor}"
                Padding="16,8">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="{Binding StatusMessage}"
                           Foreground="{DynamicResource SecondaryTextColor}" />
                <ProgressBar Value="{Binding Progress}"
                             Width="200"
                             Margin="16,0,0,0"
                             Visibility="{Binding IsScanning, Converter={StaticResource BoolToVisibility}}" />
            </StackPanel>
        </Border>

    </Grid>
</UserControl>
```

**Next Task:** [4.1.2](#task-412-create-storageintelligenceviewmodel)

---

#### Task 4.1.2: Create StorageIntelligenceViewModel

**Status:** `[ ]` Not Started

**Description:**
Create the ViewModel for Storage Intelligence view.

**Acceptance Criteria:**
- [ ] Inherits from Winhance BaseViewModel
- [ ] Uses CommunityToolkit.Mvvm attributes
- [ ] Implements scan, search, and recommendation commands
- [ ] Proper async loading patterns

**Files to Create:**
```
src/Winhance.WPF/Features/Storage/ViewModels/StorageIntelligenceViewModel.cs
```

**Implementation:**
```csharp
// StorageIntelligenceViewModel.cs
namespace Winhance.WPF.Features.Storage.ViewModels;

public partial class StorageIntelligenceViewModel : BaseViewModel
{
    private readonly IStorageScanner _storageScanner;

    [ObservableProperty]
    private ObservableCollection<DriveViewModel> _drives = [];

    [ObservableProperty]
    private DriveViewModel? _selectedDrive;

    [ObservableProperty]
    private SpaceBreakdown? _spaceBreakdown;

    [ObservableProperty]
    private ObservableCollection<SpaceRecommendation> _recommendations = [];

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private int _progress;

    [ObservableProperty]
    private bool _isScanning;

    public StorageIntelligenceViewModel(IStorageScanner storageScanner)
    {
        _storageScanner = storageScanner;
        LoadDrivesAsync();
    }

    private async void LoadDrivesAsync()
    {
        var drives = DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
            .Select(d => new DriveViewModel(d))
            .ToList();

        Drives = new ObservableCollection<DriveViewModel>(drives);
        SelectedDrive = Drives.FirstOrDefault();
    }

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync(CancellationToken ct)
    {
        if (SelectedDrive is null) return;

        IsScanning = true;
        StatusMessage = "Scanning...";

        var progress = new Progress<ScanProgress>(p =>
        {
            StatusMessage = p.Stage;
            Progress = p.Percentage;
        });

        var result = await _storageScanner.ScanDriveAsync(
            SelectedDrive.Letter,
            progress: progress,
            cancellationToken: ct);

        if (result.Success)
        {
            StatusMessage = $"Scan complete: {result.Data!.FileCount:N0} files";

            var breakdownResult = await _storageScanner.GetSpaceBreakdownAsync(
                SelectedDrive.Letter, ct);

            if (breakdownResult.Success)
            {
                SpaceBreakdown = breakdownResult.Data;
            }

            var recommendationsResult = await _storageScanner.GetRecommendationsAsync(
                SelectedDrive.Letter, ct);

            if (recommendationsResult.Success)
            {
                Recommendations = new ObservableCollection<SpaceRecommendation>(
                    recommendationsResult.Data!);
            }
        }
        else
        {
            StatusMessage = $"Scan failed: {result.ErrorMessage}";
        }

        IsScanning = false;
    }

    private bool CanScan() => SelectedDrive is not null && !IsScanning;

    [RelayCommand]
    private async Task ApplyRecommendationAsync(SpaceRecommendation recommendation)
    {
        // Show confirmation dialog
        // Execute recommendation
        // Refresh scan
    }
}
```

**Dependencies:** [4.1.1](#task-411-create-storageintelligenceview)

**Next Task:** [4.1.3](#task-413-create-driveusagechart-control)

---

#### Task 4.1.3: Create DriveUsageChart Control

**Status:** `[ ]` Not Started

**Description:**
Create a custom WPF control for displaying drive usage breakdown.

**Acceptance Criteria:**
- [ ] Displays pie/donut chart
- [ ] Themed with dynamic colors
- [ ] Shows legends with percentages
- [ ] Animated transitions

**Files to Create:**
```
src/Winhance.WPF/Features/Storage/Controls/DriveUsageChart.xaml
src/Winhance.WPF/Features/Storage/Controls/DriveUsageChart.xaml.cs
```

**Dependencies:** [4.1.2](#task-412-create-storageintelligenceviewmodel)

**Next Task:** [4.1.4](#task-414-create-deepscanview)

---

#### Task 4.1.4: Create DeepScanView

**Status:** `[ ]` Not Started

**Description:**
Create the Deep Scan view for forensic analysis.

**Acceptance Criteria:**
- [ ] Shows ADS scanner
- [ ] Shows VSS browser
- [ ] Shows entropy analysis
- [ ] Shows duplicate finder

**Files to Create:**
```
src/Winhance.WPF/Features/Storage/Views/DeepScanView.xaml
src/Winhance.WPF/Features/Storage/Views/DeepScanView.xaml.cs
src/Winhance.WPF/Features/Storage/ViewModels/DeepScanViewModel.cs
```

**Dependencies:** [4.1.3](#task-413-create-driveusagechart-control)

**Next Task:** [4.1.5](#task-415-create-aimodelmanagerview)

---

#### Task 4.1.5: Create AIModelManagerView

**Status:** `[ ]` Not Started

**Description:**
Create the AI Model Manager view.

**Acceptance Criteria:**
- [ ] Lists discovered AI models
- [ ] Shows model sizes and locations
- [ ] Supports move operations
- [ ] Shows symlink status

**Files to Create:**
```
src/Winhance.WPF/Features/Storage/Views/AIModelManagerView.xaml
src/Winhance.WPF/Features/Storage/Views/AIModelManagerView.xaml.cs
src/Winhance.WPF/Features/Storage/ViewModels/AIModelManagerViewModel.cs
```

**Dependencies:** [4.1.4](#task-414-create-deepscanview)

**Next Task:** [4.2.1](#task-421-create-borg-theme-data-structures)

---

### 4.2 Borg Theme Studio

#### Task 4.2.1: Create Borg Theme Data Structures

**Status:** `[ ]` Not Started

**Description:**
Create the data models for Borg themes.

**Acceptance Criteria:**
- [ ] `BorgTheme` class with 5 color slots
- [ ] `BorgPalette` class for predefined themes
- [ ] JSON serialization support
- [ ] Color validation

**Files to Create:**
```
src/Winhance.Core/Features/Theming/Models/BorgTheme.cs
src/Winhance.Core/Features/Theming/Models/BorgPalette.cs
src/Winhance.Core/Features/Theming/Models/BorgPresets.cs
```

**Implementation:**
```csharp
// BorgTheme.cs
namespace Winhance.Core.Features.Theming.Models;

/// <summary>
/// A Borg-style theme with 5 customizable color slots.
/// </summary>
public sealed record BorgTheme
{
    /// <summary>Theme name.</summary>
    public required string Name { get; init; }

    /// <summary>Primary color (main UI elements).</summary>
    public required string Primary { get; init; }

    /// <summary>Secondary color (accents, highlights).</summary>
    public required string Secondary { get; init; }

    /// <summary>Accent color (buttons, links).</summary>
    public required string Accent { get; init; }

    /// <summary>Text color.</summary>
    public required string Text { get; init; }

    /// <summary>Background color.</summary>
    public required string Background { get; init; }

    /// <summary>Whether this is a dark theme.</summary>
    public bool IsDark { get; init; } = true;

    /// <summary>Theme author.</summary>
    public string? Author { get; init; }

    /// <summary>Theme version.</summary>
    public string Version { get; init; } = "1.0.0";
}

// BorgPalette.cs
namespace Winhance.Core.Features.Theming.Models;

/// <summary>
/// A predefined Borg color palette.
/// </summary>
public sealed record BorgPalette
{
    public required string Name { get; init; }
    public required Color Primary { get; init; }
    public required Color Secondary { get; init; }
    public required Color Accent { get; init; }
    public required Color Text { get; init; }
    public required Color Background { get; init; }
}

// BorgPresets.cs
namespace Winhance.Core.Features.Theming.Models;

/// <summary>
/// Predefined Borg palettes.
/// </summary>
public static class BorgPresets
{
    public static readonly BorgPalette BorgGreen = new()
    {
        Name = "Borg Green",
        Primary = Color.FromRgb(0x00, 0xFF, 0x00),      // #00FF00
        Secondary = Color.FromRgb(0x00, 0xCC, 0x00),    // #00CC00
        Accent = Color.FromRgb(0x33, 0xFF, 0x33),       // #33FF33
        Text = Color.FromRgb(0x00, 0xFF, 0x00),         // #00FF00
        Background = Color.FromRgb(0x0A, 0x0A, 0x0A)    // #0A0A0A
    };

    public static readonly BorgPalette BorgRed = new()
    {
        Name = "Borg Red",
        Primary = Color.FromRgb(0xFF, 0x00, 0x00),      // #FF0000
        Secondary = Color.FromRgb(0xCC, 0x00, 0x00),    // #CC0000
        Accent = Color.FromRgb(0xFF, 0x33, 0x33),       // #FF3333
        Text = Color.FromRgb(0xFF, 0x00, 0x00),         // #FF0000
        Background = Color.FromRgb(0x0A, 0x0A, 0x0A)    // #0A0A0A
    };

    public static readonly BorgPalette BorgBlue = new()
    {
        Name = "Borg Blue",
        Primary = Color.FromRgb(0x00, 0xBF, 0xFF),      // #00BFFF
        Secondary = Color.FromRgb(0x00, 0x99, 0xCC),    // #0099CC
        Accent = Color.FromRgb(0x33, 0xCC, 0xFF),       // #33CCFF
        Text = Color.FromRgb(0x00, 0xBF, 0xFF),         // #00BFFF
        Background = Color.FromRgb(0x0A, 0x0A, 0x0A)    // #0A0A0A
    };

    public static readonly BorgPalette BorgPurple = new()
    {
        Name = "Borg Purple",
        Primary = Color.FromRgb(0x99, 0x00, 0xFF),      // #9900FF
        Secondary = Color.FromRgb(0x77, 0x00, 0xCC),    // #7700CC
        Accent = Color.FromRgb(0xAA, 0x33, 0xFF),       // #AA33FF
        Text = Color.FromRgb(0x99, 0x00, 0xFF),         // #9900FF
        Background = Color.FromRgb(0x0A, 0x0A, 0x0A)    // #0A0A0A
    };

    public static readonly BorgPalette BorgGold = new()
    {
        Name = "Borg Gold",
        Primary = Color.FromRgb(0xFF, 0xD7, 0x00),      // #FFD700
        Secondary = Color.FromRgb(0xCC, 0xAA, 0x00),    // #CCAA00
        Accent = Color.FromRgb(0xFF, 0xE0, 0x33),       // #FFE033
        Text = Color.FromRgb(0xFF, 0xD7, 0x00),         // #FFD700
        Background = Color.FromRgb(0x0A, 0x0A, 0x0A)    // #0A0A0A
    };

    public static readonly BorgPalette BorgOrange = new()
    {
        Name = "Borg Orange",
        Primary = Color.FromRgb(0xFF, 0x66, 0x00),      // #FF6600
        Secondary = Color.FromRgb(0xCC, 0x52, 0x00),    // #CC5200
        Accent = Color.FromRgb(0xFF, 0x80, 0x33),       // #FF8033
        Text = Color.FromRgb(0xFF, 0x66, 0x00),         // #FF6600
        Background = Color.FromRgb(0x0A, 0x0A, 0x0A)    // #0A0A0A
    };

    public static readonly BorgPalette BorgPink = new()
    {
        Name = "Borg Pink",
        Primary = Color.FromRgb(0xFF, 0x00, 0x80),      // #FF0080
        Secondary = Color.FromRgb(0xCC, 0x00, 0x66),    // #CC0066
        Accent = Color.FromRgb(0xFF, 0x33, 0x99),       // #FF3399
        Text = Color.FromRgb(0xFF, 0x00, 0x80),         // #FF0080
        Background = Color.FromRgb(0x0A, 0x0A, 0x0A)    // #0A0A0A
    };

    public static readonly BorgPalette BorgNeon = new()
    {
        Name = "Borg Neon",
        Primary = Color.FromRgb(0x00, 0xFF, 0xFF),      // #00FFFF
        Secondary = Color.FromRgb(0xFF, 0x00, 0xFF),    // #FF00FF
        Accent = Color.FromRgb(0xFF, 0xFF, 0x00),       // #FFFF00
        Text = Color.FromRgb(0x00, 0xFF, 0xFF),         // #00FFFF
        Background = Color.FromRgb(0x0A, 0x0A, 0x0A)    // #0A0A0A
    };

    public static IReadOnlyList<BorgPalette> All => new[]
    {
        BorgGreen, BorgRed, BorgBlue, BorgPurple,
        BorgGold, BorgOrange, BorgPink, BorgNeon
    };
}
```

**Next Task:** [4.2.2](#task-422-create-borg-theme-service)

---

#### Task 4.2.2: Create Borg Theme Service

**Status:** `[ ]` Not Started

**Description:**
Create the service for managing Borg themes.

**Acceptance Criteria:**
- [ ] Load/save themes from JSON
- [ ] Apply themes to ResourceDictionary
- [ ] Generate derived colors
- [ ] Theme change notifications

**Files to Create:**
```
src/Winhance.Infrastructure/Features/Theming/Services/BorgThemeService.cs
src/Winhance.Core/Features/Theming/Interfaces/IBorgThemeService.cs
```

**Implementation:**
```csharp
// IBorgThemeService.cs
namespace Winhance.Core.Features.Theming.Interfaces;

public interface IBorgThemeService
{
    BorgTheme CurrentTheme { get; }
    IReadOnlyList<BorgPalette> AvailablePalettes { get; }

    Task<OperationResult<bool>> ApplyThemeAsync(BorgTheme theme);
    Task<OperationResult<bool>> ApplyPaletteAsync(BorgPalette palette);
    Task<OperationResult<BorgTheme>> LoadThemeAsync(string path);
    Task<OperationResult<bool>> SaveThemeAsync(BorgTheme theme, string path);
    Task<OperationResult<bool>> SetColorAsync(int slot, Color color);

    event EventHandler<BorgTheme>? ThemeChanged;
}

// BorgThemeService.cs
namespace Winhance.Infrastructure.Features.Theming.Services;

public sealed class BorgThemeService : IBorgThemeService
{
    private BorgTheme _currentTheme;
    private readonly ILogger<BorgThemeService> _logger;

    public BorgTheme CurrentTheme => _currentTheme;
    public IReadOnlyList<BorgPalette> AvailablePalettes => BorgPresets.All;

    public event EventHandler<BorgTheme>? ThemeChanged;

    public BorgThemeService(ILogger<BorgThemeService> logger)
    {
        _logger = logger;
        _currentTheme = CreateFromPalette(BorgPresets.BorgGreen);
    }

    public async Task<OperationResult<bool>> ApplyThemeAsync(BorgTheme theme)
    {
        try
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var resources = Application.Current.Resources;

                // Apply primary colors
                resources["PrimaryColor"] = ColorFromHex(theme.Primary);
                resources["SecondaryColor"] = ColorFromHex(theme.Secondary);
                resources["AccentColor"] = ColorFromHex(theme.Accent);
                resources["PrimaryTextColor"] = ColorFromHex(theme.Text);
                resources["BackgroundColor"] = ColorFromHex(theme.Background);

                // Generate derived colors
                var primary = ColorFromHex(theme.Primary);
                resources["PrimaryColorLight"] = Lighten(primary, 0.2);
                resources["PrimaryColorDark"] = Darken(primary, 0.2);

                // Generate brushes
                resources["PrimaryBrush"] = new SolidColorBrush(primary);
                resources["BackgroundBrush"] = new SolidColorBrush(ColorFromHex(theme.Background));
            });

            _currentTheme = theme;
            ThemeChanged?.Invoke(this, theme);

            _logger.LogInformation("Applied theme: {ThemeName}", theme.Name);
            return OperationResult<bool>.Succeeded(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply theme");
            return OperationResult<bool>.Failed(ex);
        }
    }

    public async Task<OperationResult<bool>> ApplyPaletteAsync(BorgPalette palette)
    {
        var theme = CreateFromPalette(palette);
        return await ApplyThemeAsync(theme);
    }

    public async Task<OperationResult<BorgTheme>> LoadThemeAsync(string path)
    {
        try
        {
            var json = await File.ReadAllTextAsync(path);
            var theme = JsonSerializer.Deserialize<BorgTheme>(json);
            return OperationResult<BorgTheme>.Succeeded(theme!);
        }
        catch (Exception ex)
        {
            return OperationResult<BorgTheme>.Failed(ex);
        }
    }

    public async Task<OperationResult<bool>> SaveThemeAsync(BorgTheme theme, string path)
    {
        try
        {
            var json = JsonSerializer.Serialize(theme, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(path, json);
            return OperationResult<bool>.Succeeded(true);
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.Failed(ex);
        }
    }

    public async Task<OperationResult<bool>> SetColorAsync(int slot, Color color)
    {
        var theme = _currentTheme with
        {
            Primary = slot == 1 ? ColorToHex(color) : _currentTheme.Primary,
            Secondary = slot == 2 ? ColorToHex(color) : _currentTheme.Secondary,
            Accent = slot == 3 ? ColorToHex(color) : _currentTheme.Accent,
            Text = slot == 4 ? ColorToHex(color) : _currentTheme.Text,
            Background = slot == 5 ? ColorToHex(color) : _currentTheme.Background,
        };

        return await ApplyThemeAsync(theme);
    }

    private static BorgTheme CreateFromPalette(BorgPalette palette) => new()
    {
        Name = palette.Name,
        Primary = ColorToHex(palette.Primary),
        Secondary = ColorToHex(palette.Secondary),
        Accent = ColorToHex(palette.Accent),
        Text = ColorToHex(palette.Text),
        Background = ColorToHex(palette.Background),
        IsDark = true
    };

    private static Color ColorFromHex(string hex)
    {
        hex = hex.TrimStart('#');
        return Color.FromRgb(
            Convert.ToByte(hex[..2], 16),
            Convert.ToByte(hex[2..4], 16),
            Convert.ToByte(hex[4..6], 16));
    }

    private static string ColorToHex(Color color) =>
        $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private static Color Lighten(Color color, double factor)
    {
        return Color.FromRgb(
            (byte)Math.Min(255, color.R + (255 - color.R) * factor),
            (byte)Math.Min(255, color.G + (255 - color.G) * factor),
            (byte)Math.Min(255, color.B + (255 - color.B) * factor));
    }

    private static Color Darken(Color color, double factor)
    {
        return Color.FromRgb(
            (byte)(color.R * (1 - factor)),
            (byte)(color.G * (1 - factor)),
            (byte)(color.B * (1 - factor)));
    }
}
```

**Dependencies:** [4.2.1](#task-421-create-borg-theme-data-structures)

**Next Task:** [4.2.3](#task-423-create-borg-theme-studio-view)

---

#### Task 4.2.3: Create Borg Theme Studio View

**Status:** `[ ]` Not Started

**Description:**
Create the Borg Theme Studio UI.

**Acceptance Criteria:**
- [ ] Shows 5 clickable color slots
- [ ] Preset palette selection
- [ ] Color picker on click
- [ ] Import/Export buttons
- [ ] Live preview

**Files to Create:**
```
src/Winhance.WPF/Features/Theming/Views/BorgThemeStudioView.xaml
src/Winhance.WPF/Features/Theming/Views/BorgThemeStudioView.xaml.cs
src/Winhance.WPF/Features/Theming/ViewModels/BorgThemeStudioViewModel.cs
```

**Implementation:**
```xml
<!-- BorgThemeStudioView.xaml -->
<UserControl x:Class="Winhance.WPF.Features.Theming.Views.BorgThemeStudioView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:controls="clr-namespace:Winhance.WPF.Features.Theming.Controls">

    <Grid Background="{DynamicResource BackgroundColor}">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <!-- Header -->
        <TextBlock Grid.Row="0"
                   Text="Borg Theme Studio"
                   Style="{DynamicResource HeadingStyle}"
                   Margin="20" />

        <!-- Color Slots -->
        <Border Grid.Row="1"
                Background="{DynamicResource CardBackgroundColor}"
                CornerRadius="8"
                Margin="20,0,20,20"
                Padding="20">
            <StackPanel>
                <TextBlock Text="Color Slots (Click to customize)"
                           Foreground="{DynamicResource SecondaryTextColor}"
                           Margin="0,0,0,16" />

                <UniformGrid Columns="5" Rows="1">
                    <!-- Slot 1: Primary -->
                    <controls:ColorSlot
                        SlotNumber="1"
                        SlotName="Primary"
                        Color="{Binding PrimaryColor, Mode=TwoWay}"
                        Command="{Binding ChangeColorCommand}"
                        CommandParameter="1" />

                    <!-- Slot 2: Secondary -->
                    <controls:ColorSlot
                        SlotNumber="2"
                        SlotName="Secondary"
                        Color="{Binding SecondaryColor, Mode=TwoWay}"
                        Command="{Binding ChangeColorCommand}"
                        CommandParameter="2" />

                    <!-- Slot 3: Accent -->
                    <controls:ColorSlot
                        SlotNumber="3"
                        SlotName="Accent"
                        Color="{Binding AccentColor, Mode=TwoWay}"
                        Command="{Binding ChangeColorCommand}"
                        CommandParameter="3" />

                    <!-- Slot 4: Text -->
                    <controls:ColorSlot
                        SlotNumber="4"
                        SlotName="Text"
                        Color="{Binding TextColor, Mode=TwoWay}"
                        Command="{Binding ChangeColorCommand}"
                        CommandParameter="4" />

                    <!-- Slot 5: Background -->
                    <controls:ColorSlot
                        SlotNumber="5"
                        SlotName="Background"
                        Color="{Binding BackgroundColor, Mode=TwoWay}"
                        Command="{Binding ChangeColorCommand}"
                        CommandParameter="5" />
                </UniformGrid>
            </StackPanel>
        </Border>

        <!-- Preset Palettes -->
        <Border Grid.Row="2"
                Background="{DynamicResource CardBackgroundColor}"
                CornerRadius="8"
                Margin="20,0,20,20"
                Padding="20">
            <StackPanel>
                <TextBlock Text="Preset Palettes"
                           Foreground="{DynamicResource SecondaryTextColor}"
                           Margin="0,0,0,16" />

                <ItemsControl ItemsSource="{Binding Palettes}">
                    <ItemsControl.ItemsPanel>
                        <ItemsPanelTemplate>
                            <WrapPanel />
                        </ItemsPanelTemplate>
                    </ItemsControl.ItemsPanel>
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <Button Content="{Binding Name}"
                                    Command="{Binding DataContext.ApplyPaletteCommand,
                                        RelativeSource={RelativeSource AncestorType=UserControl}}"
                                    CommandParameter="{Binding}"
                                    Style="{DynamicResource PaletteButtonStyle}"
                                    Margin="0,0,8,8" />
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </StackPanel>
        </Border>

        <!-- Actions -->
        <StackPanel Grid.Row="3"
                    Orientation="Horizontal"
                    HorizontalAlignment="Center"
                    Margin="20">
            <Button Content="Import Theme"
                    Command="{Binding ImportCommand}"
                    Style="{DynamicResource SecondaryButtonStyle}"
                    Margin="0,0,8,0" />
            <Button Content="Export Theme"
                    Command="{Binding ExportCommand}"
                    Style="{DynamicResource SecondaryButtonStyle}"
                    Margin="0,0,8,0" />
            <Button Content="Reset to Default"
                    Command="{Binding ResetCommand}"
                    Style="{DynamicResource SecondaryButtonStyle}" />
        </StackPanel>
    </Grid>
</UserControl>
```

**Dependencies:** [4.2.2](#task-422-create-borg-theme-service)

**Next Task:** [4.2.4](#task-424-create-color-slot-control)

---

#### Task 4.2.4: Create Color Slot Control

**Status:** `[ ]` Not Started

**Description:**
Create the clickable color slot control with color picker.

**Acceptance Criteria:**
- [ ] Displays color swatch
- [ ] Shows slot number and name
- [ ] Opens color picker on click
- [ ] Supports two-way binding

**Files to Create:**
```
src/Winhance.WPF/Features/Theming/Controls/ColorSlot.xaml
src/Winhance.WPF/Features/Theming/Controls/ColorSlot.xaml.cs
```

**Dependencies:** [4.2.3](#task-423-create-borg-theme-studio-view)

**Next Task:** [4.2.5](#task-425-create-theme-resource-dictionary)

---

#### Task 4.2.5: Create Theme Resource Dictionary

**Status:** `[ ]` Not Started

**Description:**
Create the XAML resource dictionary for Borg theme tokens.

**Acceptance Criteria:**
- [ ] All color tokens defined
- [ ] Brush resources created
- [ ] Control styles use DynamicResource
- [ ] Default dark theme values

**Files to Create:**
```
src/Winhance.WPF/Themes/BorgTheme.xaml
```

**Dependencies:** [4.2.4](#task-424-create-color-slot-control)

---

## Phase Completion Checklist

- [ ] All 4.1.x tasks complete (Storage Intelligence Views)
- [ ] All 4.2.x tasks complete (Borg Theme Studio)
- [ ] Views render correctly
- [ ] Theme changes apply in real-time
- [ ] All 8 Borg palettes work
- [ ] Import/Export functions work

---

**[Back to Roadmap](PROJECT_ROADMAP.md)** | **Previous:** [Phase 3](PHASE_3_CSHARP_INTEGRATION.md) | **Next:** [Phase 5](PHASE_5_AI_AGENTS.md)
