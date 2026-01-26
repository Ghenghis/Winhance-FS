# UNIFIED MASTER ACTION PLAN
## Winhance-FS File Manager & Organizer - Complete Implementation Guide

**Version:** 3.0 Final
**Date:** January 24, 2026
**Status:** Consolidated from 5 source documents

---

## DOCUMENT CONSOLIDATION SUMMARY

This action plan unifies the following source documents:
1. `FILE_MANAGER_IMPLEMENTATION_STATUS.md` - Current working features
2. `COMPREHENSIVE_MISSING_FEATURES.md` - 343 missing features across all modules
3. `MASTER_FILE_MANAGER_FEATURES_AUDIT.md` - 503 features specific to File Manager
4. `Winhance-FS_Missing_Features_Roadmap.md` - Comparative analysis gaps
5. `EPIC_FEATURES.md` - 50 game-changer capabilities

**Total Unique Features Identified:** 550+
**Currently Implemented:** ~85 (15%)
**Remaining to Implement:** ~465 (85%)

---

# SECTION 1: CURRENT STATE ANALYSIS

## What Works Today (Implemented Features)

### File Browser (Dual-Pane) ✅
| Feature | Status | Quality |
|---------|--------|---------|
| Dual-pane layout | ✅ Working | Good |
| Single-pane toggle | ✅ Working | Good |
| Directory listing | ✅ Working | Good |
| Double-click navigation | ✅ Working | Good |
| Breadcrumb navigation | ✅ Working | Good |
| Address bar | ✅ Working | Basic |
| Back/Forward navigation | ✅ Working | Good |
| Copy/Cut/Paste | ✅ Working | Good |
| Delete (Recycle Bin) | ✅ Working | Good |
| Rename (F2) | ✅ Working | Good |
| New Folder | ✅ Working | Good |
| Drag-drop between panes | ✅ Working | Good |
| Context menus | ✅ Working | Basic |
| Column sorting | ✅ Working | Good |
| Hidden files toggle | ✅ Working | Good |

### Batch Rename ✅
| Feature | Status | Quality |
|---------|--------|---------|
| Live preview | ✅ Working | Good |
| Find & Replace | ✅ Working | Good |
| Add prefix/suffix | ✅ Working | Good |
| Remove text | ✅ Working | Good |
| Counter/Numbering | ✅ Working | Good |
| Change case | ✅ Working | Good |
| Change extension | ✅ Working | Good |
| DateTime insertion | ✅ Working | Good |
| Conflict detection | ✅ Working | Good |
| Undo support | ✅ Working | Good |

### Smart Organizer ✅
| Feature | Status | Quality |
|---------|--------|---------|
| Folder analysis | ✅ Working | Good |
| 16 file categories | ✅ Working | Good |
| Size/count calculation | ✅ Working | Good |
| Organize by type | ✅ Working | Good |
| Preview changes | ✅ Working | Good |
| Apply organization | ✅ Working | Good |
| Undo organization | ✅ Working | Good |
| Conflict handling | ✅ Working | Good |

### Space Recovery ✅
| Feature | Status | Quality |
|---------|--------|---------|
| Drive selection | ✅ Working | Good |
| Temp files scan | ✅ Working | Good |
| Browser cache scan | ✅ Working | Good |
| Windows cache scan | ✅ Working | Good |
| Developer cache | ✅ Working | Good |
| Large folder detection | ✅ Working | Good |

---

# SECTION 2: PRIORITY FEATURE MATRIX

## Priority Definitions

| Level | Definition | Timeline |
|-------|------------|----------|
| **P0** | Critical - App is incomplete without these | Weeks 1-4 |
| **P1** | High - Expected by power users | Weeks 5-8 |
| **P2** | Medium - Competitive advantage | Weeks 9-12 |
| **P3** | Low - Nice to have | Weeks 13-16 |

---

## PHASE 1: CRITICAL FOUNDATION (Weeks 1-4)

