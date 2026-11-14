# Winhance-FS Development Guide

## Overview

This guide provides comprehensive information for developers contributing to Winhance-FS, including setup instructions, coding standards, development workflows, and best practices.

---

## 🚀 Getting Started

### Prerequisites

#### Required Software
- **.NET 9.0 SDK** - Latest version
- **Visual Studio 2022** (17.8+) or **VS Code** with C# extension
- **Rust 1.75+** - For native components
- **Python 3.11+** - For AI services
- **Git** - Version control
- **Docker Desktop** - Container support

#### Recommended Tools
- **GitKraken** or **SourceTree** - Git GUI
- **Postman** - API testing
- **LINQPad** - C# scripting
- **Rust Analyzer** - Rust IDE support
- **Python Extension Pack** - VS Code Python tools

### Repository Setup

1. **Clone the Repository**
```bash
git clone https://github.com/your-org/Winhance-FS.git
cd Winhance-FS
```

2. **Initialize Submodules**
```bash
git submodule update --init --recursive
```

3. **Setup Development Environment**
```bash
# Install .NET dependencies
dotnet restore

# Install Rust dependencies
cd src/nexus_core
cargo build

# Install Python dependencies
cd ../nexus_ai
pip install -r requirements.txt

# Install MCP server dependencies
cd ../nexus_mcp
pip install -r requirements.txt
```

4. **Configure Development Settings**
```bash
# Copy configuration templates
cp config/appsettings.Development.json.example config/appsettings.Development.json
cp .env.example .env

# Edit configuration files
# Set your development paths and settings
```

### Running the Application

#### Option 1: Visual Studio
1. Open `Winhance-FS.sln`
2. Set `Winhance.WPF` as startup project
3. Press F5 to run

#### Option 2: Command Line
```bash
# Run WPF application
dotnet run --project src/Winhance.WPF

# Run with specific configuration
dotnet run --project src/Winhance.WPF --configuration Debug
```

#### Option 3: Docker
```bash
# Build and run all services
docker-compose up -d

# View logs
docker-compose logs -f
```

---

## 🏗️ Project Structure

```
Winhance-FS/
├── src/                          # Source code
│   ├── Winhance.Core/           # Core interfaces and models
│   │   ├── Features/            # Feature-specific interfaces
│   │   │   ├── FileManager/     # File manager interfaces
│   │   │   ├── Search/          # Search interfaces
│   │   │   ├── Archive/         # Archive interfaces
│   │   │   └── Synchronization/ # Sync interfaces
│   │   ├── Models/              # Domain models
│   │   ├── Enums/               # Enumerations
│   │   └── Events/              # Domain events
│   ├── Winhance.Infrastructure/ # Infrastructure services
│   │   ├── Services/            # Windows services
│   │   ├── Wrappers/            # Win32 API wrappers
│   │   └── Extensions/          # DI extensions
│   ├── Winhance.WPF/           # WPF application
│   │   ├── Features/            # Feature modules
│   │   │   ├── FileManager/     # File manager UI
│   │   │   │   ├── Views/       # XAML views
│   │   │   │   ├── ViewModels/  # ViewModels
│   │   │   │   ├── Controls/    # Custom controls
│   │   │   │   └── Converters/  # Value converters
│   │   │   ├── Search/          # Search UI
│   │   │   ├── Archive/         # Archive UI
│   │   │   └── Settings/        # Settings UI
│   │   ├── Themes/              # Theme resources
│   │   ├── Resources/           # Shared resources
│   │   └── Converters/          # Global converters
│   ├── nexus_core/              # Rust native engine
│   │   ├── src/                 # Rust source files
│   │   │   ├── mft/             # MFT reader
│   │   │   ├── usn/             # USN journal
│   │   │   ├── tantivy/         # Search integration
│   │   │   └── ffi/             # FFI bindings
│   │   └── Cargo.toml           # Rust dependencies
│   ├── nexus_ai/                # Python AI services
│   │   ├── agents/              # AI agents
│   │   ├── classifiers/         # File classification
│   │   ├── analyzers/           # Content analysis
│   │   └── requirements.txt     # Python dependencies
│   ├── nexus_mcp/               # MCP server
│   │   ├── tools/               # MCP tools
│   │   ├── server.py            # Server implementation
│   │   └── requirements.txt     # Python dependencies
│   └── nexus_cli/               # Command-line interface
│       ├── commands/            # CLI commands
│       ├── utils/               # Utilities
│       └── setup.py             # Package setup
├── tests/                       # Test projects
│   ├── Winhance.Core.Tests/     # Core tests
│   ├── Winhance.WPF.Tests/      # UI tests
│   ├── Integration/             # Integration tests
│   └── E2E/                     # End-to-end tests
├── docs/                        # Documentation
├── scripts/                     # Build and utility scripts
├── config/                      # Configuration files
└── .github/                     # GitHub workflows
```

