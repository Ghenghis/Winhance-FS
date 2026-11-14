# WINDSURF TASK LIST: ALL 503 FEATURES

**Generated:** 2026-01-24
**Total Features:** 503
**Priority:** Complete GUI/CLI for ALL features

---

## OVERVIEW

This document contains ALL 503 features that Windsurf needs to complete the GUI/CLI implementation for.
Claude Code is implementing all backend services and interfaces - Windsurf must wire these to the UI.

**Current Status:**
- Backend services: Claude Code is completing ALL remaining services NOW
- GUI: Windsurf needs to implement ALL ViewModel logic and XAML

---

## PART 1: FILE BROWSER FUNDAMENTALS (45 Features)

### 1.1 Directory Navigation (15 Features) - WS-NAV

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-NAV-001 | Navigate to folder (double-click) | IFileManagerService | DualPaneBrowserVM | WIRE |
| WS-NAV-002 | Navigate up to parent (Alt+Up) | ITabService.GoUp() | DualPaneBrowserVM | IMPL |
| WS-NAV-003 | Navigate back in history (Alt+Left) | ITabService.GoBack() | DualPaneBrowserVM | IMPL |
| WS-NAV-004 | Navigate forward in history (Alt+Right) | ITabService.GoForward() | DualPaneBrowserVM | IMPL |
| WS-NAV-005 | Navigate to specific path (address bar) | IAddressBarService | AddressBarVM | CREATE |
| WS-NAV-006 | Breadcrumb path navigation | IAddressBarService | BreadcrumbVM | CREATE |
| WS-NAV-007 | Path autocomplete suggestions | IAddressBarService | AddressBarVM | CREATE |
| WS-NAV-008 | Recent locations dropdown | IFavoritesService | QuickAccessVM | IMPL |
| WS-NAV-009 | Frequent locations (ML-based) | IFavoritesService | QuickAccessVM | IMPL |
| WS-NAV-010 | Quick jump to drive root | IDriveDetectionService | DriveListVM | CREATE |
| WS-NAV-011 | Quick jump to user folders | IFavoritesService | FavoritesPanelVM | WIRE |
| WS-NAV-012 | Quick jump to Libraries | IFavoritesService | FavoritesPanelVM | IMPL |
| WS-NAV-013 | Quick jump to Network | IFileManagerService | NetworkBrowserVM | CREATE |
| WS-NAV-014 | Quick jump to Recycle Bin | IFileManagerService | RecycleBinVM | CREATE |
| WS-NAV-015 | Navigate via keyboard (type to filter) | IQuickFilterService | DualPaneBrowserVM | IMPL |

### 1.2 View Modes (12 Features) - WS-VIEW

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-VIEW-001 | Details view (columns) | IViewModeService | ViewModeVM | CREATE |
| WS-VIEW-002 | List view (compact) | IViewModeService | ViewModeVM | CREATE |
| WS-VIEW-003 | Small icons view | IViewModeService | ViewModeVM | CREATE |
| WS-VIEW-004 | Medium icons view | IViewModeService | ViewModeVM | CREATE |
| WS-VIEW-005 | Large icons view | IViewModeService | ViewModeVM | CREATE |
| WS-VIEW-006 | Extra large icons | IViewModeService | ViewModeVM | CREATE |
| WS-VIEW-007 | Tiles view | IViewModeService | ViewModeVM | CREATE |
| WS-VIEW-008 | Content view | IViewModeService | ViewModeVM | CREATE |
| WS-VIEW-009 | Thumbnail grid view | IViewModeService | ViewModeVM | CREATE |
| WS-VIEW-010 | Column view (macOS style) | IViewModeService | ColumnBrowserVM | CREATE |
| WS-VIEW-011 | Remember view per folder | IViewModeService | ViewModeVM | IMPL |
| WS-VIEW-012 | Global default view setting | IUserPreferencesService | SettingsVM | IMPL |

### 1.3 Sorting & Grouping (18 Features) - WS-SORT

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-SORT-001 | Sort by name | ISortingService | SortingVM | CREATE |
| WS-SORT-002 | Sort by size | ISortingService | SortingVM | CREATE |
| WS-SORT-003 | Sort by type (extension) | ISortingService | SortingVM | CREATE |
| WS-SORT-004 | Sort by date modified | ISortingService | SortingVM | CREATE |
| WS-SORT-005 | Sort by date created | ISortingService | SortingVM | CREATE |
| WS-SORT-006 | Sort by date accessed | ISortingService | SortingVM | CREATE |
| WS-SORT-007 | Sort ascending/descending | ISortingService | SortingVM | CREATE |
| WS-SORT-008 | Natural number sorting | ISortingService | SortingVM | IMPL |
| WS-SORT-009 | Folders first option | ISortingService | SortingVM | IMPL |
| WS-SORT-010 | Group by none | IGroupingService | GroupingVM | CREATE |
| WS-SORT-011 | Group by type | IGroupingService | GroupingVM | CREATE |
| WS-SORT-012 | Group by date | IGroupingService | GroupingVM | CREATE |
| WS-SORT-013 | Group by size | IGroupingService | GroupingVM | CREATE |
| WS-SORT-014 | Group by first letter | IGroupingService | GroupingVM | CREATE |
| WS-SORT-015 | Custom sort columns | IColumnService | ColumnConfigVM | CREATE |
| WS-SORT-016 | Column width persistence | IColumnService | ColumnConfigVM | IMPL |
| WS-SORT-017 | Column order persistence | IColumnService | ColumnConfigVM | IMPL |
| WS-SORT-018 | Multi-column sorting | ISortingService | SortingVM | IMPL |

---

## PART 2: FILE SELECTION (28 Features)

### 2.1 Basic Selection (10 Features) - WS-SEL

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-SEL-001 | Single click select | ISelectionService | FileListVM | CREATE |
| WS-SEL-002 | Ctrl+click multi-select | ISelectionService | FileListVM | CREATE |
| WS-SEL-003 | Shift+click range select | ISelectionService | FileListVM | CREATE |
| WS-SEL-004 | Select all (Ctrl+A) | ISelectionService | FileListVM | CREATE |
| WS-SEL-005 | Deselect all | ISelectionService | FileListVM | CREATE |
| WS-SEL-006 | Invert selection | ISelectionService | FileListVM | CREATE |
| WS-SEL-007 | Selection count display | ISelectionService | StatusBarVM | CREATE |
| WS-SEL-008 | Selection size display | ISelectionService | StatusBarVM | CREATE |
| WS-SEL-009 | Selection persist on refresh | ISelectionService | FileListVM | IMPL |
| WS-SEL-010 | Selection persist on sort | ISelectionService | FileListVM | IMPL |

