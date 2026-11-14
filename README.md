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

**Winhance-FS** is a high-performance storage intelligence module that integrates with [Winhance](https://github.com/memstechtips/Winhance), adding advanced file system forensics, AI-powered organization, and ultra-fast search capabilities to your Windows enhancement toolkit.

Built with **Rust** for blazing-fast performance and **C#/.NET 9** for seamless Windows integration, Winhance-FS answers the question: *"Where did my disk space go?"*

---

## Requirements

```
┌──────────────────────────────────────────────────────────────┐
│  SYSTEM REQUIREMENTS                                          │
├──────────────────────────────────────────────────────────────┤
│  • Windows 10/11 (x64)                                        │
│  • .NET 9.0 Runtime                                           │
│  • 4GB RAM minimum (8GB recommended)                          │
│  • Administrator privileges for deep scan features            │
├──────────────────────────────────────────────────────────────┤
│  TESTED ON: Windows 11 23H2, 24H2, 25H2                       │
└──────────────────────────────────────────────────────────────┘
```

---

## Installation

### Quick Install via PowerShell
```powershell
irm "https://get.winhance-fs.net" | iex
```

### Download the Installer

[![Download from GitHub Releases](https://img.shields.io/badge/Download-GitHub%20Releases-181717?style=for-the-badge&logo=github&logoColor=white)](https://github.com/Ghenghis/Winhance-FS/releases)

The `Winhance-FS.Installer.exe` includes both **Installable** and **Portable** versions during setup.

| Edition | Description |
|---------|-------------|
| **x64 Installer** | Full installation with Start Menu integration |
| **x64 Portable** | No installation required, run from any folder |

> **Note:** This project is in active development. Features are being added regularly.

---

## Support the Developer

Your support makes a big difference and is very much appreciated!

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
| [Batch Rename](#batch-rename--available) | ✅ Available |
| [Smart Organizer](#smart-organizer--available) | ✅ Available |
| [Space Recovery](#space-recovery--available) | ✅ Available |
| [Undo / transaction log](#undo-and-safety) | ✅ Available (rename and organize) |
| Duplicate Finder | ⚠️ Backend only (`IDuplicateFinderService`) |
| AI model relocation | ⚠️ Backend and scripts (`scripts/relocation/`) |
| Rust deep-scan engine (MFT reader, USN journal, Tantivy search) | ⚠️ Backend only (`src/nexus_core`) |
| MCP server for AI assistants | ⚠️ Separate Python component (`src/nexus_mcp`) |
| Forensics tools (ADS, shadow copies, entropy, undelete) | 🚧 Planned |

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
| LM Studio, Ollama and Hugging Face model folders | **Relocate** to another drive (see `scripts/relocation/`) |
| NuGet, Cargo, npm and pip package caches | **Review** — reported only, never deleted by the app |

Results show size and file count for each location, plus a treemap.

---

## Undo and safety

- Batch rename and organize operations are recorded and can be undone from the app.
- Deletes go to the Recycle Bin; nothing is permanently deleted without a confirmation that says so.
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
│  │  ├── Smart Organizer                                                     │    │
│  │  └── Settings & Configuration                                            │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
│                                    │                                            │
│                                    ▼                                            │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │  SERVICE LAYER (C# Infrastructure)                                       │    │
│  │  ├── FileManagerService                                                  │    │
│  │  ├── BatchRenameService                                                  │    │
│  │  ├── OrganizerService                                                    │    │
│  │  ├── DuplicateFinderService                                              │    │
│  │  └── NexusIndexerService (Rust Interop)                                  │    │
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
│  │  ├── MFT Reader (mft_reader.rs)                                          │    │
│  │  ├── USN Journal (usn_journal.rs)                                        │    │
│  │  ├── Tantivy Search (tantivy_engine.rs)                                  │    │
│  │  └── FFI Bindings (ffi/mod.rs)                                           │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
│                                    │                                            │
│                                    ▼                                            │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │  AI TOOLS LAYER (Python)                                                 │    │
│  │  ├── nexus_mcp (MCP Server)                                              │    │
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
│   ├── nexus_core/              # Rust performance backend
│   │   ├── src/indexer/         # MFT, USN Journal
│   │   ├── src/search/          # Tantivy engine
│   │   └── src/ffi/             # C# interop
│   ├── nexus_ai/                # Python AI tools
│   ├── nexus_cli/               # Python CLI
│   └── nexus_mcp/               # Python MCP server
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