---

## 📝 Coding Standards

### C# Guidelines

#### Naming Conventions
```csharp
// Classes: PascalCase
public class FileManagerService
{
    // Fields: _camelCase
    private readonly IFileSystemService _fileSystem;
    
    // Properties: PascalCase
    public string CurrentPath { get; set; }
    
    // Methods: PascalCase
    public async Task CopyFileAsync(string source, string destination)
    {
        // Local variables: camelCase
        var fileInfo = new FileInfo(source);
        
        // Constants: PascalCase
        const int MaxRetries = 3;
    }
}

// Interfaces: I prefix
public interface IFileManagerService
{
    // Async methods: Async suffix
    Task<FileItem[]> GetFilesAsync(string path);
}

// Enums: PascalCase
public enum FileOperationType
{
    Copy,
    Move,
    Delete
}
```

#### Code Organization
```csharp
// File: FileManagerService.cs
namespace Winhance.Infrastructure.Services
{
    // Regions for organization
    #region Using Statements
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    #endregion

    /// <summary>
    /// Service for managing file operations.
    /// </summary>
    public class FileManagerService : IFileManagerService
    {
        #region Fields
        private readonly ILogger<FileManagerService> _logger;
        #endregion

        #region Constructor
        public FileManagerService(ILogger<FileManagerService> logger)
        {
            _logger = logger;
        }
        #endregion

        #region Public Methods
        public async Task CopyFileAsync(string source, string destination)
        {
            // Implementation
        }
        #endregion

        #region Private Methods
        private async Task ValidatePathsAsync(string source, string destination)
        {
            // Implementation
        }
        #endregion
    }
}
```

#### Async/Await Best Practices
```csharp
// DO: Use async all the way down
public async Task ProcessFilesAsync(string[] paths)
{
    var tasks = new List<Task>();
    
    foreach (var path in paths)
    {
        tasks.Add(ProcessFileAsync(path));
    }
    
    await Task.WhenAll(tasks);
}

// DO: Configure await
public async Task<string> ReadFileAsync(string path)
{
    var content = await File.ReadAllTextAsync(path).ConfigureAwait(false);
    return content;
}

// DON'T: Block on async code
public string GetFileContent(string path) // Bad
{
    return File.ReadAllTextAsync(path).Result; // Deadlock risk
}

// DO: Use ValueTask for high-frequency methods
public ValueTask<FileItem> GetFileAsync(string path)
{
    // Implementation
}
```

#### Exception Handling
```csharp
public async Task CopyFileAsync(string source, string destination)
{
    try
    {
        ValidatePaths(source, destination);
        await PerformCopyAsync(source, destination);
    }
    catch (FileNotFoundException ex)
    {
        _logger.LogError(ex, "Source file not found: {Source}", source);
        throw new FileManagerException("Source file not found", ex);
    }
    catch (UnauthorizedAccessException ex)
    {
        _logger.LogError(ex, "Access denied");
        throw new FileManagerException("Access denied", ex);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error copying file");
        throw new FileManagerException("Failed to copy file", ex);
    }
}
```

### WPF Guidelines