### 2.2 Advanced Selection (12 Features) - WS-ADVSEL

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-ADVSEL-001 | Lasso/rubber band selection | ISelectionService | FileListVM | IMPL |
| WS-ADVSEL-002 | Select by wildcard pattern | ISelectionService | SelectionDialogVM | CREATE |
| WS-ADVSEL-003 | Select by regex pattern | ISelectionService | SelectionDialogVM | CREATE |
| WS-ADVSEL-004 | Select by extension | ISelectionService | SelectionDialogVM | CREATE |
| WS-ADVSEL-005 | Select by date range | ISelectionService | SelectionDialogVM | CREATE |
| WS-ADVSEL-006 | Select by size range | ISelectionService | SelectionDialogVM | CREATE |
| WS-ADVSEL-007 | Select files only | ISelectionService | SelectionDialogVM | CREATE |
| WS-ADVSEL-008 | Select folders only | ISelectionService | SelectionDialogVM | CREATE |
| WS-ADVSEL-009 | Select similar files | ISelectionService | SelectionDialogVM | CREATE |
| WS-ADVSEL-010 | Select all in group | ISelectionService | FileListVM | IMPL |
| WS-ADVSEL-011 | Select by checkboxes | ISelectionService | FileListVM | IMPL |
| WS-ADVSEL-012 | Touch-friendly selection | ISelectionService | FileListVM | IMPL |

### 2.3 Selection Memory (6 Features) - WS-SELMEM

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-SELMEM-001 | Save selection as set | ISelectionService | SelectionSetsVM | CREATE |
| WS-SELMEM-002 | Load saved selection | ISelectionService | SelectionSetsVM | CREATE |
| WS-SELMEM-003 | Recent selections list | ISelectionService | SelectionSetsVM | CREATE |
| WS-SELMEM-004 | Selection to clipboard | IClipboardService | FileListVM | CREATE |
| WS-SELMEM-005 | Selection from clipboard | IClipboardService | FileListVM | CREATE |
| WS-SELMEM-006 | Selection across tabs | ISelectionService | TabContainerVM | IMPL |

---

## PART 3: FILE OPERATIONS (42 Features)

### 3.1 Basic Operations (12 Features) - WS-OPS

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-OPS-001 | Copy files (Ctrl+C) | IClipboardService | FileListVM | CREATE |
| WS-OPS-002 | Cut files (Ctrl+X) | IClipboardService | FileListVM | CREATE |
| WS-OPS-003 | Paste files (Ctrl+V) | IOperationQueueService | FileListVM | WIRE |
| WS-OPS-004 | Delete to Recycle Bin | IOperationQueueService | FileListVM | WIRE |
| WS-OPS-005 | Permanent delete (Shift+Del) | IOperationQueueService | DeleteConfirmVM | CREATE |
| WS-OPS-006 | Rename file (F2) | IFileManagerService | InlineRenameVM | CREATE |
| WS-OPS-007 | Rename extension separately | IFileManagerService | InlineRenameVM | IMPL |
| WS-OPS-008 | Create new folder | IFileManagerService | NewFolderVM | CREATE |
| WS-OPS-009 | Create new file | IFileManagerService | NewFileVM | CREATE |
| WS-OPS-010 | Create from template | ITemplateService | TemplatePickerVM | CREATE |
| WS-OPS-011 | Duplicate file/folder | IFileManagerService | FileListVM | IMPL |
| WS-OPS-012 | Open file | IFileManagerService | FileListVM | WIRE |

### 3.2 Advanced Copy/Move (15 Features) - WS-COPY

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-COPY-001 | Copy with progress dialog | IOperationQueueService | ProgressDialogVM | CREATE |
| WS-COPY-002 | Move with progress dialog | IOperationQueueService | ProgressDialogVM | CREATE |
| WS-COPY-003 | Pause/resume copy | IOperationQueueService | ProgressDialogVM | CREATE |
| WS-COPY-004 | Cancel operation | IOperationQueueService | ProgressDialogVM | WIRE |
| WS-COPY-005 | Queue multiple operations | IOperationQueueService | OperationQueueVM | CREATE |
| WS-COPY-006 | Background operations | IOperationQueueService | OperationQueueVM | IMPL |
| WS-COPY-007 | Copy speed display | IOperationQueueService | ProgressDialogVM | IMPL |
| WS-COPY-008 | ETA display | IOperationQueueService | ProgressDialogVM | IMPL |
| WS-COPY-009 | Copy verification | IOperationQueueService | ProgressDialogVM | IMPL |
| WS-COPY-010 | Resume interrupted copy | IOperationQueueService | ProgressDialogVM | IMPL |
| WS-COPY-011 | Retry failed items | IOperationQueueService | ProgressDialogVM | IMPL |
| WS-COPY-012 | Skip all errors option | IOperationQueueService | ProgressDialogVM | IMPL |
| WS-COPY-013 | Log all operations | IOperationQueueService | OperationLogVM | CREATE |
| WS-COPY-014 | Preserve timestamps | IOperationQueueService | CopyOptionsVM | CREATE |
| WS-COPY-015 | Preserve attributes | IOperationQueueService | CopyOptionsVM | IMPL |

### 3.3 Conflict Resolution (10 Features) - WS-CONF

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-CONF-001 | Conflict detection | IOperationQueueService | ConflictDialogVM | CREATE |
| WS-CONF-002 | Skip conflicting files | IOperationQueueService | ConflictDialogVM | CREATE |
| WS-CONF-003 | Overwrite always | IOperationQueueService | ConflictDialogVM | CREATE |
| WS-CONF-004 | Overwrite if newer | IOperationQueueService | ConflictDialogVM | CREATE |
| WS-CONF-005 | Overwrite if larger | IOperationQueueService | ConflictDialogVM | CREATE |
| WS-CONF-006 | Rename automatically | IOperationQueueService | ConflictDialogVM | CREATE |
| WS-CONF-007 | Compare files before decide | ICompareService | CompareDialogVM | CREATE |
| WS-CONF-008 | Remember choice | IOperationQueueService | ConflictDialogVM | IMPL |
| WS-CONF-009 | Merge folders | IOperationQueueService | ConflictDialogVM | IMPL |
| WS-CONF-010 | Conflict resolution presets | IOperationQueueService | ConflictOptionsVM | CREATE |

### 3.4 Undo/Redo System (5 Features) - WS-UNDO

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-UNDO-001 | Undo last operation (Ctrl+Z) | IBackupProtectionService | UndoManagerVM | CREATE |
| WS-UNDO-002 | Redo operation (Ctrl+Y) | IBackupProtectionService | UndoManagerVM | CREATE |
| WS-UNDO-003 | Multi-level undo | IBackupProtectionService | UndoManagerVM | IMPL |
| WS-UNDO-004 | Undo history list | IBackupProtectionService | UndoHistoryVM | CREATE |
| WS-UNDO-005 | Selective undo | IBackupProtectionService | UndoHistoryVM | IMPL |

---

## PART 4: DUAL-PANE BROWSER (25 Features)

### 4.1 Pane Management (12 Features) - WS-PANE

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-PANE-001 | Left pane | IDualPaneService | DualPaneBrowserVM | WIRE |
| WS-PANE-002 | Right pane | IDualPaneService | DualPaneBrowserVM | WIRE |
| WS-PANE-003 | Switch active pane (Tab) | IDualPaneService | DualPaneBrowserVM | IMPL |
| WS-PANE-004 | Vertical split | IDualPaneService | DualPaneBrowserVM | IMPL |
| WS-PANE-005 | Horizontal split | IDualPaneService | DualPaneBrowserVM | IMPL |
| WS-PANE-006 | Adjustable split ratio | IDualPaneService | DualPaneBrowserVM | IMPL |
| WS-PANE-007 | Collapse pane | IDualPaneService | DualPaneBrowserVM | IMPL |
| WS-PANE-008 | Maximize pane | IDualPaneService | DualPaneBrowserVM | IMPL |
| WS-PANE-009 | Swap panes | IDualPaneService | DualPaneBrowserVM | IMPL |
| WS-PANE-010 | Sync panes | IDualPaneService | DualPaneBrowserVM | IMPL |
| WS-PANE-011 | Independent navigation | ITabService | DualPaneBrowserVM | WIRE |
| WS-PANE-012 | Pane tabs | ITabService | DualPaneBrowserVM | IMPL |

