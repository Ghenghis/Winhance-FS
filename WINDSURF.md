# WINDSURF STRICT RULES - RELEASE READY IMPLEMENTATION

## CRITICAL: THIS IS A RELEASE BUILD - NO STUBS, NO MOCKS, NO PLACEHOLDERS

Every single feature MUST be:
- Fully functional with real backend integration
- Production quality code with error handling
- Complete UI/UX following existing patterns
- Build-verified (0 errors, 0 warnings)

---

## CURRENT BUILD STATUS (2026-01-26)

- **Backend**: 100% COMPLETE (Claude fixed all services)
- **WPF Project**: 4+ XAML errors to fix
- **Feature Files**: 264 XAML views, 302 ViewModels exist
- **Problem**: Many are stubs/placeholders, not real implementations

---

## ABSOLUTE RULES (VIOLATION = IMMEDIATE REVERT)

### Rule 1: NO STUBS OR PLACEHOLDERS
❌ FORBIDDEN:
- `// TODO: implement later`
- `throw new NotImplementedException()`
- `return null; // placeholder`
- Empty method bodies
- Fake data generation
- Mock services

✅ REQUIRED:
- Full implementation using backend services
- Real data binding to service methods
- Complete error handling with user feedback
- All UI states implemented (loading, success, error, empty)

### Rule 2: NO INVALID XAML
❌ FORBIDDEN:
- `data-testid` attributes (HTML, not WPF)
- Multiple root elements in XAML
- `<UserControl.CodeBehind>` tags
- Incorrect namespace references
- Missing x:Class declarations

✅ REQUIRED:
- Valid WPF XAML only
- Single root element per file
- Code-behind in separate .xaml.cs file
- Proper namespace declarations

### Rule 3: EVERY VIEWMODEL MUST INJECT REAL SERVICES
❌ FORBIDDEN:
```csharp
public MyViewModel()
{
    Items = new ObservableCollection<string> { "Fake1", "Fake2" };
}
```

✅ REQUIRED:
```csharp
public MyViewModel(IFileManagerService fileManager, ISelectionService selection)
{
    _fileManager = fileManager;
    _selection = selection;
    LoadDataAsync().ConfigureAwait(false);
}
```

### Rule 4: BUILD VERIFICATION BEFORE COMMITTING
Run these commands EVERY TIME before any commit:
```bash
dotnet build Winhance.sln --no-restore
# MUST show: 0 Error(s)

dotnet build src/Winhance.WPF/Winhance.WPF.csproj
# MUST show: Build succeeded
```

---

## YOUR TERRITORY (ONLY EDIT THESE)

```
src/Winhance.WPF/Features/FileManager/
├── Views/           ← XAML files (your responsibility)
├── ViewModels/      ← ViewModel classes (your responsibility)
├── Controls/        ← Custom controls (your responsibility)
├── Converters/      ← Value converters (your responsibility)
└── Resources/       ← Styles/templates (your responsibility)
```

## DO NOT TOUCH (Claude Code Territory)

```
src/Winhance.Core/          ← Interfaces (Claude)
src/Winhance.Infrastructure/ ← Services (Claude)
src/nexus_core/             ← Rust backend (Claude)
```

---

## IMMEDIATE FIXES REQUIRED

### Fix 1: FileCopyView.xaml - Remove embedded code-behind
**File:** `src/Winhance.WPF/Features/FileManager/Views/FileCopyView.xaml`
**Error:** MC3000: Multiple root elements at line 109
**Fix:** Remove lines 109-122 (`<UserControl.CodeBehind>...</UserControl.CodeBehind>`)

### Fix 2: FileOperationsView.xaml - Remove data-testid
**File:** `src/Winhance.WPF/Features/FileManager/Views/FileOperationsView.xaml`
**Error:** MC3072: The property 'data-testid' does not exist
**Fix:** Remove all `data-testid="..."` attributes

### Fix 3: All Feature*View.xaml files - Implement real features
**Files:** Feature100View.xaml through Feature199View.xaml (100 files)
**Problem:** These are all placeholder stubs with generic "Execute" buttons
**Fix:** Each must be a REAL implementation of the feature it represents

