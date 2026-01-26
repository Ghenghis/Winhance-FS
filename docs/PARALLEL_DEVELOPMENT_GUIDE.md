# PARALLEL DEVELOPMENT GUIDE
## Claude Code + Windsurf IDE Coordination System

**Version:** 1.0
**Date:** January 24, 2026
**Purpose:** Enable simultaneous development by two AI agents without conflicts

---

## GOLDEN RULES FOR PARALLEL DEVELOPMENT

### Rule 1: Territory Separation
Each agent works in **completely separate directories** - no overlap allowed.

### Rule 2: No Shared File Editing
If both agents need to modify the same file, one must wait or coordinate via the `.coordination/` folder.

### Rule 3: Communication via Files
Agents communicate through markdown files in `.coordination/` directory.

### Rule 4: Build Before Commit
Always run `dotnet build` before marking work complete.

---

## TERRITORY ASSIGNMENTS

### CLAUDE CODE TERRITORY (Backend & Infrastructure)

```
CLAUDE CODE OWNS THESE DIRECTORIES:
├── src/nexus_core/                    # Rust backend (ALL)
├── src/nexus_cli/                     # CLI tools (ALL)
├── src/nexus_mcp/                     # MCP server (ALL)
├── src/nexus_ai/                      # Python AI (ALL)
├── src/Winhance.Core/Features/FileManager/Interfaces/    # Interfaces
├── src/Winhance.Infrastructure/Features/FileManager/     # Services (ALL)
├── tests/                             # Python tests (ALL)
└── scripts/                           # Build scripts (ALL)
```

**Claude Code Focus Areas:**
- MFT Reader (Rust)
- USN Journal Monitor (Rust)
- Search Engine (Tantivy)
- Hashing Services (xxHash, SHA-256)
- File Operations Services
- Duplicate Detection Service
- Archive Service
- Watch Folder Service
- AI Classification (Python)
- All backend interfaces

### WINDSURF TERRITORY (UI & Presentation)

```
WINDSURF OWNS THESE DIRECTORIES:
├── src/Winhance.WPF/Features/FileManager/Views/          # XAML Views (ALL)
├── src/Winhance.WPF/Features/FileManager/ViewModels/     # ViewModels (ALL)
├── src/Winhance.WPF/Features/FileManager/Controls/       # Custom Controls
├── src/Winhance.WPF/Features/FileManager/Converters/     # Value Converters
├── src/Winhance.WPF/Features/FileManager/Resources/      # Styles, Templates
├── src/Winhance.WPF/Features/Common/Controls/            # Shared Controls
├── src/Winhance.WPF/Features/Common/Converters/          # Shared Converters
└── docs/                                                  # Documentation
```

**Windsurf Focus Areas:**
- All XAML Views
- All ViewModels
- TreeMap Visualization
- Preview Pane UI
- Tab Control UI
- Search UI
- Duplicate Finder UI
- Batch Rename UI
- Context Menus
- Dialogs and Popups
- Styles and Themes

### SHARED TERRITORY (Coordination Required)

```
SHARED - REQUIRES COORDINATION:
├── src/Winhance.Core/Features/FileManager/Models/        # Data Models
├── src/Winhance.WPF/Features/Common/Extensions/DI/       # DI Registration
└── src/Winhance.WPF/App.xaml.cs                          # App startup
```

**Coordination Protocol for Shared Files:**
1. Check `.coordination/locks/` before editing
2. Create lock file: `{filename}.lock` with agent name
3. Edit file
4. Remove lock file when done
5. Notify other agent via `.coordination/notifications/`

---

## COORDINATION SYSTEM SETUP

### Directory Structure

```
.coordination/
├── status/
│   ├── claude-code-status.md      # Claude's current work
│   └── windsurf-status.md         # Windsurf's current work
├── locks/
│   └── {filename}.lock            # File locks
├── notifications/
│   ├── to-claude.md               # Messages for Claude
│   └── to-windsurf.md             # Messages for Windsurf
├── completed/
│   ├── claude-completed.md        # Claude's completed items
│   └── windsurf-completed.md      # Windsurf's completed items
└── interfaces/
    └── contracts.md               # Shared interface definitions
```

---

## PHASE 1 TASK DISTRIBUTION (Weeks 1-4)

### CLAUDE CODE TASKS - Phase 1