### Sprint 1.1: Search Engine Foundation (Week 1)

| ID | Feature | Component | Effort | Owner |
|----|---------|-----------|--------|-------|
| P0-001 | MFT Reader implementation | Rust/nexus_core | 5 days | Rust Dev |
| P0-002 | USN Journal monitoring | Rust/nexus_core | 3 days | Rust Dev |
| P0-003 | Basic filename search UI | WPF/FileManager | 2 days | WPF Dev |
| P0-004 | Search results display | WPF/FileManager | 2 days | WPF Dev |
| P0-005 | Wire Tantivy search end-to-end | Rust/Python | 3 days | Full Stack |

**Sprint Goal:** Users can search for files by name with sub-second results

### Sprint 1.2: Navigation Enhancement (Week 2)

| ID | Feature | Component | Effort | Owner |
|----|---------|-----------|--------|-------|
| P0-006 | Tab support - New tab (Ctrl+T) | WPF/FileManager | 2 days | WPF Dev |
| P0-007 | Tab support - Close tab (Ctrl+W) | WPF/FileManager | 1 day | WPF Dev |
| P0-008 | Tab support - Switch tabs | WPF/FileManager | 1 day | WPF Dev |
| P0-009 | Favorites/Bookmarks panel | WPF/FileManager | 2 days | WPF Dev |
| P0-010 | Recent locations dropdown | WPF/FileManager | 1 day | WPF Dev |
| P0-011 | Quick Access sidebar | WPF/FileManager | 2 days | WPF Dev |
| P0-012 | Keyboard shortcut framework | WPF/Common | 1 day | WPF Dev |

**Sprint Goal:** Tabbed browsing with favorites and recent locations

### Sprint 1.3: Space Visualization (Week 3)

| ID | Feature | Component | Effort | Owner |
|----|---------|-----------|--------|-------|
| P0-013 | TreeMap visualization control | WPF/Controls | 3 days | WPF Dev |
| P0-014 | TreeMap data binding | WPF/FileManager | 2 days | WPF Dev |
| P0-015 | Drill-down navigation | WPF/FileManager | 1 day | WPF Dev |
| P0-016 | Size breakdown charts | WPF/FileManager | 2 days | WPF Dev |
| P0-017 | Export analysis to CSV/JSON | Infrastructure | 1 day | C# Dev |
| P0-018 | LiveCharts2 integration | WPF/NuGet | 1 day | WPF Dev |

**Sprint Goal:** Visual TreeMap for space analysis like WinDirStat

### Sprint 1.4: Core Operations Polish (Week 4)

| ID | Feature | Component | Effort | Owner |
|----|---------|-----------|--------|-------|
| P0-019 | Copy progress dialog | WPF/Common | 2 days | WPF Dev |
| P0-020 | Progress with speed/ETA | WPF/Common | 1 day | WPF Dev |
| P0-021 | Pause/Resume/Cancel operations | Infrastructure | 2 days | C# Dev |
| P0-022 | Conflict resolution dialog | WPF/Common | 2 days | WPF Dev |
| P0-023 | Multi-level undo system | Infrastructure | 2 days | C# Dev |
| P0-024 | Operation queue manager | Infrastructure | 1 day | C# Dev |

**Sprint Goal:** Professional file operation experience with full control

---

## PHASE 2: POWER USER FEATURES (Weeks 5-8)

### Sprint 2.1: Preview System (Week 5)

| ID | Feature | Component | Effort | Owner |
|----|---------|-----------|--------|-------|
| P1-001 | Preview pane toggle | WPF/FileManager | 1 day | WPF Dev |
| P1-002 | Text file preview | WPF/Preview | 1 day | WPF Dev |
| P1-003 | Image preview with zoom | WPF/Preview | 2 days | WPF Dev |
| P1-004 | Code syntax highlighting | WPF/Preview | 2 days | WPF Dev |
| P1-005 | Quick Look (Spacebar) | WPF/Preview | 2 days | WPF Dev |
| P1-006 | AvalonEdit integration | WPF/NuGet | 1 day | WPF Dev |
| P1-007 | PDF preview (PdfiumViewer) | WPF/Preview | 1 day | WPF Dev |

