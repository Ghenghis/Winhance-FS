# Rust Backend (`nexus_core`)

`src/nexus_core` builds `nexus_core.dll`, the search engine behind the File Manager **Search** tab and the
`Winhance-FS-MCP.exe` server. It is a plain `cdylib` with a small JSON-over-FFI API; there is no code
generation or Python binding.

## Layout

```
src/
├── Cargo.toml                 # workspace (version, shared dependency versions)
├── NexusCore.targets          # MSBuild: copies nexus_core.dll into the app / MCP output
└── nexus_core/
    ├── Cargo.toml             # feature "fulltext" = optional Tantivy engine
    ├── examples/index_drive.rs  # benchmark: cargo run --release --example index_drive -- C:\ readme
    └── src/
        ├── lib.rs             # FileEntry, IndexStats, error type
        ├── search/
        │   ├── name_index.rs  # the name index: build, query, rank, sort, save/load (+ folder walker, USN enumeration fallback)
        │   ├── mft_raw.rs     # raw NTFS Master File Table reader
        │   └── tantivy_engine.rs  # only with --features fulltext
        ├── ffi/
        │   ├── search_api.rs  # JSON API used by C# (NexusIndexerService)
        │   └── mod.rs         # older C ABI (hashing helpers), nexus_free_string
        ├── indexer/           # metadata, hashing, USN journal monitor, older MFT enumeration
        └── watcher/           # notify-based folder watcher
```

## How indexing works

1. **Roots** are normalized (`C:` → `C:\`, trailing slashes removed). Nested roots are merged into the outer one.
2. **NTFS drive roots** (`C:\`) are read with `mft_raw::read_volume`:
   - The boot sector gives the cluster size, record size and the MFT's first cluster.
   - Record 0 (`$MFT` itself) gives the MFT's extents (data runs).
   - The MFT is read in 16 MB sequential chunks; each 1 KB record gets its update-sequence fixups checked and
     applied, then is parsed in parallel (rayon): `$STANDARD_INFORMATION` (attributes, modified time),
     every `$FILE_NAME` except DOS 8.3 names (so hard links appear under each name), and the unnamed `$DATA`
     size. Extension records are merged into their base record.
   - Needs Administrator rights (the app always runs elevated). On C: with 6.1 million entries this takes about
     5.5 s on an NVMe SSD.
   - If that fails, `FSCTL_ENUM_USN_DATA` (names only, sizes filled in when results are shown) is tried, then a
     folder walk.
3. **Folders and other drives** are walked in parallel. Junctions and symbolic links are listed but never
   followed. Sizes and dates come from the directory listing (no extra system calls).
4. Records are linked into a tree (parents first). Records whose parent chain doesn't reach the root (NTFS
   metadata, orphans) are dropped, and cycles are guarded against.

## The index in memory

One entry per name: a packed UTF-8 string buffer with `u32` offsets, plus parallel arrays for parent, flags,
size and modified time. A 6-million-entry index uses roughly 270 MB. Matching is case-insensitive without
storing a lower-case copy: ASCII names are compared byte by byte, other names are folded on the fly.

Saved format (`WFSIDX01`): header, roots, then per entry: parent, flags, size, mtime, name. Written to a
temporary file and renamed. Loading validates parent indexes and string lengths.

## Queries

`Query::parse` turns text into ANDed terms: words (name contains), `"phrases"`, `*`/`?` wildcards (whole name),
path terms (any term containing `\` or `/` matches the full path), `!negation`, `ext:a;b`, `file:`, `folder:`.
Cheap terms are checked first; full paths are only built for path terms. Results are ranked: exact name, name
starts with the first word, the word starts a word in the name, then anywhere; ties go to shorter names.
`SortBy::Size` and `SortBy::Modified` order by size or date instead, and with them an empty query matches
everything. Only the top `limit` results are fully built (paths, and a `stat` to mark items deleted since
indexing).

## FFI (`ffi/search_api.rs`)

Every function takes and returns UTF-8 JSON; returned strings are freed with `nexus_free_string`. Panics are
caught and returned as `{"ok":false,...}`.

| Function | Request | Reply |
|----------|---------|-------|
| `nexus_engine_version()` | — | `{"ok":true,"version":"1.5.0","format":"WFSIDX01"}` |
| `nexus_index_build(json)` | `{"roots":["C:\\"],"save_to":"...bin"}` | index info, or `{"ok":false,"cancelled":true}` |
| `nexus_index_load(path)` | file path (plain string) | index info |
| `nexus_index_info()` | — | index info |
| `nexus_index_search(json)` | `{"query":"tax 2024","limit":200,"sort":"relevance"}` | `{"total","ms","results":[{path,name,folder,isDir,size,modified,missing}]}` |
| `nexus_index_cancel()`, `nexus_index_progress()` | — | cancel a build / items found so far |

The engine holds one index per process; a build replaces it only when complete, so searches keep working
during a rebuild.

## Building and testing

```powershell
cargo build --release --manifest-path src/Cargo.toml        # target\release\nexus_core.dll (used by dotnet build)
cargo test  --release --manifest-path src/Cargo.toml
cargo clippy --manifest-path src/Cargo.toml --all-targets -- -D warnings
cargo build --release --manifest-path src/Cargo.toml --lib --target aarch64-pc-windows-msvc   # needs MSVC ARM64 tools
```

The release workflow builds `x86_64`, `i686` and `aarch64` and passes `-p:RequireNexusCore=true` so a package
can't ship without the engine. C# tests (`tests/Winhance.Tests/FileManager/NexusSearchTests.cs`) load the DLL
from the build output.

## Not done yet

- Live updates from the USN journal (the index is rebuilt on demand; a drive takes seconds).
- Listing deleted MFT records for raw-disk undelete (the reader already sees them; they are skipped today).