### 4.2 Cross-Pane Operations (13 Features) - WS-XPANE

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-XPANE-001 | Copy to other pane (F5) | IOperationQueueService | DualPaneBrowserVM | IMPL |
| WS-XPANE-002 | Move to other pane (F6) | IOperationQueueService | DualPaneBrowserVM | IMPL |
| WS-XPANE-003 | Drag between panes | IOperationQueueService | DualPaneBrowserVM | WIRE |
| WS-XPANE-004 | Compare folders | ICompareService | FolderCompareVM | CREATE |
| WS-XPANE-005 | Sync folders | ISyncService | FolderSyncVM | CREATE |
| WS-XPANE-006 | Show unique files | ICompareService | FolderCompareVM | IMPL |
| WS-XPANE-007 | Show duplicates | IDuplicateFinderService | FolderCompareVM | WIRE |
| WS-XPANE-008 | Mirror selection | ISelectionService | DualPaneBrowserVM | IMPL |
| WS-XPANE-009 | Copy to other (preserve path) | IOperationQueueService | DualPaneBrowserVM | IMPL |
| WS-XPANE-010 | Link between panes | IDualPaneService | DualPaneBrowserVM | IMPL |
| WS-XPANE-011 | Breadcrumb sync option | IDualPaneService | DualPaneBrowserVM | IMPL |
| WS-XPANE-012 | Mark files for later | IMarkingService | DualPaneBrowserVM | CREATE |
| WS-XPANE-013 | Cross-pane search | ISearchService | DualPaneBrowserVM | WIRE |

---

## PART 5: TABBED INTERFACE (22 Features)

### 5.1 Tab Management (12 Features) - WS-TAB

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-TAB-001 | New tab (Ctrl+T) | ITabService | TabContainerVM | WIRE |
| WS-TAB-002 | Close tab (Ctrl+W) | ITabService | TabContainerVM | WIRE |
| WS-TAB-003 | Close other tabs | ITabService | TabContainerVM | WIRE |
| WS-TAB-004 | Close tabs to right | ITabService | TabContainerVM | WIRE |
| WS-TAB-005 | Reopen closed tab (Ctrl+Shift+T) | ITabService | TabContainerVM | WIRE |
| WS-TAB-006 | Switch tabs (Ctrl+Tab) | ITabService | TabContainerVM | IMPL |
| WS-TAB-007 | Direct tab switch (Ctrl+1-9) | ITabService | TabContainerVM | IMPL |
| WS-TAB-008 | Drag to reorder tabs | ITabService | TabContainerVM | IMPL |
| WS-TAB-009 | Drag tab to new window | ITabService | TabContainerVM | IMPL |
| WS-TAB-010 | Pin tab | ITabService | TabContainerVM | WIRE |
| WS-TAB-011 | Duplicate tab | ITabService | TabContainerVM | WIRE |
| WS-TAB-012 | Tab context menu | ITabService | TabContextMenuVM | CREATE |

### 5.2 Tab Sessions (10 Features) - WS-SESS

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-SESS-001 | Save session (all tabs) | ITabService | SessionManagerVM | CREATE |
| WS-SESS-002 | Restore session | ITabService | SessionManagerVM | CREATE |
| WS-SESS-003 | Named sessions | ITabService | SessionManagerVM | CREATE |
| WS-SESS-004 | Auto-save session | ITabService | SessionManagerVM | IMPL |
| WS-SESS-005 | Restore on startup | ITabService | SessionManagerVM | IMPL |
| WS-SESS-006 | Session manager UI | ITabService | SessionManagerVM | CREATE |
| WS-SESS-007 | Export session | ITabService | SessionManagerVM | IMPL |
| WS-SESS-008 | Import session | ITabService | SessionManagerVM | IMPL |
| WS-SESS-009 | Tab groups | ITabService | TabGroupsVM | CREATE |
| WS-SESS-010 | Collapse tab group | ITabService | TabGroupsVM | IMPL |

---

## PART 6: QUICK ACCESS & FAVORITES (18 Features)

### 6.1 Favorites System (10 Features) - WS-FAV

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-FAV-001 | Add to favorites | IFavoritesService | FavoritesPanelVM | WIRE |
| WS-FAV-002 | Remove from favorites | IFavoritesService | FavoritesPanelVM | WIRE |
| WS-FAV-003 | Favorites sidebar | IFavoritesService | FavoritesPanelVM | WIRE |
| WS-FAV-004 | Favorites tree structure | IFavoritesService | FavoritesPanelVM | IMPL |
| WS-FAV-005 | Drag to favorites | IFavoritesService | FavoritesPanelVM | IMPL |
| WS-FAV-006 | Reorder favorites | IFavoritesService | FavoritesPanelVM | WIRE |
| WS-FAV-007 | Favorites keyboard shortcuts | IFavoritesService | FavoritesPanelVM | IMPL |
| WS-FAV-008 | Favorites sync | IFavoritesService | FavoritesPanelVM | IMPL |
| WS-FAV-009 | Favorites groups | IFavoritesService | FavoritesPanelVM | WIRE |
| WS-FAV-010 | Favorites search | IFavoritesService | FavoritesPanelVM | IMPL |

### 6.2 Quick Access (8 Features) - WS-QUICK

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-QUICK-001 | Recent files list | IFavoritesService | QuickAccessVM | CREATE |
| WS-QUICK-002 | Recent folders list | IFavoritesService | QuickAccessVM | WIRE |
| WS-QUICK-003 | Frequent folders | IFavoritesService | QuickAccessVM | WIRE |
| WS-QUICK-004 | Pinned folders | IFavoritesService | QuickAccessVM | CREATE |
| WS-QUICK-005 | Quick access auto-populate | IFavoritesService | QuickAccessVM | IMPL |
| WS-QUICK-006 | Clear recent history | IFavoritesService | QuickAccessVM | WIRE |
| WS-QUICK-007 | Quick access customization | IFavoritesService | QuickAccessSettingsVM | CREATE |
| WS-QUICK-008 | Quick access folder colors | IFavoritesService | QuickAccessVM | IMPL |

---

## PART 7: FILTERING & QUICK SEARCH (18 Features)

### 7.1 Quick Filter (10 Features) - WS-FILTER

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-FILTER-001 | Filter bar | IQuickFilterService | FilterBarVM | CREATE |
| WS-FILTER-002 | Filter by name | IQuickFilterService | FilterBarVM | CREATE |
| WS-FILTER-003 | Filter as you type | IQuickFilterService | FilterBarVM | CREATE |
| WS-FILTER-004 | Clear filter (Escape) | IQuickFilterService | FilterBarVM | CREATE |
| WS-FILTER-005 | Filter history | IQuickFilterService | FilterBarVM | IMPL |
| WS-FILTER-006 | Filter by extension dropdown | IQuickFilterService | FilterBarVM | CREATE |
| WS-FILTER-007 | Multiple extension filter | IQuickFilterService | FilterBarVM | IMPL |
| WS-FILTER-008 | Exclude filter | IQuickFilterService | FilterBarVM | IMPL |
| WS-FILTER-009 | Filter with wildcards | IQuickFilterService | FilterBarVM | IMPL |
| WS-FILTER-010 | Filter persistence | IQuickFilterService | FilterBarVM | IMPL |