---

## BACKEND SERVICES READY FOR USE

All these services are FULLY IMPLEMENTED and ready for injection:

| Service | Interface | What It Does |
|---------|-----------|--------------|
| TabService | ITabService | Multi-tab management, history, pinning |
| AddressBarService | IAddressBarService | Breadcrumbs, autocomplete, navigation |
| FavoritesService | IFavoritesService | Bookmarks, pinned items, recent |
| ViewModeService | IViewModeService | Icons/Details/Tiles/Gallery views |
| SortingService | ISortingService | Multi-column sorting, grouping |
| SelectionService | ISelectionService | Pattern selection, range select |
| ClipboardService | IClipboardService | Cut/copy/paste with formats |
| SearchService | ISearchService | Full-text, fuzzy, filters |
| QuickFilterService | IQuickFilterService | Real-time filtering |
| PreviewService | IPreviewService | Image/text/hex preview |
| QuickLookService | IQuickLookService | Spacebar quick preview |
| MetadataService | IMetadataService | EXIF, ID3, document metadata |
| ArchiveService | IArchiveService | ZIP/7Z/RAR operations |
| CompareService | ICompareService | File/folder comparison |
| SyncService | ISyncService | Folder synchronization |
| OrganizerService | IOrganizerService | Auto-organize rules |
| WatchFolderService | IWatchFolderService | Folder monitoring |
| DuplicateFinderService | IDuplicateFinderService | Duplicate detection |
| SpaceAnalyzerService | ISpaceAnalyzerService | Disk usage treemap |
| OperationQueueService | IOperationQueueService | Operation queuing |
| BatchRenameService | IBatchRenameService | Batch rename with preview |
| AdvancedFileOperations | IAdvancedFileOperations | Split/join, secure delete |
| ColumnService | IColumnService | Column configuration |
| ContextMenuService | IContextMenuService | Shell context menu |

---

## VIEWMODEL TEMPLATE (USE THIS PATTERN)

```csharp
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Core.Features.FileManager.Models;

namespace Winhance.WPF.Features.FileManager.ViewModels
{
    public partial class MyFeatureViewModel : ObservableObject
    {
        private readonly IFileManagerService _fileManager;
        private readonly ISelectionService _selection;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private ObservableCollection<FileItem> _items = new();

        public MyFeatureViewModel(
            IFileManagerService fileManager,
            ISelectionService selection)
        {
            _fileManager = fileManager;
            _selection = selection;
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var result = await _fileManager.GetItemsAsync(path);
                Items = new ObservableCollection<FileItem>(result);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
```

---

## XAML TEMPLATE (USE THIS PATTERN)

```xml
<UserControl x:Class="Winhance.WPF.Features.FileManager.Views.MyFeatureView"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d">

    <Grid>
        <!-- Loading State -->
        <ProgressBar IsIndeterminate="True"
            Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibilityConverter}}"/>

        <!-- Error State -->
        <TextBlock Text="{Binding ErrorMessage}"
            Foreground="Red"
            Visibility="{Binding ErrorMessage, Converter={StaticResource NullToVisibilityConverter}}"/>

        <!-- Content -->
        <ListView ItemsSource="{Binding Items}"
            Visibility="{Binding IsLoading, Converter={StaticResource InverseBoolToVisibilityConverter}}">
            <!-- Real data binding, not placeholders -->
        </ListView>
    </Grid>
</UserControl>
```

---

## COMPLETION CHECKLIST

Before marking ANY feature complete:

- [ ] Builds without errors (`dotnet build`)
- [ ] No TODO comments in code
- [ ] No NotImplementedException
- [ ] No hardcoded/fake data
- [ ] ViewModel injects real services
- [ ] All UI states handled (loading, error, empty, success)
- [ ] Keyboard shortcuts work
- [ ] Context menu items work
- [ ] Matches existing app styling
- [ ] Code-behind only has InitializeComponent()

---

## PRIORITY ORDER