| Task ID | Feature | Files to Create/Modify | Priority |
|---------|---------|------------------------|----------|
| CC-P1-001 | MFT Reader | `nexus_core/src/indexer/mft_reader.rs` | P0 |
| CC-P1-002 | USN Journal | `nexus_core/src/indexer/usn_journal.rs` | P0 |
| CC-P1-003 | Content Hasher | `nexus_core/src/indexer/content_hasher.rs` | P0 |
| CC-P1-004 | Tantivy Wiring | `nexus_core/src/search/` | P0 |
| CC-P1-005 | ISearchService | `Winhance.Core/.../Interfaces/ISearchService.cs` | P0 |
| CC-P1-006 | SearchService | `Winhance.Infrastructure/.../SearchService.cs` | P0 |
| CC-P1-007 | ITabService | `Winhance.Core/.../Interfaces/ITabService.cs` | P0 |
| CC-P1-008 | TabService | `Winhance.Infrastructure/.../TabService.cs` | P0 |
| CC-P1-009 | IFavoritesService | `Winhance.Core/.../Interfaces/IFavoritesService.cs` | P0 |
| CC-P1-010 | FavoritesService | `Winhance.Infrastructure/.../FavoritesService.cs` | P0 |
| CC-P1-011 | IOperationQueueService | `Winhance.Core/.../Interfaces/IOperationQueueService.cs` | P0 |
| CC-P1-012 | OperationQueueService | `Winhance.Infrastructure/.../OperationQueueService.cs` | P0 |

### WINDSURF TASKS - Phase 1

| Task ID | Feature | Files to Create/Modify | Priority |
|---------|---------|------------------------|----------|
| WS-P1-001 | Tab Container View | `Views/TabContainerView.xaml` | P0 |
| WS-P1-002 | Tab Container VM | `ViewModels/TabContainerViewModel.cs` | P0 |
| WS-P1-003 | Search Box Control | `Controls/SearchBox.xaml` | P0 |
| WS-P1-004 | Search Results View | `Views/SearchResultsView.xaml` | P0 |
| WS-P1-005 | Search Results VM | `ViewModels/SearchResultsViewModel.cs` | P0 |
| WS-P1-006 | Favorites Panel | `Views/FavoritesPanel.xaml` | P0 |
| WS-P1-007 | Favorites Panel VM | `ViewModels/FavoritesPanelViewModel.cs` | P0 |
| WS-P1-008 | TreeMap Control | `Controls/TreeMapControl.xaml` | P0 |
| WS-P1-009 | TreeMap View | `Views/TreeMapView.xaml` | P0 |
| WS-P1-010 | TreeMap VM | `ViewModels/TreeMapViewModel.cs` | P0 |
| WS-P1-011 | Progress Dialog | `Views/OperationProgressDialog.xaml` | P0 |
| WS-P1-012 | Conflict Dialog | `Views/ConflictResolutionDialog.xaml` | P0 |

---

## PHASE 2 TASK DISTRIBUTION (Weeks 5-8)

### CLAUDE CODE TASKS - Phase 2

| Task ID | Feature | Files to Create/Modify | Priority |
|---------|---------|------------------------|----------|
| CC-P2-001 | IArchiveService | `Interfaces/IArchiveService.cs` | P1 |
| CC-P2-002 | ArchiveService | `Services/ArchiveService.cs` | P1 |
| CC-P2-003 | IDuplicateService | `Interfaces/IDuplicateScannerService.cs` | P1 |
| CC-P2-004 | DuplicateService | `Services/DuplicateScannerService.cs` | P1 |
| CC-P2-005 | IPreviewService | `Interfaces/IPreviewService.cs` | P1 |
| CC-P2-006 | PreviewService | `Services/PreviewService.cs` | P1 |
| CC-P2-007 | IBatchRenameService impl | `Services/BatchRenameService.cs` | P1 |
| CC-P2-008 | Regex rename logic | `Services/BatchRenameService.cs` | P1 |
| CC-P2-009 | EXIF extraction | `Services/MetadataService.cs` | P1 |
| CC-P2-010 | Preset persistence | `Services/PresetService.cs` | P1 |

### WINDSURF TASKS - Phase 2