### 7.2 Column Filters (8 Features) - WS-COLFILT

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-COLFILT-001 | Size column filter | IQuickFilterService | ColumnFilterVM | CREATE |
| WS-COLFILT-002 | Date column filter | IQuickFilterService | ColumnFilterVM | CREATE |
| WS-COLFILT-003 | Type column filter | IQuickFilterService | ColumnFilterVM | CREATE |
| WS-COLFILT-004 | Attribute column filter | IQuickFilterService | ColumnFilterVM | CREATE |
| WS-COLFILT-005 | Multiple column filters | IQuickFilterService | ColumnFilterVM | IMPL |
| WS-COLFILT-006 | Save filter preset | IQuickFilterService | FilterPresetsVM | CREATE |
| WS-COLFILT-007 | Filter indicator | IQuickFilterService | FileListVM | IMPL |
| WS-COLFILT-008 | Filter count display | IQuickFilterService | StatusBarVM | IMPL |

---

## PART 8: SMART ORGANIZER (35 Features)

### 8.1 Type-Based Organization (10 Features) - WS-ORG

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-ORG-001 | Images folder | IOrganizerService | OrganizerVM | WIRE |
| WS-ORG-002 | Videos folder | IOrganizerService | OrganizerVM | WIRE |
| WS-ORG-003 | Music folder | IOrganizerService | OrganizerVM | WIRE |
| WS-ORG-004 | Documents folder | IOrganizerService | OrganizerVM | WIRE |
| WS-ORG-005 | Archives folder | IOrganizerService | OrganizerVM | WIRE |
| WS-ORG-006 | Code folder | IOrganizerService | OrganizerVM | WIRE |
| WS-ORG-007 | Executables folder | IOrganizerService | OrganizerVM | WIRE |
| WS-ORG-008 | Spreadsheets folder | IOrganizerService | OrganizerVM | IMPL |
| WS-ORG-009 | Presentations folder | IOrganizerService | OrganizerVM | IMPL |
| WS-ORG-010 | Custom type mappings | IOrganizerService | TypeMappingsVM | CREATE |

### 8.2 Date-Based Organization (10 Features) - WS-DATEORG

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-DATEORG-001 | Year folders | IOrganizerService | DateOrganizeVM | CREATE |
| WS-DATEORG-002 | Year/Month folders | IOrganizerService | DateOrganizeVM | CREATE |
| WS-DATEORG-003 | Year/Month/Day folders | IOrganizerService | DateOrganizeVM | CREATE |
| WS-DATEORG-004 | Week-based folders | IOrganizerService | DateOrganizeVM | CREATE |
| WS-DATEORG-005 | Quarter-based folders | IOrganizerService | DateOrganizeVM | CREATE |
| WS-DATEORG-006 | Use creation date | IOrganizerService | DateOrganizeVM | IMPL |
| WS-DATEORG-007 | Use modification date | IOrganizerService | DateOrganizeVM | IMPL |
| WS-DATEORG-008 | Use EXIF date (photos) | IOrganizerService | DateOrganizeVM | IMPL |
| WS-DATEORG-009 | Date format customization | IOrganizerService | DateFormatVM | CREATE |
| WS-DATEORG-010 | Handle missing dates | IOrganizerService | DateOrganizeVM | IMPL |

### 8.3 AI-Based Organization (10 Features) - WS-AIORG

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-AIORG-001 | Content-based classification | IAIClassificationService | AIOrganizeVM | CREATE |
| WS-AIORG-002 | Image content recognition | IAIClassificationService | AIOrganizeVM | CREATE |
| WS-AIORG-003 | Document topic detection | IAIClassificationService | AIOrganizeVM | CREATE |
| WS-AIORG-004 | Project boundary detection | IOrganizerService | AIOrganizeVM | IMPL |
| WS-AIORG-005 | Semantic grouping | IAIClassificationService | AIOrganizeVM | CREATE |
| WS-AIORG-006 | Auto-suggested categories | IAIClassificationService | AIOrganizeVM | CREATE |
| WS-AIORG-007 | Learning from corrections | IAIClassificationService | AIOrganizeVM | IMPL |
| WS-AIORG-008 | Confidence score display | IAIClassificationService | AIOrganizeVM | IMPL |
| WS-AIORG-009 | Manual override option | IOrganizerService | AIOrganizeVM | IMPL |
| WS-AIORG-010 | Training mode | IAIClassificationService | AITrainingVM | CREATE |

### 8.4 Custom Organization (5 Features) - WS-CUSTORG

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-CUSTORG-001 | By first letter | IOrganizerService | CustomOrganizeVM | CREATE |
| WS-CUSTORG-002 | By file size | IOrganizerService | CustomOrganizeVM | CREATE |
| WS-CUSTORG-003 | By project name | IOrganizerService | CustomOrganizeVM | CREATE |
| WS-CUSTORG-004 | By author/creator | IOrganizerService | CustomOrganizeVM | CREATE |
| WS-CUSTORG-005 | Custom strategy builder | IOrganizerService | StrategyBuilderVM | CREATE |

---

## PART 9: ORGANIZATION RULES ENGINE (30 Features)

### 9.1 Condition Types (15 Features) - WS-COND

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-COND-001 | Name contains | IOrganizerService | RuleBuilderVM | CREATE |
| WS-COND-002 | Name starts with | IOrganizerService | RuleBuilderVM | CREATE |
| WS-COND-003 | Name ends with | IOrganizerService | RuleBuilderVM | CREATE |
| WS-COND-004 | Name matches regex | IOrganizerService | RuleBuilderVM | CREATE |
| WS-COND-005 | Extension equals | IOrganizerService | RuleBuilderVM | CREATE |
| WS-COND-006 | Extension in list | IOrganizerService | RuleBuilderVM | CREATE |
| WS-COND-007 | Size greater than | IOrganizerService | RuleBuilderVM | CREATE |
| WS-COND-008 | Size less than | IOrganizerService | RuleBuilderVM | CREATE |
| WS-COND-009 | Size between | IOrganizerService | RuleBuilderVM | CREATE |
| WS-COND-010 | Date newer than | IOrganizerService | RuleBuilderVM | CREATE |
| WS-COND-011 | Date older than | IOrganizerService | RuleBuilderVM | CREATE |
| WS-COND-012 | Date between | IOrganizerService | RuleBuilderVM | CREATE |
| WS-COND-013 | Path contains | IOrganizerService | RuleBuilderVM | CREATE |
| WS-COND-014 | Attribute is set | IOrganizerService | RuleBuilderVM | CREATE |
| WS-COND-015 | Content contains | IOrganizerService | RuleBuilderVM | CREATE |