### Phase 1: Fix Build Errors (BLOCKING)
1. Fix FileCopyView.xaml (remove embedded code)
2. Fix FileOperationsView.xaml (remove data-testid)
3. Run `dotnet build` - must show 0 errors

### Phase 2: Core Views (Critical Path)
1. DualPaneBrowserView + ViewModel
2. TabContainerView + ViewModel
3. SearchResultsView + ViewModel
4. TreeMapView + ViewModel

### Phase 3: Dialogs
1. OperationProgressDialog
2. ConflictResolutionDialog
3. PropertiesDialog
4. QuickLookWindow

### Phase 4: Feature Views (ALL 436 FEATURES)
Replace ALL Feature*View.xaml stubs with real implementations.

---

## COMPLETE FEATURE LIST (ALL 436 - WINDSURF MUST COMPLETE)

### CATEGORY 1: DUAL-PANE BROWSER (47 Features)

**Panel Management (12)**
| ID | Feature | Description |
|----|---------|-------------|
| DP-001 | Independent Panels | Each pane navigates independently |
| DP-002 | Sync Scroll | Synchronized scrolling between panes |
| DP-003 | Swap Panels | Swap left/right pane contents |
| DP-004 | Splitter Resize | Adjustable pane width ratio |
| DP-005 | Lock Panel | Prevent navigation in a panel |
| DP-006 | Focus Indicator | Visual indicator of active pane |
| DP-007 | Copy to Other | Copy selection to other pane |
| DP-008 | Move to Other | Move selection to other pane |
| DP-009 | Compare Panes | Compare contents of both panes |
| DP-010 | Sync Navigation | Navigate both panes together |
| DP-011 | Mirror Mode | Mirror operations in both panes |
| DP-012 | Single Pane Mode | Toggle to single pane view |

**Tab Management (15)**
| ID | Feature | Description |
|----|---------|-------------|
| TB-001 | Multi-Tab | Multiple tabs per pane |
| TB-002 | Drag Reorder | Drag tabs to reorder |
| TB-003 | Tab Pinning | Pin tabs to prevent close |
| TB-004 | Color Coding | Color-code tabs by category |
| TB-005 | Tab Groups | Group related tabs |
| TB-006 | New Tab Button | Quick new tab creation |
| TB-007 | Close Tab | Close individual tabs |
| TB-008 | Close Others | Close all tabs except current |
| TB-009 | Tab Context Menu | Right-click tab options |
| TB-010 | Duplicate Tab | Duplicate current tab |
| TB-011 | Tab History | Tab-specific back/forward |
| TB-012 | Session Restore | Restore tabs on restart |
| TB-013 | Tab Shortcuts | Ctrl+1-9 for tab switching |
| TB-014 | Tab Overflow | Scroll when many tabs |
| TB-015 | Tab Preview | Hover preview of tab content |

**Navigation (20)**
| ID | Feature | Description |
|----|---------|-------------|
| NV-001 | Back/Forward | History navigation |
| NV-002 | Breadcrumbs | Clickable path breadcrumbs |
| NV-003 | Autocomplete | Path autocomplete |
| NV-004 | Favorites | Quick access to favorites |
| NV-005 | Recent | Recent locations list |
| NV-006 | Shortcuts | Navigation keyboard shortcuts |
| NV-007 | Go to Parent | Navigate to parent folder |
| NV-008 | Go to Root | Navigate to drive root |
| NV-009 | Type-ahead | Type to navigate |
| NV-010 | Path Editing | Direct path editing |
| NV-011 | Path History | Dropdown of recent paths |
| NV-012 | Quick Access | Windows quick access integration |
| NV-013 | Libraries | Windows libraries support |
| NV-014 | Network | Network location browsing |
| NV-015 | Special Folders | Desktop, Documents, etc. |
| NV-016 | Search Integration | Search from address bar |
| NV-017 | Environment Vars | %APPDATA% expansion |
| NV-018 | UNC Paths | \\\\server\\share support |
| NV-019 | Path Validation | Invalid path handling |
| NV-020 | Refresh | F5 to refresh current view |