#### MVVM Pattern
```csharp
// ViewModel with CommunityToolkit.Mvvm
public partial class FileManagerViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<FileItem> _files = new();
    
    [ObservableProperty]
    private FileItem? _selectedFile;
    
    [ObservableProperty]
    private bool _isLoading;
    
    [RelayCommand]
    private async Task LoadFilesAsync(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        
        try
        {
            IsLoading = true;
            var files = await _fileService.GetFilesAsync(path);
            
            Files.Clear();
            foreach (var file in files)
            {
                Files.Add(file);
            }
        }
        catch (Exception ex)
        {
            // Handle error
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand(CanExecute = nameof(CanDeleteFile))]
    private async Task DeleteFileAsync(FileItem? file)
    {
        if (file == null) return;
        
        await _fileService.DeleteAsync(file.FullPath);
        Files.Remove(file);
    }
    
    private bool CanDeleteFile(FileItem? file)
    {
        return file != null && !file.IsDirectory;
    }
}
```

#### XAML Best Practices
```xml
<!-- Use styles and resources -->
<UserControl x:Class="Winhance.WPF.Features.FileManager.Views.FileManagerView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <UserControl.Resources>
        <Style x:Key="FileListItemStyle" TargetType="ListBoxItem">
            <!-- Style definition -->
        </Style>
    </UserControl.Resources>
    
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>
        
        <!-- Toolbar -->
        <ToolBar Grid.Row="0">
            <Button Content="Copy" Command="{Binding CopyCommand}"/>
            <Button Content="Move" Command="{Binding MoveCommand}"/>
            <Button Content="Delete" Command="{Binding DeleteCommand}"/>
        </ToolBar>
        
        <!-- File List -->
        <ListBox Grid.Row="1"
                 ItemsSource="{Binding Files}"
                 SelectedItem="{Binding SelectedFile}"
                 ItemContainerStyle="{StaticResource FileListItemStyle}">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal">
                        <Image Source="{Binding Icon}" Width="16" Height="16"/>
                        <TextBlock Text="{Binding Name}" Margin="5,0"/>
                    </StackPanel>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>
    </Grid>
</UserControl>
```

### Rust Guidelines

#### Code Style
```rust
// Use rustfmt for consistent formatting
// Use clippy for linting

pub struct MftReader {
    volume_handle: HANDLE,
    buffer: Vec<u8>,
}

impl MftReader {
    /// Creates a new MFT reader for the given volume.
    /// 
    /// # Arguments
    /// * `volume` - Volume path (e.g., "C:")
    /// 
    /// # Returns
    /// * `Result<Self, MftError>` - Reader instance or error
    pub fn new(volume: &str) -> Result<Self, MftError> {
        // Implementation
    }
    
    /// Enumerates all files in the MFT.
    pub fn enumerate_files(&self) -> impl Iterator<Item = FileRecord> {
        // Implementation
    }
}

// Use Result for error handling
pub type Result<T> = std::result::Result<T, WinhanceError>;

// Define custom error types
#[derive(Debug, thiserror::Error)]
pub enum WinhanceError {
    #[error("IO error: {0}")]
    Io(#[from] std::io::Error),
    
    #[error("MFT error: {0}")]
    Mft(String),
    
    #[error("Invalid path: {0}")]
    InvalidPath(String),
}
```

### Python Guidelines

#### Code Style
```python
# Follow PEP 8
# Use type hints
# Use docstrings

from typing import List, Dict, Optional
from dataclasses import dataclass

@dataclass
class FileClassification:
    """Represents a file classification result."""
    
    file_path: str
    category: str
    confidence: float
    metadata: Dict[str, Any]

class FileClassifier:
    """Classifies files based on content and metadata."""
    
    def __init__(self, model_path: str):
        """Initialize the classifier.
        
        Args:
            model_path: Path to the trained model
        """
        self.model_path = model_path
        self._load_model()
    
    def classify_file(self, file_path: str) -> Optional[FileClassification]:
        """Classify a single file.
        
        Args:
            file_path: Path to the file to classify
            
        Returns:
            Classification result or None if classification fails
        """
        try:
            # Implementation
            return classification
        except Exception as e:
            logger.error(f"Failed to classify {file_path}: {e}")
            return None
```

---

## 🧪 Testing

### Unit Testing