| Task ID | Feature | Files to Create/Modify | Priority |
|---------|---------|------------------------|----------|
| WS-P2-001 | Preview Pane View | `Views/PreviewPane.xaml` | P1 |
| WS-P2-002 | Preview Pane VM | `ViewModels/PreviewPaneViewModel.cs` | P1 |
| WS-P2-003 | Text Preview Control | `Controls/TextPreviewControl.xaml` | P1 |
| WS-P2-004 | Image Preview Control | `Controls/ImagePreviewControl.xaml` | P1 |
| WS-P2-005 | Code Preview Control | `Controls/CodePreviewControl.xaml` | P1 |
| WS-P2-006 | Archive Browser View | `Views/ArchiveBrowserView.xaml` | P1 |
| WS-P2-007 | Archive Browser VM | `ViewModels/ArchiveBrowserViewModel.cs` | P1 |
| WS-P2-008 | Duplicate Finder View | `Views/DuplicateFinderView.xaml` | P1 |
| WS-P2-009 | Duplicate Finder VM | `ViewModels/DuplicateFinderViewModel.cs` | P1 |
| WS-P2-010 | Preset Manager Dialog | `Views/PresetManagerDialog.xaml` | P1 |

---

## INTERFACE CONTRACTS

Both agents must agree on these interfaces. Claude Code creates the interface, Windsurf consumes it.

### ISearchService Contract

```csharp
// Claude Code creates this in Winhance.Core
public interface ISearchService
{
    Task<IEnumerable<SearchResult>> SearchAsync(
        string query,
        SearchOptions options,
        CancellationToken ct = default);

    Task<bool> IndexDriveAsync(string driveLetter, IProgress<int> progress, CancellationToken ct = default);

    Task<IndexStatus> GetIndexStatusAsync(CancellationToken ct = default);
}

public class SearchResult
{
    public string FullPath { get; set; }
    public string Name { get; set; }
    public long Size { get; set; }
    public DateTime Modified { get; set; }
    public double Score { get; set; }
}

public class SearchOptions
{
    public bool UseRegex { get; set; }
    public string[] Extensions { get; set; }
    public long? MinSize { get; set; }
    public long? MaxSize { get; set; }
    public DateTime? ModifiedAfter { get; set; }
    public int MaxResults { get; set; } = 1000;
}
```

### ITabService Contract

```csharp
// Claude Code creates this in Winhance.Core
public interface ITabService
{
    ObservableCollection<TabItem> Tabs { get; }
    TabItem ActiveTab { get; set; }

    TabItem CreateTab(string path);
    void CloseTab(TabItem tab);
    void CloseAllExcept(TabItem tab);
    TabItem DuplicateTab(TabItem tab);

    Task SaveSessionAsync(string name);
    Task<bool> LoadSessionAsync(string name);
    IEnumerable<string> GetSavedSessions();
}

public class TabItem
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string CurrentPath { get; set; }
    public Stack<string> BackHistory { get; set; }
    public Stack<string> ForwardHistory { get; set; }
    public bool IsPinned { get; set; }
}
```

### IArchiveService Contract

```csharp
// Claude Code creates this in Winhance.Core
public interface IArchiveService
{
    Task<IEnumerable<ArchiveEntry>> GetContentsAsync(string archivePath, CancellationToken ct = default);
    Task ExtractAsync(string archivePath, string destination, IProgress<int> progress, CancellationToken ct = default);
    Task ExtractFilesAsync(string archivePath, IEnumerable<string> files, string destination, CancellationToken ct = default);
    Task CreateAsync(string archivePath, IEnumerable<string> files, ArchiveFormat format, CancellationToken ct = default);
    bool IsArchive(string path);
}

public class ArchiveEntry
{
    public string Name { get; set; }
    public string FullPath { get; set; }
    public long Size { get; set; }
    public long CompressedSize { get; set; }
    public DateTime Modified { get; set; }
    public bool IsDirectory { get; set; }
}

public enum ArchiveFormat { Zip, SevenZip, Tar, TarGz }
```

### IDuplicateScannerService Contract

```csharp
// Claude Code creates this in Winhance.Core
public interface IDuplicateScannerService
{
    Task<DuplicateScanResult> ScanAsync(
        string path,
        DuplicateScanOptions options,
        IProgress<DuplicateScanProgress> progress,
        CancellationToken ct = default);

    Task DeleteDuplicatesAsync(
        IEnumerable<DuplicateGroup> groups,
        DuplicateKeepStrategy strategy,
        CancellationToken ct = default);
}

public class DuplicateScanOptions
{
    public bool Recursive { get; set; } = true;
    public long MinSize { get; set; } = 1;
    public long? MaxSize { get; set; }
    public string[] Extensions { get; set; }
    public string[] ExcludePaths { get; set; }
    public bool UseQuickHash { get; set; } = true;
    public bool VerifyWithFullHash { get; set; } = false;
}

public enum DuplicateKeepStrategy
{
    KeepFirst, KeepLast, KeepOldest, KeepNewest,
    KeepShortestPath, KeepLongestPath, KeepSelected
}
```

---

## COMMUNICATION TEMPLATES