### CATEGORY 2: VIEW MODES (35 Features)

**View Types (10)**
| ID | Feature | Description |
|----|---------|-------------|
| VM-001 | Details View | List with columns |
| VM-002 | Icons View | Icon grid |
| VM-003 | Tiles View | Tiles with details |
| VM-004 | Thumbnails | Image thumbnails |
| VM-005 | Small Icons | Small icon list |
| VM-006 | Medium Icons | Medium icon grid |
| VM-007 | Large Icons | Large icon grid |
| VM-008 | Extra Large | Extra large icons |
| VM-009 | Content View | Content preview |
| VM-010 | Gallery View | Photo gallery mode |

**Column Configuration (12)**
| ID | Feature | Description |
|----|---------|-------------|
| CL-001 | Column Select | Choose visible columns |
| CL-002 | Column Resize | Resize column width |
| CL-003 | Column Reorder | Drag to reorder columns |
| CL-004 | Auto-size | Double-click to auto-size |
| CL-005 | Column Sort | Click to sort |
| CL-006 | Custom Columns | Add custom columns |
| CL-007 | Column Presets | Save column configurations |
| CL-008 | Size Format | KB/MB/GB toggle |
| CL-009 | Date Format | Date display format |
| CL-010 | Attribute Icons | Show attribute icons |
| CL-011 | Extension Column | Separate extension column |
| CL-012 | Type Column | File type description |

**Sorting & Grouping (13)**
| ID | Feature | Description |
|----|---------|-------------|
| SG-001 | Sort by Name | Alphabetical sorting |
| SG-002 | Sort by Date | Date modified sorting |
| SG-003 | Sort by Size | Size sorting |
| SG-004 | Sort by Type | Type sorting |
| SG-005 | Natural Sort | Natural number sorting |
| SG-006 | Multi-column | Sort by multiple columns |
| SG-007 | Group by Type | Group files by type |
| SG-008 | Group by Date | Group by date modified |
| SG-009 | Group by Size | Group by size range |
| SG-010 | Group by Letter | Group by first letter |
| SG-011 | Collapsible Groups | Collapse/expand groups |
| SG-012 | Folders First | Always show folders first |
| SG-013 | Reverse Sort | Reverse sort order |

### CATEGORY 3: FILE OPERATIONS (52 Features)

**Basic Operations (15)**
| ID | Feature | Description |
|----|---------|-------------|
| FO-001 | Copy | Copy files/folders |
| FO-002 | Cut | Cut files/folders |
| FO-003 | Paste | Paste files/folders |
| FO-004 | Delete | Delete to recycle bin |
| FO-005 | Permanent Delete | Shift+Delete |
| FO-006 | Rename | Rename file/folder |
| FO-007 | Create Folder | New folder creation |
| FO-008 | Create File | New file creation |
| FO-009 | Move Dialog | Move with destination picker |
| FO-010 | Copy Dialog | Copy with destination picker |
| FO-011 | Properties | File properties dialog |
| FO-012 | Open | Open with default app |
| FO-013 | Open With | Open with app picker |
| FO-014 | Edit | Edit in default editor |
| FO-015 | Print | Print file |

**Batch Rename (10)**
| ID | Feature | Description |
|----|---------|-------------|
| BR-001 | Rename Wizard | Step-by-step batch rename |
| BR-002 | Sequential | Add sequential numbers |
| BR-003 | Find/Replace | Find and replace in names |
| BR-004 | Regex Rename | Regex pattern rename |
| BR-005 | Case Change | Change case (upper/lower) |
| BR-006 | Preview | Preview rename results |
| BR-007 | Undo Rename | Undo last rename |
| BR-008 | Templates | Rename templates |
| BR-009 | Date Insert | Insert date in name |
| BR-010 | Counter | Counter with padding |

