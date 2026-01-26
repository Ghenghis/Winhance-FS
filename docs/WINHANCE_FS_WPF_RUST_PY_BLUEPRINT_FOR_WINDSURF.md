# Winhance-FS File Manager Expansion Blueprint (WPF + C# UI, Rust engine, Python tools)

**Goal:** Combine the strongest ideas (and proven UX patterns) from the referenced projects into a **Windows-native, WPF-first** file manager that surpasses Explorer in power-user workflows, previewing, automation, indexing, and AI assistance—while staying modular and professional.

**Target workstation:** Windows 11, RTX 3090 Ti, 128 GB RAM.  
**AI runtime options:** Local-first (LM Studio, Ollama, vLLM) with optional cloud API providers.

---

## 1) Repos reviewed (what to reuse vs emulate)

### A) UltimateCommander (WPF “Total Commander”-style)
- Repo: https://github.com/wegorich/UltimateCommander-WPF-file-Manager
- Use it for: **UX interaction reference** (twin-pane commander flow)
- Treat as: **reference only**, not a dependency (older code style; not modern MVVM-first)

### B) Double Commander (feature benchmark)
- Repo: https://github.com/doublecmd/doublecmd
- Use it for: **feature completeness benchmark** (commander workflows, operations model)
- Treat as: **feature spec**, not source reuse (different tech stack)

### C) FilterTreeView (WPF/MVVM filter tree)
- Repo: https://github.com/Dirkster99/FilterTreeView
- Use it for: **direct WPF/MVVM patterns** (searchable/filterable TreeView + highlighting)
- Treat as: reusable component patterns (and potentially code reuse)

### D) Spacedrive (Rust-first indexing + library concepts)
- Repo: https://github.com/spacedriveapp/spacedrive
- Use it for: **architecture ideas** (library/index model, metadata-first browsing, Rust engine patterns)
- Treat as: design inspiration (not merged code)

---

## 2) Compatibility check: will they work together?

They “work together” at the **design level**, not as a literal merge:

- UltimateCommander → UX reference only
- Double Commander → feature benchmark
- FilterTreeView → WPF component patterns
- Spacedrive → indexing/library architecture inspiration

✅ The combined implementation stack should be:
**WPF/C# UI shell** + **Rust core engine** + **Python tool workers**.

---

## 3) Baseline modules (project-side assumptions)

This blueprint assumes you have / will build:

- **Rust Deep Scan Engine** (fast indexing; optional MFT/USN for NTFS)
- **C# service layer** (orchestration, job management, IPC/FFI)
- **Python worker layer** (AI/ML and batch automation, optional)
- **WPF UI** (MVVM, virtualization, command routing, theming)

---

## 4) Gaps and the repos/libs to fill them

### 4.1 Media preview + playback inside WPF
**Need:** thumbnails, preview pane, video/audio playback, metadata extraction.

- LibVLCSharp: https://github.com/videolan/libvlcsharp
- MediaInfo .NET wrapper: https://github.com/cschlote/MediaInfoDotNet

### 4.2 Archives as folders (zip/7z/rar/tar)
- SharpCompress: https://github.com/adamhathcock/sharpcompress

### 4.3 Instant filename search (Everything integration)
- Everything SDK docs: https://www.voidtools.com/support/everything/sdk/
- Everything IPC docs: https://www.voidtools.com/support/everything/sdk/ipc/
- .NET client: https://github.com/sgrottel/EverythingSearchClient

### 4.4 Shell integration (context menus, overlays)
- SharpShell: https://github.com/dwmkerr/sharpshell

### 4.5 Win32 API coverage (clean P/Invoke)
- Vanara: https://github.com/dahall/Vanara

---

## 5) “Explorer-surpassing” feature set (complete target)

### 5.1 Commander-grade navigation
- Twin-pane (optional quad-pane)
- Tabs per pane
- Breadcrumb + quick path bar
- Drive dropdown + favorites
- Pane sync (follow mode) + pane lock
- Per-pane history + pinned locations

### 5.2 High-performance operations
- Background copy/move queue (pause/resume/cancel)
- Verified copy (hash validation)
- Transaction log + rollback for destructive ops
- Bulk operations with safety prompts
- Undo where possible (rename/move/delete workflows)

### 5.3 Power search (fast + rich)
- “Everything mode” instant filename search (global)
- Rust index mode for: filters (size/date/type), fuzzy, regex, optional content search
- Saved searches + smart folders (virtual)
- Search history, pinned searches, recent filters

### 5.4 Organization beyond folders
- Tags, ratings, color labels, notes/comments
- Collections (manual groupings across folders)
- Rule-based organizer (watch folders + rules)
- Duplicate detection:
  - hash-based (exact)
  - perceptual hash (images)
  - optional embedding similarity (media)

### 5.5 Preview & inspection (major differentiator)
- Preview pane:
  - images (EXIF, dimensions, DPI)
  - video (timeline scrub, codec info, frame capture)
  - audio (duration/bitrate, metadata)
  - text/code (syntax highlight, safe large-file viewer, hex view)
  - pdf/doc (preview and metadata)
- Side-by-side compare (images/video)
- Contact sheets / thumbnail grids

### 5.6 Archives & packages
- Browse archives as folders
- Preview inside archives
- Batch extract + verify
- Nested archives support

### 5.7 Windows integration
- Right-click shell actions:
  - “Open in Winhance-FS”
  - “Analyze storage”
  - “Batch rename”
  - “Find duplicates”
- “Open terminal here” (PowerShell/WSL)
- Admin elevation flow for privileged operations
- Jump lists / recent locations (optional)

### 5.8 Command palette + automation
- Command palette (Ctrl+Shift+P)
- User-defined commands/macros (scriptable)
- Pluggable tools:
  - Python workers
  - Rust plugins (advanced)
  - external CLI tools

---

## 6) Architecture (WPF + C# + Rust + Python)

### 6.1 Module boundaries