#### C# Unit Tests
```csharp
[TestFixture]
public class FileManagerServiceTests
{
    private Mock<IFileSystemService> _mockFileSystem;
    private FileManagerService _fileManager;
    
    [SetUp]
    public void Setup()
    {
        _mockFileSystem = new Mock<IFileSystemService>();
        _fileManager = new FileManagerService(_mockFileSystem.Object);
    }
    
    [Test]
    public async Task CopyFile_WithValidPaths_ShouldCallFileSystem()
    {
        // Arrange
        var source = "C:\\test.txt";
        var destination = "C:\\backup\\test.txt";
        
        // Act
        await _fileManager.CopyFileAsync(source, destination);
        
        // Assert
        _mockFileSystem.Verify(
            x => x.CopyFileAsync(source, destination, It.IsAny<IProgress<OperationProgress>>()),
            Times.Once);
    }
    
    [Test]
    public async Task CopyFile_WithNullSource_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _fileManager.CopyFileAsync(null!, "destination"));
    }
    
    [TestCase("source.txt", "destination.txt", true)]
    [TestCase("source.txt", "", false)]
    public async Task CopyFile_WithVariousInputs_ShouldBehaveExpected(
        string source, string destination, bool shouldSucceed)
    {
        // Implementation
    }
}
```

#### Rust Unit Tests
```rust
#[cfg(test)]
mod tests {
    use super::*;
    
    #[test]
    fn test_mft_reader_creation() {
        let result = MftReader::new("C:");
        assert!(result.is_ok());
    }
    
    #[tokio::test]
    async fn test_file_enumeration() {
        let reader = MftReader::new("C:").unwrap();
        let files: Vec<FileRecord> = reader.enumerate_files().take(10).collect();
        assert!(!files.is_empty());
    }
    
    #[test]
    fn test_invalid_volume() {
        let result = MftReader::new("X:");
        assert!(result.is_err());
    }
}
```

#### Python Unit Tests
```python
import pytest
from unittest.mock import Mock, patch

class TestFileClassifier:
    def setup_method(self):
        self.classifier = FileClassifier("model.pkl")
    
    def test_classify_pdf_file(self):
        # Arrange
        file_path = "test.pdf"
        
        # Act
        result = self.classifier.classify_file(file_path)
        
        # Assert
        assert result is not None
        assert result.category == "Document"
        assert result.confidence > 0.8
    
    @patch('os.path.exists')
    def test_classify_nonexistent_file(self, mock_exists):
        # Arrange
        mock_exists.return_value = False
        
        # Act
        result = self.classifier.classify_file("nonexistent.pdf")
        
        # Assert
        assert result is None
```

### Integration Testing

#### Database Integration
```csharp
[TestFixture]
public class SearchServiceIntegrationTests
{
    private ServiceProvider _serviceProvider;
    private ISearchService _searchService;
    
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var services = new ServiceCollection();
        services.AddSearchServices()
                .AddTestDatabase();
        
        _serviceProvider = services.BuildServiceProvider();
        _searchService = _serviceProvider.GetRequiredService<ISearchService>();
    }
    
    [Test]
    public async Task SearchFiles_WithRealDatabase_ShouldReturnResults()
    {
        // Arrange
        await SeedTestDataAsync();
        
        // Act
        var results = await _searchService.SearchAsync("*.txt");
        
        // Assert
        Assert.IsNotEmpty(results);
        Assert.IsTrue(results.All(r => r.Name.EndsWith(".txt")));
    }
}
```

### UI Testing

#### WPF UI Tests
```csharp
[TestFixture]
public class FileManagerUITests
{
    private FileManagerView _view;
    private FileManagerViewModel _viewModel;
    
    [SetUp]
    public void Setup()
    {
        _viewModel = new FileManagerViewModel();
        _view = new FileManagerView { DataContext = _viewModel };
    }
    
    [Test]
    public void CopyButton_WhenNoFileSelected_ShouldBeDisabled()
    {
        // Arrange
        var copyButton = _view.FindChild<Button>("CopyButton");
        
        // Act
        _viewModel.SelectedFile = null;
        
        // Assert
        Assert.IsFalse(copyButton.IsEnabled);
    }
    
    [Test]
    public async Task LoadFiles_WhenCalled_ShouldUpdateUI()
    {
        // Arrange
        var fileListView = _view.FindChild<ListBox>("FileListView");
        
        // Act
        await _viewModel.LoadFilesCommand.ExecuteAsync("C:\\Test");
        
        // Assert
        Assert.IsNotEmpty(fileListView.Items);
    }
}
```