**Advanced Operations (10)**
| ID | Feature | Description |
|----|---------|-------------|
| AO-001 | Split File | Split large files |
| AO-002 | Join Files | Join split files |
| AO-003 | Secure Delete | Multi-pass secure delete |
| AO-004 | Symbolic Link | Create symbolic links |
| AO-005 | Hard Link | Create hard links |
| AO-006 | Junction | Create junctions |
| AO-007 | Checksum MD5 | Calculate MD5 |
| AO-008 | Checksum SHA | Calculate SHA-256 |
| AO-009 | Verify Checksum | Verify against checksum |
| AO-010 | Touch | Update timestamps |

**Conflict Resolution (10)**
| ID | Feature | Description |
|----|---------|-------------|
| CR-001 | Skip | Skip conflicting file |
| CR-002 | Overwrite | Overwrite existing |
| CR-003 | Rename | Auto-rename conflict |
| CR-004 | Compare | Compare before deciding |
| CR-005 | Keep Newer | Keep newer file |
| CR-006 | Keep Larger | Keep larger file |
| CR-007 | Apply All | Apply to all conflicts |
| CR-008 | Remember | Remember choice |
| CR-009 | Skip All | Skip all conflicts |
| CR-010 | Overwrite All | Overwrite all |

**Progress & Queue (7)**
| ID | Feature | Description |
|----|---------|-------------|
| PQ-001 | Progress Dialog | Show operation progress |
| PQ-002 | Speed Display | Show transfer speed |
| PQ-003 | Time Remaining | Estimated time remaining |
| PQ-004 | Pause/Resume | Pause and resume operations |
| PQ-005 | Cancel | Cancel operation |
| PQ-006 | Queue | Operation queue |
| PQ-007 | Background | Run in background |

### CATEGORY 4: SELECTION (28 Features)

**Basic Selection (10)**
| ID | Feature | Description |
|----|---------|-------------|
| SL-001 | Click Select | Single click selection |
| SL-002 | Multi-select | Ctrl+click multi-select |
| SL-003 | Range Select | Shift+click range |
| SL-004 | Select All | Ctrl+A select all |
| SL-005 | Invert | Invert selection |
| SL-006 | Deselect All | Escape to deselect |
| SL-007 | Rubber Band | Drag to select |
| SL-008 | Checkbox Mode | Checkbox selection |
| SL-009 | Focus vs Select | Separate focus and select |
| SL-010 | Selection Count | Show selection count |

**Advanced Selection (18)**
| ID | Feature | Description |
|----|---------|-------------|
| AS-001 | Select by Pattern | Wildcard pattern |
| AS-002 | Select by Extension | By file extension |
| AS-003 | Select by Date | By date range |
| AS-004 | Select by Size | By size range |
| AS-005 | Select Files Only | Exclude folders |
| AS-006 | Select Folders Only | Exclude files |
| AS-007 | Select Modified | Recently modified |
| AS-008 | Save Selection | Save selection set |
| AS-009 | Load Selection | Load selection set |
| AS-010 | Compare Selection | Compare with saved |
| AS-011 | Add to Selection | Add to current |
| AS-012 | Remove from Selection | Remove from current |
| AS-013 | Select Same Type | Same extension |
| AS-014 | Select Same Size | Same size |
| AS-015 | Select Hidden | Include hidden files |
| AS-016 | Select Read-only | Select read-only |
| AS-017 | Select by Regex | Regex pattern |
| AS-018 | Quick Select | Type to select |

### CATEGORY 5: SEARCH (42 Features)

**Basic Search (12)**
| ID | Feature | Description |
|----|---------|-------------|
| SR-001 | Search Box | Quick search box |
| SR-002 | Instant Search | Search as you type |
| SR-003 | Recursive | Search subfolders |
| SR-004 | Wildcards | * and ? wildcards |
| SR-005 | Case Sensitive | Case sensitive option |
| SR-006 | Whole Word | Match whole word |
| SR-007 | File Name | Search file names |
| SR-008 | Content | Search file content |
| SR-009 | Clear Search | Clear search |
| SR-010 | Search History | Recent searches |
| SR-011 | Search Scope | Limit search scope |
| SR-012 | Index Status | Show index status |