**Sprint Goal:** File preview without opening external apps

### Sprint 2.2: Archive Support (Week 6)

| ID | Feature | Component | Effort | Owner |
|----|---------|-----------|--------|-------|
| P1-008 | Browse ZIP contents | Infrastructure | 2 days | C# Dev |
| P1-009 | Browse 7z contents | Infrastructure | 2 days | C# Dev |
| P1-010 | Extract archive | Infrastructure | 1 day | C# Dev |
| P1-011 | Extract selected files | Infrastructure | 1 day | C# Dev |
| P1-012 | Create ZIP archive | Infrastructure | 2 days | C# Dev |
| P1-013 | SharpCompress integration | Infrastructure/NuGet | 1 day | C# Dev |
| P1-014 | Archive context menu | WPF/FileManager | 1 day | WPF Dev |

**Sprint Goal:** Browse and manipulate archives like folders

### Sprint 2.3: Duplicate Detection (Week 7)

| ID | Feature | Component | Effort | Owner |
|----|---------|-----------|--------|-------|
| P1-015 | Hash-based duplicate scan | Infrastructure | 2 days | C# Dev |
| P1-016 | Quick hash (partial file) | Infrastructure | 1 day | C# Dev |
| P1-017 | Duplicate results UI | WPF/FileManager | 2 days | WPF Dev |
| P1-018 | Selection strategies (keep oldest) | Infrastructure | 1 day | C# Dev |
| P1-019 | Preview before delete | WPF/FileManager | 1 day | WPF Dev |
| P1-020 | Cross-drive scanning | Infrastructure | 1 day | C# Dev |
| P1-021 | Exclusion paths config | WPF/Settings | 1 day | WPF Dev |

**Sprint Goal:** Find and remove duplicate files safely

### Sprint 2.4: Batch Rename Enhancement (Week 8)

| ID | Feature | Component | Effort | Owner |
|----|---------|-----------|--------|-------|
| P1-022 | Regex rename support | Infrastructure | 2 days | C# Dev |
| P1-023 | EXIF date extraction | Infrastructure | 2 days | C# Dev |
| P1-024 | Rule reordering UI | WPF/FileManager | 1 day | WPF Dev |
| P1-025 | Save/Load presets | Infrastructure | 1 day | C# Dev |
| P1-026 | Preset manager UI | WPF/FileManager | 1 day | WPF Dev |
| P1-027 | Remove characters rule | Infrastructure | 1 day | C# Dev |
| P1-028 | Trim/Pad whitespace | Infrastructure | 1 day | C# Dev |

**Sprint Goal:** Professional batch rename with presets

---

## PHASE 3: ADVANCED FEATURES (Weeks 9-12)

### Sprint 3.1: Watch Folders & Automation (Week 9)

| ID | Feature | Component | Effort | Owner |
|----|---------|-----------|--------|-------|
| P2-001 | Watch folder service | Infrastructure | 3 days | C# Dev |
| P2-002 | File arrival triggers | Infrastructure | 2 days | C# Dev |
| P2-003 | Watch folder config UI | WPF/Settings | 2 days | WPF Dev |
| P2-004 | Rule-based auto-organize | Infrastructure | 2 days | C# Dev |
| P2-005 | Notification on organize | Infrastructure | 1 day | C# Dev |

**Sprint Goal:** Auto-organize files as they arrive

### Sprint 3.2: Organization Strategies (Week 10)

| ID | Feature | Component | Effort | Owner |
|----|---------|-----------|--------|-------|
| P2-006 | Organize by date (Year/Month) | Infrastructure | 2 days | C# Dev |
| P2-007 | Organize by size bins | Infrastructure | 1 day | C# Dev |
| P2-008 | Organize by project detection | Infrastructure | 2 days | C# Dev |
| P2-009 | Custom rules engine UI | WPF/FileManager | 3 days | WPF Dev |
| P2-010 | Rule condition builder | WPF/FileManager | 2 days | WPF Dev |