---

## 🔄 Development Workflow

### Git Workflow

#### Branch Strategy
```
main                    # Production branch
├── develop             # Integration branch
│   ├── feature/search  # Feature branches
│   ├── feature/sync
│   └── feature/ui
├── release/v0.1.0     # Release branches
└── hotfix/fix-123     # Hotfix branches
```

#### Commit Messages
```
feat(filemanager): Add copy file functionality
fix(search): Resolve null reference exception
docs(readme): Update installation instructions
style(wpf): Format XAML according to standards
refactor(core): Extract common interface
test(search): Add unit tests for search service
```

#### Pull Request Process
1. Create feature branch from `develop`
2. Implement changes with tests
3. Ensure all tests pass
4. Update documentation
5. Create pull request to `develop`
6. Code review
7. Merge after approval

### Build Process

#### Local Build
```bash
# Build entire solution
dotnet build Winhance-FS.sln

# Build specific project
dotnet build src/Winhance.WPF

# Build with configuration
dotnet build Winhance-FS.sln --configuration Release

# Build and run tests
dotnet test
```

#### CI/CD Pipeline
```yaml
# .github/workflows/ci.yml
name: CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ develop ]

jobs:
  build:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Setup Rust
      uses: actions-rs/toolchain@v1
      with:
        toolchain: stable
    
    - name: Setup Python
      uses: actions/setup-python@v4
      with:
        python-version: '3.11'
    
    - name: Restore dependencies
      run: |
        dotnet restore
        pip install -r src/nexus_ai/requirements.txt
    
    - name: Build
      run: dotnet build --configuration Release
    
    - name: Test
      run: dotnet test --no-build --configuration Release
    
    - name: Package
      run: dotnet pack --configuration Release
```

---

## 🔧 Debugging

### Debugging WPF Applications

#### Breakpoints and Inspection
```csharp
// Use DebuggerDisplay attribute
[DebuggerDisplay("Name = {Name}, Size = {Size}")]
public class FileItem
{
    public string Name { get; set; }
    public long Size { get; set; }
}

// Use DebuggerStepThrough for boilerplate
[DebuggerStepThrough]
private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
{
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
```

#### Visual Studio Debugger Tips
1. **Data Tips**: Hover over variables to inspect
2. **Immediate Window**: Execute code while debugging
3. **Watch Window**: Monitor expressions
4. **Call Stack**: Navigate call hierarchy
5. **IntelliTrace**: Historical debugging

#### Debugging Bindings
```xml
<!-- Enable binding tracing -->
<Window xmlns:diag="clr-namespace:System.Diagnostics;assembly=WindowsBase">
    <Window.Resources>
        <Style TargetType="TextBlock">
            <Setter Property="diag:PresentationTraceSources.TraceLevel" Value="High"/>
        </Style>
    </Window.Resources>
</Window>
```

### Debugging Rust Code

#### VS Code with Rust Analyzer
```json
// .vscode/launch.json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Debug Rust",
            "type": "lldb",
            "request": "launch",
            "program": "${workspaceFolder}/target/debug/nexus_core.dll",
            "args": [],
            "cwd": "${workspaceFolder}"
        }
    ]
}
```

#### Logging
```rust
use log::{debug, info, warn, error};

pub fn process_file(path: &Path) -> Result<()> {
    debug!("Processing file: {:?}", path);
    
    match process_file_internal(path) {
        Ok(_) => {
            info!("Successfully processed: {:?}", path);
            Ok(())
        }
        Err(e) => {
            error!("Failed to process {:?}: {}", path, e);
            Err(e)
        }
    }
}
```

### Debugging Python Services

#### pdb Debugger
```python
import pdb

def process_files(files):
    for file in files:
        pdb.set_trace()  # Breakpoint
        # Debug code here
        process_file(file)
```

#### VS Code Python Debugging
```json
// .vscode/launch.json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Python: MCP Server",
            "type": "python",
            "request": "launch",
            "program": "${workspaceFolder}/src/nexus_mcp/server.py",
            "console": "integratedTerminal",
            "env": {
                "PYTHONPATH": "${workspaceFolder}/src"
            }
        }
    ]
}
```

---

## 📊 Performance Optimization