**Advanced Search (15)**
| ID | Feature | Description |
|----|---------|-------------|
| XS-001 | Regex Search | Regular expressions |
| XS-002 | Size Filter | Filter by size |
| XS-003 | Date Filter | Filter by date |
| XS-004 | Type Filter | Filter by type |
| XS-005 | Attribute Filter | Filter by attributes |
| XS-006 | Boolean AND | AND operator |
| XS-007 | Boolean OR | OR operator |
| XS-008 | Boolean NOT | NOT operator |
| XS-009 | Search Archives | Search inside archives |
| XS-010 | Save Search | Save search query |
| XS-011 | Load Search | Load saved search |
| XS-012 | Search Templates | Pre-built searches |
| XS-013 | Fuzzy Match | Fuzzy matching |
| XS-014 | Similar Files | Find similar names |
| XS-015 | Exclude Pattern | Exclude from search |

**Search Results (15)**
| ID | Feature | Description |
|----|---------|-------------|
| RS-001 | Results List | Display results |
| RS-002 | Result Count | Show result count |
| RS-003 | Highlighting | Highlight matches |
| RS-004 | Sort Results | Sort search results |
| RS-005 | Group Results | Group by folder |
| RS-006 | Navigate | Navigate to result |
| RS-007 | Select All Results | Select all results |
| RS-008 | Copy Results | Copy result paths |
| RS-009 | Export Results | Export to file |
| RS-010 | Open All | Open all results |
| RS-011 | Delete Results | Delete from results |
| RS-012 | Refresh Search | Re-run search |
| RS-013 | Stop Search | Stop ongoing search |
| RS-014 | Result Actions | Right-click actions |
| RS-015 | Preview Result | Preview in panel |

### CATEGORY 6: QUICK FILTER (18 Features)

| ID | Feature | Description |
|----|---------|-------------|
| QF-001 | Filter Bar | Quick filter bar |
| QF-002 | Instant Filter | Filter as you type |
| QF-003 | Extension Filter | Filter by extension |
| QF-004 | Size Filter | Filter by size range |
| QF-005 | Date Filter | Filter by date range |
| QF-006 | Attribute Filter | Filter by attributes |
| QF-007 | Presets | Filter presets |
| QF-008 | Invert Filter | Show non-matching |
| QF-009 | Clear Filter | Clear all filters |
| QF-010 | Filter Count | Show filtered count |
| QF-011 | Combine Filters | Multiple filters |
| QF-012 | Save Preset | Save filter preset |
| QF-013 | Load Preset | Load filter preset |
| QF-014 | Recent Filters | Recent filter history |
| QF-015 | Hidden Toggle | Show/hide hidden |
| QF-016 | System Toggle | Show/hide system |
| QF-017 | Archive Filter | Show archives only |
| QF-018 | Media Filter | Show media files |

### CATEGORY 7: PREVIEW (32 Features)

**Preview Panel (12)**
| ID | Feature | Description |
|----|---------|-------------|
| PP-001 | Image Preview | Preview images |
| PP-002 | Text Preview | Preview text files |
| PP-003 | Code Preview | Syntax highlighted code |
| PP-004 | PDF Preview | Preview PDF files |
| PP-005 | Video Preview | Preview video files |
| PP-006 | Audio Preview | Preview audio files |
| PP-007 | Hex Preview | Hex view |
| PP-008 | Archive Preview | Archive contents |
| PP-009 | Markdown | Render markdown |
| PP-010 | HTML Preview | Render HTML |
| PP-011 | Panel Toggle | Toggle preview panel |
| PP-012 | Panel Size | Resize preview panel |

**Quick Look (10)**
| ID | Feature | Description |
|----|---------|-------------|
| QL-001 | Spacebar Preview | Space to preview |
| QL-002 | Floating Window | Floating preview |
| QL-003 | Full Screen | Full screen preview |
| QL-004 | Navigate Files | Arrow keys navigate |
| QL-005 | Zoom | Zoom in/out |
| QL-006 | Slideshow | Auto slideshow |
| QL-007 | Copy Content | Copy preview content |
| QL-008 | Open External | Open in app |
| QL-009 | Close | Escape to close |
| QL-010 | Pin Window | Pin preview window |