### 9.2 Condition Logic (6 Features) - WS-LOGIC

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-LOGIC-001 | AND conditions | IOrganizerService | RuleBuilderVM | IMPL |
| WS-LOGIC-002 | OR conditions | IOrganizerService | RuleBuilderVM | IMPL |
| WS-LOGIC-003 | NOT conditions | IOrganizerService | RuleBuilderVM | IMPL |
| WS-LOGIC-004 | Nested conditions | IOrganizerService | RuleBuilderVM | IMPL |
| WS-LOGIC-005 | Condition groups | IOrganizerService | RuleBuilderVM | IMPL |
| WS-LOGIC-006 | Condition testing | IOrganizerService | RuleBuilderVM | CREATE |

### 9.3 Action Types (9 Features) - WS-ACT

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-ACT-001 | Move to folder | IOrganizerService | RuleBuilderVM | CREATE |
| WS-ACT-002 | Copy to folder | IOrganizerService | RuleBuilderVM | CREATE |
| WS-ACT-003 | Rename file | IOrganizerService | RuleBuilderVM | CREATE |
| WS-ACT-004 | Delete file | IOrganizerService | RuleBuilderVM | CREATE |
| WS-ACT-005 | Add tag | IOrganizerService | RuleBuilderVM | CREATE |
| WS-ACT-006 | Compress file | IArchiveService | RuleBuilderVM | CREATE |
| WS-ACT-007 | Create symlink | IOrganizerService | RuleBuilderVM | CREATE |
| WS-ACT-008 | Set attributes | IOrganizerService | RuleBuilderVM | CREATE |
| WS-ACT-009 | Execute script | IOrganizerService | RuleBuilderVM | CREATE |

---

## PART 10: WATCH FOLDERS & AUTOMATION (25 Features)

### 10.1 Watch Folder Setup (10 Features) - WS-WATCH

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-WATCH-001 | Create watch folder | IWatchFolderService | WatchFolderVM | CREATE |
| WS-WATCH-002 | Configure watch folder | IWatchFolderService | WatchConfigVM | CREATE |
| WS-WATCH-003 | Enable/disable watch | IWatchFolderService | WatchFolderVM | CREATE |
| WS-WATCH-004 | Watch subfolders option | IWatchFolderService | WatchConfigVM | IMPL |
| WS-WATCH-005 | Watch specific extensions | IWatchFolderService | WatchConfigVM | IMPL |
| WS-WATCH-006 | Watch exclude patterns | IWatchFolderService | WatchConfigVM | IMPL |
| WS-WATCH-007 | Watch delay (settle time) | IWatchFolderService | WatchConfigVM | IMPL |
| WS-WATCH-008 | Watch multiple folders | IWatchFolderService | WatchFolderVM | CREATE |
| WS-WATCH-009 | Watch folder list UI | IWatchFolderService | WatchFolderListVM | CREATE |
| WS-WATCH-010 | Watch folder status | IWatchFolderService | WatchFolderVM | IMPL |

### 10.2 Automation Triggers (8 Features) - WS-TRIG

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-TRIG-001 | On file created | IWatchFolderService | TriggerConfigVM | CREATE |
| WS-TRIG-002 | On file modified | IWatchFolderService | TriggerConfigVM | CREATE |
| WS-TRIG-003 | On file renamed | IWatchFolderService | TriggerConfigVM | CREATE |
| WS-TRIG-004 | On file deleted | IWatchFolderService | TriggerConfigVM | CREATE |
| WS-TRIG-005 | On schedule | ISchedulerService | SchedulerVM | CREATE |
| WS-TRIG-006 | On startup | IWatchFolderService | TriggerConfigVM | IMPL |
| WS-TRIG-007 | On idle | IWatchFolderService | TriggerConfigVM | IMPL |
| WS-TRIG-008 | Manual trigger | IWatchFolderService | WatchFolderVM | CREATE |

### 10.3 Automation Settings (7 Features) - WS-AUTO

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-AUTO-001 | Process existing files | IWatchFolderService | AutomationSettingsVM | CREATE |
| WS-AUTO-002 | Process delay | IWatchFolderService | AutomationSettingsVM | IMPL |
| WS-AUTO-003 | Conflict handling | IWatchFolderService | AutomationSettingsVM | IMPL |
| WS-AUTO-004 | Error handling | IWatchFolderService | AutomationSettingsVM | IMPL |
| WS-AUTO-005 | Notification on complete | IWatchFolderService | AutomationSettingsVM | IMPL |
| WS-AUTO-006 | Log all operations | IWatchFolderService | AutomationLogVM | CREATE |
| WS-AUTO-007 | Dry run mode | IWatchFolderService | AutomationSettingsVM | IMPL |

---

## PART 11: BATCH RENAME (50 Features)

### 11.1 Rename Rules (28 Features) - WS-REN

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-REN-001 | Find and replace | IBatchRenameService | BatchRenameVM | WIRE |
| WS-REN-002 | Find regex replace | IBatchRenameService | BatchRenameVM | WIRE |
| WS-REN-003 | Add prefix | IBatchRenameService | BatchRenameVM | WIRE |
| WS-REN-004 | Add suffix | IBatchRenameService | BatchRenameVM | WIRE |
| WS-REN-005 | Insert at position | IBatchRenameService | BatchRenameVM | WIRE |
| WS-REN-006 | Remove characters | IBatchRenameService | BatchRenameVM | WIRE |
| WS-REN-007 | Remove by pattern | IBatchRenameService | BatchRenameVM | IMPL |
| WS-REN-008 | Trim whitespace | IBatchRenameService | BatchRenameVM | IMPL |
| WS-REN-009 | Sequential counter | IBatchRenameService | BatchRenameVM | WIRE |
| WS-REN-010 | Counter start value | IBatchRenameService | BatchRenameVM | WIRE |
| WS-REN-011 | Counter step | IBatchRenameService | BatchRenameVM | IMPL |
| WS-REN-012 | Counter padding | IBatchRenameService | BatchRenameVM | WIRE |
| WS-REN-013 | Counter per folder | IBatchRenameService | BatchRenameVM | IMPL |
| WS-REN-014 | Roman numerals | IBatchRenameService | BatchRenameVM | IMPL |
| WS-REN-015 | Lowercase all | IBatchRenameService | BatchRenameVM | WIRE |
| WS-REN-016 | Uppercase all | IBatchRenameService | BatchRenameVM | WIRE |
| WS-REN-017 | Title case | IBatchRenameService | BatchRenameVM | WIRE |
| WS-REN-018 | Sentence case | IBatchRenameService | BatchRenameVM | WIRE |
| WS-REN-019 | Toggle case | IBatchRenameService | BatchRenameVM | IMPL |
| WS-REN-020 | CamelCase | IBatchRenameService | BatchRenameVM | IMPL |
| WS-REN-021 | Change extension | IBatchRenameService | BatchRenameVM | WIRE |
| WS-REN-022 | Add extension | IBatchRenameService | BatchRenameVM | IMPL |
| WS-REN-023 | Remove extension | IBatchRenameService | BatchRenameVM | IMPL |
| WS-REN-024 | Lowercase extension | IBatchRenameService | BatchRenameVM | IMPL |
| WS-REN-025 | Extension case match | IBatchRenameService | BatchRenameVM | IMPL |
| WS-REN-026 | Current date insertion | IBatchRenameService | BatchRenameVM | WIRE |
| WS-REN-027 | File date insertion | IBatchRenameService | BatchRenameVM | IMPL |
| WS-REN-028 | EXIF date insertion | IBatchRenameService | MetadataRenameVM | CREATE |