**Sprint Goal:** Multiple organization strategies

### Sprint 3.3: Advanced Search (Week 11)

| ID | Feature | Component | Effort | Owner |
|----|---------|-----------|--------|-------|
| P2-011 | Wildcard search (*, ?) | Infrastructure | 1 day | C# Dev |
| P2-012 | Size filters (>100MB) | Infrastructure | 1 day | C# Dev |
| P2-013 | Date filters | Infrastructure | 1 day | C# Dev |
| P2-014 | Boolean operators (AND, OR) | Infrastructure | 2 days | C# Dev |
| P2-015 | Saved searches | Infrastructure | 1 day | C# Dev |
| P2-016 | Search history dropdown | WPF/FileManager | 1 day | WPF Dev |
| P2-017 | Content search (inside files) | Rust/Python | 3 days | Full Stack |

**Sprint Goal:** Advanced search with filters and saved queries

### Sprint 3.4: Shell Integration (Week 12)

| ID | Feature | Component | Effort | Owner |
|----|---------|-----------|--------|-------|
| P2-018 | Context menu registration | WPF/Installer | 2 days | C# Dev |
| P2-019 | "Open with Winhance" | WPF/Installer | 1 day | C# Dev |
| P2-020 | "Analyze folder size" | WPF/Installer | 1 day | C# Dev |
| P2-021 | "Find duplicates" | WPF/Installer | 1 day | C# Dev |
| P2-022 | Copy path formats | Infrastructure | 1 day | C# Dev |
| P2-023 | Open terminal here | Infrastructure | 1 day | C# Dev |
| P2-024 | Taskbar progress | WPF/Common | 2 days | WPF Dev |

**Sprint Goal:** Windows Explorer integration

---

## PHASE 4: DIFFERENTIATION FEATURES (Weeks 13-16)

### Sprint 4.1: AI & Intelligence (Week 13)

| ID | Feature | Component | Effort | Owner |
|----|---------|-----------|--------|-------|
| P2-025 | AI file classification | Python/nexus_ai | 3 days | Python Dev |
| P2-026 | Semantic search | Python/nexus_ai | 3 days | Python Dev |
| P2-027 | Smart naming suggestions | Python/nexus_ai | 2 days | Python Dev |
| P2-028 | Screenshot OCR | Python/nexus_ai | 2 days | Python Dev |

**Sprint Goal:** AI-powered file understanding

### Sprint 4.2: Professional Viewers (Week 14)

| ID | Feature | Component | Effort | Owner |
|----|---------|-----------|--------|-------|
| P2-029 | EXIF metadata inspector | WPF/Preview | 2 days | WPF Dev |
| P2-030 | Hex view for binaries | WPF/Preview | 2 days | WPF Dev |
| P2-031 | JSON/XML tree view | WPF/Preview | 2 days | WPF Dev |
| P2-032 | Markdown rendered preview | WPF/Preview | 1 day | WPF Dev |
| P2-033 | Audio waveform display | WPF/Preview | 2 days | WPF Dev |
| P2-034 | Video thumbnail scrubbing | WPF/Preview | 1 day | WPF Dev |

**Sprint Goal:** Professional content inspection

### Sprint 4.3: Virtual Organization (Week 15)

| ID | Feature | Component | Effort | Owner |
|----|---------|-----------|--------|-------|
| P2-035 | Manual file tagging | Infrastructure | 2 days | C# Dev |
| P2-036 | Tag database (SQLite) | Infrastructure | 1 day | C# Dev |
| P2-037 | Tag filtering UI | WPF/FileManager | 2 days | WPF Dev |
| P2-038 | Virtual folders (saved queries) | Infrastructure | 2 days | C# Dev |
| P2-039 | Collections UI | WPF/FileManager | 2 days | WPF Dev |
| P2-040 | File notes attachment | Infrastructure | 1 day | C# Dev |

