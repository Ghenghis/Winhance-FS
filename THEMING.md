# Winhance-FS Theming Documentation

> **Purpose:** Complete guide to the Borg Theme Studio and theming system  
> **Last Updated:** 2026-01-26  
> **Version:** 1.0

---

## � Navigation

**Related Documents:**
- [Architecture Documentation](ARCHITECTURE.md)
- [Project Roadmap Hub](docs/roadmap/PROJECT-ROADMAP-HUB.md)
- [WPF Development Guide](docs/development/wpf.md)
- [User Interface Guide](docs/ui/README.md)
- [Settings Configuration](docs/configuration/settings.md)

---

## �📋 Overview

Winhance-FS features the **Borg Theme Studio**, a powerful theming system that allows complete customization of the application's appearance. With 8 pre-designed color palettes and real-time click-to-change functionality, users can instantly transform the look and feel of their file manager.

---

## 🎨 Borg Theme Studio

### Theme Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Borg Theme Studio                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────┐ │
│  │   Color Palettes│    │   Live Preview │    │  Editor     │ │
│  │                 │    │                 │    │             │ │
│  │ • Borg Original │    │ • Real-time    │    │ • RGB Sliders│ │
│  │ • Arctic        │    │ • All Views    │    │ • HSL Wheel  │ │
│  │ • Solar         │    │ • Transitions  │    │ • Hex Input  │ │
│  │ • Matrix        │    │ • Animations   │    │ • Opacity   │ │
│  │ • Crimson       │    └─────────────────┘    │ • Gradients │ │
│  │ • Forest        │                         │             │ │
│  │ • Ocean         │    ┌─────────────────┐    └─────────────┘ │
│  │ • Custom        │    │   Export/Import│                    │
│  └─────────────────┘    │                 │                    │
│           │              │ • Save Theme   │                    │
│           ▼              │ • Load Theme   │                    │
│  ┌─────────────────────────────────────────────────────┐       │
│  │              Theme Application                       │       │
│  │                                                     │       │
│  │ • Instant Apply  • Smooth Transitions  • Persist   │       │
│  └─────────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🎭 Color Palettes

### 1. Borg Original (Default)
```json
{
  "name": "Borg Original",
  "description": "Classic cyberpunk green theme",
  "colors": {
    "primary": "#00FF41",
    "secondary": "#00CC33",
    "background": "#0A0A0A",
    "surface": "#1A1A1A",
    "text": "#00FF41",
    "textSecondary": "#00CC33",
    "accent": "#00FF88",
    "error": "#FF0040",
    "warning": "#FFAA00",
    "success": "#00FF88"
  }
}
```

### 2. Arctic
```json
{
  "name": "Arctic",
  "description": "Cool blue and cyan palette",
  "colors": {
    "primary": "#00E5FF",
    "secondary": "#00ACC1",
    "background": "#0D1117",
    "surface": "#161B22",
    "text": "#E6EDF3",
    "textSecondary": "#8B949E",
    "accent": "#58A6FF",
    "error": "#F85149",
    "warning": "#D29922",
    "success": "#3FB950"
  }
}
```

### 3. Solar
```json
{
  "name": "Solar",
  "description": "Warm orange and yellow theme",
  "colors": {
    "primary": "#FFB300",
    "secondary": "#FF8C00",
    "background": "#1A1A00",
    "surface": "#2D2D00",
    "text": "#FFD54F",
    "textSecondary": "#FFB300",
    "accent": "#FFC107",
    "error": "#FF5252",
    "warning": "#FFD740",
    "success": "#69F0AE"
  }
}
```

### 4. Matrix
```json
{
  "name": "Matrix",
  "description": "Digital rain green aesthetic",
  "colors": {
    "primary": "#00FF41",
    "secondary": "#008F11",
    "background": "#000000",
    "surface": "#0D0D0D",
    "text": "#00FF41",
    "textSecondary": "#00CC33",
    "accent": "#00FF88",
    "error": "#FF1744",
    "warning": "#FFEA00",
    "success": "#00E676"
  }
}
```

