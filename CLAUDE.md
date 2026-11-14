# Claude Code Rules for Winhance-FS

## PROJECT STATUS (2026-11-14)

- **Build:** full solution 0 errors / 0 warnings (Core, Infrastructure, WPF, Tests).
- **Tests:** `dotnet test tests/Winhance.Tests` — all passing, including DI-validation tests that build the real
  app host and resolve every File Manager service and registered ViewModel.
- **Shipping File Manager UI:** File Browser, Batch Rename, Smart Organizer, Space Recovery — end-to-end tested
  against sandbox folders (see `CHANGELOG.md` → Unreleased).
- **Backend:** all 28 FileManager services implemented and registered via `AddFileManagerServices()` in
  `CompositionRoot`.
- **Removed 2026-11-14:** unreachable WPF code (500 generated placeholder views, ViewModels for services that
  were never implemented, experimental self-healing framework) and stale reports. Archived outside the repo at
  `C:\Winhance-FS-archive\2026-11-14` (see its `MANIFEST.txt`); git history also has the tracked files.
- The user authorized Claude Code to edit Windsurf territory for the 2026-11-14 build fix and polish.

---

## CRITICAL: Parallel Development Mode Active

This project is being developed simultaneously by Claude Code and Windsurf.
Follow these rules to avoid conflicts.

---

## Your Territory (Claude Code ONLY)

You are ONLY allowed to edit files in these directories:

### Rust Backend
- `src/nexus_core/` - Rust core library
- `src/nexus_cli/` - Python CLI
- `src/nexus_mcp/` - MCP Server
- `src/nexus_ai/` - Python AI agents

### C# Backend
- `src/Winhance.Core/Features/FileManager/Interfaces/` - Interface definitions
- `src/Winhance.Infrastructure/Features/FileManager/` - Service implementations
- `src/Winhance.Core/Features/FileManager/Models/` - Model definitions

### Other
- `tests/` - All tests
- `scripts/` - Build scripts
- `.coordination/` - Coordination files

---

## DO NOT EDIT (Windsurf Territory)

NEVER edit files in:
- `src/Winhance.WPF/Features/FileManager/Views/` - XAML (Windsurf)
- `src/Winhance.WPF/Features/FileManager/ViewModels/` - ViewModels (Windsurf)
- `src/Winhance.WPF/Features/FileManager/Controls/` - Controls (Windsurf)
- `src/Winhance.WPF/Features/FileManager/Converters/` - Converters (Windsurf)
- `src/Winhance.WPF/Features/FileManager/Resources/` - Styles (Windsurf)

---

## Coordination System

| File | Purpose |
|------|---------|
| `.coordination/status/claude-code-status.md` | Your status updates |
| `.coordination/status/windsurf-status.md` | Windsurf status (read-only) |
| `.coordination/notifications/to-claude.md` | Messages from Windsurf |
| `.coordination/notifications/to-windsurf.md` | Messages to Windsurf |
| `.coordination/completed/claude-completed.md` | Your completed work log |
| `.coordination/interfaces/contracts.md` | Interface contracts |

### Workflow
1. Check for messages in `to-claude.md` before starting
2. Update your status in `claude-code-status.md` when working
3. Log completed work in `claude-completed.md`
4. Notify Windsurf via `to-windsurf.md` when interfaces change

---

## Completed Work (Backend 100% Done)

### All 28 FileManager Services Implemented ✅