### Status Update Template

```markdown
# Agent Status Update

**Agent:** [Claude Code / Windsurf]
**Timestamp:** [ISO 8601]
**Current Task:** [Task ID]

## Completed Since Last Update
- [ ] Task description

## Currently Working On
- Task ID: Description

## Blocked On
- Waiting for [other agent] to complete [task]

## Next Up
- Task ID: Description

## Notes for Other Agent
- Any important information
```

### Lock File Template

```markdown
# File Lock

**Locked By:** [Claude Code / Windsurf]
**Locked At:** [ISO 8601]
**File:** [relative path]
**Reason:** [why editing]
**Expected Duration:** [minutes]
```

### Notification Template

```markdown
# Notification

**From:** [Claude Code / Windsurf]
**To:** [Claude Code / Windsurf]
**Timestamp:** [ISO 8601]
**Priority:** [High / Medium / Low]

## Message
[Content]

## Action Required
- [ ] Specific action needed

## Related Files
- file1.cs
- file2.xaml
```

---

## DAILY WORKFLOW

### Morning Sync (Both Agents)

1. Read `.coordination/status/` files from other agent
2. Check `.coordination/notifications/` for messages
3. Update own status file with today's plan
4. Check for any lock files on shared resources

### During Development

1. **Before editing shared file:**
   - Check `.coordination/locks/`
   - Create lock file if none exists
   - Wait or notify if locked

2. **After completing a feature:**
   - Update `.coordination/completed/` file
   - Remove any lock files
   - Notify other agent if they depend on your work

3. **When blocked:**
   - Update status file with blocker
   - Create notification for other agent
   - Work on different task

### End of Session

1. Commit all changes with descriptive message
2. Update status file with progress
3. List any incomplete work
4. Note any blockers for next session

---

## BUILD & TEST PROTOCOL

### Before Marking Task Complete

```bash
# Claude Code (Backend)
cd src/nexus_core && cargo build
cd ../.. && dotnet build src/Winhance.Infrastructure

# Windsurf (Frontend)
dotnet build src/Winhance.WPF
```

### Integration Testing

When both agents complete related features:

```bash
# Full build
dotnet build Winhance.sln

# Run tests
dotnet test

# Manual verification
dotnet run --project src/Winhance.WPF
```

---

## CONFLICT RESOLUTION

### If Both Agents Edit Same File

1. First to commit wins
2. Second agent must:
   - Pull latest changes
   - Merge their changes
   - Notify first agent of merge

### If Interface Changes Needed

1. Agent needing change creates notification
2. Both agents discuss via notifications
3. Claude Code (interface owner) makes change
4. Windsurf updates consuming code

### If Build Breaks

1. Agent who broke build must fix immediately
2. Notify other agent of the break
3. Other agent pauses until fixed
4. Document what caused the break

---

## QUICK START COMMANDS

### For Claude Code

```bash
# Start working on backend
cd D:\Winhance-FS-Repo

# Check Windsurf status
cat .coordination/status/windsurf-status.md

# Update your status
# Edit .coordination/status/claude-code-status.md

# Work on Rust
cd src/nexus_core
cargo build

# Work on C# services
cd src/Winhance.Infrastructure
dotnet build
```

### For Windsurf

```bash
# Start working on frontend
cd D:\Winhance-FS-Repo

# Check Claude status
cat .coordination/status/claude-code-status.md

# Update your status
# Edit .coordination/status/windsurf-status.md

# Work on WPF
cd src/Winhance.WPF
dotnet build
```

---

## EMERGENCY PROCEDURES

### If Agent Goes Offline

- Other agent can take over ONLY their own territory
- Shared files remain locked until timeout (30 min)
- Document takeover in notifications

### If Major Conflict

1. Stop all work
2. Both agents pull latest
3. Identify conflicting changes
4. Coordinate resolution via notifications
5. One agent merges, other verifies

---

## SUCCESS METRICS

### Velocity Tracking

| Week | Claude Code Tasks | Windsurf Tasks | Combined |
|------|-------------------|----------------|----------|
| 1 | Target: 6 | Target: 6 | 12 |
| 2 | Target: 6 | Target: 6 | 12 |
| 3 | Target: 6 | Target: 6 | 12 |
| 4 | Target: 6 | Target: 6 | 12 |

### Quality Metrics

- Build breaks: Target 0 per week
- Merge conflicts: Target < 2 per week
- Blocked time: Target < 10% of work time

---

*Parallel Development Guide v1.0*
*Enabling 2x development velocity through coordination*