### 5. Crimson
```json
{
  "name": "Crimson",
  "description": "Bold red and dark theme",
  "colors": {
    "primary": "#FF1744",
    "secondary": "#D50000",
    "background": "#1A0000",
    "surface": "#2D0000",
    "text": "#FF5252",
    "textSecondary": "#FF1744",
    "accent": "#FF6B6B",
    "error": "#FF1744",
    "warning": "#FFD600",
    "success": "#00E676"
  }
}
```

### 6. Forest
```json
{
  "name": "Forest",
  "description": "Natural green and brown tones",
  "colors": {
    "primary": "#66BB6A",
    "secondary": "#4CAF50",
    "background": "#0D1B0D",
    "surface": "#1B2E1B",
    "text": "#A5D6A7",
    "textSecondary": "#81C784",
    "accent": "#8BC34A",
    "error": "#E53935",
    "warning": "#FFB300",
    "success": "#00C853"
  }
}
```

### 7. Ocean
```json
{
  "name": "Ocean",
  "description": "Deep blue and cyan depths",
  "colors": {
    "primary": "#039BE5",
    "secondary": "#0288D1",
    "background": "#0A1929",
    "surface": "#132F4C",
    "text": "#B3E5FC",
    "textSecondary": "#4FC3F7",
    "accent": "#29B6F6",
    "error": "#FF5252",
    "warning": "#FFD740",
    "success": "#00E676"
  }
}
```

### 8. Custom
```json
{
  "name": "Custom",
  "description": "User-defined theme",
  "colors": {
    "primary": "#USER_DEFINED",
    "secondary": "#USER_DEFINED",
    "background": "#USER_DEFINED",
    "surface": "#USER_DEFINED",
    "text": "#USER_DEFINED",
    "textSecondary": "#USER_DEFINED",
    "accent": "#USER_DEFINED",
    "error": "#USER_DEFINED",
    "warning": "#USER_DEFINED",
    "success": "#USER_DEFINED"
  }
}
```

---

## 🖱️ Click-to-Change Functionality

### Theme Selection Interface

```xml
<!-- Theme Palette Selector -->
<StackPanel Orientation="Horizontal" Margin="10">
    <TextBlock Text="Theme:" VerticalAlignment="Center" Margin="0,0,10,0"/>
    
    <!-- Theme Palette Buttons -->
    <Button Name="BorgOriginalBtn" 
            Click="ThemeButton_Click"
            Tag="BorgOriginal"
            Style="{StaticResource ThemeButtonStyle}">
        <Rectangle Width="30" Height="30" Fill="#00FF41"/>
    </Button>
    
    <Button Name="ArcticBtn" 
            Click="ThemeButton_Click"
            Tag="Arctic"
            Style="{StaticResource ThemeButtonStyle}">
        <Rectangle Width="30" Height="30" Fill="#00E5FF"/>
    </Button>
    
    <!-- ... more theme buttons ... -->
</StackPanel>
```

### Theme Application Code

```csharp
// ThemeManager.cs
public partial class ThemeManager : ObservableObject
{
    [ObservableProperty]
    private ThemeInfo _currentTheme;

    [ObservableProperty]
    private bool _isTransitioning;

    // Click-to-change theme handler
    public async Task ApplyThemeAsync(string themeName)
    {
        IsTransitioning = true;
        
        try
        {
            var theme = await LoadThemeAsync(themeName);
            
            // Smooth transition animation
            await AnimateThemeTransitionAsync();
            
            // Apply colors to resources
            ApplyThemeResources(theme);
            
            // Notify all components
            ThemeChanged?.Invoke(this, theme);
            
            CurrentTheme = theme;
            
            // Save preference
            await SaveThemePreferenceAsync(themeName);
        }
        finally
        {
            IsTransitioning = false;
        }
    }

    private void ApplyThemeResources(ThemeInfo theme)
    {
        var app = Application.Current;
        var resources = app.Resources;
        
        // Update color resources
        resources["PrimaryColor"] = theme.Colors.Primary;
        resources["SecondaryColor"] = theme.Colors.Secondary;
        resources["BackgroundColor"] = theme.Colors.Background;
        resources["SurfaceColor"] = theme.Colors.Surface;
        resources["TextColor"] = theme.Colors.Text;
        resources["TextSecondaryColor"] = theme.Colors.TextSecondary;
        resources["AccentColor"] = theme.Colors.Accent;
        resources["ErrorColor"] = theme.Colors.Error;
        resources["WarningColor"] = theme.Colors.Warning;
        resources["SuccessColor"] = theme.Colors.Success;
    }
}
```

