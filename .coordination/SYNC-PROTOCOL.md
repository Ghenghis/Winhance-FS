# CLAUDE + WINDSURF SYNC PROTOCOL

## Real-Time Coordination System

---

## DIRECTORY STRUCTURE

```
.coordination/
├── WINDSURF-MASTER-TASKS.md      # All 388+ features checklist
├── WINDSURF-GUI-IMPLEMENTATION.md # GUI implementation guide
├── WINDSURF-CLI-IMPLEMENTATION.md # CLI implementation guide
├── CLAUDE-BACKEND-STATUS.md       # Backend completion status
├── SYNC-PROTOCOL.md               # This file
├── status/
│   ├── claude-code-status.md      # Claude's current work
│   └── windsurf-status.md         # Windsurf's current work
├── notifications/
│   ├── to-claude.md               # Messages for Claude
│   └── to-windsurf.md             # Messages for Windsurf
├── completed/
│   ├── claude-completed.md        # Claude's completed items
│   └── windsurf-completed.md      # Windsurf's completed items
├── issues/
│   ├── bugs.md                    # Bug reports
│   └── blockers.md                # Blocking issues
└── interfaces/
    └── contracts.md               # Interface contracts
```

---

## RULES FOR CLAUDE

### Territory (DO edit)
- `src/Winhance.Core/Features/FileManager/Interfaces/` - All interfaces
- `src/Winhance.Infrastructure/Features/FileManager/Services/` - All services
- `src/nexus_core/` - Rust backend
- `src/nexus_cli/` - Python CLI backend logic
- `src/nexus_ai/` - AI features
- `tests/` - All tests
- `.coordination/` - Status updates

### NO-GO Zone (DO NOT edit)
- `src/Winhance.WPF/Features/FileManager/Views/` - WINDSURF ONLY
- `src/Winhance.WPF/Features/FileManager/ViewModels/` - WINDSURF ONLY
- `src/Winhance.WPF/Features/FileManager/Controls/` - WINDSURF ONLY
- `src/Winhance.WPF/Features/FileManager/Converters/` - WINDSURF ONLY

---

## RULES FOR WINDSURF

### Territory (DO edit)
- `src/Winhance.WPF/Features/FileManager/Views/` - All XAML
- `src/Winhance.WPF/Features/FileManager/ViewModels/` - All ViewModels
- `src/Winhance.WPF/Features/FileManager/Controls/` - Custom controls
- `src/Winhance.WPF/Features/FileManager/Converters/` - Value converters
- `src/Winhance.WPF/Features/FileManager/Resources/` - Styles
- `src/nexus_cli/commands/` - CLI command implementations
- `.coordination/` - Status updates

### NO-GO Zone (DO NOT edit)
- `src/Winhance.Core/Features/FileManager/Interfaces/` - CLAUDE ONLY
- `src/Winhance.Infrastructure/Features/FileManager/Services/` - CLAUDE ONLY
- `src/nexus_core/` - CLAUDE ONLY

---

## COMMUNICATION PROTOCOL

### When Claude Completes Something

1. Update `.coordination/completed/claude-completed.md`:
```markdown
## 2026-01-24

### Completed
- [x] IPreviewService interface (450 lines)
- [x] PreviewService implementation (400 lines)
- [x] Added PreviewType enum with 12 types

### Notes for Windsurf
- PreviewService.GeneratePreviewAsync() returns BitmapSource for images
- Use PreviewPanel.xaml to display, bind to CurrentPreview property
```

2. Create notification in `.coordination/notifications/to-windsurf.md`:
```markdown
## New Service Available

**Service:** PreviewService
**Interface:** IPreviewService
**Location:** src/Winhance.Infrastructure/Features/FileManager/Services/PreviewService.cs

**Key Methods:**
- `Task<ImageSource> GenerateImagePreviewAsync(string path, int maxSize)`
- `Task<TextPreviewResult> GenerateTextPreviewAsync(string path)`
- `Task<CodePreviewResult> GenerateCodePreviewAsync(string path)`

**How to Use:**
```csharp
private readonly IPreviewService _preview;