**Sprint Goal:** Organize beyond physical folders

### Sprint 4.4: Symlink & Storage Tools (Week 16)

| ID | Feature | Component | Effort | Owner |
|----|---------|-----------|--------|-------|
| P2-041 | Symlink creation UI | WPF/FileManager | 2 days | WPF Dev |
| P2-042 | Hardlink creation | Infrastructure | 1 day | C# Dev |
| P2-043 | Junction creation | Infrastructure | 1 day | C# Dev |
| P2-044 | Link target visualization | WPF/FileManager | 1 day | WPF Dev |
| P2-045 | Broken link detection | Infrastructure | 1 day | C# Dev |
| P2-046 | Storage tiering wizard | WPF/FileManager | 3 days | WPF Dev |
| P2-047 | Model relocator service | Infrastructure | 1 day | C# Dev |

**Sprint Goal:** Complete symlink and storage management

---

# SECTION 3: EPIC GAME-CHANGER FEATURES

These are the 50 features from EPIC_FEATURES.md organized by implementation phase:

## Category 1: Intelligent File Classification (10 Features)

| # | Feature | Phase | Priority |
|---|---------|-------|----------|
| 1 | Windows System File Detector | Phase 4 | P2 |
| 2 | Installation Footprint Mapper | Phase 4 | P3 |
| 3 | User-Created File Identifier | Phase 4 | P2 |
| 4 | Orphan File Detective | Phase 3 | P2 |
| 5 | File Origin Tracker | Phase 4 | P3 |
| 6 | Dependency Chain Analyzer | Phase 4 | P3 |
| 7 | Active Usage Monitor | Phase 3 | P2 |
| 8 | Sensitivity Classifier | Phase 4 | P3 |
| 9 | Project Boundary Detector | Phase 3 | P1 |
| 10 | Duplicate Intelligence | Phase 2 | P1 |

## Category 2: Real-Time Agent Organizers (10 Features)

| # | Feature | Phase | Priority |
|---|---------|-------|----------|
| 11 | Downloads Auto-Sorter Agent | Phase 3 | P1 |
| 12 | Desktop Cleaner Agent | Phase 3 | P2 |
| 13 | Screenshot Organizer Agent | Phase 3 | P2 |
| 14 | Media Library Agent | Phase 4 | P3 |
| 15 | Development Workspace Agent | Phase 3 | P2 |
| 16 | Document Flow Agent | Phase 4 | P3 |
| 17 | Email Attachment Agent | Phase 4 | P3 |
| 18 | Temp File Guardian Agent | Phase 3 | P1 |
| 19 | Cloud Sync Optimizer Agent | Phase 4 | P3 |
| 20 | Archive Management Agent | Phase 3 | P2 |

## Category 3: Touch & Mouse Seamless UI (10 Features)

| # | Feature | Phase | Priority |
|---|---------|-------|----------|
| 21 | Gesture-Based File Operations | Phase 4 | P3 |
| 22 | Drag Zones | Phase 2 | P2 |
| 23 | Quick Action Wheel | Phase 3 | P2 |
| 24 | Smart Thumbnails | Phase 2 | P1 |
| 25 | Batch Selection Tools | Phase 2 | P1 |
| 26 | Split View Manager | Phase 1 | P0 |
| 27 | Breadcrumb Touch Navigation | Phase 1 | P0 |
| 28 | File Cards View | Phase 3 | P2 |
| 29 | Voice Commands | Phase 4 | P3 |
| 30 | Accessibility Mode | Phase 3 | P1 |

## Category 4: Safety & Intelligence (10 Features)