**WPF (C#) — UI shell**
- Views, theming, accessibility
- MVVM viewmodels + commands
- Preview pane host controls
- In-app context menus + command palette

**C# — orchestration/services**
- Job system (indexing, hashing, preview extraction)
- IPC/FFI boundaries
- Permission elevation and safety policies
- Plugin registry (viewers, actions)

**Rust — performance-critical engine**
- Fast scan/index/search
- Hashing & duplicate grouping acceleration
- Optional: thumbnail extraction workers
- Optional: NTFS-specific optimizations (MFT/USN)

**Python — AI & batch tooling**
- LLM-driven planning (organization suggestions, NL query → filters)
- Media embedding pipelines (optional)
- Model management scripts
- Batch automation rules

### 6.2 Interop strategy
- C# ↔ Rust: FFI (C ABI / UniFFI) for high-throughput APIs
- C# ↔ Python: worker process via JSON-RPC/gRPC (never block UI)
- Everything: IPC client wrapper for instant filename search

---

## 7) AI integration (local + optional cloud)

### 7.1 Providers (one abstraction)
- LM Studio (OpenAI-compatible local server)
- Ollama (local inference + embeddings)
- vLLM (self-hosted throughput)
- Optional cloud APIs (opt-in, per-feature)

### 7.2 Useful AI features (non-gimmick)
- Natural language search → structured filters
- Storage summaries: “what’s taking space and why”
- Cleanup/relocation plans (safe, reversible)
- Auto-tagging & project classification
- “Organize Downloads” and “Archive old projects” assistants

---

## 8) Link list (complete)

**Provided targets**
- UltimateCommander: https://github.com/wegorich/UltimateCommander-WPF-file-Manager
- Double Commander: https://github.com/doublecmd/doublecmd
- FilterTreeView: https://github.com/Dirkster99/FilterTreeView
- Spacedrive: https://github.com/spacedriveapp/spacedrive

**Gap-fill repos**
- LibVLCSharp: https://github.com/videolan/libvlcsharp
- MediaInfoDotNet: https://github.com/cschlote/MediaInfoDotNet
- SharpCompress: https://github.com/adamhathcock/sharpcompress
- EverythingSearchClient: https://github.com/sgrottel/EverythingSearchClient
- Everything SDK: https://www.voidtools.com/support/everything/sdk/
- Everything IPC: https://www.voidtools.com/support/everything/sdk/ipc/
- SharpShell: https://github.com/dwmkerr/sharpshell
- Vanara: https://github.com/dahall/Vanara

---

## 9) Windsurf IDE “build plan” prompts (copy/paste)

### 9.1 Repo-wide build instruction (Architect prompt)
**Use this in Windsurf Architect mode:**
- Create a WPF (.NET 8/9) solution with strict MVVM
- Add a Rust crate for indexing/search with a stable FFI boundary
- Add a Python worker service folder for optional AI tools
- Implement a dual-pane file manager UI with tabs, virtualization, and commands
- Implement preview pane with LibVLCSharp + metadata via MediaInfo
- Implement archive browsing via SharpCompress
- Implement Everything IPC search bridge
- Keep every feature behind a FeatureFlag + capability detection
- Add CI, formatting, analyzers, and tests for core services

### 9.2 Folder skeleton (requested structure)
- src/App.Wpf (WPF UI)
- src/Core (domain models, interfaces)
- src/Infrastructure (OS integration, Everything bridge, shell hooks)
- src/Engine.Rust (index/search/hash)
- src/Tools.Python (workers, AI, batch tools)
- tests/* (unit + integration)

---

## 10) Implementation order (keeps it professional)
1) WPF shell + dual-pane + tabs + virtualization
2) Everything bridge for instant filename search
3) Rust index/search + filters + paging
4) Preview pane (LibVLCSharp + MediaInfo)
5) Archive-as-folder (SharpCompress)
6) FilterTreeView-based nav tree (saved searches, tags)
7) Shell integration (SharpShell) + safety policies
8) AI layer (LM Studio/Ollama/vLLM + optional cloud opt-in)

---

Winhance-FS Missing Features Roadmap

Source: Comparative analysis of Winhance-FS.txt vs FEATURES.md
Goal: Parity with Spacedrive, Double Commander, and FilterTreeView.

1. Advanced Navigation & Layout

Current Status: Dual-pane and Tabs are implemented. The following are missing:

[ ] Split Orientation Toggle: Ability to switch split-screen from Vertical (side-by-side) to Horizontal (top-bottom).

[ ] Workspace Snapshots: Capability to Save/Load named window layouts (e.g., "Debug Layout" with specific tabs open in specific paths).

[ ] Pane Sync Mode: A "Follow" toggle where navigating a folder in the left pane automatically navigates the right pane to the same path (critical for comparison workflows).

[ ] Locked Panes: Ability to "Lock" a tab or pane to a specific directory to prevent accidental navigation.

[ ] Independent Pane History: Separate Back/Forward history stacks for Left vs. Right panes (Explorer shares history, which is annoying).

2. Professional Content Viewers

Current Status: Basic text/image preview implemented. Missing "Pro" inspectors:

Text & Code

[ ] Syntax Highlighting: Colorized view for code files (.rs, .cs, .py, .js, .json, .xml).

[ ] Hex View: Binary inspection mode for executables or unknown file types.

[ ] Encoding Detection: Auto-detect and toggle between UTF-8, ASCII, UTF-16, and ANSI.

Images

[ ] EXIF & Metadata Inspector: View camera data (ISO, Shutter, GPS) for images.

[ ] Histogram: Visual color distribution graph.

[ ] Perceptual Hash View: Visualize the "fingerprint" of an image used for duplicate detection.

[ ] Side-by-Side Compare: Select two images and view them split-screen with synchronized zoom.

Media

[ ] Audio Waveform: Visual waveform generated for audio files.

[ ] Video Scrubbing: Thumbnail-based timeline scrubbing for video files without opening a player.

3. Advanced Search & Filtering

Current Status: Deep Scan and Regex are implemented. Missing UI-centric filtering:

[ ] Filterable TreeView: A folder tree that dynamically filters/hides nodes as you type (Key feature of FilterTreeView).

[ ] Multi-Column Filters: Ability to filter by Size AND Date simultaneously (e.g., "> 100MB" AND "Modified Today").

[ ] Saved Search Profiles: Save complex regex/filter combinations as named presets.

[ ] Search History: Dropdown of recently used search queries.

4. Context Menus & Automation

Current Status: Basic context menu implemented. Missing scripting/customization:

[ ] Scriptable Context Entries: Allow users to add menu items that execute custom Python, Rust, or PowerShell scripts on the selected files.

[ ] Internal Command Routing: Menu items that execute Winhance internal logic (e.g., "Copy Path as JSON", "Calculate SHA256") without invoking the slow Windows Shell.

[ ] Safety-Aware Filtering: Option to visually dim or hide destructive operations (Delete, Move) to prevent accidents.

[ ] Command Palette (Ctrl+Shift+P): Keyboard-driven menu to search for any action in the app (Spacedrive style).

5. Archive Management

Current Status: Archives detected by organizer. Missing "First-Class" browsing:

[ ] Transparent Browsing: Navigate into .zip, .7z, .rar, and .tar files as if they were regular folders.

[ ] Direct Manipulation: Copy/Paste files into and out of archives without a manual extraction wizard.

[ ] Archive Diff: Compare the contents of an archive against a folder without extracting.

6. Metadata & Virtual Organization

Current Status: Smart Organizer (Rule-based) is implemented. Missing Manual/Virtual organization:

[ ] Manual Tagging: User-defined color tags or text labels stored in a database (independent of NTFS attributes).

[ ] Virtual Folders: Folders defined by queries (e.g., "All High Priority PDFs") rather than physical location.

[ ] Collections: Manual grouping of files from different drives into a single "Collection" view.

[ ] File Notes: Ability to attach text notes to specific files (stored in Winhance database).

7. Storage Control & Links

Current Status: Model Relocation implemented. Missing generic tools:

[ ] Symlink Visualizer: Visual indicator in the file list showing where a symlink points (and highlighting broken links).

[ ] GUI Link Creator: Interface to create Hard Links, Junctions, and Symlinks easily.

[ ] Storage Tiering Wizard: Generic tool to move any folder from SSD to HDD while leaving a symlink (not just AI models).

8. File Operations (Power User)

Current Status: Transaction system implemented. Missing queue management:

[ ] Background Operation Queue: A manager for file operations allowing Pause, Resume, and Reordering of copy jobs.

[ ] Verify After Copy: Checkbox to automatically hash-verify files after copying to ensure data integrity.

el.cs(22,24): warning CS8618: Non-nullable field '_groupName' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\UnifiedConfigurationDialogViewModel.cs(23,54): warning CS8618: Non-nullable field '_parentSection' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Converters\EnumMatchToVisibilityConverter.cs(62,20): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Converters\EnumMatchToVisibilityConverter.cs(18,23): warning CS8618: Non-nullable property 'MatchValue' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Converters\BooleanToValueConverter.cs(24,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Converters\BooleanToValueConverter.cs(27,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Converters\BooleanToValueConverter.cs(31,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Converters\BooleanToValueConverter.cs(47,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Converters\BooleanToValueConverter.cs(51,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Converters\BooleanToValueConverter.cs(58,20): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\AdvancedTools\ViewModels\WIMUtilViewModel.cs(1900,21): warning CS8602: Dereference of a possibly null reference. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\SoftwareAppsViewModel.cs(61,46): warning CS8625: Cannot convert null literal to non-nullable reference type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\SoftwareAppsViewModel.cs(82,55): warning CS8625: Cannot convert null literal to non-nullable reference type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\ExternalAppsHelpViewModel.cs(13,25): warning CS8618: Non-nullable property 'CloseHelpCommand' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\ExternalAppsViewModel.cs(35,70): warning CS9107: Parameter 'ILogService logService' is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]  
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\ExternalAppsViewModel.cs(35,92): warning CS9107: Parameter 'IDialogService dialogService' is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\ExternalAppsViewModel.cs(35,128): warning CS9107: Parameter 'ILocalizationService localizationService' is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\ExternalAppsViewModel.cs(91,33): warning CS8618: Non-nullable field '_allItemsView' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\ExternalAppsViewModel.cs(93,35): warning CS8618: Non-nullable event 'SelectedItemsChanged' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the event as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\ExternalAppsViewModel.cs(31,31): warning CS9113: Parameter 'configurationService' is unread. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\SoftwareAppsViewModel.cs(349,39): warning CS8602: Dereference of a possibly null reference. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\SoftwareAppsViewModel.cs(355,39): warning CS8602: Dereference of a possibly null reference. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\ExternalAppsViewModel.cs(85,33): warning CS8625: Cannot convert null literal to non-nullable reference type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\ExternalAppsViewModel.cs(103,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\SoftwareAppsViewModel.cs(544,34): warning CS8625: Cannot convert null literal to non-nullable reference type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\SoftwareAppsViewModel.cs(579,20): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\SettingItemViewModel.cs(790,77): warning CS8620: Argument of type 'SettingDefinition?[]' cannot be used for parameter 'settings' of type 'IEnumerable<SettingDefinition>' in 'Task<Dictionary<string, SettingStateResult>> ISystemSettingsDiscoveryService.GetSettingStatesAsync(IEnumerable<SettingDefinition> settings)' due to differences in the nullability of reference types. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\SettingItemViewModel.cs(803,97): warning CS8604: Possible null reference argument for parameter 'setting' in 'int IComboBoxSetupService.ResolveIndexFromRawValues(SettingDefinition setting, Dictionary<string, object?> rawValues)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\SettingItemViewModel.cs(821,81): warning CS8604: Possible null reference argument for parameter 'setting' in 'int SettingItemViewModel.ConvertSystemValueToDisplayValue(SettingDefinition setting, int systemValue)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Models\LogMessageViewModel.cs(14,23): warning CS8618: Non-nullable property 'Message' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\WindowInitializationService.cs(38,55): warning CS8600: Converting null literal or possible null value to non-nullable type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\FeatureViewModelFactory.cs(22,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\FeatureViewModelFactory.cs(28,36): warning CS8600: Converting null literal or possible null value to non-nullable type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\FeatureViewModelFactory.cs(32,28): warning CS8600: Converting null literal or possible null value to non-nullable type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\FeatureViewModelFactory.cs(36,28): warning CS8600: Converting null literal or possible null value to non-nullable type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\FeatureViewModelFactory.cs(40,28): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\FeatureViewModelFactory.cs(57,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Utilities\FileLogger.cs(39,47): warning CS8604: Possible null reference argument for parameter 'path' in 'DirectoryInfo Directory.CreateDirectory(string path)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]   
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Utilities\VisualTreeHelpers.cs(11,37): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Utilities\VisualTreeHelpers.cs(25,20): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\ConfigImportOverlayViewModel.cs(8,24): warning CS8618: Non-nullable field '_statusText' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\ConfigImportOverlayViewModel.cs(9,24): warning CS8618: Non-nullable field '_detailText' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\ConfigImportOverlayViewModel.cs(31,50): warning CS8618: Non-nullable event 'PropertyChanged' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the event as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Utilities\VisualTreeHelpers.cs(30,40): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Utilities\VisualTreeHelpers.cs(43,20): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(218,59): warning CS8600: Converting null literal or possible null value to non-nullable type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(227,103): warning CS8604: Possible null reference argument for parameter 'overlayWindow' in 'Task ConfigurationService.ApplyConfigurationWithOptionsAsync(UnifiedConfigurationFile config, List<string> selectedSections, ImportOptions options, ConfigImportOverlayWindow overlayWindow = null)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\BaseSettingsFeatureViewModel.cs(239,73): warning CS8602: Dereference of a possibly null reference. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\LocalizationService.cs(103,24): warning CS8619: Nullability of reference types in value of type 'List<string?>' doesn't match target type 'IEnumerable<string>'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\SettingsLoadingService.cs(26,35): warning CS9113: Parameter 'powerPlanComboBoxService' is unread. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\MainViewModel.cs(59,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\MoreMenuViewModel.cs(45,12): warning CS8618: Non-nullable property 'ChangeLanguageCommand' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\MainViewModel.cs(120,16): warning CS8618: Non-nullable field '_currentViewModel' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\FlyoutManagementService.cs(183,40): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\FlyoutManagementService.cs(200,20): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\DialogService.cs(241,20): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\MoreMenuViewModel.cs(296,30): warning CS8600: Converting null literal or possible null value to non-nullable type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\MoreMenuViewModel.cs(297,54): warning CS8604: Possible null reference argument for parameter 'type' in 'object? Activator.CreateInstance(Type type)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]   
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\MoreMenuViewModel.cs(297,29): warning CS8600: Converting null literal or possible null value to non-nullable type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\MoreMenuViewModel.cs(298,31): warning CS8602: Dereference of a possibly null reference. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\UserPreferencesService.cs(128,36): warning CS8600: Converting null literal or possible null value to non-nullable type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\UserPreferencesService.cs(131,47): warning CS8604: Possible null reference argument for parameter 'path' in 'DirectoryInfo Directory.CreateDirectory(string path)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(309,132): warning CS8604: Possible null reference argument for parameter 'state' in '(int? selectedIndex, Dictionary<string, object> customStateValues, string powerPlanGuid, string powerPlanName) ConfigurationService.GetSelectionStateFromState(SettingDefinition setting, SettingStateResult state)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(364,55): warning CS8601: Possible null reference assignment. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(365,55): warning CS8601: Possible null reference assignment. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\UserPreferencesService.cs(170,47): warning CS8602: Dereference of a possibly null reference. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\UserPreferencesService.cs(193,50): warning CS8605: Unboxing a possibly null value. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\UserPreferencesService.cs(221,42): warning CS8600: Converting null literal or possible null value to non-nullable type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\UserPreferencesService.cs(222,28): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\UserPreferencesService.cs(245,36): warning CS8601: Possible null reference assignment. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\SettingsLoadingService.cs(139,29): warning CS8601: Possible null reference assignment. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(479,24): warning CS8619: Nullability of reference types in value of type '(int?, Dictionary<string, object>?, string?, string?)' doesn't match target type '(int? selectedIndex, Dictionary<string, object> customStateValues, string powerPlanGuid, string powerPlanName)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(482,24): warning CS8619: Nullability of reference types in value of type '(int?, Dictionary<string, object>?, string?, string?)' doesn't match target type '(int? selectedIndex, Dictionary<string, object> customStateValues, string powerPlanGuid, string powerPlanName)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(490,24): warning CS8619: Nullability of reference types in value of type '(int? index, Dictionary<string, object>?, string? guid, string? name)' doesn't match target type '(int? selectedIndex, Dictionary<string, object> customStateValues, string powerPlanGuid, string powerPlanName)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(509,24): warning CS8619: Nullability of reference types in value of type '(int?, Dictionary<string, object>?, string?, string?)' doesn't match target type '(int? selectedIndex, Dictionary<string, object> customStateValues, string powerPlanGuid, string powerPlanName)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(512,20): warning CS8619: Nullability of reference types in value of type '(int? index, Dictionary<string, object>?, string?, string?)' doesn't match target type '(int? selectedIndex, Dictionary<string, object> customStateValues, string powerPlanGuid, string powerPlanName)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(548,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(556,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(569,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(600,20): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(609,20): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(737,127): warning CS8604: Possible null reference argument for parameter 'overlayWindow' in 'Task<bool> ConfigurationService.ApplyFeatureGroupWithOptionsAsync(FeatureGroupSection featureGroup, string groupName, ImportOptions options, List<string> selectedSections, ConfigImportOverlayWindow overlayWindow = null)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(744,129): warning CS8604: Possible null reference argument for parameter 'overlayWindow' in 'Task<bool> ConfigurationService.ApplyFeatureGroupWithOptionsAsync(FeatureGroupSection featureGroup, string groupName, ImportOptions options, List<string> selectedSections, ConfigImportOverlayWindow overlayWindow = null)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(803,41): warning CS8602: Dereference of a possibly null reference. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(901,28): warning CS8602: Dereference of a possibly null reference. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(1034,28): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(1042,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(1048,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(1199,51): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\Views\ExternalAppsTableView.xaml.cs(9,22): warning CS0414: The field 'ExternalAppsTableView._isLoaded' is assigned but its value is never used [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\Views\WindowsAppsTableView.xaml.cs(9,22): warning CS0414: The field 'WindowsAppsTableView._isLoaded' is assigned but its value is never used [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\MainViewModel.cs(479,18): warning MVVMTK0034: The field Winhance.WPF.Features.Common.ViewModels.MainViewModel._currentViewModel is annotated with [ObservableProperty] and should not be directly referenced (use the generated property instead) (https://aka.ms/mvvmtoolkit/errors/mvvmtk0034) [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF_la31z0cf_wpftmp.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\WinhanceSettingsViewModel.cs(7,7): warning CS0105: The using directive for 'Winhance.Core.Features.Common.Interfaces' appeared previously in this namespace [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Controls\ContentLoadingOverlay.xaml.cs(13,51): warning CS0108: 'ContentLoadingOverlay.IsVisibleProperty' hides inherited member 'UIElement.IsVisibleProperty'. Use the new keyword if hiding was intended. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Controls\ContentLoadingOverlay.xaml.cs(27,21): warning CS0108: 'ContentLoadingOverlay.IsVisible' hides inherited member 'UIElement.IsVisible'. Use the new keyword if hiding was intended. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Controls\ContentLoadingOverlay.xaml.cs(51,50): warning CS8612: Nullability of reference types in type of 'event PropertyChangedEventHandler ContentLoadingOverlay.PropertyChanged' doesn't match implicitly implemented member 'event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(717,55): warning CS8625: Cannot convert null literal to non-nullable reference type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(772,55): warning CS8625: Cannot convert null literal to non-nullable reference type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\BaseAppFeatureViewModel.cs(326,54): warning CS0693: Type parameter 'T' has the same name as the type parameter from outer type 'BaseAppFeatureViewModel<T>' [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]       
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\FeatureViewModelFactory.cs(17,48): warning CS8625: Cannot convert null literal to non-nullable reference type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\DialogService.cs(275,28): warning CS8625: Cannot convert null literal to non-nullable reference type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\DialogService.cs(276,37): warning CS8625: Cannot convert null literal to non-nullable reference type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\DialogService.cs(399,47): warning CS8625: Cannot convert null literal to non-nullable reference type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\DialogService.cs(400,48): warning CS8625: Cannot convert null literal to non-nullable reference type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Views\DonationDialog.xaml.cs(55,89): warning CS8625: Cannot convert null literal to non-nullable reference type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Views\DonationDialog.xaml.cs(55,119): warning CS8625: Cannot convert null literal to non-nullable reference type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\BaseAppFeatureViewModel.cs(86,52): warning CS8625: Cannot convert null literal to non-nullable reference type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Views\UpdateDialog.xaml.cs(34,72): warning CS8612: Nullability of reference types in type of 'event PropertyChangedEventHandler UpdateDialog.PropertyChanged' doesn't match implicitly implemented member 'event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\ExternalAppsViewModel.cs(421,36): warning CS8765: Nullability of type of parameter 'parameter' doesn't match overridden member (possibly because of nullability attributes). [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\ExternalAppsViewModel.cs(580,34): warning CS0108: 'ExternalAppsViewModel.ShowConfirmationAsync(string, IEnumerable<AppItemViewModel>)' hides inherited member 'BaseAppFeatureViewModel<AppItemViewModel>.ShowConfirmationAsync(string, IEnumerable<AppItemViewModel>)'. Use the new keyword if hiding was intended. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\BaseCategoryViewModel.cs(55,21): warning CS0114: 'BaseCategoryViewModel.Initialize()' hides inherited member 'BaseContainerViewModel.Initialize()'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]    
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\BaseCategoryViewModel.cs(275,21): warning CS0114: 'BaseCategoryViewModel.OnNavigatedTo(object)' hides inherited member 'BaseViewModel.OnNavigatedTo(object?)'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\BaseCategoryViewModel.cs(277,21): warning CS0114: 'BaseCategoryViewModel.OnNavigatedFrom()' hides inherited member 'BaseViewModel.OnNavigatedFrom()'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]  
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\BaseCategoryViewModel.cs(275,21): warning CS8767: Nullability of reference types in type of parameter 'parameter' of 'void BaseCategoryViewModel.OnNavigatedTo(object parameter = null)' doesn't match implicitly implemented member 'void IFeatureViewModel.OnNavigatedTo(object? parameter = null)' (possibly because of nullability attributes). [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\BaseContainerViewModel.cs(63,29): warning CS0108: 'BaseContainerViewModel.Dispose()' hides inherited member 'BaseViewModel.Dispose()'. Use the new keyword if hiding was intended. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\BaseContainerViewModel.cs(69,32): warning CS0114: 'BaseContainerViewModel.Dispose(bool)' hides inherited member 'BaseViewModel.Dispose(bool)'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\BaseCategoryViewModel.cs(275,54): warning CS8625: Cannot convert null literal to non-nullable reference type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\ConfigImportOverlayViewModel.cs(31,50): warning CS8612: Nullability of reference types in type of 'event PropertyChangedEventHandler ConfigImportOverlayViewModel.PropertyChanged' doesn't match implicitly implemented member 'event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\ConfigImportOverlayViewModel.cs(33,83): warning CS8625: Cannot convert null literal to non-nullable reference type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\BaseSettingsFeatureViewModel.cs(40,21): warning CS0114: 'BaseSettingsFeatureViewModel.IsVisibleInSearch' hides inherited member 'BaseFeatureViewModel.IsVisibleInSearch'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\BaseSettingsFeatureViewModel.cs(102,21): warning CS0114: 'BaseSettingsFeatureViewModel.ApplySearchFilter(string)' hides inherited member 'BaseFeatureViewModel.ApplySearchFilter(string)'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\BaseSettingsFeatureViewModel.cs(203,29): warning CS0114: 'BaseSettingsFeatureViewModel.OnNavigatedFrom()' hides inherited member 'BaseViewModel.OnNavigatedFrom()'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\BaseSettingsFeatureViewModel.cs(209,29): warning CS0114: 'BaseSettingsFeatureViewModel.OnNavigatedTo(object?)' hides inherited member 'BaseViewModel.OnNavigatedTo(object?)'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\BaseSettingsFeatureViewModel.cs(247,21): warning CS0108: 'BaseSettingsFeatureViewModel.Dispose()' hides inherited member 'BaseViewModel.Dispose()'. Use the new keyword if hiding was intended. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\BaseSettingsFeatureViewModel.cs(253,32): warning CS0114: 'BaseSettingsFeatureViewModel.Dispose(bool)' hides inherited member 'BaseViewModel.Dispose(bool)'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\WindowsAppsViewModel.cs(146,21): warning CS0108: 'WindowsAppsViewModel.HasSelectedItems' hides inherited member 'BaseAppFeatureViewModel<AppItemViewModel>.HasSelectedItems'. Use the new keyword if hiding was intended. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\WindowsAppsViewModel.cs(591,36): warning CS8765: Nullability of type of parameter 'parameter' doesn't match overridden member (possibly because of nullability attributes). [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\WindowsAppsViewModel.cs(723,47): warning CS0108: 'WindowsAppsViewModel.FilterItems(IEnumerable<AppItemViewModel>)' hides inherited member 'BaseAppFeatureViewModel<AppItemViewModel>.FilterItems(IEnumerable<AppItemViewModel>)'. Use the new keyword if hiding was intended. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\WindowsAppsViewModel.cs(783,34): warning CS0108: 'WindowsAppsViewModel.ShowConfirmationAsync(string, IEnumerable<AppItemViewModel>)' hides inherited member 'BaseAppFeatureViewModel<AppItemViewModel>.ShowConfirmationAsync(string, IEnumerable<AppItemViewModel>)'. Use the new keyword if hiding was intended. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\WindowsAppsViewModel.cs(1102,22): warning CS0108: 'WindowsAppsViewModel.InvalidateHasSelectedItemsCache()' hides inherited member 'BaseAppFeatureViewModel<AppItemViewModel>.InvalidateHasSelectedItemsCache()'. Use the new keyword if hiding was intended. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]       
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\WindowsAppsViewModel.cs(1135,21): warning CS0108: 'WindowsAppsViewModel.IsSearchActive' hides inherited member 'BaseAppFeatureViewModel<AppItemViewModel>.IsSearchActive'. Use the new keyword if hiding was intended. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\App.xaml.cs(148,25): warning CS8625: Cannot convert null literal to non-nullable reference type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]  
D:\Winhance-FS-Repo\src\Winhance.WPF\App.xaml.cs(48,16): warning CS8618: Non-nullable field '_host' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj] 
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Views\UpdateDialog.xaml.cs(41,17): warning CS8618: Non-nullable event 'PropertyChanged' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the event as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\App.xaml.cs(201,72): warning CS8604: Possible null reference argument for parameter 'result' in 'Task IStartupNotificationService.ShowBackupNotificationAsync(BackupResult result)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\App.xaml.cs(202,64): warning CS8604: Possible null reference argument for parameter 'result' in 'void IStartupNotificationService.ShowMigrationNotification(ScriptMigrationResult result)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]        
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\WindowsAppsViewModel.cs(39,53): warning CS9107: Parameter 'ITaskProgressService progressService' is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]     
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\WindowsAppsViewModel.cs(39,70): warning CS9107: Parameter 'ILogService logService' is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\WindowsAppsViewModel.cs(39,92): warning CS9107: Parameter 'IDialogService dialogService' is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\WindowsAppsViewModel.cs(39,128): warning CS9107: Parameter 'ILocalizationService localizationService' is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\WindowsAppsViewModel.cs(51,35): warning CS8618: Non-nullable event 'SelectedItemsChanged' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the event as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\WindowsAppsViewModel.cs(58,33): warning CS8618: Non-nullable field '_allItemsView' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\WindowsAppsViewModel.cs(34,31): warning CS9113: Parameter 'configurationService' is unread. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Optimize\ViewModels\PowerOptimizationsViewModel.cs(34,129): warning CS8604: Possible null reference argument for parameter 'planToDelete' in 'Task PowerOptimizationsViewModel.DeletePowerPlan(PowerPlanComboBoxOption planToDelete)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\WindowsAppsViewModel.cs(68,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\WindowsAppsViewModel.cs(193,33): warning CS8625: Cannot convert null literal to non-nullable reference type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Views\UnifiedConfigurationDialog.xaml.cs(49,52): warning CS8602: Dereference of a possibly null reference. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Views\UnifiedConfigurationDialog.xaml.cs(53,56): warning CS8602: Dereference of a possibly null reference. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Views\UnifiedConfigurationDialog.xaml.cs(59,41): warning CS8601: Possible null reference assignment. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Views\UnifiedConfigurationDialog.xaml.cs(23,16): warning CS8618: Non-nullable field '_viewModel' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Views\UnifiedConfigurationDialog.xaml.cs(23,16): warning CS8618: Non-nullable field '_logService' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\BaseAppFeatureViewModel.cs(30,15): warning CS9113: Parameter 'eventBus' is unread. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Behaviors\GridViewColumnResizeBehavior.cs(95,33): warning CS8600: Converting null literal or possible null value to non-nullable type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Behaviors\GridViewColumnResizeBehavior.cs(104,33): warning CS8600: Converting null literal or possible null value to non-nullable type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Optimize\ViewModels\PowerOptimizationsViewModel.cs(82,43): warning CS8602: Dereference of a possibly null reference. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Optimize\ViewModels\PowerOptimizationsViewModel.cs(82,43): warning CS8602: Dereference of a possibly null reference. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Controls\ContentLoadingOverlay.xaml.cs(53,16): warning CS8618: Non-nullable event 'PropertyChanged' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the event as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Views\MainWindow.xaml.cs(20,16): warning CS8618: Non-nullable field '_windowIconService' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\AdvancedTools\ViewModels\WIMUtilViewModel.cs(736,21): warning CS8602: Dereference of a possibly null reference. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Behaviors\GridViewColumnResizeBehavior.cs(189,20): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Optimize\ViewModels\PowerOptimizationsViewModel.cs(191,40): warning CS8602: Dereference of a possibly null reference. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Optimize\ViewModels\PowerOptimizationsViewModel.cs(199,43): warning CS8602: Dereference of a possibly null reference. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\BaseAppFeatureViewModel.cs(190,13): warning CS8604: Possible null reference argument for parameter 'failedItems' in 'void IDialogService.ShowOperationResult(string operationType, int successCount, int totalCount, IEnumerable<string> successItems, IEnumerable<string> failedItems = null, IEnumerable<string> skippedItems = null, bool hasConnectivityIssues = false, bool isUserCancelled = false)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\BaseAppFeatureViewModel.cs(191,13): warning CS8604: Possible null reference argument for parameter 'skippedItems' in 'void IDialogService.ShowOperationResult(string operationType, int successCount, int totalCount, IEnumerable<string> successItems, IEnumerable<string> failedItems = null, IEnumerable<string> skippedItems = null, bool hasConnectivityIssues = false, bool isUserCancelled = false)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Controls\ResponsiveScrollViewer.cs(91,64): warning CS8604: Possible null reference argument for parameter 'source' in 'bool ResponsiveScrollViewer.IsEventSourceInScrollViewer(ScrollViewer scrollViewer, DependencyObject source)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Behaviors\ComboBoxDropdownBehavior.cs(191,46): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Behaviors\ComboBoxDropdownBehavior.cs(201,41): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Behaviors\ComboBoxDropdownBehavior.cs(214,20): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\ExternalAppsHelpViewModel.cs(13,25): warning CS8618: Non-nullable property 'CloseHelpCommand' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Views\LoadingWindow.xaml.cs(18,16): warning CS8618: Non-nullable field '_themeManager' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Views\LoadingWindow.xaml.cs(18,16): warning CS8618: Non-nullable field '_windowIconService' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Views\LoadingWindow.xaml.cs(18,16): warning CS8618: Non-nullable field '_windowEffectsService' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Behaviors\DataGridSelectionBehavior.cs(138,20): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\ExternalAppsViewModel.cs(35,70): warning CS9107: Parameter 'ILogService logService' is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\ExternalAppsViewModel.cs(35,92): warning CS9107: Parameter 'IDialogService dialogService' is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\ExternalAppsViewModel.cs(35,128): warning CS9107: Parameter 'ILocalizationService localizationService' is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\ExternalAppsViewModel.cs(91,33): warning CS8618: Non-nullable field '_allItemsView' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\ExternalAppsViewModel.cs(93,35): warning CS8618: Non-nullable event 'SelectedItemsChanged' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the event as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\ExternalAppsViewModel.cs(31,31): warning CS9113: Parameter 'configurationService' is unread. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\ExternalAppsViewModel.cs(85,33): warning CS8625: Cannot convert null literal to non-nullable reference type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\ExternalAppsViewModel.cs(103,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\SoftwareAppsViewModel.cs(61,46): warning CS8625: Cannot convert null literal to non-nullable reference type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\SoftwareAppsViewModel.cs(82,55): warning CS8625: Cannot convert null literal to non-nullable reference type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Converters\BooleanConverter.cs(17,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Converters\BooleanConverter.cs(20,20): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\AdvancedTools\ViewModels\WIMUtilViewModel.cs(1398,21): warning CS8602: Dereference of a possibly null reference. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Converters\TabViewModelSelector.cs(15,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\SoftwareAppsViewModel.cs(349,39): warning CS8602: Dereference of a possibly null reference. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\SoftwareAppsViewModel.cs(355,39): warning CS8602: Dereference of a possibly null reference. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Converters\StringEqualityConverter.cs(25,20): warning CS8602: Dereference of a possibly null reference. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\SoftwareAppsViewModel.cs(544,34): warning CS8625: Cannot convert null literal to non-nullable reference type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\ViewModels\SoftwareAppsViewModel.cs(579,20): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\AdvancedTools\Services\AutounattendXmlGeneratorService.cs(127,128): warning CS8604: Possible null reference argument for parameter 'state' in '(int? selectedIndex, Dictionary<string, object> customStateValues, string powerPlanGuid, string powerPlanName) AutounattendXmlGeneratorService.GetSelectionStateFromState(SettingDefinition setting, SettingStateResult state)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\AdvancedTools\Services\AutounattendXmlGeneratorService.cs(152,30): warning CS8600: Converting null literal or possible null value to non-nullable type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\AdvancedTools\Services\AutounattendXmlGeneratorService.cs(150,46): warning CS8619: Nullability of reference types in value of type 'Dictionary<string, object?>' doesn't match target type 'Dictionary<string, object>'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Models\LogMessageViewModel.cs(14,23): warning CS8618: Non-nullable property 'Message' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\AdvancedTools\ViewModels\WIMUtilViewModel.cs(1900,21): warning CS8602: Dereference of a possibly null reference. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\AdvancedTools\Services\AutounattendXmlGeneratorService.cs(226,20): warning CS8619: Nullability of reference types in value of type '(int?, Dictionary<string, object>?, string?, string?)' doesn't match target type '(int? selectedIndex, Dictionary<string, object> customStateValues, string powerPlanGuid, string powerPlanName)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\AdvancedTools\Services\AutounattendXmlGeneratorService.cs(229,20): warning CS8619: Nullability of reference types in value of type '(int?, Dictionary<string, object>?, string?, string?)' doesn't match target type '(int? selectedIndex, Dictionary<string, object> customStateValues, string powerPlanGuid, string powerPlanName)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\AdvancedTools\Services\AutounattendXmlGeneratorService.cs(237,20): warning CS8619: Nullability of reference types in value of type '(int? index, Dictionary<string, object>?, string? guid, string? name)' doesn't match target type '(int? selectedIndex, Dictionary<string, object> customStateValues, string powerPlanGuid, string powerPlanName)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\AdvancedTools\Services\AutounattendXmlGeneratorService.cs(256,20): warning CS8619: Nullability of reference types in value of type '(int?, Dictionary<string, object>?, string?, string?)' doesn't match target type '(int? selectedIndex, Dictionary<string, object> customStateValues, string powerPlanGuid, string powerPlanName)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\AdvancedTools\Services\AutounattendXmlGeneratorService.cs(259,16): warning CS8619: Nullability of reference types in value of type '(int? index, Dictionary<string, object>?, string?, string?)' doesn't match target type '(int? selectedIndex, Dictionary<string, object> customStateValues, string powerPlanGuid, string powerPlanName)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Converters\EnumMatchToVisibilityConverter.cs(62,20): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Converters\EnumMatchToVisibilityConverter.cs(18,23): warning CS8618: Non-nullable property 'MatchValue' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Converters\BooleanToValueConverter.cs(24,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Converters\BooleanToValueConverter.cs(27,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Converters\BooleanToValueConverter.cs(31,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Converters\BooleanToValueConverter.cs(47,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Converters\BooleanToValueConverter.cs(51,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Converters\BooleanToValueConverter.cs(58,20): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\WinhanceSettingsViewModel.cs(36,16): warning CS8618: Non-nullable field '_themes' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Utilities\FileLogger.cs(39,47): warning CS8604: Possible null reference argument for parameter 'path' in 'DirectoryInfo Directory.CreateDirectory(string path)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Utilities\VisualTreeHelpers.cs(11,37): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Utilities\VisualTreeHelpers.cs(25,20): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Utilities\VisualTreeHelpers.cs(30,40): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Utilities\VisualTreeHelpers.cs(43,20): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\DialogService.cs(241,20): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\UnifiedConfigurationDialogViewModel.cs(446,24): warning CS8618: Non-nullable field '_name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\UnifiedConfigurationDialogViewModel.cs(447,24): warning CS8618: Non-nullable field '_description' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\UnifiedConfigurationDialogViewModel.cs(451,24): warning CS8618: Non-nullable field '_sectionKey' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\WindowInitializationService.cs(38,55): warning CS8600: Converting null literal or possible null value to non-nullable type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(218,59): warning CS8600: Converting null literal or possible null value to non-nullable type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(227,103): warning CS8604: Possible null reference argument for parameter 'overlayWindow' in 'Task ConfigurationService.ApplyConfigurationWithOptionsAsync(UnifiedConfigurationFile config, List<string> selectedSections, ImportOptions options, ConfigImportOverlayWindow overlayWindow = null)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\UnifiedConfigurationDialogViewModel.cs(326,25): warning CS8625: Cannot convert null literal to non-nullable reference type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\UnifiedConfigurationDialogViewModel.cs(327,29): warning CS8625: Cannot convert null literal to non-nullable reference type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\UnifiedConfigurationDialogViewModel.cs(99,16): warning CS8618: Non-nullable field '_title' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\UnifiedConfigurationDialogViewModel.cs(99,16): warning CS8618: Non-nullable field '_description' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\UnifiedConfigurationDialogViewModel.cs(99,16): warning CS8618: Non-nullable property 'OkCommand' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\UnifiedConfigurationDialogViewModel.cs(99,16): warning CS8618: Non-nullable property 'CancelCommand' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\UserPreferencesService.cs(128,36): warning CS8600: Converting null literal or possible null value to non-nullable type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\UserPreferencesService.cs(131,47): warning CS8604: Possible null reference argument for parameter 'path' in 'DirectoryInfo Directory.CreateDirectory(string path)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]       
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\UserPreferencesService.cs(170,47): warning CS8602: Dereference of a possibly null reference. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\UserPreferencesService.cs(193,50): warning CS8605: Unboxing a possibly null value. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\UserPreferencesService.cs(221,42): warning CS8600: Converting null literal or possible null value to non-nullable type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\UserPreferencesService.cs(222,28): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\UserPreferencesService.cs(245,36): warning CS8601: Possible null reference assignment. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\UnifiedConfigurationDialogViewModel.cs(18,24): warning CS8618: Non-nullable field '_key' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\UnifiedConfigurationDialogViewModel.cs(19,24): warning CS8618: Non-nullable field '_label' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\UnifiedConfigurationDialogViewModel.cs(22,24): warning CS8618: Non-nullable field '_groupName' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\UnifiedConfigurationDialogViewModel.cs(23,54): warning CS8618: Non-nullable field '_parentSection' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\BaseSettingsFeatureViewModel.cs(239,73): warning CS8602: Dereference of a possibly null reference. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\SettingsLoadingService.cs(26,35): warning CS9113: Parameter 'powerPlanComboBoxService' is unread. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(309,132): warning CS8604: Possible null reference argument for parameter 'state' in '(int? selectedIndex, Dictionary<string, object> customStateValues, string powerPlanGuid, string powerPlanName) ConfigurationService.GetSelectionStateFromState(SettingDefinition setting, SettingStateResult state)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(364,55): warning CS8601: Possible null reference assignment. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(365,55): warning CS8601: Possible null reference assignment. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\ConfigImportOverlayViewModel.cs(8,24): warning CS8618: Non-nullable field '_statusText' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\ConfigImportOverlayViewModel.cs(9,24): warning CS8618: Non-nullable field '_detailText' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\ConfigImportOverlayViewModel.cs(31,50): warning CS8618: Non-nullable event 'PropertyChanged' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the event as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\LocalizationService.cs(103,24): warning CS8619: Nullability of reference types in value of type 'List<string?>' doesn't match target type 'IEnumerable<string>'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\MainViewModel.cs(59,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\FeatureViewModelFactory.cs(22,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\FeatureViewModelFactory.cs(28,36): warning CS8600: Converting null literal or possible null value to non-nullable type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\FeatureViewModelFactory.cs(32,28): warning CS8600: Converting null literal or possible null value to non-nullable type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\FeatureViewModelFactory.cs(36,28): warning CS8600: Converting null literal or possible null value to non-nullable type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\FeatureViewModelFactory.cs(40,28): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\FeatureViewModelFactory.cs(57,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\FlyoutManagementService.cs(183,40): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\FlyoutManagementService.cs(200,20): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\MainViewModel.cs(120,16): warning CS8618: Non-nullable field '_currentViewModel' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\MoreMenuViewModel.cs(45,12): warning CS8618: Non-nullable property 'ChangeLanguageCommand' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(479,24): warning CS8619: Nullability of reference types in value of type '(int?, Dictionary<string, object>?, string?, string?)' doesn't match target type '(int? selectedIndex, Dictionary<string, object> customStateValues, string powerPlanGuid, string powerPlanName)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(482,24): warning CS8619: Nullability of reference types in value of type '(int?, Dictionary<string, object>?, string?, string?)' doesn't match target type '(int? selectedIndex, Dictionary<string, object> customStateValues, string powerPlanGuid, string powerPlanName)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(490,24): warning CS8619: Nullability of reference types in value of type '(int? index, Dictionary<string, object>?, string? guid, string? name)' doesn't match target type '(int? selectedIndex, Dictionary<string, object> customStateValues, string powerPlanGuid, string powerPlanName)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(509,24): warning CS8619: Nullability of reference types in value of type '(int?, Dictionary<string, object>?, string?, string?)' doesn't match target type '(int? selectedIndex, Dictionary<string, object> customStateValues, string powerPlanGuid, string powerPlanName)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(512,20): warning CS8619: Nullability of reference types in value of type '(int? index, Dictionary<string, object>?, string?, string?)' doesn't match target type '(int? selectedIndex, Dictionary<string, object> customStateValues, string powerPlanGuid, string powerPlanName)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(548,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(556,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(569,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\MoreMenuViewModel.cs(296,30): warning CS8600: Converting null literal or possible null value to non-nullable type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\MoreMenuViewModel.cs(297,54): warning CS8604: Possible null reference argument for parameter 'type' in 'object? Activator.CreateInstance(Type type)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\MoreMenuViewModel.cs(297,29): warning CS8600: Converting null literal or possible null value to non-nullable type. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\MoreMenuViewModel.cs(298,31): warning CS8602: Dereference of a possibly null reference. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\SettingsLoadingService.cs(139,29): warning CS8601: Possible null reference assignment. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\SettingItemViewModel.cs(790,77): warning CS8620: Argument of type 'SettingDefinition?[]' cannot be used for parameter 'settings' of type 'IEnumerable<SettingDefinition>' in 'Task<Dictionary<string, SettingStateResult>> ISystemSettingsDiscoveryService.GetSettingStatesAsync(IEnumerable<SettingDefinition> settings)' due to differences in the nullability of reference types. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\SettingItemViewModel.cs(803,97): warning CS8604: Possible null reference argument for parameter 'setting' in 'int IComboBoxSetupService.ResolveIndexFromRawValues(SettingDefinition setting, Dictionary<string, object?> rawValues)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\SettingItemViewModel.cs(821,81): warning CS8604: Possible null reference argument for parameter 'setting' in 'int SettingItemViewModel.ConvertSystemValueToDisplayValue(SettingDefinition setting, int systemValue)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(600,20): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(609,20): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(737,127): warning CS8604: Possible null reference argument for parameter 'overlayWindow' in 'Task<bool> ConfigurationService.ApplyFeatureGroupWithOptionsAsync(FeatureGroupSection featureGroup, string groupName, ImportOptions options, List<string> selectedSections, ConfigImportOverlayWindow overlayWindow = null)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(744,129): warning CS8604: Possible null reference argument for parameter 'overlayWindow' in 'Task<bool> ConfigurationService.ApplyFeatureGroupWithOptionsAsync(FeatureGroupSection featureGroup, string groupName, ImportOptions options, List<string> selectedSections, ConfigImportOverlayWindow overlayWindow = null)'. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(803,41): warning CS8602: Dereference of a possibly null reference. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(901,28): warning CS8602: Dereference of a possibly null reference. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(1034,28): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(1042,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(1048,24): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\Services\ConfigurationService.cs(1199,51): warning CS8603: Possible null reference return. [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\Views\WindowsAppsTableView.xaml.cs(9,22): warning CS0414: The field 'WindowsAppsTableView._isLoaded' is assigned but its value is never used [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\SoftwareApps\Views\ExternalAppsTableView.xaml.cs(9,22): warning CS0414: The field 'ExternalAppsTableView._isLoaded' is assigned but its value is never used [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
D:\Winhance-FS-Repo\src\Winhance.WPF\Features\Common\ViewModels\MainViewModel.cs(479,18): warning MVVMTK0034: The field Winhance.WPF.Features.Common.ViewModels.MainViewModel._currentViewModel is annotated with [ObservableProperty] and should not be directly referenced (use the generated property instead) (https://aka.ms/mvvmtoolkit/errors/mvvmtk0034) [D:\Winhance-FS-Repo\src\Winhance.WPF\Winhance.WPF.csproj]
    635 Warning(s)
    0 Error(s)