---

## 🎨 Theme Studio Features

### 1. Live Color Editor

```xml
<!-- Color Picker Control -->
<Grid Margin="20">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="Auto"/>
    </Grid.ColumnDefinitions>
    
    <!-- Color Preview -->
    <Rectangle Grid.Column="0" 
               Width="60" Height="60" 
               Fill="{Binding SelectedColor}"
               Stroke="{Binding TextColor}"
               StrokeThickness="2"/>
    
    <!-- Color Sliders -->
    <StackPanel Grid.Column="1" Margin="20,0">
        <!-- RGB Sliders -->
        <Slider Minimum="0" Maximum="255" 
                Value="{Binding RedChannel}" 
                Foreground="#FF0000"/>
        <Slider Minimum="0" Maximum="255" 
                Value="{Binding GreenChannel}" 
                Foreground="#00FF00"/>
        <Slider Minimum="0" Maximum="255" 
                Value="{Binding BlueChannel}" 
                Foreground="#0000FF"/>
        
        <!-- HSL Sliders -->
        <Slider Minimum="0" Maximum="360" 
                Value="{Binding Hue}"/>
        <Slider Minimum="0" Maximum="100" 
                Value="{Binding Saturation}"/>
        <Slider Minimum="0" Maximum="100" 
                Value="{Binding Lightness}"/>
    </StackPanel>
    
    <!-- Hex Input -->
    <TextBox Grid.Column="2" 
             Text="{Binding HexColor, UpdateSourceTrigger=PropertyChanged}"
             Width="100" Height="30"/>
</Grid>
```

### 2. Real-time Preview

```csharp
// Preview Window
public partial class ThemePreviewWindow : Window
{
    public ThemePreviewWindow(ThemeInfo theme)
    {
        InitializeComponent();
        ApplyPreviewTheme(theme);
    }

    private void ApplyPreviewTheme(ThemeInfo theme)
    {
        // Apply to all preview elements
        this.Resources["PrimaryColor"] = theme.Colors.Primary;
        this.Resources["BackgroundColor"] = theme.Colors.Background;
        
        // Update sample UI elements
        SampleButton.Background = new SolidColorBrush(theme.Colors.Primary);
        SampleTextBlock.Foreground = new SolidColorBrush(theme.Colors.Text);
        SampleBorder.Background = new SolidColorBrush(theme.Colors.Surface);
    }
}
```

### 3. Theme Export/Import

```csharp
// Theme Serialization
public class ThemeSerializer
{
    public async Task ExportThemeAsync(ThemeInfo theme, string path)
    {
        var json = JsonSerializer.Serialize(theme, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        await File.WriteAllTextAsync(path, json);
    }

    public async Task<ThemeInfo> ImportThemeAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<ThemeInfo>(json);
    }
}
```

---

## 🎭 Theme Components

### 1. Color Resources

```xml
<!-- Themes/BorgOriginal.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <!-- Primary Colors -->
    <Color x:Key="PrimaryColor">#00FF41</Color>
    <Color x:Key="SecondaryColor">#00CC33</Color>
    <Color x:Key="AccentColor">#00FF88</Color>
    
    <!-- Background Colors -->
    <Color x:Key="BackgroundColor">#0A0A0A</Color>
    <Color x:Key="SurfaceColor">#1A1A1A</Color>
    <Color x:Key="OverlayColor">#2A2A2A</Color>
    
    <!-- Text Colors -->
    <Color x:Key="TextColor">#00FF41</Color>
    <Color x:Key="TextSecondaryColor">#00CC33</Color>
    <Color x:Key="TextDisabledColor">#666666</Color>
    
    <!-- Status Colors -->
    <Color x:Key="ErrorColor">#FF0040</Color>
    <Color x:Key="WarningColor">#FFAA00</Color>
    <Color x:Key="SuccessColor">#00FF88</Color>
    <Color x:Key="InfoColor">#0099FF</Color>
    
    <!-- Brush Resources -->
    <SolidColorBrush x:Key="PrimaryBrush" Color="{StaticResource PrimaryColor}"/>
    <SolidColorBrush x:Key="SecondaryBrush" Color="{StaticResource SecondaryColor}"/>
    <SolidColorBrush x:Key="BackgroundBrush" Color="{StaticResource BackgroundColor}"/>
    <SolidColorBrush x:Key="SurfaceBrush" Color="{StaticResource SurfaceColor}"/>
    <SolidColorBrush x:Key="TextBrush" Color="{StaticResource TextColor}"/>
    <SolidColorBrush x:Key="TextSecondaryBrush" Color="{StaticResource TextSecondaryColor}"/>
</ResourceDictionary>
```