| Service | Interface | File |
|---------|-----------|------|
| TabService | ITabService | TabService.cs |
| AddressBarService | IAddressBarService | AddressBarService.cs |
| FavoritesService | IFavoritesService | FavoritesService.cs |
| ViewModeService | IViewModeService | ViewModeService.cs |
| SortingService | ISortingService | SortingService.cs |
| SelectionService | ISelectionService | SelectionService.cs |
| ClipboardService | IClipboardService | ClipboardService.cs |
| SearchService | ISearchService | SearchService.cs |
| QuickFilterService | IQuickFilterService | QuickFilterService.cs |
| PreviewService | IPreviewService | PreviewService.cs |
| QuickLookService | IQuickLookService | QuickLookService.cs |
| MetadataService | IMetadataService | MetadataService.cs |
| ArchiveService | IArchiveService | ArchiveService.cs |
| CompareService | ICompareService | CompareService.cs |
| SyncService | ISyncService | SyncService.cs |
| OrganizerService | IOrganizerService | OrganizerService.cs |
| WatchFolderService | IWatchFolderService | WatchFolderService.cs |
| DuplicateFinderService | IDuplicateFinderService | DuplicateFinderService.cs |
| SpaceAnalyzerService | ISpaceAnalyzerService | SpaceAnalyzerService.cs |
| OperationQueueService | IOperationQueueService | OperationQueueService.cs |
| BatchRenameService | IBatchRenameService | BatchRenameService.cs |
| AdvancedFileOperations | IAdvancedFileOperations | AdvancedFileOperationsService.cs |
| ColumnService | IColumnService | ColumnService.cs |
| ContextMenuService | IContextMenuService | ContextMenuService.cs |
| FileManagerService | IFileManagerService | FileManagerService.cs |
| FileOperationsService | IFileOperationsService | FileOperationsService.cs |
| DriveDetectionService | IDriveDetectionService | DriveDetectionService.cs |
| BackupProtectionService | IBackupProtectionService | BackupProtectionService.cs |

---

## Remaining Claude Tasks

### Rust Backend (Optional Enhancements)
1. `mft_reader.rs` - MFT reading for fast indexing
2. `usn_journal.rs` - Real-time file change monitoring
3. `content_hasher.rs` - xxHash and SHA-256
4. Tantivy search engine integration

### Maintenance
- Monitor for interface change requests from Windsurf
- Bug fixes in Infrastructure services
- Performance optimizations

---

## Architecture Rules

### Clean Architecture Enforcement
1. **Core Layer** - NO external dependencies except BCL
   - No WPF references (System.Windows.*)
   - No System.Drawing references
   - Pure interfaces and models only

2. **Infrastructure Layer** - Implements Core interfaces
   - NO WPF references (System.Windows.*)
   - No System.Drawing references
   - Use streams/byte arrays for binary data, not ImageSource/Bitmap

3. **WPF Layer** - Windsurf territory
   - ViewModels inject Infrastructure services
   - Views bind to ViewModels

### Interface Design Rules
- Define all types in the interface file
- Use records for immutable data
- Use classes for stateful objects
- Events for async notifications
- Task-based async pattern

---

## Build Commands

```bash
# Rust
cd src/nexus_core && cargo build

# C# Infrastructure only
dotnet build src/Winhance.Infrastructure

# Full solution (will fail on WPF until Windsurf fixes XAML)
dotnet build Winhance.sln
```

---

## Documentation

| Document | Purpose |
|----------|---------|
| `README.md` | What ships, with honest status per feature |
| `CHANGELOG.md` | Release notes |
| `WINDSURF.md` | Windsurf guidelines |
| `docs/PARALLEL_DEVELOPMENT_GUIDE.md` | Coordination details |
| `.coordination/QUICK_START.md` | Quick reference |

---

## AI Agent Architecture

### Four-Pillar System
| Agent | Focus | Model |
|-------|-------|-------|
| **Claude Code** | Backend, C#, Rust | Claude 3.5/4 |
| **Windsurf** | WPF UI, XAML, ViewModels | GPT-4 + Cascade |
| **Antigravity** | Autonomous agents, Chrome | Gemini 3 Pro |
| **Open Interpreter** | Coordination, CI/CD | Any API |

### Mode Toggle (Settings)
- **Cloud Only**: Full power, requires internet
- **Local Only**: Privacy-focused, offline
- **Hybrid**: Auto-route based on task complexity

---

## Quality Standards

### Before Committing
1. `dotnet build src/Winhance.Infrastructure` = 0 errors
2. All interface methods implemented
3. No TODO comments in production code
4. No NotImplementedException
5. Update coordination files

### Code Style
- Use file-scoped namespaces
- Use primary constructors where applicable
- Prefer async/await for I/O operations
- Use cancellation tokens for long-running operations
- Log errors, not just throw

---

## Contact

Questions? Add to `.coordination/notifications/to-windsurf.md`