| # | Feature | Phase | Priority |
|---|---------|-------|----------|
| 31 | Operation Impact Preview | Phase 1 | P0 |
| 32 | Smart Undo System | Phase 1 | P0 |
| 33 | Safety Flag System | Phase 2 | P2 |
| 34 | Risk Assessment Engine | Phase 3 | P2 |
| 35 | Intelligent Confirmation | Phase 2 | P1 |
| 36 | Automatic Restore Points | Phase 2 | P1 |
| 37 | File Integrity Monitor | Phase 3 | P2 |
| 38 | Permission Analyzer | Phase 3 | P2 |
| 39 | Network File Safety | Phase 4 | P3 |
| 40 | Encryption Advisor | Phase 4 | P3 |

## Category 5: Power User & Automation (10 Features)

| # | Feature | Phase | Priority |
|---|---------|-------|----------|
| 41 | Rule-Based Auto-Organization | Phase 3 | P1 |
| 42 | Scheduled Operations | Phase 3 | P2 |
| 43 | Workflow Automation | Phase 4 | P3 |
| 44 | Command Palette | Phase 2 | P1 |
| 45 | Batch Rename Pro | Phase 2 | P1 |
| 46 | Space Recovery Wizard | Phase 1 | P0 |
| 47 | File Comparison Tool | Phase 3 | P2 |
| 48 | Search Everything+ | Phase 1 | P0 |
| 49 | Tag & Label System | Phase 4 | P2 |
| 50 | Integration Hub | Phase 4 | P3 |

---

# SECTION 4: TECHNICAL REQUIREMENTS

## Required NuGet Packages

| Package | Version | Purpose | Phase |
|---------|---------|---------|-------|
| LiveCharts2.WPF | 2.0+ | TreeMap visualization | Phase 1 |
| AvalonEdit | 6.3+ | Code preview/syntax | Phase 2 |
| PdfiumViewer | 3.0+ | PDF preview | Phase 2 |
| SharpCompress | 0.36+ | Archive support | Phase 2 |
| NAudio | 2.2+ | Audio waveform | Phase 4 |
| ImageSharp | 3.1+ | Image processing | Phase 2 |
| SQLite-net | 1.8+ | Tag database | Phase 4 |

## Required Rust Crates

| Crate | Version | Purpose | Phase |
|-------|---------|---------|-------|
| ntfs | 0.4+ | MFT reading | Phase 1 |
| windows-rs | 0.52+ | Win32 APIs | Phase 1 |
| xxhash-rust | 0.8+ | Fast hashing | Phase 1 |
| memchr | 2.7+ | SIMD search | Phase 2 |
| tantivy | 0.22+ | Full-text search | Phase 1 |
| walkdir | 2.5+ | Directory traversal | Phase 1 |

## Required Python Packages

| Package | Version | Purpose | Phase |
|---------|---------|---------|-------|
| watchdog | 4.0+ | File system events | Phase 3 |
| sentence-transformers | 2.2+ | Semantic search | Phase 4 |
| pillow | 10.0+ | Image processing | Phase 3 |
| mutagen | 1.47+ | Audio metadata | Phase 4 |
| pypdf | 4.0+ | PDF parsing | Phase 4 |

---

# SECTION 5: TEAM ALLOCATION

## Recommended Team Structure

| Role | Count | Responsibilities |
|------|-------|------------------|
| **Rust Developer** | 1-2 | MFT reader, search engine, indexing, hashing |
| **C#/WPF Developer** | 2 | UI components, Infrastructure services |
| **Python Developer** | 1 | AI features, automation, agents |
| **UI/UX Designer** | 1 | Design system, visualizations, icons |
| **QA Engineer** | 1 | Testing, quality assurance |

## Sprint Capacity (Per Phase)

| Phase | Duration | Story Points | Features |
|-------|----------|--------------|----------|
| Phase 1 | 4 weeks | 80 pts | 24 features |
| Phase 2 | 4 weeks | 80 pts | 28 features |
| Phase 3 | 4 weeks | 70 pts | 24 features |
| Phase 4 | 4 weeks | 60 pts | 24 features |

---

# SECTION 6: MILESTONES & DELIVERABLES

## Milestone 1: Alpha Release (End of Phase 1)
**Target Date:** Week 4

