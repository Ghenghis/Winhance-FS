# Winhance-FS - Storage Intelligence & File Forensics

```
██╗    ██╗██╗███╗   ██╗██╗  ██╗ █████╗ ███╗   ██╗ ██████╗███████╗      ███████╗███████╗
██║    ██║██║████╗  ██║██║  ██║██╔══██╗████╗  ██║██╔════╝██╔════╝      ██╔════╝██╔════╝
██║ █╗ ██║██║██╔██╗ ██║███████║███████║██╔██╗ ██║██║     █████╗  █████╗█████╗  ███████╗
██║███╗██║██║██║╚██╗██║██╔══██║██╔══██║██║╚██╗██║██║     ██╔══╝  ╚════╝██╔══╝  ╚════██║
╚███╔███╔╝██║██║ ╚████║██║  ██║██║  ██║██║ ╚████║╚██████╗███████╗      ██║     ███████║
 ╚══╝╚══╝ ╚═╝╚═╝  ╚═══╝╚═╝  ╚═╝╚═╝  ╚═╝╚═╝  ╚═══╝ ╚═════╝╚══════╝      ╚═╝     ╚══════╝
                    STORAGE INTELLIGENCE & FILE FORENSICS FOR WINDOWS
```

<p align="center">
  <a href="https://github.com/Ghenghis/Winhance-FS/releases"><img src="https://img.shields.io/github/v/release/Ghenghis/Winhance-FS?style=for-the-badge&logo=github" alt="Release"></a>
  <a href="https://github.com/Ghenghis/Winhance-FS/blob/main/LICENSE.txt"><img src="https://img.shields.io/github/license/Ghenghis/Winhance-FS?style=for-the-badge" alt="License"></a>
  <a href="https://github.com/Ghenghis/Winhance-FS/stargazers"><img src="https://img.shields.io/github/stars/Ghenghis/Winhance-FS?style=for-the-badge" alt="Stars"></a>
  <img src="https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet" alt=".NET 9">
  <img src="https://img.shields.io/badge/Rust-Backend-DEA584?style=for-the-badge&logo=rust" alt="Rust">
</p>

---

