# Phase 2: Rust Backend (nexus-native)

> **[Back to Roadmap](PROJECT_ROADMAP.md)** | **Previous:** [Phase 1](PHASE_1_FOUNDATION.md) | **Next:** [Phase 3](PHASE_3_CSHARP_INTEGRATION.md)

---

## Phase Overview

| Attribute | Value |
|-----------|-------|
| **Status** | `[ ]` Not Started |
| **Priority** | Critical |
| **Estimated Tasks** | 25 |
| **Dependencies** | [Phase 1](PHASE_1_FOUNDATION.md) Complete |

---

## Objectives

1. Implement high-performance MFT parser using the `ntfs` crate
2. Build SIMD-optimized search engine with `memchr` and `aho-corasick`
3. Create Bloom filter indexing with `fastbloom` for fast negative lookups
4. Implement full-text search with `tantivy`
5. Generate C# bindings via UniFFI
6. Achieve performance targets: < 1 sec index, < 5ms search

---

## Performance Targets

| Metric | Target | Benchmark Tool |
|--------|--------|----------------|
| MFT Scan (1M files) | < 1 second | `cargo bench mft_scan` |
| Search Latency | < 5 ms | `cargo bench search` |
| Memory per 1M files | < 30 MB | `heaptrack` |
| Throughput | > 10 GB/s | `cargo bench simd` |

---

## Task List

### 2.1 MFT Parser Module

#### Task 2.1.1: Implement MFT Scanner Core

**Status:** `[ ]` Not Started

**Description:**
Create the core MFT scanning functionality using the `ntfs` crate.

**Acceptance Criteria:**
- [ ] `src/mft/parser.rs` implements `MftScanner` struct
- [ ] Can scan any NTFS drive's MFT
- [ ] Returns `Vec<MftEntry>` with file metadata
- [ ] Handles errors gracefully with `Result<T, NexusError>`

**Files to Create:**
```
src/nexus-native/src/mft/mod.rs
src/nexus-native/src/mft/parser.rs
src/nexus-native/src/mft/entry.rs
src/nexus-native/src/error.rs
```

**Implementation:**
```rust
// src/mft/parser.rs
use ntfs::Ntfs;
use std::fs::File;
use crate::error::NexusError;
use crate::mft::entry::MftEntry;

pub struct MftScanner {
    drive_letter: char,
}

impl MftScanner {
    pub fn new(drive_letter: char) -> Self {
        Self { drive_letter }
    }

    pub fn scan(&self) -> Result<Vec<MftEntry>, NexusError> {
        let path = format!("\\\\.\\{}:", self.drive_letter);
        let file = File::open(&path)?;
        let ntfs = Ntfs::new(&file)?;

        let mut entries = Vec::with_capacity(1_000_000);
        let mft = ntfs.mft();

        for record in mft.iter(&ntfs, &file) {
            let record = record?;
            if let Some(file_name) = record.file_name(&ntfs, &file)? {
                entries.push(MftEntry {
                    name: file_name.name().to_string_lossy().into_owned(),
                    size: record.data_size(),
                    is_directory: record.is_directory(),
                    created: record.creation_time(),
                    modified: record.modification_time(),
                    accessed: record.access_time(),
                    mft_record_number: record.record_number(),
                    parent_record_number: file_name.parent_directory().record_number(),
                });
            }
        }

        Ok(entries)
    }
}
```

**Tests to Write:**
```rust
#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_scan_c_drive() {
        let scanner = MftScanner::new('C');
        let result = scanner.scan();
        assert!(result.is_ok());
        assert!(result.unwrap().len() > 0);
    }
}
```