**Deliverables:**
- [ ] MFT-based instant file search working
- [ ] Tabbed browsing with favorites
- [ ] TreeMap space visualization
- [ ] Professional file operation dialogs
- [ ] Multi-level undo system

**Success Criteria:**
- Search returns results in <100ms for 1M files
- Tab operations work without data loss
- TreeMap displays 1TB+ drives smoothly

## Milestone 2: Beta Release (End of Phase 2)
**Target Date:** Week 8

**Deliverables:**
- [ ] Preview pane with syntax highlighting
- [ ] Archive browsing (ZIP, 7z)
- [ ] Duplicate detection working
- [ ] Enhanced batch rename with presets
- [ ] All P1 features complete

**Success Criteria:**
- Preview renders within 500ms
- Archives browse without extraction lag
- Duplicates found within 5min for 100GB

## Milestone 3: Release Candidate (End of Phase 3)
**Target Date:** Week 12

**Deliverables:**
- [ ] Watch folder automation
- [ ] Advanced search with filters
- [ ] Windows shell integration
- [ ] Multiple organization strategies
- [ ] All P2 features complete

**Success Criteria:**
- Watch folders process files within 2 seconds
- Shell context menus work reliably
- Search filters reduce results correctly

## Milestone 4: Version 1.0 (End of Phase 4)
**Target Date:** Week 16

**Deliverables:**
- [ ] AI classification working
- [ ] Professional viewers complete
- [ ] Virtual folders and tagging
- [ ] Symlink management tools
- [ ] All planned features complete

**Success Criteria:**
- AI classifies with >85% accuracy
- All 100 P0-P2 features working
- Performance targets met

---

# SECTION 7: RISK ASSESSMENT

## High Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| MFT Reader complexity | Blocks all search features | Allocate extra time, have fallback to standard enumeration |
| TreeMap performance | Poor UX for large drives | Use virtualization, limit depth |
| Archive library compatibility | Can't browse all formats | Test multiple libraries early |

## Medium Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| AI model size | Slow startup | Lazy loading, optional download |
| Watch folder reliability | Missed files | Use USN Journal as backup |
| Shell integration conflicts | Other apps affected | Registry sandboxing, rollback capability |

## Low Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Preview pane memory | High RAM usage | Limit cache size |
| Preset compatibility | Old presets don't work | Version presets, migration tool |

---

# SECTION 8: QUICK REFERENCE CHECKLISTS

## Phase 1 Checklist (Weeks 1-4)

### Week 1: Search Foundation
- [ ] Implement `mft_reader.rs` with ntfs crate
- [ ] Implement `usn_journal.rs` for real-time
- [ ] Create `SearchView.xaml` basic UI
- [ ] Wire search to Tantivy
- [ ] Add search result display

### Week 2: Navigation
- [ ] Implement `TabControl` for multiple locations
- [ ] Add `FavoritesPanel.xaml`
- [ ] Add `RecentLocationsDropdown`
- [ ] Implement `QuickAccessSidebar`
- [ ] Add keyboard shortcut infrastructure

### Week 3: Visualization
- [ ] Install LiveCharts2.WPF package
- [ ] Create `TreeMapControl.xaml`
- [ ] Bind space analysis data
- [ ] Add drill-down navigation
- [ ] Add size breakdown pie/bar charts

### Week 4: Operations
- [ ] Create `OperationProgressDialog.xaml`
- [ ] Implement speed/ETA calculation
- [ ] Add pause/resume/cancel buttons
- [ ] Create `ConflictResolutionDialog.xaml`
- [ ] Implement multi-level undo stack

## Phase 2 Checklist (Weeks 5-8)

### Week 5: Preview
- [ ] Add `PreviewPane.xaml` toggle
- [ ] Implement text file preview
- [ ] Add image preview with zoom
- [ ] Install AvalonEdit, add syntax highlighting
- [ ] Implement Quick Look popup