### 11.2 Metadata Rename (12 Features) - WS-META

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-META-001 | EXIF date/time | IMetadataService | MetadataRenameVM | CREATE |
| WS-META-002 | Camera model | IMetadataService | MetadataRenameVM | CREATE |
| WS-META-003 | Lens info | IMetadataService | MetadataRenameVM | CREATE |
| WS-META-004 | GPS location | IMetadataService | MetadataRenameVM | CREATE |
| WS-META-005 | Image dimensions | IMetadataService | MetadataRenameVM | CREATE |
| WS-META-006 | ID3 artist | IMetadataService | MetadataRenameVM | CREATE |
| WS-META-007 | ID3 album | IMetadataService | MetadataRenameVM | CREATE |
| WS-META-008 | ID3 track number | IMetadataService | MetadataRenameVM | CREATE |
| WS-META-009 | ID3 title | IMetadataService | MetadataRenameVM | CREATE |
| WS-META-010 | PDF title | IMetadataService | MetadataRenameVM | CREATE |
| WS-META-011 | PDF author | IMetadataService | MetadataRenameVM | CREATE |
| WS-META-012 | Office properties | IMetadataService | MetadataRenameVM | CREATE |

### 11.3 Batch Rename UX (10 Features) - WS-RENUX

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-RENUX-001 | Live preview | IBatchRenameService | BatchRenameVM | WIRE |
| WS-RENUX-002 | Before/after columns | IBatchRenameService | BatchRenameVM | WIRE |
| WS-RENUX-003 | Conflict highlighting | IBatchRenameService | BatchRenameVM | WIRE |
| WS-RENUX-004 | Warning indicators | IBatchRenameService | BatchRenameVM | IMPL |
| WS-RENUX-005 | Preview refresh | IBatchRenameService | BatchRenameVM | WIRE |
| WS-RENUX-006 | Save preset | IBatchRenameService | RenamePresetsVM | CREATE |
| WS-RENUX-007 | Load preset | IBatchRenameService | RenamePresetsVM | CREATE |
| WS-RENUX-008 | Preset library | IBatchRenameService | RenamePresetsVM | CREATE |
| WS-RENUX-009 | Recent renames | IBatchRenameService | RenameHistoryVM | CREATE |
| WS-RENUX-010 | Undo last rename | IBatchRenameService | BatchRenameVM | WIRE |

---

## PART 12: SPACE RECOVERY (45 Features)

### 12.1 Visual Analysis (8 Features) - WS-VIS

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-VIS-001 | TreeMap visualization | ISpaceAnalyzerService | TreeMapVM | CREATE |
| WS-VIS-002 | Sunburst chart | ISpaceAnalyzerService | SunburstVM | CREATE |
| WS-VIS-003 | Bar chart by type | ISpaceAnalyzerService | SpaceChartVM | CREATE |
| WS-VIS-004 | Pie chart by type | ISpaceAnalyzerService | SpaceChartVM | CREATE |
| WS-VIS-005 | Timeline chart (age) | ISpaceAnalyzerService | TimelineVM | CREATE |
| WS-VIS-006 | Interactive drill-down | ISpaceAnalyzerService | TreeMapVM | IMPL |
| WS-VIS-007 | Zoom in/out | ISpaceAnalyzerService | TreeMapVM | IMPL |
| WS-VIS-008 | Tooltip on hover | ISpaceAnalyzerService | TreeMapVM | IMPL |

### 12.2 Analysis Reports (8 Features) - WS-REPORT

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-REPORT-001 | Largest files list | ISpaceAnalyzerService | SpaceRecoveryVM | WIRE |
| WS-REPORT-002 | Largest folders list | ISpaceAnalyzerService | SpaceRecoveryVM | WIRE |
| WS-REPORT-003 | Size by extension | ISpaceAnalyzerService | SpaceRecoveryVM | WIRE |
| WS-REPORT-004 | Size by file type | ISpaceAnalyzerService | SpaceRecoveryVM | WIRE |
| WS-REPORT-005 | Size by age | ISpaceAnalyzerService | SpaceRecoveryVM | WIRE |
| WS-REPORT-006 | Empty folders | ISpaceAnalyzerService | SpaceRecoveryVM | WIRE |
| WS-REPORT-007 | Duplicates report | IDuplicateFinderService | SpaceRecoveryVM | WIRE |
| WS-REPORT-008 | Export to CSV/JSON | ISpaceAnalyzerService | SpaceRecoveryVM | IMPL |

### 12.3 Cleanup Categories (25+ Features) - WS-CLEAN

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-CLEAN-001 | Windows Temp | ISpaceAnalyzerService | CleanupCategoriesVM | CREATE |
| WS-CLEAN-002 | User Temp | ISpaceAnalyzerService | CleanupCategoriesVM | CREATE |
| WS-CLEAN-003 | Windows Update cache | ISpaceAnalyzerService | CleanupCategoriesVM | CREATE |
| WS-CLEAN-004 | Thumbnail cache | ISpaceAnalyzerService | CleanupCategoriesVM | CREATE |
| WS-CLEAN-005 | Prefetch files | ISpaceAnalyzerService | CleanupCategoriesVM | CREATE |
| WS-CLEAN-006 | Windows Error Reports | ISpaceAnalyzerService | CleanupCategoriesVM | CREATE |
| WS-CLEAN-007 | Delivery Optimization | ISpaceAnalyzerService | CleanupCategoriesVM | CREATE |
| WS-CLEAN-008 | Chrome cache | ISpaceAnalyzerService | BrowserCleanupVM | CREATE |
| WS-CLEAN-009 | Edge cache | ISpaceAnalyzerService | BrowserCleanupVM | CREATE |
| WS-CLEAN-010 | Firefox cache | ISpaceAnalyzerService | BrowserCleanupVM | CREATE |
| WS-CLEAN-011 | npm cache | ISpaceAnalyzerService | DevCleanupVM | CREATE |
| WS-CLEAN-012 | pip cache | ISpaceAnalyzerService | DevCleanupVM | CREATE |
| WS-CLEAN-013 | NuGet cache | ISpaceAnalyzerService | DevCleanupVM | CREATE |
| WS-CLEAN-014 | Docker images | ISpaceAnalyzerService | DevCleanupVM | CREATE |
| WS-CLEAN-015 | node_modules (stale) | ISpaceAnalyzerService | DevCleanupVM | CREATE |

---

## PART 13: DUPLICATE FINDER (30 Features)

### 13.1 Detection Methods (8 Features) - WS-DUP

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-DUP-001 | Size-based prefilter | IDuplicateFinderService | DuplicateScanVM | CREATE |
| WS-DUP-002 | Quick hash (partial) | IDuplicateFinderService | DuplicateScanVM | CREATE |
| WS-DUP-003 | Full SHA-256 hash | IDuplicateFinderService | DuplicateScanVM | CREATE |
| WS-DUP-004 | Byte comparison | IDuplicateFinderService | DuplicateScanVM | CREATE |
| WS-DUP-005 | Name-based matching | IDuplicateFinderService | DuplicateScanVM | CREATE |
| WS-DUP-006 | Name + size matching | IDuplicateFinderService | DuplicateScanVM | CREATE |
| WS-DUP-007 | Perceptual image hash | IPerceptualHashService | SimilarImagesVM | CREATE |
| WS-DUP-008 | Audio fingerprint | IAudioFingerprintService | SimilarAudioVM | CREATE |