**Winhance-FS** is a fork of [Winhance](https://github.com/memstechtips/Winhance) by memstechtips. It keeps
everything stock Winhance does (software install/removal, Optimize, Customize, Advanced Tools) and adds a
**File Manager** section for answering *"Where did my disk space go?"* and getting it back: a dual-pane browser,
**instant search across whole drives** (a Rust engine that reads the NTFS file table directly), batch rename, a
folder organizer, space recovery and a duplicate finder, plus an **Environment Doctor** for PATH and environment
variables and **Forensics** tools (hidden streams, encrypted or disguised files, previous versions, Recycle Bin
recovery), all with previews, confirmations and undo. A standalone, read-only **MCP server** brings the search
and inspection tools to AI assistants such as Claude.

---

## Screenshots

All screenshots come from the automated end-to-end test sweep (`tests/e2e/`) running the real app. File
operations use a throwaway sandbox folder; the Environment Doctor runs on a **demo environment snapshot**
(`tests/e2e/demo-environment.json`, a fictional `C:\Users\Demo` machine) so no real variables are shown.

### Stock Winhance pages (kept, rebranded "Winhance-FS by Ghenghis")

| Software & Apps | Optimize |
|:---:|:---:|
| ![Software & Apps](docs/images/stock-software-apps.png) | ![Optimize](docs/images/stock-optimize.png) |
| **Customize** | **Settings** |
| ![Customize](docs/images/stock-customize.png) | ![Settings](docs/images/stock-settings.png) |

### New: File Manager

| File Browser — dual pane, copy/move between panes | Right-click menu |
|:---:|:---:|
| ![File Browser](docs/images/file-browser.png) | ![Context menu](docs/images/file-browser-context-menu.png) |
| **Search — instant results while you type** | **Search — query syntax, deleted files marked** |
| ![Search](docs/images/search.png) | ![Search syntax](docs/images/search-syntax.png) |
| **Rename validation (invalid names rejected)** | **Delete asks first and uses the Recycle Bin** |
| ![Rename validation](docs/images/file-browser-rename-validation.png) | ![Delete confirmation](docs/images/file-browser-delete-confirm.png) |
| **Batch Rename — live preview** | **Batch Rename — regex rule** |
| ![Batch Rename preview](docs/images/batch-rename.png) | ![Batch Rename regex](docs/images/batch-rename-regex.png) |
| **Smart Organizer — preview before moving** | **Smart Organizer — strategies** |
| ![Smart Organizer preview](docs/images/smart-organizer.png) | ![Smart Organizer strategies](docs/images/smart-organizer-strategies.png) |
| **Space Recovery — treemap, clean temp files, relocate AI models** | **Duplicates — identical files grouped in sets** |
| ![Space Recovery](docs/images/space-recovery.png) | ![Duplicates](docs/images/duplicates.png) |
| **Duplicates — keep rule marks the extra copies** | |
| ![Duplicates marked](docs/images/duplicates-marked.png) | |

### New: Environment Doctor (environment variables and PATH)

| Problems — severity, scope, proposed fix | Repair preview — backed up, verified, auto-rollback |
|:---:|:---:|
| ![Environment Doctor problems](docs/images/env-problems.png) | ![Repair preview](docs/images/env-repair-preview.png) |
| **PATH — every entry with status** | **Command conflicts — which copy runs** |
| ![PATH entries](docs/images/env-path.png) | ![Command conflicts](docs/images/env-commands.png) |
| **All variables — secrets always hidden** | **Backups — restore any point** |
| ![All variables](docs/images/env-variables.png) | ![Backups](docs/images/env-backups.png) |

### New: Forensics (sandbox test files)

| Hidden streams — download markers, hidden data, hidden programs | Content check — encrypted (ransomware) and disguised files |
|:---:|:---:|
| ![Hidden streams](docs/images/forensics-streams.png) | ![Content check](docs/images/forensics-content.png) |
| **Previous versions — Windows snapshots per drive** | **Recycle Bin — search and restore** |
| ![Previous versions](docs/images/forensics-versions.png) | ![Recycle Bin](docs/images/forensics-recycle-bin.png) |

---

## What's different from stock Winhance

| Area | Stock Winhance | Winhance-FS |
|------|----------------|-------------|
| **File Manager section** | — | New: File Browser, Search, Batch Rename, Smart Organizer, Space Recovery, Environment Doctor, Duplicates, Forensics |
| File search | — | Rust engine: whole NTFS drives read from the Master File Table (6 million files in about 6 s on a fast SSD), other drives and folders scanned; results while you type (tens of milliseconds); words, wildcards, path terms, exclusions, `ext:` and `file:`/`folder:` filters; sort by name match, size or date; index saved and reloaded |
| AI assistants | — | `Winhance-FS-MCP.exe`: a read-only MCP server (search, largest files, duplicates, hidden streams, content check, previous versions, Recycle Bin) for Claude Desktop, Claude Code, VS Code, Windsurf, Cursor |
| Dual-pane browser | — | F5/F6 copy/move between panes, Explorer-compatible cut/copy/paste, F2 rename with validation, Recycle Bin delete, right-click menu |
| Batch rename | — | 8 rule types, live preview, conflict detection, undo of the last batch |
| Folder organizer | — | By type / date / project / size / AI category, preview before moving, undo |
| Space recovery | — | Finds temp files, AI model folders (LM Studio, Ollama, Hugging Face) and package caches; cleans old temp files; **relocates model folders to another drive** and leaves a link so the apps keep working |
| Duplicate finder | — | Size → quick hash → SHA-256; keep rules (newest, oldest, shortest path, first folder); never follows junctions or counts hard links; every copy re-verified before it goes to the Recycle Bin |
| Forensics | — | Hidden NTFS streams (Mark of the Web, hidden data, programs hidden in streams) with preview and removal; content check for encrypted (ransomware) and disguised files by header and entropy; previous versions from Windows snapshots; Recycle Bin search and restore |
| Environment variables / PATH | — | Environment Doctor: detects 45 kinds of problems, command conflicts and runtime homes; previewed repairs with backup, verification and automatic rollback |
| Updates | Checks memstechtips/Winhance | Checks this repository's releases; version history and rollback |
| Other tooling | — | Python CLI and AI agents (`src/nexus_cli`, `src/nexus_ai`) and an earlier Python MCP server (`src/nexus_mcp`): separate, experimental components |
| Software & Apps, Optimize, Customize, Advanced Tools | ✔ | Unchanged from upstream apart from small fixes |

---

## Requirements

- Windows 10 or 11, x64
- [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) for the framework-dependent
  portable zip (the self-contained builds include it)
- Administrator rights (Winhance asks on start, as stock Winhance does)

---

## Installation

Download from [GitHub Releases](https://github.com/Ghenghis/Winhance-FS/releases):

| File | What it is |
|------|------------|
| `Winhance-FS-Setup-vX.Y.Z.exe` | Installer with Start Menu entry and uninstaller |
| `Winhance-x64-Portable-vX.Y.Z.zip` | Self-contained portable — unzip and run `Winhance.exe` |
| `Winhance-x64-FrameworkDependent-Portable-vX.Y.Z.zip` | Smaller portable; needs the .NET 9 Desktop Runtime |
| `Winhance-FS-MCP-x64-vX.Y.Z.zip` | MCP server for AI assistants, one exe ([setup](docs/MCP.md)) |

Each file has a `.sha256` next to it. Check a download with:

```powershell
(Get-FileHash .\Winhance-x64-Portable-vX.Y.Z.zip -Algorithm SHA256).Hash
```

To build it yourself: `dotnet build Winhance.sln`, or `scripts/release.ps1 -Version X.Y.Z` for a portable zip
in `artifacts/release/`.

> **Note:** This project is in active development. See [CHANGELOG.md](CHANGELOG.md) for what changed in each
> release.

---

## Credits and support

Winhance-FS is built on [Winhance](https://github.com/memstechtips/Winhance) by **memstechtips**. If stock
Winhance is useful to you, please support its developer:

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/memstechtips)
[![PayPal](https://img.shields.io/badge/PayPal-00457C?style=for-the-badge&logo=paypal&logoColor=white)](https://paypal.me/memstech)

---

## Features Overview

| Status | Meaning |
|--------|---------|
| ✅ **Available** | In the app, end-to-end tested |
| ⚠️ **Backend only** | Implemented in the backend or CLI; no UI in the app yet |
| 🚧 **Planned** | Not implemented yet |

| Area | Status |
|------|--------|
| [File Browser](#file-browser--available) | ✅ Available |
| [Search](#search--available) (Rust engine, NTFS file table) | ✅ Available |
| [Batch Rename](#batch-rename--available) | ✅ Available |
| [Smart Organizer](#smart-organizer--available) | ✅ Available |
| [Space Recovery](#space-recovery--available) | ✅ Available |
| [Environment Doctor](#environment-doctor--available) (environment variables and PATH) | ✅ Available |
| [Undo / transaction log](#undo-and-safety) | ✅ Available (rename and organize) |
| [Duplicate Finder](#duplicate-finder--available) | ✅ Available |
| [Forensics](#forensics--available): hidden streams, content/entropy check, previous versions, Recycle Bin restore | ✅ Available |
| [AI model relocation](#space-recovery--available) | ✅ Available (Space Recovery → Relocate Selected) |
| [MCP server for AI assistants](#mcp-server-for-ai-assistants--available) | ✅ Available (`Winhance-FS-MCP.exe`) |
| Live index updates (USN journal) | 🚧 Planned: rebuild the index to pick up changes (seconds per drive) |
| Raw-disk undelete (recovering files already removed from the Recycle Bin by reading the MFT) | 🚧 Planned |

---

## File Browser ✅ Available

A dual-pane browser in the **File Manager** section of Winhance.

| Action | How |
|--------|-----|
| Open file / enter folder | Double-click or **Enter** |
| Go up / back | **Backspace**, or the ↑ and ← buttons |
| Copy / move selection to the other pane | **F5** / **F6** |
| Cut, copy, paste | **Ctrl+X**, **Ctrl+C**, **Ctrl+V** (works with Windows Explorer in both directions) |
| Rename | **F2** (invalid names are rejected before anything changes) |
| Delete | **Del** — asks first and sends items to the Recycle Bin |
| New folder | **Ctrl+Shift+N** |
| Refresh | **Ctrl+R** |
| Properties | **Alt+Enter** |
| Everything above | Right-click menu |

Multi-select with **Ctrl**/**Shift**. Name conflicts never overwrite: a copy becomes `name (1).ext`.
Pasting a folder into itself is refused, and moving a folder to another drive copies first and removes the
source only after the copy completes.

---

## Search ✅ Available

The **Search** tab finds any file or folder by name on whole drives, with results while you type.

| Step | What happens |
|------|--------------|
| **Build Index** | Tick drives and/or add folders. NTFS drives are read straight from the drive's Master File Table, the way "Everything" does it: about 6 seconds for 6 million files on a fast SSD, with every file's size and date and every name of a hard-linked file. Other drives (FAT32, exFAT, network, cloud) and added folders are scanned folder by folder, never following junctions or links. Read-only. The index is saved to `%LOCALAPPDATA%\Winhance-FS\search-index.bin` and loaded the next time you open the tab. |
| **Type** | Results update as you type, typically in tens of milliseconds across millions of files. The best name matches come first: exact name, then names that start with the text, then names where it starts a word. |
| **Open** | Enter or double-click opens the item; **Open Location** shows it in Explorer; **Copy Path**. Items deleted or moved since the index was built are greyed and marked. |

| Query | Finds |
|-------|-------|
| `tax 2024` | names containing both words (any order, any case) |
| `"annual report"` | the exact phrase |
| `*.iso`, `IMG_????.jpg` | wildcard match on the whole name |
| `projects\ readme` | `readme` in any path containing `projects\` |
| `report !draft` | excludes names containing `draft` |
| `ext:pdf;docx` | only those types |
| `file:` / `folder:` | only files / only folders |

Sort by **Largest first** or **Newest first** with an empty box to list the biggest or latest files on every
indexed drive. Rebuild the index to pick up changes (live updates are planned).

---

## Batch Rename ✅ Available

Rename many files at once with a live preview that updates as you type.

| Rule | Options |
|------|---------|
| Find & Replace | Plain text or regular expression, match case |
| Add Text | Prefix, suffix or at a character index |
| Remove Text | Remove every match of a regular expression |
| Counter | Start, step, digits, separator, prefix or suffix |
| Change Case | lower, UPPER, Title, Sentence, tOGGLE |
| Change Extension | New extension for every file |
| Date | File modified, file created or current date, any .NET date format |
| Regex | Pattern → replacement |

Rules apply in order and can be reordered. Conflicts are flagged in the preview before anything is renamed,
and **Undo Last Batch** restores the original names.

---

## Smart Organizer ✅ Available

Sorts a folder's files into category subfolders.

- **Strategies:** by file type, date, project, size or AI category
- **Preview first:** Analyze shows every category, file count and destination before anything moves
- **Destination:** subfolders in the same folder, or **Move to** another folder
- **Undo:** restores every file and removes the folders it created

---

## Space Recovery ✅ Available

Finds space you can get back on a drive:

| Location | Action |
|----------|--------|
| Windows temporary files | **Clean** — files older than 24 hours, skipping files in use, after confirmation |
| LM Studio, Ollama and Hugging Face model folders | **Relocate Selected** — moves one folder to a drive you pick (checks free space, copies and verifies before removing the original) and leaves a link at the old path |
| NuGet, Cargo, npm and pip package caches | **Review** — reported only, never deleted by the app |

Results show size and file count for each location, plus a treemap.

---

## Environment Doctor ✅ Available

The **Environment** tab in File Manager diagnoses and repairs Windows environment variables and PATH, for
the current user and the whole system. Scanning is read-only; nothing changes until you review the
fixes and choose **Fix Selected**.

| Area | What it finds |
|------|---------------|
| **PATH** (User and System) | Folders that no longer exist, duplicates (including `%VAR%` vs expanded forms and User copies of System entries), quotes, stray spaces, empty `;;` entries, relative paths, files listed as folders, undefined `%VARIABLES%`, folders with no programs, user-writable folders in the System PATH, missing essential Windows folders (`system32`, `Wbem`, PowerShell, OpenSSH), length over legacy limits |
| **Registry type** | Values with `%VARIABLES%` stored as `REG_SZ`, so Windows never expands them (a classic cause of "command not found") |
| **Command conflicts** | Which copy of `python`, `node`, `java`, `git`, `dotnet`, `go`, `cargo` and 20 more actually runs, every shadowed copy with its version, the Microsoft Store `python` alias hiding a real install, Windows commands (`find`, `sort`, `curl`, `tar`, `ssh`) shadowed by Git/MSYS/conda tools. **Make this run** moves the folder you choose ahead. |
| **Runtime homes** | `JAVA_HOME`, `JDK_HOME`, `GOROOT`, `ANDROID_HOME`/`ANDROID_SDK_ROOT`, `CUDA_PATH`, `DOTNET_ROOT`, `MAVEN_HOME`, `GRADLE_HOME`, `NVM_*` and more: missing folder, pointing at `in`, invalid layout, a different install than the one on PATH, with a suggested replacement found on disk |
| **Developer pitfalls** | Global `PYTHONHOME`/`PYTHONPATH`, `NODE_ENV`, `_JAVA_OPTIONS`/`JAVA_TOOL_OPTIONS`, `PY_PYTHON` naming a Python that is not installed, conda folders on the global PATH, conflicting SDK pairs (`CUDA_PATH` vs `CUDA_HOME` …), User values overriding System ones, manual `PATH_BACKUP` copies, Linux-style `/paths` |
| **Core variables** | `ComSpec`, `windir`, `OS`, `PATHEXT` (missing `.EXE` etc.), `PSModulePath`, `TEMP`/`TMP` missing or not writable |
| **Security** | `SSLKEYLOGFILE` (TLS keys written to disk), passwords, tokens and API keys stored as plain variables (values are always hidden in the app and in reports) |

**Repairs are safe by construction:** only unambiguous fixes are pre-selected; every fix is previewed; a full
backup (JSON + double-clickable `.reg`) is saved first; the plan refuses anything that would empty the System
PATH, drop `system32`, delete core variables or exceed Windows limits; if another program changed a value since
the scan, nothing is written; every value is read back after writing and **all changes roll back
automatically** on any mismatch; running programs are notified (`WM_SETTINGCHANGE`). Any backup can be restored
from the **Backups** section, and a restore is itself backed up. **Export Report** saves a shareable text report
with secrets hidden.

---

## Duplicate Finder ✅ Available

The **Duplicates** tab finds files with identical content in one or more folders.

| Step | What happens |
|------|--------------|
| **Find Duplicates** | Files are grouped by size, then a quick hash (start and end of the file), then confirmed with SHA-256. Junctions and symbolic links are never followed, overlapping folders list a file once, and hard links (one file with two names) are not reported — so a file is never shown as its own duplicate. Filters: minimum size, file types, hidden files. |
| **Mark the Others** | A keep rule picks one copy per set — newest, oldest, shortest path, or the copy in the first folder you listed — and marks the rest. Tick or untick copies by hand. |
| **Delete Marked** | Asks first. Right before each delete the file is re-checked: it must still exist, not be a hard link to the kept copy, and match the kept copy byte for byte (SHA-256). A set always keeps at least one copy. Files go to the Recycle Bin. |

---

## Forensics ✅ Available

The **Forensics** tab inspects a file or folder. All checks are read-only; removing or restoring always asks.

| Section | What it does |
|---------|--------------|
| **Hidden Streams** | Lists NTFS alternate data streams, which Explorer never shows: *download markers* (Zone.Identifier, with the site a file came from), metadata written by Windows and sync apps, *unrecognized hidden data*, and *Windows programs hidden inside a stream* (a malware trick). Shows the first bytes of any stream. **Remove Selected** deletes streams without touching the file; removing a download marker is the same as **Unblock** in file Properties. |
| **Content Check** | Reads each file's header and measures its entropy (randomness, 0–8 bits per byte). Flags **programs disguised as documents** (`holiday.jpg` that is really an `.exe`), **likely encrypted** files — a `.docx`, `.pdf` or `.txt` with no valid header and random content, or a known ransomware extension — and **wrong extensions** (a `.png` that is really a JPEG). |
| **Previous Versions** | Lists earlier copies of a file or folder kept in Windows snapshots (Volume Shadow Copies, made by System Restore and backups), one row per distinct version, compared with the current file. **Restore a Copy** saves it next to the original; **Replace Current** asks first. Shows which drives have snapshots and opens **System Protection** to turn them on. |
| **Recycle Bin** | Lists deleted items for every drive with original location, date and size; filter by name; **Restore** puts items back where they were (recreating the folder), **Restore To** puts them elsewhere. Never overwrites an existing file. |

Not included: recovering files that were already removed from the Recycle Bin by reading raw disk structures
(MFT). Look in **Previous Versions** for those.

---

## MCP server for AI assistants ✅ Available

`Winhance-FS-MCP.exe` (one self-contained file from the release zip) is a read-only
[Model Context Protocol](https://modelcontextprotocol.io) server. It gives Claude Desktop, Claude Code, VS Code,
Windsurf, Cursor and other assistants these tools: `search_files`, `largest_files`, `search_index_status`,
`build_search_index`, `find_duplicates`, `hidden_streams`, `check_file_contents`, `previous_versions` and
`recycle_bin`. It shares the app's search index and never changes, moves or deletes files.

```json
{ "mcpServers": { "winhance-fs": { "command": "C:\\Tools\\Winhance-FS-MCP\\Winhance-FS-MCP.exe" } } }
```

Setup for each client, the tools and troubleshooting: [docs/MCP.md](docs/MCP.md).

---

## Undo and safety

- Batch rename and organize operations are recorded and can be undone from the app.
- Deletes go to the Recycle Bin; nothing is permanently deleted without a confirmation that says so. The
  Forensics tab can restore Recycle Bin items and earlier versions from Windows snapshots.
- The clipboard follows the Windows convention (copy vs. move), so files cut or copied in Explorer
  behave the same when pasted into Winhance, and vice versa.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           WINHANCE-FS ARCHITECTURE                              │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │  PRESENTATION LAYER (C# WPF)                                            │    │
│  │  ├── File Manager Views                                                  │    │
│  │  ├── Batch Rename UI                                                     │    │
│  │  ├── Smart Organizer, Space Recovery, Duplicates                         │    │
│  │  ├── Environment Doctor, Forensics                                       │    │
│  │  └── Settings & Configuration                                            │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
│                                    │                                            │
│                                    ▼                                            │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │  SERVICE LAYER (C# Infrastructure)                                       │    │
│  │  ├── FileManagerService                                                  │    │
│  │  ├── BatchRenameService                                                  │    │
│  │  ├── OrganizerService                                                    │    │
│  │  ├── DuplicateFinderService, ForensicsService                            │    │
│  │  ├── EnvironmentDoctorService (+ registry / snapshot stores)             │    │
│  │  └── NexusIndexerService (Rust engine, JSON over FFI)                    │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
│                                    │                                            │
│                                    ▼                                            │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │  DOMAIN LAYER (C# Core)                                                  │    │
│  │  ├── Interfaces (IFileManagerService, etc.)                              │    │
│  │  ├── Models (FileEntry, DuplicateGroup, etc.)                            │    │
│  │  └── Events (FileOperationEvent, etc.)                                   │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
│                                    │                                            │
│                                    ▼                                            │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │  NATIVE LAYER (Rust nexus_core)                                          │    │
│  │  ├── Name index + query engine (search/name_index.rs)                    │    │
│  │  ├── Raw MFT reader (search/mft_raw.rs), folder walker                   │    │
│  │  ├── USN Journal (usn_journal.rs), Tantivy (optional "fulltext")         │    │
│  │  └── JSON FFI for the app and MCP server (ffi/search_api.rs)             │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
│                                    │                                            │
│                                    ▼                                            │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │  AI TOOLS                                                                │    │
│  │  ├── Winhance.Mcp (MCP server exe, C#, read-only)                        │    │
│  │  ├── nexus_mcp (earlier Python MCP server, experimental)                 │    │
│  │  ├── nexus_ai (ML Tools)                                                 │    │
│  │  └── nexus_cli (Command Line)                                            │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## Project Structure

```
Winhance-FS-Repo/
├── src/
│   ├── Winhance.Core/           # C# Domain layer (interfaces, models)
│   ├── Winhance.Infrastructure/ # C# Service layer
│   ├── Winhance.WPF/            # C# WPF Presentation layer
│   ├── Winhance.Mcp/            # MCP server for AI assistants (Winhance-FS-MCP.exe)
│   ├── nexus_core/              # Rust search engine (nexus_core.dll)
│   │   ├── src/indexer/         # MFT enumeration, USN Journal
│   │   ├── src/search/          # Name index, raw MFT reader, optional Tantivy
│   │   └── src/ffi/             # C# interop
│   ├── nexus_ai/                # Python AI tools
│   ├── nexus_cli/               # Python CLI
│   └── nexus_mcp/               # Earlier Python MCP server (experimental)
├── docs/                        # Documentation
├── scripts/                     # Utility scripts
└── .github/workflows/           # CI/CD pipelines
```

---

## Documentation

### User Guides
- [Features Overview](docs/FEATURES.md)
- [File Manager Guide](docs/FILE_MANAGER.md)
- [Batch Rename System](docs/BATCH_RENAME.md)
- [Smart Organizer](docs/FILE_ORGANIZER.md)
- [MCP Server for AI assistants](docs/MCP.md)
- [Theming Guide](docs/THEMING.md)

### Developer Guides
- [Architecture Guide](docs/ARCHITECTURE.md)
- [Development Setup](docs/DEVELOPMENT.md)
- [Rust Backend](docs/RUST_BACKEND.md)
- [Contributing](CONTRIBUTING.md)

---

## Feedback and Community

If you have feedback, suggestions, or need help with Winhance-FS, please join the discussion:

[![Join the Discussion](https://img.shields.io/badge/Join-the%20Discussion-2D9F2D?style=for-the-badge&logo=github&logoColor=white)](https://github.com/Ghenghis/Winhance-FS/discussions)
[![Join Discord Community](https://img.shields.io/badge/Join-Discord%20Community-5865F2?style=for-the-badge&logo=discord&logoColor=white)](https://www.discord.gg/zWGANV8QAX)

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE.txt) file for details.

---

<p align="center">
  <i>Built with ❤️ as an extension to <a href="https://github.com/memstechtips/Winhance">Winhance</a></i>
</p>