**Next Task:** [2.1.2](#task-212-implement-mft-entry-struct)

---

#### Task 2.1.2: Implement MFT Entry Struct

**Status:** `[ ]` Not Started

**Description:**
Create the `MftEntry` data structure with efficient memory layout.

**Acceptance Criteria:**
- [ ] `MftEntry` struct is memory-efficient
- [ ] Implements `Clone`, `Debug`, `Serialize`
- [ ] Compatible with UniFFI export

**Files to Create:**
```
src/nexus-native/src/mft/entry.rs
```

**Implementation:**
```rust
// src/mft/entry.rs
use serde::{Deserialize, Serialize};

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct MftEntry {
    pub name: String,
    pub size: u64,
    pub is_directory: bool,
    pub created: Option<u64>,      // Windows FILETIME
    pub modified: Option<u64>,
    pub accessed: Option<u64>,
    pub mft_record_number: u64,
    pub parent_record_number: u64,
}

// Compact representation for indexing
#[repr(C, packed)]
pub struct CompactEntry {
    pub name_offset: u32,
    pub name_len: u16,
    pub parent_idx: u32,
    pub size: u64,
    pub modified: u64,
    pub flags: u8,
}

impl MftEntry {
    pub fn extension(&self) -> Option<&str> {
        self.name.rsplit('.').next()
    }

    pub fn is_hidden(&self) -> bool {
        self.name.starts_with('.')
    }
}
```

**Dependencies:** [2.1.1](#task-211-implement-mft-scanner-core)

**Next Task:** [2.1.3](#task-213-implement-usn-journal-monitor)

---

#### Task 2.1.3: Implement USN Journal Monitor

**Status:** `[ ]` Not Started

**Description:**
Create real-time file change detection using USN Journal.

**Acceptance Criteria:**
- [ ] Can monitor USN Journal for changes
- [ ] Provides async callback interface
- [ ] Supports cancellation token
- [ ] Minimal CPU usage when idle

**Files to Create:**
```
src/nexus-native/src/mft/usn.rs
```

**Implementation:**
```rust
// src/mft/usn.rs
use tokio::sync::mpsc;
use windows::Win32::Storage::FileSystem::*;

pub struct UsnMonitor {
    drive_letter: char,
    running: std::sync::atomic::AtomicBool,
}

#[derive(Clone, Debug)]
pub struct UsnChange {
    pub path: String,
    pub reason: ChangeReason,
    pub timestamp: u64,
}

#[derive(Clone, Debug)]
pub enum ChangeReason {
    Created,
    Modified,
    Deleted,
    Renamed { old_name: String },
}

impl UsnMonitor {
    pub fn new(drive_letter: char) -> Self {
        Self {
            drive_letter,
            running: std::sync::atomic::AtomicBool::new(false),
        }
    }

    pub async fn start(&self, sender: mpsc::Sender<UsnChange>) -> Result<(), NexusError> {
        self.running.store(true, std::sync::atomic::Ordering::SeqCst);

        while self.running.load(std::sync::atomic::Ordering::SeqCst) {
            // Read USN Journal changes
            // Send to channel
            tokio::time::sleep(std::time::Duration::from_millis(100)).await;
        }

        Ok(())
    }

    pub fn stop(&self) {
        self.running.store(false, std::sync::atomic::Ordering::SeqCst);
    }
}
```

**Dependencies:** [2.1.2](#task-212-implement-mft-entry-struct)

**Next Task:** [2.2.1](#task-221-implement-simd-substring-search)

---

### 2.2 SIMD Search Engine

#### Task 2.2.1: Implement SIMD Substring Search

**Status:** `[ ]` Not Started

**Description:**
Create SIMD-optimized substring search using `memchr`.

**Acceptance Criteria:**
- [ ] `SimdSearcher` struct implemented
- [ ] Uses `memchr::memmem` for vectorized search
- [ ] Achieves > 10 GB/s throughput
- [ ] Benchmarks included

**Files to Create:**
```
src/nexus-native/src/search/mod.rs
src/nexus-native/src/search/simd.rs
```

**Implementation:**
```rust
// src/search/simd.rs
use memchr::memmem;

pub struct SimdSearcher {
    case_sensitive: bool,
}

impl SimdSearcher {
    pub fn new(case_sensitive: bool) -> Self {
        Self { case_sensitive }
    }

    pub fn find_all(&self, haystack: &[u8], needle: &[u8]) -> Vec<usize> {
        let finder = memmem::Finder::new(needle);
        finder.find_iter(haystack).collect()
    }

    pub fn contains(&self, haystack: &[u8], needle: &[u8]) -> bool {
        let finder = memmem::Finder::new(needle);
        finder.find(haystack).is_some()
    }

    pub fn search_files<'a>(
        &self,
        files: &'a [MftEntry],
        pattern: &str,
    ) -> Vec<&'a MftEntry> {
        let pattern_bytes = if self.case_sensitive {
            pattern.as_bytes().to_vec()
        } else {
            pattern.to_lowercase().as_bytes().to_vec()
        };

        files
            .iter()
            .filter(|entry| {
                let name_bytes = if self.case_sensitive {
                    entry.name.as_bytes()
                } else {
                    &entry.name.to_lowercase().as_bytes()
                };
                self.contains(name_bytes, &pattern_bytes)
            })
            .collect()
    }
}
```

**Benchmark:**
```rust
// benches/search_bench.rs
use criterion::{criterion_group, criterion_main, Criterion};

fn bench_simd_search(c: &mut Criterion) {
    let haystack = "x".repeat(1_000_000);
    let needle = "pattern";

    c.bench_function("simd_search_1mb", |b| {
        let searcher = SimdSearcher::new(true);
        b.iter(|| searcher.find_all(haystack.as_bytes(), needle.as_bytes()))
    });
}

criterion_group!(benches, bench_simd_search);
criterion_main!(benches);
```

**Next Task:** [2.2.2](#task-222-implement-multi-pattern-search)

---

#### Task 2.2.2: Implement Multi-Pattern Search

**Status:** `[ ]` Not Started

**Description:**
Implement Aho-Corasick algorithm for searching multiple patterns simultaneously.

**Acceptance Criteria:**
- [ ] `MultiPatternSearcher` struct implemented
- [ ] Uses `aho-corasick` crate
- [ ] Supports regex patterns
- [ ] Returns matches with pattern IDs

**Files to Create:**
```
src/nexus-native/src/search/multi.rs
```

**Implementation:**
```rust
// src/search/multi.rs
use aho_corasick::{AhoCorasick, MatchKind};

pub struct MultiPatternSearcher {
    automaton: AhoCorasick,
    patterns: Vec<String>,
}

pub struct PatternMatch {
    pub pattern_index: usize,
    pub start: usize,
    pub end: usize,
}

impl MultiPatternSearcher {
    pub fn new(patterns: &[&str]) -> Result<Self, NexusError> {
        let automaton = AhoCorasick::builder()
            .match_kind(MatchKind::LeftmostFirst)
            .build(patterns)?;

        Ok(Self {
            automaton,
            patterns: patterns.iter().map(|s| s.to_string()).collect(),
        })
    }

    pub fn find_all(&self, text: &str) -> Vec<PatternMatch> {
        self.automaton
            .find_iter(text)
            .map(|m| PatternMatch {
                pattern_index: m.pattern().as_usize(),
                start: m.start(),
                end: m.end(),
            })
            .collect()
    }

    pub fn search_files<'a>(
        &self,
        files: &'a [MftEntry],
    ) -> Vec<(&'a MftEntry, Vec<PatternMatch>)> {
        files
            .iter()
            .filter_map(|entry| {
                let matches = self.find_all(&entry.name);
                if matches.is_empty() {
                    None
                } else {
                    Some((entry, matches))
                }
            })
            .collect()
    }
}
```

**Dependencies:** [2.2.1](#task-221-implement-simd-substring-search)

**Next Task:** [2.3.1](#task-231-implement-bloom-filter-index)

---

### 2.3 Bloom Filter Index

#### Task 2.3.1: Implement Bloom Filter Index

**Status:** `[ ]` Not Started

**Description:**
Create Bloom filter for fast negative lookups during search.

**Acceptance Criteria:**
- [ ] `BloomIndex` struct implemented
- [ ] False positive rate < 0.1%
- [ ] Negative lookup < 30ns
- [ ] Serializable for persistence

**Files to Create:**
```
src/nexus-native/src/search/bloom.rs
```

**Implementation:**
```rust
// src/search/bloom.rs
use fastbloom::BloomFilter;
use serde::{Deserialize, Serialize};

#[derive(Serialize, Deserialize)]
pub struct BloomIndex {
    filter: BloomFilter,
    item_count: usize,
}

impl BloomIndex {
    pub fn new(expected_items: usize) -> Self {
        Self {
            filter: BloomFilter::with_rate(0.001, expected_items),
            item_count: 0,
        }
    }

    pub fn insert(&mut self, item: &str) {
        self.filter.insert(item);
        self.item_count += 1;
    }

    pub fn insert_file(&mut self, entry: &MftEntry) {
        // Insert filename
        self.insert(&entry.name);

        // Insert name parts for partial matching
        for part in entry.name.split(|c: char| !c.is_alphanumeric()) {
            if part.len() >= 3 {
                self.insert(part);
            }
        }

        // Insert extension
        if let Some(ext) = entry.extension() {
            self.insert(ext);
        }
    }

    pub fn might_contain(&self, item: &str) -> bool {
        self.filter.contains(item)
    }

    pub fn save(&self, path: &std::path::Path) -> Result<(), NexusError> {
        let bytes = bincode::serialize(self)?;
        std::fs::write(path, bytes)?;
        Ok(())
    }

    pub fn load(path: &std::path::Path) -> Result<Self, NexusError> {
        let bytes = std::fs::read(path)?;
        let index = bincode::deserialize(&bytes)?;
        Ok(index)
    }
}
```

**Dependencies:** [2.2.2](#task-222-implement-multi-pattern-search)

**Next Task:** [2.4.1](#task-241-implement-tantivy-index)

---

### 2.4 Full-Text Index (Tantivy)

#### Task 2.4.1: Implement Tantivy Index

**Status:** `[ ]` Not Started

**Description:**
Create full-text search index using Tantivy.

**Acceptance Criteria:**
- [ ] `SearchIndex` struct with Tantivy backend
- [ ] Supports fuzzy matching
- [ ] Incremental updates
- [ ] Multi-field search (name, path, extension)

**Files to Create:**
```
src/nexus-native/src/index/mod.rs
src/nexus-native/src/index/tantivy.rs
```

**Implementation:**
```rust
// src/index/tantivy.rs
use tantivy::{
    schema::{Schema, STORED, TEXT, Field},
    Index, IndexWriter, Document,
    query::QueryParser,
    collector::TopDocs,
};

pub struct SearchIndex {
    index: Index,
    schema: Schema,
    path_field: Field,
    name_field: Field,
    extension_field: Field,
    size_field: Field,
}

#[derive(Debug)]
pub struct SearchResult {
    pub path: String,
    pub name: String,
    pub score: f32,
}

impl SearchIndex {
    pub fn create(index_path: &std::path::Path) -> Result<Self, NexusError> {
        let mut schema_builder = Schema::builder();

        let path_field = schema_builder.add_text_field("path", TEXT | STORED);
        let name_field = schema_builder.add_text_field("name", TEXT | STORED);
        let extension_field = schema_builder.add_text_field("extension", TEXT | STORED);
        let size_field = schema_builder.add_u64_field("size", tantivy::schema::INDEXED);

        let schema = schema_builder.build();
        let index = Index::create_in_dir(index_path, schema.clone())?;

        Ok(Self {
            index,
            schema,
            path_field,
            name_field,
            extension_field,
            size_field,
        })
    }

    pub fn add_files(&self, files: &[MftEntry]) -> Result<(), NexusError> {
        let mut writer = self.index.writer(100_000_000)?;

        for entry in files {
            let mut doc = Document::new();
            doc.add_text(self.name_field, &entry.name);
            if let Some(ext) = entry.extension() {
                doc.add_text(self.extension_field, ext);
            }
            doc.add_u64(self.size_field, entry.size);
            writer.add_document(doc)?;
        }

        writer.commit()?;
        Ok(())
    }

    pub fn search(&self, query: &str, limit: usize) -> Result<Vec<SearchResult>, NexusError> {
        let reader = self.index.reader()?;
        let searcher = reader.searcher();

        let query_parser = QueryParser::for_index(&self.index, vec![self.name_field]);
        let query = query_parser.parse_query(query)?;

        let top_docs = searcher.search(&query, &TopDocs::with_limit(limit))?;

        let mut results = Vec::new();
        for (score, doc_address) in top_docs {
            let doc = searcher.doc(doc_address)?;
            if let Some(name) = doc.get_first(self.name_field) {
                results.push(SearchResult {
                    path: String::new(), // TODO: reconstruct path
                    name: name.as_text().unwrap_or_default().to_string(),
                    score,
                });
            }
        }

        Ok(results)
    }
}
```

**Dependencies:** [2.3.1](#task-231-implement-bloom-filter-index)

**Next Task:** [2.5.1](#task-251-implement-entropy-calculator)

---

### 2.5 Forensics Module

#### Task 2.5.1: Implement Entropy Calculator

**Status:** `[ ]` Not Started

**Description:**
Create file entropy calculation for detecting encrypted/compressed files.

**Acceptance Criteria:**
- [ ] Shannon entropy calculation
- [ ] Classification thresholds defined
- [ ] Batch processing support

**Files to Create:**
```
src/nexus-native/src/forensics/mod.rs
src/nexus-native/src/forensics/entropy.rs
```

**Implementation:**
```rust
// src/forensics/entropy.rs
pub struct EntropyCalculator;

#[derive(Debug, Clone)]
pub enum EntropyClass {
    LowEntropy,      // < 4.0 - text, structured data
    MediumEntropy,   // 4.0 - 7.0 - code, documents
    HighEntropy,     // 7.0 - 7.9 - compressed
    VeryHighEntropy, // > 7.9 - encrypted
}

impl EntropyCalculator {
    pub fn calculate(data: &[u8]) -> f64 {
        if data.is_empty() {
            return 0.0;
        }

        let mut freq = [0u64; 256];
        for &byte in data {
            freq[byte as usize] += 1;
        }

        let len = data.len() as f64;
        let mut entropy = 0.0;

        for &count in &freq {
            if count > 0 {
                let p = count as f64 / len;
                entropy -= p * p.log2();
            }
        }

        entropy
    }

    pub fn classify(entropy: f64) -> EntropyClass {
        match entropy {
            e if e < 4.0 => EntropyClass::LowEntropy,
            e if e < 7.0 => EntropyClass::MediumEntropy,
            e if e < 7.9 => EntropyClass::HighEntropy,
            _ => EntropyClass::VeryHighEntropy,
        }
    }

    pub fn analyze_file(path: &std::path::Path) -> Result<(f64, EntropyClass), NexusError> {
        let data = std::fs::read(path)?;
        let entropy = Self::calculate(&data);
        let class = Self::classify(entropy);
        Ok((entropy, class))
    }
}
```

**Next Task:** [2.5.2](#task-252-implement-ads-scanner)

---

#### Task 2.5.2: Implement ADS Scanner

**Status:** `[ ]` Not Started

**Description:**
Scan for NTFS Alternate Data Streams.

**Acceptance Criteria:**
- [ ] Detect all ADS on a file/directory
- [ ] Read ADS content
- [ ] Identify suspicious streams

**Files to Create:**
```
src/nexus-native/src/forensics/ads.rs
```

**Dependencies:** [2.5.1](#task-251-implement-entropy-calculator)

**Next Task:** [2.5.3](#task-253-implement-vss-accessor)

---

#### Task 2.5.3: Implement VSS Accessor

**Status:** `[ ]` Not Started

**Description:**
Access Volume Shadow Copy snapshots.

**Acceptance Criteria:**
- [ ] List available shadow copies
- [ ] Access files from snapshots
- [ ] Compare versions

**Files to Create:**
```
src/nexus-native/src/forensics/vss.rs
```

**Dependencies:** [2.5.2](#task-252-implement-ads-scanner)

**Next Task:** [2.6.1](#task-261-create-uniffi-interface)

---

### 2.6 UniFFI Bindings

#### Task 2.6.1: Create UniFFI Interface

**Status:** `[ ]` Not Started

**Description:**
Define the UniFFI interface for C# interop.

**Acceptance Criteria:**
- [ ] `nexus.udl` defines all exported types
- [ ] Async functions properly exposed
- [ ] Error types mapped
- [ ] Callbacks supported

**Files to Create/Update:**
```
src/nexus-native/src/nexus.udl
src/nexus-native/build.rs
```

**Implementation:**
```udl
// nexus.udl
namespace nexus {
    // === MFT Types ===
    dictionary MftEntry {
        string name;
        u64 size;
        boolean is_directory;
        u64? created;
        u64? modified;
        u64? accessed;
        u64 mft_record_number;
        u64 parent_record_number;
    };

    // === Scan Types ===
    dictionary ScanOptions {
        boolean include_hidden;
        boolean include_system;
        u32 max_depth;
    };

    dictionary ScanResult {
        sequence<MftEntry> entries;
        u64 total_size;
        u64 file_count;
        u64 directory_count;
        u64 scan_duration_ms;
    };

    // === Search Types ===
    dictionary SearchOptions {
        boolean case_sensitive;
        boolean use_regex;
        u32 max_results;
    };

    dictionary SearchResult {
        string path;
        string name;
        f32 score;
    };

    // === Error Type ===
    [Error]
    enum NexusError {
        "IoError",
        "NtfsError",
        "IndexError",
        "SearchError",
        "AccessDenied",
    };

    // === Main Interface ===
    interface NexusRuntime {
        constructor();

        [Throws=NexusError]
        ScanResult scan_drive(string drive_letter, ScanOptions options);

        [Throws=NexusError]
        sequence<SearchResult> search(string query, SearchOptions options);

        [Throws=NexusError]
        void build_index(string drive_letter);

        [Throws=NexusError]
        f64 calculate_entropy(string path);
    };
};
```

**Next Task:** [2.6.2](#task-262-generate-c-bindings)

---

#### Task 2.6.2: Generate C# Bindings

**Status:** `[ ]` Not Started

**Description:**
Generate C# bindings from UniFFI definitions.

**Acceptance Criteria:**
- [ ] `nexus_native.cs` generated
- [ ] All types correctly mapped
- [ ] Async wrappers created
- [ ] NuGet package configured

**Commands:**
```bash
cd src/nexus-native
cargo build --release
cargo run --bin uniffi-bindgen generate --library target/release/nexus_native.dll --language csharp --out-dir ../Winhance.Infrastructure/Features/Storage/Native/
```

**Dependencies:** [2.6.1](#task-261-create-uniffi-interface)

**Next Task:** [2.7.1](#task-271-implement-cli-tool)

---

### 2.7 CLI Tool

#### Task 2.7.1: Implement CLI Tool

**Status:** `[ ]` Not Started

**Description:**
Create command-line interface for nexus-native.

**Acceptance Criteria:**
- [ ] `nexus-cli` binary builds
- [ ] `scan` command works
- [ ] `search` command works
- [ ] `benchmark` command works
- [ ] Progress output shown

**Files to Create:**
```
src/nexus-native/src/bin/nexus-cli.rs
```

**Implementation:**
```rust
// src/bin/nexus-cli.rs
use clap::{Parser, Subcommand};

#[derive(Parser)]
#[command(name = "nexus-cli")]
#[command(about = "Winhance-FS Storage Intelligence CLI")]
struct Cli {
    #[command(subcommand)]
    command: Commands,
}

#[derive(Subcommand)]
enum Commands {
    /// Scan a drive's MFT
    Scan {
        /// Drive letter (e.g., C)
        #[arg(short, long)]
        drive: char,

        /// Output format (json, table)
        #[arg(short, long, default_value = "table")]
        format: String,
    },

    /// Search indexed files
    Search {
        /// Search query
        query: String,

        /// Maximum results
        #[arg(short, long, default_value = "100")]
        limit: usize,
    },

    /// Run benchmarks
    Benchmark {
        /// Benchmark type (mft, search, all)
        #[arg(short, long, default_value = "all")]
        bench_type: String,
    },

    /// Build search index
    Index {
        /// Drive letter
        #[arg(short, long)]
        drive: char,
    },
}

fn main() {
    let cli = Cli::parse();

    match cli.command {
        Commands::Scan { drive, format } => {
            // Implement scan
        }
        Commands::Search { query, limit } => {
            // Implement search
        }
        Commands::Benchmark { bench_type } => {
            // Implement benchmark
        }
        Commands::Index { drive } => {
            // Implement index
        }
    }
}
```

**Dependencies:** [2.6.2](#task-262-generate-c-bindings)

---

## Phase Completion Checklist

- [ ] All 2.1.x tasks complete (MFT Parser)
- [ ] All 2.2.x tasks complete (SIMD Search)
- [ ] All 2.3.x tasks complete (Bloom Filter)
- [ ] All 2.4.x tasks complete (Tantivy Index)
- [ ] All 2.5.x tasks complete (Forensics)
- [ ] All 2.6.x tasks complete (UniFFI Bindings)
- [ ] All 2.7.x tasks complete (CLI Tool)
- [ ] `cargo test` passes all tests
- [ ] `cargo bench` meets performance targets
- [ ] C# bindings compile without errors

---

## Benchmarks

Run benchmarks after completing each section:

```bash
cd src/nexus-native

# Run all benchmarks
cargo bench

# Run specific benchmark
cargo bench mft_scan
cargo bench simd_search
cargo bench bloom_filter

# Generate HTML report
cargo bench -- --save-baseline current
```

---

**[Back to Roadmap](PROJECT_ROADMAP.md)** | **Previous:** [Phase 1](PHASE_1_FOUNDATION.md) | **Next:** [Phase 3](PHASE_3_CSHARP_INTEGRATION.md)