### 13.2 Duplicate Management (12 Features) - WS-DUPMGMT

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-DUPMGMT-001 | Keep first | IDuplicateFinderService | DuplicateResultsVM | CREATE |
| WS-DUPMGMT-002 | Keep last | IDuplicateFinderService | DuplicateResultsVM | CREATE |
| WS-DUPMGMT-003 | Keep oldest | IDuplicateFinderService | DuplicateResultsVM | CREATE |
| WS-DUPMGMT-004 | Keep newest | IDuplicateFinderService | DuplicateResultsVM | CREATE |
| WS-DUPMGMT-005 | Keep shortest path | IDuplicateFinderService | DuplicateResultsVM | CREATE |
| WS-DUPMGMT-006 | Keep longest path | IDuplicateFinderService | DuplicateResultsVM | CREATE |
| WS-DUPMGMT-007 | Delete duplicates | IDuplicateFinderService | DuplicateResultsVM | CREATE |
| WS-DUPMGMT-008 | Move to folder | IDuplicateFinderService | DuplicateResultsVM | CREATE |
| WS-DUPMGMT-009 | Replace with hardlink | IDuplicateFinderService | DuplicateResultsVM | CREATE |
| WS-DUPMGMT-010 | Replace with symlink | IDuplicateFinderService | DuplicateResultsVM | CREATE |
| WS-DUPMGMT-011 | Preview before action | IDuplicateFinderService | DuplicatePreviewVM | CREATE |
| WS-DUPMGMT-012 | Undo action | IBackupProtectionService | DuplicateResultsVM | IMPL |

---

## PART 14: SEARCH ENGINE (35 Features)

### 14.1 Search Types (8 Features) - WS-SRCH

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-SRCH-001 | Filename search | ISearchService | SearchResultsVM | WIRE |
| WS-SRCH-002 | Wildcard search | ISearchService | SearchResultsVM | WIRE |
| WS-SRCH-003 | Regex search | ISearchService | SearchResultsVM | WIRE |
| WS-SRCH-004 | Content search | ISearchService | ContentSearchVM | CREATE |
| WS-SRCH-005 | Boolean search | ISearchService | AdvancedSearchVM | CREATE |
| WS-SRCH-006 | Phrase search | ISearchService | AdvancedSearchVM | CREATE |
| WS-SRCH-007 | Fuzzy search | ISearchService | SearchResultsVM | IMPL |
| WS-SRCH-008 | Semantic search | ISemanticSearchService | SemanticSearchVM | CREATE |

### 14.2 Search Filters (8 Features) - WS-SRCHFILT

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-SRCHFILT-001 | Filter by extension | ISearchService | SearchFilterVM | CREATE |
| WS-SRCHFILT-002 | Filter by size | ISearchService | SearchFilterVM | CREATE |
| WS-SRCHFILT-003 | Filter by date | ISearchService | SearchFilterVM | CREATE |
| WS-SRCHFILT-004 | Filter by attributes | ISearchService | SearchFilterVM | CREATE |
| WS-SRCHFILT-005 | Filter by path | ISearchService | SearchFilterVM | CREATE |
| WS-SRCHFILT-006 | Filter by type category | ISearchService | SearchFilterVM | CREATE |
| WS-SRCHFILT-007 | Exclude patterns | ISearchService | SearchFilterVM | IMPL |
| WS-SRCHFILT-008 | Combined filters | ISearchService | SearchFilterVM | IMPL |

### 14.3 Search Indexing (15 Features) - WS-INDEX

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-INDEX-001 | MFT-based indexing | INexusIndexerService | IndexingVM | CREATE |
| WS-INDEX-002 | Full drive indexing | ISearchService | IndexingVM | WIRE |
| WS-INDEX-003 | Incremental indexing | ISearchService | IndexingVM | IMPL |
| WS-INDEX-004 | USN Journal monitoring | INexusIndexerService | IndexingVM | CREATE |
| WS-INDEX-005 | Background indexing | ISearchService | IndexingVM | IMPL |
| WS-INDEX-006 | Index compression | ISearchService | IndexingVM | IMPL |
| WS-INDEX-007 | Index exclusions | ISearchService | IndexSettingsVM | CREATE |
| WS-INDEX-008 | Index scheduling | ISearchService | IndexSettingsVM | CREATE |
| WS-INDEX-009 | View index status | ISearchService | IndexStatusVM | CREATE |
| WS-INDEX-010 | Rebuild index | ISearchService | IndexStatusVM | CREATE |
| WS-INDEX-011 | Pause indexing | ISearchService | IndexStatusVM | CREATE |
| WS-INDEX-012 | Index size info | ISearchService | IndexStatusVM | IMPL |
| WS-INDEX-013 | Index optimization | ISearchService | IndexStatusVM | IMPL |
| WS-INDEX-014 | Multi-index support | ISearchService | IndexSettingsVM | IMPL |
| WS-INDEX-015 | Index backup/restore | ISearchService | IndexSettingsVM | IMPL |

---

## PART 15: PREVIEW & VIEWERS (25 Features)

### 15.1 Preview Pane (15 Features) - WS-PREV

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-PREV-001 | Preview pane toggle | IPreviewService | PreviewPaneVM | CREATE |
| WS-PREV-002 | Auto-preview on select | IPreviewService | PreviewPaneVM | CREATE |
| WS-PREV-003 | Preview pane resize | IPreviewService | PreviewPaneVM | IMPL |
| WS-PREV-004 | Preview position (right/bottom) | IPreviewService | PreviewPaneVM | IMPL |
| WS-PREV-005 | Image preview | IPreviewService | ImagePreviewVM | CREATE |
| WS-PREV-006 | Text preview | IPreviewService | TextPreviewVM | CREATE |
| WS-PREV-007 | Video thumbnail | IPreviewService | VideoPreviewVM | CREATE |
| WS-PREV-008 | Audio waveform | IPreviewService | AudioPreviewVM | CREATE |
| WS-PREV-009 | PDF preview | IPreviewService | PdfPreviewVM | CREATE |
| WS-PREV-010 | Office preview | IPreviewService | OfficePreviewVM | CREATE |
| WS-PREV-011 | Code preview | IPreviewService | CodePreviewVM | CREATE |
| WS-PREV-012 | Markdown preview | IPreviewService | MarkdownPreviewVM | CREATE |
| WS-PREV-013 | JSON/XML tree | IPreviewService | JsonTreeVM | CREATE |
| WS-PREV-014 | Archive contents | IArchiveService | ArchivePreviewVM | CREATE |
| WS-PREV-015 | Hex preview | IPreviewService | HexPreviewVM | CREATE |