### C# Performance Tips

#### Memory Management
```csharp
// Use ArrayPool for large arrays
using System.Buffers;

public void ProcessLargeData(byte[] data)
{
    var buffer = ArrayPool<byte>.Shared.Rent(data.Length);
    try
    {
        // Process data
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }
}

// Use Span<T> for zero-copy operations
public void ProcessSpan(ReadOnlySpan<byte> data)
{
    // Process without allocation
}

// Use ValueTask for high-frequency async
public ValueTask<int> ReadByteAsync()
{
    // Implementation
}
```

#### Async Optimization
```csharp
// Configure await for library code
public async Task<string> ReadFileAsync(string path)
{
    return await File.ReadAllTextAsync(path).ConfigureAwait(false);
}

// Parallel processing
public async Task ProcessFilesAsync(string[] files)
{
    var semaphore = new SemaphoreSlim(Environment.ProcessorCount);
    
    var tasks = files.Select(async file =>
    {
        await semaphore.WaitAsync();
        try
        {
            return await ProcessFileAsync(file);
        }
        finally
        {
            semaphore.Release();
        }
    });
    
    await Task.WhenAll(tasks);
}
```

### Rust Performance Tips

#### Zero-Copy Operations
```rust
// Use slices instead of owned data
pub fn process_data(data: &[u8]) -> &[u8] {
    &data[1..] // Return slice without allocation
}

// Use Cow for conditional ownership
use std::borrow::Cow;

pub fn normalize_path(path: &str) -> Cow<str> {
    if path.contains('\\') {
        Cow::Owned(path.replace('\\', "/"))
    } else {
        Cow::Borrowed(path)
    }
}
```

#### SIMD Operations
```rust
use std::arch::x86_64::*;

pub fn checksum_avx2(data: &[u8]) -> u64 {
    #[target_feature(enable = "avx2")]
    unsafe {
        // AVX2 implementation
    }
}
```

### WPF Performance Tips

#### UI Virtualization
```xml
<ListBox VirtualizingPanel.IsVirtualizing="True"
         VirtualizingPanel.VirtualizationMode="Recycling"
         VirtualizingPanel.IsContainerVirtualizable="True"
         ScrollViewer.IsDeferredScrollingEnabled="True">
```

#### Data Binding Optimization
```csharp
// Use OneTime binding for static data
<TextBlock Text="{Binding Title, Mode=OneTime}"/>

// Use UpdateSourceTrigger explicitly
<TextBox Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"/>

// Use ObservableCollection with NotifyCollectionChangedAction
public class FastObservableCollection<T> : ObservableCollection<T>
{
    // Optimized implementation
}
```

---

## 📚 Resources

### Documentation
- [Microsoft .NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [WPF Documentation](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
- [Rust Book](https://doc.rust-lang.org/book/)
- [Python Documentation](https://docs.python.org/3/)

### Tools and Extensions
- **Visual Studio Extensions:**
  - .NET Memory Profiler
  - Performance Profiler
  - CodeMaid
  - WPF Designer

- **VS Code Extensions:**
  - C# Dev Kit
  - Rust Analyzer
  - Python
  - Docker

### Community
- [Discord Server](https://discord.gg/winhance-fs)
- [GitHub Discussions](https://github.com/your-org/Winhance-FS/discussions)
- [Stack Overflow](https://stackoverflow.com/questions/tagged/winhance-fs)

---

## ❓ Getting Help

### Troubleshooting Common Issues

#### Build Errors
1. **Clean solution**: `dotnet clean`
2. **Clear NuGet cache**: `dotnet nuget locals all --clear`
3. **Restore packages**: `dotnet restore`

#### Runtime Errors
1. Check event logs for detailed errors
2. Enable debug logging in configuration
3. Use Visual Studio debugger to trace issues

#### Performance Issues
1. Use profiler to identify bottlenecks
2. Check for memory leaks
3. Monitor CPU and disk I/O

### Reporting Issues

When reporting issues, include:
1. Operating system and version
2. .NET/Rust/Python versions
3. Steps to reproduce
4. Error messages and stack traces
5. Expected vs actual behavior

---

*This development guide is continuously updated. For the latest information, check the repository wiki and documentation.*
