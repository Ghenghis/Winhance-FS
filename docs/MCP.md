# Winhance-FS MCP Server

`Winhance-FS-MCP.exe` is a [Model Context Protocol](https://modelcontextprotocol.io) server. It lets AI assistants
(Claude Desktop, Claude Code, VS Code, Windsurf, Cursor and others) search your drives instantly and inspect
files with the same tools as the Winhance-FS app.

- **One file, nothing to install.** A self-contained exe with the Rust search engine inside. No Python, no
  .NET install needed.
- **Read-only.** It never changes, moves or deletes your files. The only thing it writes is the search index
  (`build_search_index`).
- **Local.** It talks to your assistant over stdio on your PC and makes no network connections.

## Download

From [GitHub Releases](https://github.com/Ghenghis/Winhance-FS/releases): `Winhance-FS-MCP-x64-vX.Y.Z.zip`
(or `-ARM64-` for ARM PCs). Unzip it anywhere, for example `C:\Tools\Winhance-FS-MCP\`. Check the download
against its `.sha256`:

```powershell
(Get-FileHash .\Winhance-FS-MCP-x64-vX.Y.Z.zip -Algorithm SHA256).Hash
```

Double-clicking the exe (or running it with `--help`) prints a ready-to-paste configuration with its own path.

## Connect your assistant

Replace the path with where you unzipped it. In JSON, backslashes are doubled.

**Claude Desktop**: Settings → Developer → Edit Config (`%APPDATA%\Claude\claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "winhance-fs": { "command": "C:\\Tools\\Winhance-FS-MCP\\Winhance-FS-MCP.exe" }
  }
}
```

**Claude Code**:

```powershell
claude mcp add winhance-fs -- "C:\Tools\Winhance-FS-MCP\Winhance-FS-MCP.exe"
```

**VS Code** (`.vscode/mcp.json` or the user `mcp.json`):

```json
{
  "servers": {
    "winhance-fs": { "type": "stdio", "command": "C:\\Tools\\Winhance-FS-MCP\\Winhance-FS-MCP.exe" }
  }
}
```

**Windsurf** (`%USERPROFILE%\.codeium\windsurf\mcp_config.json`) and **Cursor** (`%USERPROFILE%\.cursor\mcp.json`)
use the same `mcpServers` block as Claude Desktop.

Restart the assistant after editing its configuration.

## Tools

| Tool | What it does | Try asking |
|------|--------------|------------|
| `search_files` | Finds files and folders by name on every indexed drive in milliseconds. Words must all appear; `"quoted text"`, `*.iso` / `IMG_????.jpg` wildcards, path terms with `\`, `!exclude`, `ext:pdf;docx`, `file:` / `folder:`. Sort by relevance, size or date. | "Find my 2024 tax return PDF" |
| `largest_files` | Largest files on all indexed drives or under one folder, optionally by type. | "What are the 20 biggest videos in my Downloads?" |
| `search_index_status` | Which drives and folders are indexed, how many items, when. | "Is my D: drive in the search index?" |
| `build_search_index` | Builds or rebuilds the index for drives (`C:\`) or folders. | "Index C: and D:\Projects" |
| `find_duplicates` | Files with identical content (SHA-256) in one or more folders, with wasted space. | "Find duplicate photos in Pictures and D:\Backup" |
| `hidden_streams` | NTFS alternate data streams: download markers (with the source site), hidden data, programs hidden inside files. | "Did anything in Downloads come from the internet with a hidden payload?" |
| `check_file_contents` | Programs disguised as documents, files that look encrypted (ransomware sign), wrong extensions. | "Check Documents for encrypted or disguised files" |
| `previous_versions` | Earlier copies of a file or folder in Windows snapshots (System Restore, backups). | "Are there older versions of report.docx?" |
| `recycle_bin` | Deleted items on every drive with original location, date and size. | "What did I delete yesterday?" |

To delete duplicates, remove hidden streams or restore files, use the Winhance-FS app (Duplicates and Forensics
tabs). It previews, asks first and uses the Recycle Bin.

## The search index

The server shares its index with the app: `%LOCALAPPDATA%\Winhance-FS\search-index.bin`. When the app saves a
newer index, the server picks it up on the next search.

The fastest way to build it is in the app: **File Manager → Search → Build Index**. The app runs as
Administrator, so whole NTFS drives are read straight from the file table: on a fast SSD a drive with 6 million
files takes about 6 seconds. The server can build it too (`build_search_index`). Without Administrator rights it scans folder
by folder, which can take minutes for a whole drive, so give it folders, or run the assistant as Administrator.

To use a separate index, set `WINHANCE_FS_INDEX` to a file path in the server's environment, for example
`"env": { "WINHANCE_FS_INDEX": "D:\\Indexes\\work.bin" }` in the client configuration.

## Troubleshooting

- **"No search index yet"**: build one in the app (File Manager → Search) or ask the assistant to call
  `build_search_index`.
- **The server doesn't appear**: run the exe with `--version` in a terminal to check it starts. The client's
  MCP log shows the server's stderr.
- **A drive shows "folder scan" instead of "NTFS file table"**: the index was built without Administrator rights,
  or the drive isn't NTFS (FAT32, exFAT, network or cloud drives are always scanned).
- **Files with several names (hard links)**, mostly Windows system files, are listed under every name.

## Building from source

```powershell
cargo build --release --manifest-path src/Cargo.toml
dotnet publish src/Winhance.Mcp -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
python tests/e2e/mcp_smoke.py path\to\Winhance-FS-MCP.exe
```

The earlier Python server in `src/nexus_mcp` is experimental and not packaged. It needs the full `nexus-fs`
Python environment (PyTorch and other ML packages).