var image = await _preview.GenerateImagePreviewAsync(path, 800);
PreviewImage.Source = image;
```

**Related Tasks:** WS-211 to WS-223
```

### When Windsurf Needs Something

Create request in `.coordination/notifications/to-claude.md`:
```markdown
## Request: Need New Interface Method

**Interface:** ISelectionService
**Need:** Method to select files by content type (MIME type)

**Proposed Signature:**
```csharp
void SelectByMimeType(string mimeType, IEnumerable<string> allItems);
```

**Use Case:**
- User clicks "Select All Images" button
- Should select jpg, png, gif, webp, etc. by MIME type not just extension

**Priority:** High
**Blocking:** WS-082 (Select by type feature)
```

### When There's a Bug

Create report in `.coordination/issues/bugs.md`:
```markdown
## BUG-001: SelectionService throws on empty folder

**Reporter:** Windsurf
**Date:** 2026-01-24
**Severity:** Medium

**Steps to Reproduce:**
1. Navigate to empty folder
2. Press Ctrl+A (Select All)
3. Exception thrown

**Expected:** No selection, no error
**Actual:** NullReferenceException in SelectAll()

**Stack Trace:**
```
at SelectionService.SelectAll(items)
at FileListViewModel.SelectAllCommand_Execute()
```

**Fix Needed By:** Claude
```

### When There's a Blocker

Create entry in `.coordination/issues/blockers.md`:
```markdown
## BLOCK-001: Cannot implement WS-245 (Browse ZIP as folder)

**Reporter:** Windsurf
**Date:** 2026-01-24

**Blocked Feature:** WS-245 - Browse ZIP as folder
**Reason:** IArchiveService not implemented

**Waiting For:** Claude to implement ArchiveService
**Impact:** 8 features blocked (WS-245 to WS-252)

**Workaround:** None
```

---

## STATUS UPDATE FORMAT

### Claude Status (claude-code-status.md)
```markdown
# Claude Code Status

**Date:** 2026-01-24
**Current Task:** Implementing remaining services

## In Progress
- [ ] PreviewService implementation (60% complete)
- [ ] ArchiveService implementation (0%)

## Completed Today
- [x] SelectionService (630 lines)
- [x] AddressBarService (400 lines)
- [x] All 28 interfaces defined

## Blocked
- None

## Next Up
- MetadataService
- CompareService
- SyncService

## ETA
All services complete by end of day
```

### Windsurf Status (windsurf-status.md)
```markdown
# Windsurf Status

**Date:** 2026-01-24
**Current Task:** File list control implementation

## In Progress
- [ ] FileListView.xaml control (40% complete)
- [ ] DualPaneBrowserView.xaml (20% complete)

## Completed Today
- [x] WS-029: Details view implemented
- [x] WS-030: List view implemented
- [x] WS-040-046: Column headers

## Blocked
- WS-245-252: Archive features - waiting for ArchiveService

## Next Up
- Sorting column headers
- Context menu
- Keyboard shortcuts

## ETA
Core browser features by end of day
```

---

## DAILY SYNC CHECKLIST

### Morning
1. Check `.coordination/notifications/to-[me].md` for messages
2. Update status in `.coordination/status/[me]-status.md`
3. Check `.coordination/issues/blockers.md` for dependencies

### During Work
1. Update status when starting new task
2. Create notification when completing interface/service
3. Report bugs immediately when found

### End of Day
1. Update `.coordination/completed/[me]-completed.md`
2. Update status with tomorrow's plan
3. Clear resolved notifications

---

## EMERGENCY PROTOCOL

If there's a critical bug or conflict:

1. Stop working on affected area
2. Create entry in `.coordination/issues/blockers.md` with URGENT tag
3. Create notification to other party
4. Wait for response before continuing

---

## MERGE CONFLICT PREVENTION

1. **Never edit files in other's territory**
2. **Create interfaces before implementations**
3. **Notify when interface changes**
4. **Use feature branches if needed**

---

## TESTING PROTOCOL

1. Claude: Write unit tests for services
2. Windsurf: Write UI tests for views
3. Both: Run full build before committing
4. Both: Report test failures immediately

---

**FOLLOW THIS PROTOCOL FOR SMOOTH COLLABORATION**