### 2. Theme-Aware Controls

```csharp
// ThemeAwareButton.cs
public class ThemeAwareButton : Button
{
    static ThemeAwareButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ThemeAwareButton),
            new FrameworkPropertyMetadata(typeof(ThemeAwareButton)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        
        // Subscribe to theme changes
        ThemeManager.Instance.ThemeChanged += OnThemeChanged;
    }

    private void OnThemeChanged(object sender, ThemeInfo e)
    {
        // Update visual states
        VisualStateManager.GoToState(this, "ThemeChanged", true);
    }
}
```

### 3. Animated Transitions

```xml
<!-- Theme Transition Storyboard -->
<Storyboard x:Key="ThemeTransitionStoryboard">
    <ColorAnimation Storyboard.TargetProperty="(Control.Background).(SolidColorBrush.Color)"
                    Duration="0:0:0.3"
                    To="{Binding Source={x:Static ThemeManager.Instance}, Path=NewTheme.Colors.Background}">
        <ColorAnimation.EasingFunction>
            <CubicEase EasingMode="EaseInOut"/>
        </ColorAnimation.EasingFunction>
    </ColorAnimation>
    
    <ColorAnimation Storyboard.TargetProperty="(Control.Foreground).(SolidColorBrush.Color)"
                    Duration="0:0:0.3"
                    To="{Binding Source={x:Static ThemeManager.Instance}, Path=NewTheme.Colors.Text}">
        <ColorAnimation.EasingFunction>
            <CubicEase EasingMode="EaseInOut"/>
        </ColorAnimation.EasingFunction>
    </ColorAnimation>
</Storyboard>
```

---

## 🔧 Advanced Theming

### 1. Custom Theme Creation

```csharp
// Create custom theme programmatically
public class CustomThemeBuilder
{
    public ThemeInfo BuildTheme(string name, ThemeBaseColors colors)
    {
        return new ThemeInfo
        {
            Name = name,
            Colors = new ThemeColors
            {
                Primary = colors.Primary,
                Secondary = AdjustBrightness(colors.Primary, -0.2),
                Background = colors.Background,
                Surface = AdjustBrightness(colors.Background, 0.1),
                Text = GetContrastColor(colors.Background),
                TextSecondary = AdjustOpacity(GetContrastColor(colors.Background), 0.7),
                Accent = colors.Accent,
                Error = GenerateErrorColor(colors),
                Warning = GenerateWarningColor(colors),
                Success = GenerateSuccessColor(colors)
            },
            GeneratedAt = DateTime.UtcNow
        };
    }

    private Color AdjustBrightness(Color color, double factor)
    {
        // Convert to HSL, adjust lightness, convert back
        var hsl = ColorToHSL(color);
        hsl.L = Math.Clamp(hsl.L + factor, 0, 1);
        return HSLToColor(hsl);
    }
}
```

### 2. Theme Variants

```json
{
  "name": "Borg Original",
  "variants": {
    "light": {
      "primary": "#00AA28",
      "background": "#FFFFFF",
      "text": "#000000"
    },
    "dark": {
      "primary": "#00FF41",
      "background": "#0A0A0A",
      "text": "#00FF41"
    },
    "dim": {
      "primary": "#00CC33",
      "background": "#1A1A1A",
      "text": "#E0E0E0"
    }
  }
}
```