### 15.2 Quick Look (10 Features) - WS-QLOOK

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-QLOOK-001 | Spacebar quick look | IQuickLookService | QuickLookVM | CREATE |
| WS-QLOOK-002 | Quick look zoom | IQuickLookService | QuickLookVM | IMPL |
| WS-QLOOK-003 | Quick look pan | IQuickLookService | QuickLookVM | IMPL |
| WS-QLOOK-004 | Navigate in quick look | IQuickLookService | QuickLookVM | IMPL |
| WS-QLOOK-005 | Close on click outside | IQuickLookService | QuickLookVM | IMPL |
| WS-QLOOK-006 | Full-screen quick look | IQuickLookService | QuickLookVM | IMPL |
| WS-QLOOK-007 | Open from quick look | IQuickLookService | QuickLookVM | IMPL |
| WS-QLOOK-008 | Quick look slideshow | IQuickLookService | QuickLookVM | IMPL |
| WS-QLOOK-009 | Quick look video play | IQuickLookService | QuickLookVM | IMPL |
| WS-QLOOK-010 | Quick look audio play | IQuickLookService | QuickLookVM | IMPL |

---

## PART 16: ARCHIVE SUPPORT (25 Features)

### 16.1 Archive Viewing (8 Features) - WS-ARC

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-ARC-001 | Browse ZIP contents | IArchiveService | ArchiveBrowserVM | CREATE |
| WS-ARC-002 | Browse 7z contents | IArchiveService | ArchiveBrowserVM | CREATE |
| WS-ARC-003 | Browse RAR contents | IArchiveService | ArchiveBrowserVM | CREATE |
| WS-ARC-004 | Browse TAR/GZ contents | IArchiveService | ArchiveBrowserVM | CREATE |
| WS-ARC-005 | Nested archive support | IArchiveService | ArchiveBrowserVM | IMPL |
| WS-ARC-006 | Search in archive | IArchiveService | ArchiveBrowserVM | IMPL |
| WS-ARC-007 | Preview in archive | IArchiveService | ArchivePreviewVM | IMPL |
| WS-ARC-008 | Archive properties | IArchiveService | ArchivePropertiesVM | CREATE |

### 16.2 Extract Operations (8 Features) - WS-EXTRACT

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-EXTRACT-001 | Extract all | IArchiveService | ExtractDialogVM | CREATE |
| WS-EXTRACT-002 | Extract here | IArchiveService | ExtractDialogVM | CREATE |
| WS-EXTRACT-003 | Extract to folder | IArchiveService | ExtractDialogVM | CREATE |
| WS-EXTRACT-004 | Extract selected | IArchiveService | ExtractDialogVM | CREATE |
| WS-EXTRACT-005 | Extract with path | IArchiveService | ExtractDialogVM | IMPL |
| WS-EXTRACT-006 | Extract progress | IArchiveService | ExtractProgressVM | CREATE |
| WS-EXTRACT-007 | Password prompt | IArchiveService | PasswordDialogVM | CREATE |
| WS-EXTRACT-008 | Extract and open | IArchiveService | ExtractDialogVM | IMPL |

### 16.3 Create Operations (9 Features) - WS-COMPRESS

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-COMPRESS-001 | Create ZIP | IArchiveService | CreateArchiveVM | CREATE |
| WS-COMPRESS-002 | Create 7z | IArchiveService | CreateArchiveVM | CREATE |
| WS-COMPRESS-003 | Create TAR.GZ | IArchiveService | CreateArchiveVM | CREATE |
| WS-COMPRESS-004 | Compression level | IArchiveService | CreateArchiveVM | IMPL |
| WS-COMPRESS-005 | Password protection | IArchiveService | CreateArchiveVM | IMPL |
| WS-COMPRESS-006 | Split archive | IArchiveService | CreateArchiveVM | IMPL |
| WS-COMPRESS-007 | Add to archive | IArchiveService | CreateArchiveVM | IMPL |
| WS-COMPRESS-008 | Update archive | IArchiveService | CreateArchiveVM | IMPL |
| WS-COMPRESS-009 | Self-extracting | IArchiveService | CreateArchiveVM | IMPL |

---

## PART 17: CONTEXT MENU & INTEGRATION (20 Features)

### 17.1 Context Menu Items (12 Features) - WS-CTX

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-CTX-001 | Open with Winhance | IContextMenuService | ContextMenuVM | WIRE |
| WS-CTX-002 | Analyze folder size | IContextMenuService | ContextMenuVM | WIRE |
| WS-CTX-003 | Find duplicates | IContextMenuService | ContextMenuVM | WIRE |
| WS-CTX-004 | Batch rename | IContextMenuService | ContextMenuVM | WIRE |
| WS-CTX-005 | Organize folder | IContextMenuService | ContextMenuVM | WIRE |
| WS-CTX-006 | Copy path | IContextMenuService | ContextMenuVM | WIRE |
| WS-CTX-007 | Copy name | IContextMenuService | ContextMenuVM | WIRE |
| WS-CTX-008 | Calculate hash | IContextMenuService | HashDialogVM | CREATE |
| WS-CTX-009 | Open terminal here | IContextMenuService | ContextMenuVM | WIRE |
| WS-CTX-010 | Create symlink | IContextMenuService | SymlinkDialogVM | CREATE |
| WS-CTX-011 | Take ownership | IContextMenuService | ContextMenuVM | IMPL |
| WS-CTX-012 | Secure delete | IContextMenuService | SecureDeleteVM | CREATE |

### 17.2 Shell Extensions (8 Features) - WS-SHELL

| Task ID | Feature | Backend Service | ViewModel | Status |
|---------|---------|-----------------|-----------|--------|
| WS-SHELL-001 | Taskbar progress | ITaskbarIntegrationService | ProgressVM | CREATE |
| WS-SHELL-002 | Jump lists | IJumpListService | JumpListVM | CREATE |
| WS-SHELL-003 | Thumbnail handler | IThumbnailService | - | IMPL |
| WS-SHELL-004 | Property sheet | IPropertySheetService | PropertiesVM | CREATE |
| WS-SHELL-005 | Icon overlays | IOverlayService | - | IMPL |
| WS-SHELL-006 | Drag-drop handler | IDragDropService | - | IMPL |
| WS-SHELL-007 | Toast notifications | INotificationService | - | IMPL |
| WS-SHELL-008 | System tray icon | ITrayService | TrayVM | CREATE |

---

## SUMMARY

**Total Windsurf Tasks:** 503
**ViewModels to CREATE:** ~85
**ViewModels to WIRE:** ~120
**ViewModels with IMPL needed:** ~298

### Priority Order for Windsurf

1. **CRITICAL (Complete First):**
   - TabContainerVM - Wire to ITabService
   - DualPaneBrowserVM - Wire to services
   - FavoritesPanelVM - Wire to IFavoritesService
   - SearchResultsVM - Wire to ISearchService
   - ProgressDialogVM - Wire to IOperationQueueService
   - BatchRenameVM - Already exists, complete wiring
   - SpaceRecoveryVM - Wire to ISpaceAnalyzerService

2. **HIGH (Complete Week 2):**
   - All Filter/Sort ViewModels
   - All Selection ViewModels
   - Preview pane ViewModels
   - Conflict dialog ViewModels

3. **MEDIUM (Complete Week 3-4):**
   - Archive ViewModels
   - Duplicate finder ViewModels
   - Watch folder ViewModels
   - Context menu ViewModels

4. **LOWER (Complete Week 5+):**
   - AI organization ViewModels
   - Metadata rename ViewModels
   - Advanced shell integration

---

**Document Version:** 1.0
**Generated by:** Claude Code
**For:** Windsurf Agent
**Date:** 2026-01-24