**Metadata Display (10)**
| ID | Feature | Description |
|----|---------|-------------|
| MD-001 | EXIF Display | Show EXIF data |
| MD-002 | ID3 Display | Show ID3 tags |
| MD-003 | Video Metadata | Video file info |
| MD-004 | Document Props | Document properties |
| MD-005 | Edit Metadata | Edit EXIF/ID3 |
| MD-006 | Remove Metadata | Strip metadata |
| MD-007 | Copy Metadata | Copy to other files |
| MD-008 | Export Metadata | Export to file |
| MD-009 | Metadata Panel | Dedicated panel |
| MD-010 | Custom Fields | Custom metadata |

### CATEGORY 8: ARCHIVES (25 Features)

| ID | Feature | Description |
|----|---------|-------------|
| AR-001 | Browse Archive | Browse inside archives |
| AR-002 | Create ZIP | Create ZIP archives |
| AR-003 | Create 7Z | Create 7Z archives |
| AR-004 | Extract All | Extract all files |
| AR-005 | Extract Selected | Extract selected |
| AR-006 | Extract Here | Extract to current folder |
| AR-007 | Password | Password protection |
| AR-008 | Split Archive | Split large archives |
| AR-009 | Add to Archive | Add files to archive |
| AR-010 | Update Archive | Update existing |
| AR-011 | Test Archive | Verify archive integrity |
| AR-012 | Convert Archive | Convert between formats |
| AR-013 | Self-Extract | Create self-extracting |
| AR-014 | Comment | Add archive comment |
| AR-015 | Compression Level | Set compression |
| AR-016 | Exclude Patterns | Exclude files |
| AR-017 | Multi-volume | Multi-volume archives |
| AR-018 | Archive Properties | Show archive info |
| AR-019 | Encrypt Names | Encrypt file names |
| AR-020 | Solid Archive | Solid compression |
| AR-021 | Recovery Record | Add recovery |
| AR-022 | Quick Extract | Context menu extract |
| AR-023 | Open in Archive | Navigate like folder |
| AR-024 | Drag from Archive | Drag to extract |
| AR-025 | Drag to Archive | Drag to add |

### CATEGORY 9: SYNC & COMPARE (22 Features)

**Folder Compare (12)**
| ID | Feature | Description |
|----|---------|-------------|
| FC-001 | Compare Folders | Compare two folders |
| FC-002 | Show Differences | Highlight differences |
| FC-003 | Show Same | Show identical files |
| FC-004 | Show Left Only | Show left-only files |
| FC-005 | Show Right Only | Show right-only files |
| FC-006 | Color Coding | Color by status |
| FC-007 | Size Compare | Compare by size |
| FC-008 | Date Compare | Compare by date |
| FC-009 | Content Compare | Compare contents |
| FC-010 | Refresh Compare | Refresh comparison |
| FC-011 | Select Differences | Select all different |
| FC-012 | Export Comparison | Export results |

**Folder Sync (10)**
| ID | Feature | Description |
|----|---------|-------------|
| FS-001 | Sync Wizard | Step-by-step sync |
| FS-002 | One-Way Sync | Source to destination |
| FS-003 | Two-Way Sync | Bidirectional sync |
| FS-004 | Mirror Sync | Exact mirror |
| FS-005 | Preview Sync | Preview changes |
| FS-006 | Sync Filters | Exclude patterns |
| FS-007 | Conflict Handling | Handle conflicts |
| FS-008 | Scheduled Sync | Schedule sync jobs |
| FS-009 | Sync Log | Sync history log |
| FS-010 | Sync Profiles | Save sync settings |

### CATEGORY 10: ORGANIZER (35 Features)