### 3. Component-Specific Overrides

```xml
<!-- Component-specific theme overrides -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
    <!-- File Manager specific colors -->
    <Color x:Key="FileListBackground">#0F0F0F</Color>
    <Color x:Key="FileListSelectedBackground">#00FF88</Color>
    <Color x:Key="FileListHoverBackground">#1A1A1A</Color>
    
    <!-- Search bar specific colors -->
    <Color x:Key="SearchBarBackground">#1A1A1A</Color>
    <Color x:Key="SearchBarBorder">#333333</Color>
    <Color x:Key="SearchBarText">#00FF41</Color>
</ResourceDictionary>
```

---

## 📱 Theme Persistence

### 1. Settings Storage

```csharp
// ThemeSettings.cs
public class ThemeSettings
{
    public string SelectedTheme { get; set; } = "BorgOriginal";
    public bool EnableTransitions { get; set; } = true;
    public double TransitionDuration { get; set; } = 300;
    public Dictionary<string, string> CustomColors { get; set; } = new();
    public string LastModified { get; set; } = DateTime.UtcNow.ToString("O");
}
```

### 2. Theme Loading on Startup

```csharp
// App.xaml.cs
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    
    // Load saved theme
    var settings = SettingsService.Load<ThemeSettings>();
    ThemeManager.Instance.ApplyThemeAsync(settings.SelectedTheme).Wait();
    
    // Apply theme transitions setting
    ThemeManager.Instance.EnableTransitions = settings.EnableTransitions;
    ThemeManager.Instance.TransitionDuration = TimeSpan.FromMilliseconds(settings.TransitionDuration);
}
```

---

## 🎨 Best Practices

### 1. Color Accessibility

```csharp
// Ensure sufficient contrast ratio
public class ColorAccessibility
{
    public static double CalculateContrastRatio(Color foreground, Color background)
    {
        var l1 = CalculateLuminance(foreground);
        var l2 = CalculateLuminance(background);
        
        var lighter = Math.Max(l1, l2);
        var darker = Math.Min(l1, l2);
        
        return (lighter + 0.05) / (darker + 0.05);
    }

    public static bool IsAccessible(Color foreground, Color background)
    {
        return CalculateContrastRatio(foreground, background) >= 4.5; // WCAG AA
    }
}
```

### 2. Performance Optimization

```csharp
// Freeze brushes for better performance
public static class ThemeOptimizer
{
    public static void OptimizeTheme(ResourceDictionary theme)
    {
        foreach (var resource in theme.Values)
        {
            if (resource is SolidColorBrush brush)
            {
                brush.Freeze();
            }
        }
    }
}
```

### 3. Theme Testing

```csharp
// Automated theme testing
[TestFixture]
public class ThemeTests
{
    [Test]
    public void AllThemes_HaveAccessibleColors()
    {
        foreach (var theme in ThemeManager.Instance.AvailableThemes)
        {
            var isAccessible = ColorAccessibility.IsAccessible(
                theme.Colors.Text, 
                theme.Colors.Background);
            
            Assert.IsTrue(isAccessible, $"Theme {theme.Name} has poor contrast");
        }
    }
}
```

---

## 🔗 Navigation

**Related Documents:**
- [Architecture Documentation](ARCHITECTURE.md)
- [WPF Development Guide](docs/development/wpf.md)
- [User Interface Guide](docs/ui/README.md)
- [Settings Configuration](docs/configuration/settings.md)

---

## 📚 References

### Color Tools
- [Adobe Color](https://color.adobe.com/) - Color palette generator
- [Coolors.co](https://coolors.co/) - Color scheme generator
- [Contrast Checker](https://webaim.org/resources/contrastchecker/) - WCAG contrast validation

### WPF Theming Resources
- [WPF Resource System](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/advanced/merged-resource-dictionaries)
- [Dynamic Resources](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/advanced/dynamic-resources)
- [Styles and Templates](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/controls/styles-templates-overview)

---

*Express yourself with colors! The Borg Theme Studio puts the power of customization in your hands.* 🎨