### Week 6: Archives
- [ ] Install SharpCompress package
- [ ] Implement ZIP virtual folder browsing
- [ ] Implement 7z virtual folder browsing
- [ ] Add extract operations
- [ ] Add create archive feature

### Week 7: Duplicates
- [ ] Implement hash-based scanning
- [ ] Add quick hash (partial file)
- [ ] Create `DuplicatesView.xaml`
- [ ] Add selection strategies
- [ ] Add preview before delete

### Week 8: Batch Rename
- [ ] Add regex rename rule
- [ ] Add EXIF date extraction
- [ ] Add rule reordering drag-drop
- [ ] Implement preset save/load
- [ ] Add remove characters rule

---

# SECTION 9: FILE LOCATIONS

## New Files to Create

### Phase 1
```
src/Winhance.WPF/Features/FileManager/Views/
├── TabContainer.xaml
├── FavoritesPanel.xaml
├── QuickAccessSidebar.xaml
├── SearchResultsView.xaml
├── TreeMapView.xaml
└── OperationProgressDialog.xaml

src/nexus_core/src/indexer/
├── mft_reader.rs (implement)
├── usn_journal.rs (implement)
└── content_hasher.rs (implement)
```

### Phase 2
```
src/Winhance.WPF/Features/FileManager/Views/
├── PreviewPane.xaml
├── QuickLookPopup.xaml
├── ArchiveBrowserView.xaml
├── DuplicateFinderView.xaml
└── PresetManagerDialog.xaml

src/Winhance.Infrastructure/Features/FileManager/Services/
├── ArchiveService.cs
├── DuplicateScannerService.cs
└── PresetService.cs
```

### Phase 3
```
src/Winhance.WPF/Features/FileManager/Views/
├── WatchFolderConfigView.xaml
├── RuleBuilderView.xaml
├── AdvancedSearchView.xaml
└── ShellIntegrationSettings.xaml

src/Winhance.Infrastructure/Features/FileManager/Services/
├── WatchFolderService.cs
├── ShellIntegrationService.cs
└── AdvancedSearchService.cs
```

---

# SECTION 10: SUCCESS METRICS

## Performance Targets

| Metric | Target | Current |
|--------|--------|---------|
| Index 1M files | < 1 second | Not measured |
| Search latency | < 100ms | Not measured |
| TreeMap render (1TB) | < 2 seconds | Not measured |
| Duplicate scan (100GB) | < 5 minutes | Not measured |
| Archive browse | < 500ms | Not measured |
| Preview render | < 500ms | Not measured |

## Quality Targets

| Metric | Target | Current |
|--------|--------|---------|
| Unit test coverage | > 70% | ~10% |
| Integration tests | > 50 tests | ~5 tests |
| Bug escape rate | < 5% | Unknown |
| User satisfaction | > 4.0/5.0 | Not measured |

## Feature Completion

| Phase | Features | Target Completion |
|-------|----------|-------------------|
| Phase 1 | 24 | 100% |
| Phase 2 | 28 | 100% |
| Phase 3 | 24 | 100% |
| Phase 4 | 24 | 100% |
| **Total** | **100** | **100%** |

---

# APPENDIX A: FEATURE ID REFERENCE

## P0 Features (24)
P0-001 through P0-024

## P1 Features (28)
P1-001 through P1-028

## P2 Features (48)
P2-001 through P2-047

---

# APPENDIX B: GLOSSARY

| Term | Definition |
|------|------------|
| MFT | Master File Table - NTFS internal structure |
| USN | Update Sequence Number Journal - Windows change tracking |
| TreeMap | Hierarchical visualization of nested rectangles |
| Quick Look | macOS-style instant preview with spacebar |
| Symlink | Symbolic link pointing to another file/folder |
| Junction | Windows-specific directory link |

---

*Unified Master Action Plan - Version 3.0*
*Consolidating 5 source documents into single implementation guide*
*Total Features: 100 prioritized items across 4 phases*
*Estimated Duration: 16 weeks*