**Auto-Organization (15)**
| ID | Feature | Description |
|----|---------|-------------|
| OG-001 | Rule Engine | Create organization rules |
| OG-002 | By Extension | Organize by extension |
| OG-003 | By Date | Organize by date |
| OG-004 | By Size | Organize by size |
| OG-005 | By Type | Organize by type |
| OG-006 | By Name | Organize by name pattern |
| OG-007 | Templates | Organization templates |
| OG-008 | Preview | Preview organization |
| OG-009 | Undo | Undo organization |
| OG-010 | Custom Rules | Custom rule creation |
| OG-011 | Rule Priority | Rule order/priority |
| OG-012 | Rule Groups | Group related rules |
| OG-013 | Import Rules | Import rules |
| OG-014 | Export Rules | Export rules |
| OG-015 | Rule Testing | Test rules |

**Watch Folders (10)**
| ID | Feature | Description |
|----|---------|-------------|
| WF-001 | Watch Setup | Set up folder watch |
| WF-002 | Auto-Move | Auto-move new files |
| WF-003 | Auto-Rename | Auto-rename files |
| WF-004 | Auto-Organize | Auto-organize |
| WF-005 | Watch Triggers | Define triggers |
| WF-006 | Watch Logging | Log all actions |
| WF-007 | Watch Status | Show watch status |
| WF-008 | Pause Watch | Pause watching |
| WF-009 | Watch Filters | Filter what to watch |
| WF-010 | Watch Schedule | Schedule watching |

**Duplicate Finder (10)**
| ID | Feature | Description |
|----|---------|-------------|
| DF-001 | Find Wizard | Step-by-step finder |
| DF-002 | By Hash | Find by content hash |
| DF-003 | By Name | Find by name |
| DF-004 | By Size | Find by size only |
| DF-005 | Duplicate Groups | Group duplicates |
| DF-006 | Auto-Select | Auto-select for delete |
| DF-007 | Preview | Preview duplicates |
| DF-008 | Safe Delete | Safe deletion |
| DF-009 | Link Replace | Replace with links |
| DF-010 | Export List | Export duplicate list |

### CATEGORY 11: SPACE ANALYZER (20 Features)

| ID | Feature | Description |
|----|---------|-------------|
| SA-001 | Treemap View | Interactive treemap |
| SA-002 | Sunburst View | Sunburst chart |
| SA-003 | Size List | List by size |
| SA-004 | Largest Files | Show largest files |
| SA-005 | Largest Folders | Show largest folders |
| SA-006 | By Extension | Size by extension |
| SA-007 | By Type | Size by type |
| SA-008 | Drill Down | Click to drill down |
| SA-009 | Breadcrumbs | Navigate analysis |
| SA-010 | Cleanup Wizard | Cleanup suggestions |
| SA-011 | Temporary Files | Find temp files |
| SA-012 | Old Files | Find old files |
| SA-013 | Empty Folders | Find empty folders |
| SA-014 | Junk Files | Find junk files |
| SA-015 | System Files | Show system usage |
| SA-016 | Hidden Usage | Show hidden usage |
| SA-017 | Export Report | Export analysis |
| SA-018 | Compare Scans | Compare over time |
| SA-019 | Schedule Scan | Schedule analysis |
| SA-020 | Quick Scan | Quick estimation |

### CATEGORY 12: CONTEXT MENU (40+ Features)

Full Windows Shell integration with all operations from IContextMenuService

### CATEGORY 13: KEYBOARD SHORTCUTS (50+ Features)

All standard and custom shortcuts

### CATEGORY 14: DRAG & DROP (20 Features)

Cross-pane, external, archive, queue integration

### CATEGORY 15: SETTINGS & PREFERENCES (30 Features)

All configuration dialogs and persistence

---

## STATUS REPORTING

Update `.coordination/status/windsurf-status.md` after EVERY session with:
- What files you modified
- Build status (MUST be passing)
- Next tasks
- Any blockers

---

## COORDINATION

- Read `.coordination/notifications/to-windsurf.md` at session start
- Check `.coordination/interfaces/contracts.md` for interface definitions
- Backend services are COMPLETE - start using them NOW

---

## FINAL REMINDER

**NO STUBS. NO MOCKS. NO PLACEHOLDERS.**

Every feature must be FULLY FUNCTIONAL with REAL backend integration.

This is a RELEASE BUILD. Quality matters.
