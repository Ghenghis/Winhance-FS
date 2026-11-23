//! Instant file-name search index.
//!
//! Every file and folder name under the indexed roots is kept in memory as a compact table (name, parent,
//! flags, size, modified time), so a query is a parallel scan over names: typically a few milliseconds for a
//! million entries. Whole NTFS drives are read from the Master File Table (the technique "Everything" uses),
//! which lists a drive in seconds; folders and non-NTFS drives are walked. The table is saved to a small
//! binary file so the index is available immediately the next time.

use crate::{NexusError, Result};
use rayon::prelude::*;
use serde::Serialize;
use std::collections::HashMap;
use std::fs;
use std::io::{BufReader, BufWriter, Read, Write};
use std::path::{Path, PathBuf};
use std::sync::atomic::{AtomicBool, AtomicU64, Ordering};
use std::time::{Instant, SystemTime, UNIX_EPOCH};

const NONE: u32 = u32::MAX;
const FLAG_DIR: u8 = 1;
const FLAG_HIDDEN: u8 = 2;
const FLAG_SYSTEM: u8 = 4;
const FLAG_ROOT: u8 = 8;
const FLAG_NO_STAT: u8 = 16; // size/modified unknown (read from the MFT); filled in when shown
const UNKNOWN_TIME: i64 = i64::MIN;
const MAGIC: &[u8; 8] = b"WFSIDX01";

const ATTR_HIDDEN: u32 = 0x2;
const ATTR_SYSTEM: u32 = 0x4;
const ATTR_DIRECTORY: u32 = 0x10;
const ATTR_REPARSE: u32 = 0x400;

/// Cancellation and progress shared with the caller while an index is built.
#[derive(Default)]
pub struct BuildControl {
    pub cancel: AtomicBool,
    pub progress: AtomicU64,
}

/// How one root was indexed.
#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct RootInfo {
    pub path: String,
    /// "mft" (read from the NTFS Master File Table) or "scan" (folder walk).
    pub method: String,
    pub entries: u64,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub error: Option<String>,
}

/// One search hit.
#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct Hit {
    pub path: String,
    pub name: String,
    pub folder: String,
    pub is_dir: bool,
    pub size: u64,
    /// Unix seconds; absent when unknown.
    #[serde(skip_serializing_if = "Option::is_none")]
    pub modified: Option<i64>,
    /// True when the item was deleted or moved after the index was built.
    pub missing: bool,
}

/// Search results.
#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct SearchOutcome {
    pub total: u64,
    pub ms: u64,
    pub results: Vec<Hit>,
}

/// Result order.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Default)]
pub enum SortBy {
    /// Best name match first.
    #[default]
    Relevance,
    /// Largest first.
    Size,
    /// Most recently modified first.
    Modified,
}

impl SortBy {
    pub fn parse(text: &str) -> SortBy {
        match text.to_ascii_lowercase().as_str() {
            "size" | "largest" => SortBy::Size,
            "modified" | "newest" | "date" => SortBy::Modified,
            _ => SortBy::Relevance,
        }
    }
}

/// Parsed query. Terms are ANDed; see [`Query::parse`].
#[derive(Debug, Clone, Default, PartialEq)]
pub struct Query {
    pub terms: Vec<Term>,
    pub extensions: Vec<String>,
    pub files_only: bool,
    pub folders_only: bool,
}

#[derive(Debug, Clone, PartialEq)]
pub struct Term {
    pub text: String,
    pub kind: TermKind,
    pub negate: bool,
}

#[derive(Debug, Clone, Copy, PartialEq)]
pub enum TermKind {
    /// Name contains the text.
    Contains,
    /// Whole name matches a pattern with `*` and `?`.
    Wildcard,
    /// Full path contains the text (the term had a `\` or `/`).
    Path,
}

impl Query {
    /// Parses a query. Rules (all case-insensitive):
    /// - words must all appear in the name: `tax 2024` finds `2024 Tax Return.pdf`;
    /// - `"quoted text"` is one term, spaces included;
    /// - `*` and `?` match the whole name: `*.iso`, `IMG_????.jpg`;
    /// - a term with `\` or `/` matches the full path: `projects\ readme`;
    /// - `!word` excludes names containing word;
    /// - `ext:pdf;docx` keeps only those extensions; `file:` / `folder:` keep only files or folders.
    pub fn parse(text: &str) -> Query {
        let mut query = Query::default();
        for raw in tokenize(text) {
            let lower = raw.to_lowercase();
            if lower == "file:" || lower == "files:" {
                query.files_only = true;
                continue;
            }
            if lower == "folder:" || lower == "folders:" {
                query.folders_only = true;
                continue;
            }
            if let Some(list) = lower.strip_prefix("ext:") {
                query.extensions.extend(
                    list.split([';', ',', '|'])
                        .map(|e| {
                            e.trim()
                                .trim_start_matches("*.")
                                .trim_start_matches('.')
                                .to_string()
                        })
                        .filter(|e| !e.is_empty()),
                );
                continue;
            }
            let (negate, body) = match lower.strip_prefix('!') {
                Some(rest) if !rest.is_empty() => (true, rest.to_string()),
                _ => (false, lower),
            };
            if body.is_empty() {
                continue;
            }
            let kind = if body.contains('\\') || body.contains('/') {
                TermKind::Path
            } else if body.contains('*') || body.contains('?') {
                TermKind::Wildcard
            } else {
                TermKind::Contains
            };
            let text = if kind == TermKind::Path {
                body.replace('/', "\\")
            } else {
                body
            };
            query.terms.push(Term { text, kind, negate });
        }
        query
    }

    pub fn is_empty(&self) -> bool {
        self.terms.iter().all(|t| t.negate)
            && self.extensions.is_empty()
            && !self.files_only
            && !self.folders_only
    }
}

fn tokenize(text: &str) -> Vec<String> {
    let mut tokens = Vec::new();
    let mut current = String::new();
    let mut quoted = false;
    for c in text.chars() {
        match c {
            '"' => quoted = !quoted,
            c if c.is_whitespace() && !quoted => {
                if !current.is_empty() {
                    tokens.push(std::mem::take(&mut current));
                }
            }
            c => current.push(c),
        }
    }
    if !current.is_empty() {
        tokens.push(current);
    }
    tokens
}

/// Case-insensitive match of `name` against a lower-case `*` / `?` pattern.
pub fn wildcard_match(pattern: &str, name: &str) -> bool {
    if name.is_ascii() {
        return glob(pattern.as_bytes(), name.as_bytes(), |b| {
            b.to_ascii_lowercase()
        });
    }
    let p: Vec<char> = pattern.chars().collect();
    let n: Vec<char> = name.to_lowercase().chars().collect();
    glob(&p, &n, |c| c)
}

/// Case-insensitive substring test; `needle` must be lower-case. Allocation-free for ASCII names.
pub fn contains_ci(hay: &str, needle: &str) -> bool {
    if !hay.is_ascii() {
        return hay.to_lowercase().contains(needle);
    }
    let (h, n) = (hay.as_bytes(), needle.as_bytes());
    if n.is_empty() {
        return true;
    }
    if n.len() > h.len() {
        return false;
    }
    (0..=h.len() - n.len()).any(|s| {
        h[s..s + n.len()]
            .iter()
            .zip(n)
            .all(|(a, b)| a.to_ascii_lowercase() == *b)
    })
}

fn glob<T: PartialEq + Copy + From<u8>>(p: &[T], n: &[T], fold: impl Fn(T) -> T) -> bool {
    let star = T::from(b'*');
    let any = T::from(b'?');
    let (mut pi, mut ni) = (0usize, 0usize);
    let (mut star_at, mut mark) = (None::<usize>, 0usize);
    while ni < n.len() {
        if pi < p.len() && (p[pi] == any || p[pi] == fold(n[ni])) {
            pi += 1;
            ni += 1;
        } else if pi < p.len() && p[pi] == star {
            star_at = Some(pi);
            mark = ni;
            pi += 1;
        } else if let Some(s) = star_at {
            pi = s + 1;
            mark += 1;
            ni = mark;
        } else {
            return false;
        }
    }
    while pi < p.len() && p[pi] == star {
        pi += 1;
    }
    pi == p.len()
}

/// The in-memory name table.
#[derive(Default)]
pub struct NameIndex {
    pub roots: Vec<RootInfo>,
    /// Unix seconds when the index was built.
    pub built: i64,
    pub build_ms: u64,
    /// All names, back to back; `offsets[i]` is where name i starts.
    text: String,
    offsets: Vec<u32>,
    parent: Vec<u32>,
    flags: Vec<u8>,
    size: Vec<u64>,
    mtime: Vec<i64>,
}

/// Raw record before parents are resolved.
pub(crate) struct Raw {
    pub key: u64,
    pub parent: u64,
    pub name: String,
    pub attrs: u32,
    pub size: u64,
    pub mtime: i64,
    /// size and mtime are known
    pub stat: bool,
}

/// Key -> record position. Keys from the MFT and the folder walk are dense, so a vector is used when possible.
enum Slots {
    Dense(Vec<u32>),
    Sparse(HashMap<u64, u32>),
}

impl Slots {
    fn new(records: &[Raw]) -> Slots {
        let max = records.iter().map(|r| r.key).max().unwrap_or(0);
        if max <= (records.len() as u64) * 4 + 1024 {
            let mut v = vec![NONE; max as usize + 1];
            for (i, r) in records.iter().enumerate() {
                v[r.key as usize] = i as u32;
            }
            Slots::Dense(v)
        } else {
            Slots::Sparse(
                records
                    .iter()
                    .enumerate()
                    .map(|(i, r)| (r.key, i as u32))
                    .collect(),
            )
        }
    }

    fn get(&self, key: u64) -> Option<u32> {
        match self {
            Slots::Dense(v) => v.get(key as usize).copied().filter(|&i| i != NONE),
            Slots::Sparse(m) => m.get(&key).copied(),
        }
    }
}

impl NameIndex {
    pub fn len(&self) -> usize {
        self.offsets.len()
    }

    pub fn is_empty(&self) -> bool {
        self.offsets.is_empty()
    }

    fn name(&self, i: u32) -> &str {
        let start = self.offsets[i as usize] as usize;
        let end = self
            .offsets
            .get(i as usize + 1)
            .map_or(self.text.len(), |&e| e as usize);
        &self.text[start..end]
    }

    pub fn folder_count(&self) -> u64 {
        self.flags
            .iter()
            .filter(|f| *f & FLAG_DIR != 0 && *f & FLAG_ROOT == 0)
            .count() as u64
    }

    pub fn file_count(&self) -> u64 {
        self.flags.iter().filter(|f| *f & FLAG_DIR == 0).count() as u64
    }

    /// Builds an index of the given roots (drives such as `C:\` or folders). Nested roots are merged into
    /// the outer one. NTFS drive roots are read from the MFT when the process may open the volume
    /// (Administrator); otherwise, and for folders, the tree is walked without following junctions or links.
    pub fn build(roots: &[String], ctrl: &BuildControl) -> Result<NameIndex> {
        let start = Instant::now();
        let roots = normalize_roots(roots)?;
        let mut index = NameIndex::default();
        for root in &roots {
            if ctrl.cancel.load(Ordering::Relaxed) {
                return Err(NexusError::Index("Cancelled".into()));
            }
            let before = index.len();
            let mut info = RootInfo {
                path: root.clone(),
                method: "scan".into(),
                entries: 0,
                error: None,
            };
            let mut done = false;
            if is_drive_root(root) && is_ntfs(root) {
                // Raw MFT first (fastest, with sizes, dates and hard links), then the Windows enumeration API.
                let records = super::mft_raw::read_volume(root, ctrl).or_else(|raw_err| {
                    if ctrl.cancel.load(Ordering::Relaxed) {
                        return Err(raw_err);
                    }
                    mft_records(root, ctrl).map_err(|_| raw_err)
                });
                match records {
                    Ok(records) => {
                        index.add_tree(root, records, 5);
                        info.method = "mft".into();
                        done = true;
                    }
                    Err(e) => info.error = Some(format!("{e} — scanned folders instead")),
                }
            }
            if ctrl.cancel.load(Ordering::Relaxed) {
                return Err(NexusError::Index("Cancelled".into()));
            }
            if !done {
                let records = walk_records(Path::new(root), ctrl);
                index.add_tree(root, records, 0);
            }
            info.entries = (index.len() - before).saturating_sub(1) as u64;
            index.roots.push(info);
        }
        if ctrl.cancel.load(Ordering::Relaxed) {
            return Err(NexusError::Index("Cancelled".into()));
        }
        index.built = unix_now();
        index.build_ms = start.elapsed().as_millis() as u64;
        Ok(index)
    }

    /// Appends one root and its records. `root_key` is the key the records use for the root itself.
    fn add_tree(&mut self, root: &str, mut records: Vec<Raw>, root_key: u64) {
        records.retain(|r| r.key != root_key);
        let root_idx = self.push(root, NONE, FLAG_DIR | FLAG_ROOT, 0, UNKNOWN_TIME);
        let base = self.len() as u32;
        let slot = Slots::new(&records);
        // Resolve each record to a position; drop records whose parent chain does not reach the root
        // (NTFS metadata, orphans) and guard against cycles.
        const UNRESOLVED: u32 = u32::MAX - 1;
        const DROPPED: u32 = u32::MAX - 2;
        let mut keep = vec![UNRESOLVED; records.len()];
        let mut order: Vec<u32> = Vec::with_capacity(records.len());
        let mut stack: Vec<u32> = Vec::new();
        for i in 0..records.len() as u32 {
            if keep[i as usize] != UNRESOLVED {
                continue;
            }
            stack.clear();
            let mut cur = i;
            let outcome = loop {
                let state = keep[cur as usize];
                if state == DROPPED {
                    break false;
                }
                if state != UNRESOLVED {
                    break true;
                }
                if stack.len() > 4096 || stack.contains(&cur) {
                    break false;
                }
                stack.push(cur);
                let p = records[cur as usize].parent;
                if p == root_key {
                    break true;
                }
                match slot.get(p) {
                    Some(next) => cur = next,
                    None => break false,
                }
            };
            for &s in stack.iter().rev() {
                if outcome {
                    keep[s as usize] = 0; // placeholder: assigned below in parent-first order
                    order.push(s);
                } else {
                    keep[s as usize] = DROPPED;
                }
            }
        }
        // `order` lists parents before children; assign final positions.
        let mut position = vec![NONE; records.len()];
        for (n, &i) in order.iter().enumerate() {
            position[i as usize] = base + n as u32;
        }
        let bytes: usize = order.iter().map(|&i| records[i as usize].name.len()).sum();
        self.reserve(order.len(), bytes);
        for &i in &order {
            let r = &records[i as usize];
            let parent = if r.parent == root_key {
                root_idx
            } else {
                position[slot.get(r.parent).expect("resolved parent") as usize]
            };
            let mut flags = 0u8;
            if r.attrs & ATTR_DIRECTORY != 0 {
                flags |= FLAG_DIR;
            }
            if r.attrs & ATTR_HIDDEN != 0 {
                flags |= FLAG_HIDDEN;
            }
            if r.attrs & ATTR_SYSTEM != 0 {
                flags |= FLAG_SYSTEM;
            }
            if !r.stat {
                flags |= FLAG_NO_STAT;
            }
            self.push(&r.name, parent, flags, r.size, r.mtime);
        }
    }

    fn reserve(&mut self, n: usize, bytes: usize) {
        self.text.reserve(bytes);
        self.offsets.reserve(n);
        self.parent.reserve(n);
        self.flags.reserve(n);
        self.size.reserve(n);
        self.mtime.reserve(n);
    }

    fn push(&mut self, name: &str, parent: u32, flags: u8, size: u64, mtime: i64) -> u32 {
        let idx = self.offsets.len() as u32;
        // Names are stored back to back; offsets are u32, so the table holds up to 4 GB of names.
        self.offsets
            .push(self.text.len().min(u32::MAX as usize) as u32);
        self.text.push_str(name);
        self.parent.push(parent);
        self.flags.push(flags);
        self.size.push(size);
        self.mtime.push(mtime);
        idx
    }

    /// Full path of an entry.
    pub fn full_path(&self, idx: u32) -> String {
        let mut chain = Vec::new();
        let mut cur = idx;
        while cur != NONE && chain.len() < 4096 {
            chain.push(cur);
            cur = self.parent[cur as usize];
        }
        let mut path = String::new();
        for &i in chain.iter().rev() {
            if !path.is_empty() && !path.ends_with('\\') {
                path.push('\\');
            }
            path.push_str(self.name(i));
        }
        path
    }

    fn lower_path(&self, idx: u32) -> String {
        self.full_path(idx).to_lowercase()
    }

    /// Runs a query. Results are ranked: exact name, then names starting with the first term, then names
    /// where it starts a word, then anywhere; ties go to shorter names.
    pub fn search(&self, query: &Query, limit: usize) -> SearchOutcome {
        self.search_sorted(query, limit, SortBy::Relevance)
    }

    /// Runs a query with a chosen order. With [`SortBy::Size`] or [`SortBy::Modified`] an empty query
    /// matches everything, e.g. the largest files of all indexed drives.
    pub fn search_sorted(&self, query: &Query, limit: usize, sort: SortBy) -> SearchOutcome {
        let start = Instant::now();
        if query.is_empty() && sort == SortBy::Relevance {
            return SearchOutcome {
                total: 0,
                ms: 0,
                results: Vec::new(),
            };
        }
        let first = query
            .terms
            .iter()
            .find(|t| !t.negate && t.kind == TermKind::Contains)
            .map(|t| t.text.as_str());
        // Cheap checks first: building a full path is the expensive part.
        let mut terms: Vec<&Term> = query.terms.iter().collect();
        terms.sort_by_key(|t| match t.kind {
            TermKind::Contains => 0,
            TermKind::Wildcard => 1,
            TermKind::Path => 2,
        });
        let mut matches: Vec<(u64, u32)> = (0..self.len() as u32)
            .into_par_iter()
            .filter_map(|i| {
                let flags = self.flags[i as usize];
                if flags & FLAG_ROOT != 0 {
                    return None;
                }
                let is_dir = flags & FLAG_DIR != 0;
                if (query.files_only && is_dir) || (query.folders_only && !is_dir) {
                    return None;
                }
                let name = self.name(i);
                if !query.extensions.is_empty() {
                    if is_dir {
                        return None;
                    }
                    let ext = name.rsplit_once('.').map(|(_, e)| e).unwrap_or("");
                    if !query
                        .extensions
                        .iter()
                        .any(|e| ext.eq_ignore_ascii_case(e) || ext.to_lowercase() == *e)
                    {
                        return None;
                    }
                }
                let mut path_cache: Option<String> = None;
                for term in &terms {
                    let hit = match term.kind {
                        TermKind::Contains => contains_ci(name, &term.text),
                        TermKind::Wildcard => wildcard_match(&term.text, name),
                        TermKind::Path => {
                            let p = path_cache.get_or_insert_with(|| self.lower_path(i));
                            p.contains(term.text.as_str())
                        }
                    };
                    if hit == term.negate {
                        return None;
                    }
                }
                match sort {
                    SortBy::Size => return Some((u64::MAX - self.size[i as usize], i)),
                    SortBy::Modified => {
                        let m = self.mtime[i as usize];
                        let key = if m == UNKNOWN_TIME {
                            u64::MAX
                        } else {
                            (i64::MAX - m) as u64
                        };
                        return Some((key, i));
                    }
                    SortBy::Relevance => {}
                }
                let name = name.to_lowercase(); // matches only: ranking needs the folded name
                let name = name.as_str();
                let rank: u64 = match first {
                    Some(t) if name == t => 0,
                    Some(t) if name.starts_with(t) => 1,
                    Some(t)
                        if name.match_indices(t).any(|(at, _)| {
                            name[..at]
                                .chars()
                                .next_back()
                                .is_some_and(|c| !c.is_alphanumeric())
                        }) =>
                    {
                        2
                    }
                    Some(_) => 3,
                    None => 1,
                };
                Some(((rank << 32) | name.chars().count() as u64, i))
            })
            .collect();
        let total = matches.len() as u64;
        let limit = limit.clamp(1, 10_000);
        if matches.len() > limit {
            matches.select_nth_unstable(limit - 1);
            matches.truncate(limit);
        }
        matches.sort_unstable();
        let results = matches.par_iter().map(|&(_, i)| self.hit(i)).collect();
        SearchOutcome {
            total,
            ms: start.elapsed().as_millis() as u64,
            results,
        }
    }

    fn hit(&self, idx: u32) -> Hit {
        let path = self.full_path(idx);
        let folder = self.full_path(self.parent[idx as usize]);
        let flags = self.flags[idx as usize];
        let mut size = self.size[idx as usize];
        let mut modified = self.mtime[idx as usize];
        let mut missing = false;
        if flags & FLAG_NO_STAT != 0 {
            match fs::symlink_metadata(long_path(&path)) {
                Ok(meta) => {
                    size = if meta.is_dir() { 0 } else { meta.len() };
                    modified = meta.modified().map(to_unix).unwrap_or(UNKNOWN_TIME);
                }
                Err(_) => missing = true,
            }
        } else if fs::symlink_metadata(long_path(&path)).is_err() {
            missing = true;
        }
        Hit {
            name: self.name(idx).to_string(),
            path,
            folder,
            is_dir: flags & FLAG_DIR != 0,
            size,
            modified: (modified != UNKNOWN_TIME).then_some(modified),
            missing,
        }
    }

    /// Saves the index to a file (written to a temporary file first, then moved into place).
    pub fn save(&self, file: &Path) -> Result<()> {
        if let Some(dir) = file.parent() {
            fs::create_dir_all(dir)?;
        }
        let tmp = file.with_extension("tmp");
        {
            let mut w = BufWriter::with_capacity(1 << 20, fs::File::create(&tmp)?);
            w.write_all(MAGIC)?;
            w.write_all(&self.built.to_le_bytes())?;
            w.write_all(&self.build_ms.to_le_bytes())?;
            w.write_all(&(self.roots.len() as u32).to_le_bytes())?;
            for r in &self.roots {
                write_str(&mut w, &r.path)?;
                write_str(&mut w, &r.method)?;
                w.write_all(&r.entries.to_le_bytes())?;
                write_str(&mut w, r.error.as_deref().unwrap_or(""))?;
            }
            w.write_all(&(self.len() as u32).to_le_bytes())?;
            for i in 0..self.len() {
                w.write_all(&self.parent[i].to_le_bytes())?;
                w.write_all(&[self.flags[i]])?;
                w.write_all(&self.size[i].to_le_bytes())?;
                w.write_all(&self.mtime[i].to_le_bytes())?;
                write_str(&mut w, self.name(i as u32))?;
            }
            w.flush()?;
        }
        fs::rename(&tmp, file)?;
        Ok(())
    }

    /// Loads an index saved by [`NameIndex::save`].
    pub fn load(file: &Path) -> Result<NameIndex> {
        let mut r = BufReader::with_capacity(1 << 20, fs::File::open(file)?);
        let mut magic = [0u8; 8];
        r.read_exact(&mut magic)?;
        if &magic != MAGIC {
            return Err(NexusError::Index(
                "Not a Winhance-FS search index (or an older format)".into(),
            ));
        }
        let mut index = NameIndex {
            built: read_i64(&mut r)?,
            build_ms: read_u64(&mut r)?,
            ..Default::default()
        };
        let roots = read_u32(&mut r)?;
        for _ in 0..roots {
            let path = read_str(&mut r)?;
            let method = read_str(&mut r)?;
            let entries = read_u64(&mut r)?;
            let error = read_str(&mut r)?;
            index.roots.push(RootInfo {
                path,
                method,
                entries,
                error: (!error.is_empty()).then_some(error),
            });
        }
        let count = read_u32(&mut r)? as usize;
        if count > 200_000_000 {
            return Err(NexusError::Index("Index file is damaged".into()));
        }
        for i in 0..count {
            let parent = read_u32(&mut r)?;
            let mut flags = [0u8; 1];
            r.read_exact(&mut flags)?;
            let size = read_u64(&mut r)?;
            let mtime = read_i64(&mut r)?;
            let name = read_str(&mut r)?;
            if parent != NONE && parent as usize >= i {
                return Err(NexusError::Index("Index file is damaged".into()));
            }
            index.push(&name, parent, flags[0], size, mtime);
        }
        Ok(index)
    }
}

fn write_str<W: Write>(w: &mut W, s: &str) -> std::io::Result<()> {
    let bytes = s.as_bytes();
    w.write_all(&(bytes.len() as u32).to_le_bytes())?;
    w.write_all(bytes)
}

fn read_u32<R: Read>(r: &mut R) -> std::io::Result<u32> {
    let mut b = [0u8; 4];
    r.read_exact(&mut b)?;
    Ok(u32::from_le_bytes(b))
}

fn read_u64<R: Read>(r: &mut R) -> std::io::Result<u64> {
    let mut b = [0u8; 8];
    r.read_exact(&mut b)?;
    Ok(u64::from_le_bytes(b))
}

fn read_i64<R: Read>(r: &mut R) -> std::io::Result<i64> {
    Ok(read_u64(r)? as i64)
}

fn read_str<R: Read>(r: &mut R) -> Result<String> {
    let len = read_u32(r)? as usize;
    if len > 65_536 {
        return Err(NexusError::Index("Index file is damaged".into()));
    }
    let mut buf = vec![0u8; len];
    r.read_exact(&mut buf)?;
    String::from_utf8(buf).map_err(|_| NexusError::Index("Index file is damaged".into()))
}

fn unix_now() -> i64 {
    to_unix(SystemTime::now())
}

fn to_unix(t: SystemTime) -> i64 {
    match t.duration_since(UNIX_EPOCH) {
        Ok(d) => d.as_secs() as i64,
        Err(e) => -(e.duration().as_secs() as i64),
    }
}

/// `\\?\` prefix for paths longer than MAX_PATH.
fn long_path(path: &str) -> PathBuf {
    if path.len() >= 248 && !path.starts_with(r"\\") {
        PathBuf::from(format!(r"\\?\{path}"))
    } else {
        PathBuf::from(path)
    }
}

fn is_drive_root(root: &str) -> bool {
    let b = root.as_bytes();
    b.len() == 3 && b[0].is_ascii_alphabetic() && b[1] == b':' && b[2] == b'\\'
}

/// Canonical form: absolute, backslashes, drive roots as `C:\`, folders without a trailing slash; nested
/// and duplicate roots removed.
fn normalize_roots(roots: &[String]) -> Result<Vec<String>> {
    let mut out: Vec<String> = Vec::new();
    for root in roots {
        let mut r = root.trim().replace('/', "\\");
        if r.is_empty() {
            continue;
        }
        if r.len() == 2 && r.ends_with(':') {
            r.push('\\');
        }
        if r.len() > 3 {
            while r.ends_with('\\') {
                r.pop();
            }
        }
        if r.as_bytes().get(1) == Some(&b':') {
            r = r[..1].to_uppercase() + &r[1..];
        }
        let meta = fs::metadata(&r).map_err(|e| NexusError::InvalidPath(format!("{r}: {e}")))?;
        if !meta.is_dir() {
            return Err(NexusError::InvalidPath(format!(
                "{r} is not a folder or drive"
            )));
        }
        out.push(r);
    }
    if out.is_empty() {
        return Err(NexusError::InvalidPath(
            "Choose at least one drive or folder to index".into(),
        ));
    }
    out.sort_by_key(|r| r.len());
    let mut kept: Vec<String> = Vec::new();
    for r in out {
        let lower = r.to_lowercase();
        let nested = kept.iter().any(|k| {
            let k = k.to_lowercase();
            lower == k
                || lower.starts_with(&if k.ends_with('\\') {
                    k.clone()
                } else {
                    format!("{k}\\")
                })
        });
        if !nested {
            kept.push(r);
        }
    }
    Ok(kept)
}

/// Walks a folder tree in parallel. Junctions and symbolic links are listed but never followed.
fn walk_records(root: &Path, ctrl: &BuildControl) -> Vec<Raw> {
    struct Node {
        name: String,
        attrs: u32,
        size: u64,
        mtime: i64,
        children: Vec<Node>,
    }

    fn read(dir: &Path, ctrl: &BuildControl) -> Vec<Node> {
        if ctrl.cancel.load(Ordering::Relaxed) {
            return Vec::new();
        }
        let Ok(entries) = fs::read_dir(dir) else {
            return Vec::new();
        };
        let mut files = Vec::new();
        let mut dirs = Vec::new();
        for entry in entries.flatten() {
            let Ok(meta) = entry.metadata() else { continue };
            let attrs = attributes(&meta);
            let node = Node {
                name: entry.file_name().to_string_lossy().into_owned(),
                attrs,
                size: if meta.is_dir() { 0 } else { meta.len() },
                mtime: meta.modified().map(to_unix).unwrap_or(UNKNOWN_TIME),
                children: Vec::new(),
            };
            if attrs & ATTR_DIRECTORY != 0 && attrs & ATTR_REPARSE == 0 {
                dirs.push((entry.path(), node));
            } else {
                files.push(node);
            }
        }
        ctrl.progress
            .fetch_add((files.len() + dirs.len()) as u64, Ordering::Relaxed);
        let dirs: Vec<Node> = dirs
            .into_par_iter()
            .map(|(path, mut node)| {
                node.children = read(&path, ctrl);
                node
            })
            .collect();
        files.extend(dirs);
        files
    }

    fn flatten(nodes: Vec<Node>, parent: u64, next: &mut u64, out: &mut Vec<Raw>) {
        for node in nodes {
            *next += 1;
            let key = *next;
            out.push(Raw {
                key,
                parent,
                name: node.name,
                attrs: node.attrs,
                size: node.size,
                mtime: node.mtime,
                stat: true,
            });
            flatten(node.children, key, next, out);
        }
    }

    let tree = read(root, ctrl);
    let mut out = Vec::new();
    let mut next = 0u64;
    flatten(tree, 0, &mut next, &mut out);
    out
}

#[cfg(windows)]
fn attributes(meta: &fs::Metadata) -> u32 {
    use std::os::windows::fs::MetadataExt;
    meta.file_attributes()
}

#[cfg(not(windows))]
fn attributes(meta: &fs::Metadata) -> u32 {
    let mut a = 0;
    if meta.is_dir() {
        a |= ATTR_DIRECTORY;
    }
    if meta.file_type().is_symlink() {
        a |= ATTR_REPARSE;
    }
    a
}

#[cfg(windows)]
fn is_ntfs(root: &str) -> bool {
    use windows::core::PCWSTR;
    use windows::Win32::Storage::FileSystem::GetVolumeInformationW;
    let wide: Vec<u16> = root.encode_utf16().chain(std::iter::once(0)).collect();
    let mut fs_name = [0u16; 32];
    let ok = unsafe {
        GetVolumeInformationW(
            PCWSTR(wide.as_ptr()),
            None,
            None,
            None,
            None,
            Some(&mut fs_name),
        )
    };
    if ok.is_err() {
        return false;
    }
    let end = fs_name
        .iter()
        .position(|&c| c == 0)
        .unwrap_or(fs_name.len());
    String::from_utf16_lossy(&fs_name[..end]).eq_ignore_ascii_case("NTFS")
}

#[cfg(not(windows))]
fn is_ntfs(_root: &str) -> bool {
    false
}

/// Lists every file record of an NTFS volume with FSCTL_ENUM_USN_DATA. Needs Administrator rights.
#[cfg(windows)]
fn mft_records(root: &str, ctrl: &BuildControl) -> Result<Vec<Raw>> {
    use std::mem::size_of;
    use windows::core::PCWSTR;
    use windows::Win32::Foundation::{CloseHandle, ERROR_HANDLE_EOF};
    use windows::Win32::Storage::FileSystem::{
        CreateFileW, FILE_FLAG_BACKUP_SEMANTICS, FILE_SHARE_DELETE, FILE_SHARE_READ,
        FILE_SHARE_WRITE, OPEN_EXISTING,
    };
    use windows::Win32::System::Ioctl::FSCTL_ENUM_USN_DATA;
    use windows::Win32::System::IO::DeviceIoControl;

    let volume: Vec<u16> = format!(r"\\.\{}", &root[..2])
        .encode_utf16()
        .chain(std::iter::once(0))
        .collect();
    let handle = unsafe {
        CreateFileW(
            PCWSTR(volume.as_ptr()),
            0x8000_0000, // GENERIC_READ
            FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
            None,
            OPEN_EXISTING,
            FILE_FLAG_BACKUP_SEMANTICS,
            None,
        )
    }
    .map_err(|_| {
        NexusError::PermissionDenied("reading the drive table needs Administrator rights".into())
    })?;

    struct Guard(windows::Win32::Foundation::HANDLE);
    impl Drop for Guard {
        fn drop(&mut self) {
            let _ = unsafe { CloseHandle(self.0) };
        }
    }
    let _guard = Guard(handle);

    #[repr(C)]
    struct MftEnumDataV0 {
        start_file_reference_number: u64,
        low_usn: i64,
        high_usn: i64,
    }
    let mut input = MftEnumDataV0 {
        start_file_reference_number: 0,
        low_usn: 0,
        high_usn: i64::MAX,
    };
    let mut buffer = vec![0u64; 128 * 1024]; // 1 MB, 8-byte aligned
    let buffer_bytes = buffer.len() * 8;
    let mut records = Vec::with_capacity(1 << 20);
    const MASK: u64 = 0x0000_FFFF_FFFF_FFFF;

    loop {
        if ctrl.cancel.load(Ordering::Relaxed) {
            return Err(NexusError::Index("Cancelled".into()));
        }
        let mut returned = 0u32;
        let ok = unsafe {
            DeviceIoControl(
                handle,
                FSCTL_ENUM_USN_DATA,
                Some(&input as *const _ as *const _),
                size_of::<MftEnumDataV0>() as u32,
                Some(buffer.as_mut_ptr() as *mut _),
                buffer_bytes as u32,
                Some(&mut returned),
                None,
            )
        };
        if let Err(e) = ok {
            if e.code() == ERROR_HANDLE_EOF.to_hresult() {
                break;
            }
            if records.is_empty() {
                return Err(NexusError::Windows(format!(
                    "the drive table could not be read ({e})"
                )));
            }
            break;
        }
        let returned = returned as usize;
        if returned <= 8 {
            break;
        }
        let bytes = unsafe { std::slice::from_raw_parts(buffer.as_ptr() as *const u8, returned) };
        let next = u64::from_le_bytes(bytes[0..8].try_into().unwrap());
        let mut offset = 8usize;
        while offset + 60 <= returned {
            let rec = &bytes[offset..];
            let len = u32::from_le_bytes(rec[0..4].try_into().unwrap()) as usize;
            if len < 60 || offset + len > returned {
                break;
            }
            let major = u16::from_le_bytes(rec[4..6].try_into().unwrap());
            if major == 2 {
                let frn = u64::from_le_bytes(rec[8..16].try_into().unwrap()) & MASK;
                let parent = u64::from_le_bytes(rec[16..24].try_into().unwrap()) & MASK;
                let attrs = u32::from_le_bytes(rec[52..56].try_into().unwrap());
                let name_len = u16::from_le_bytes(rec[56..58].try_into().unwrap()) as usize;
                let name_off = u16::from_le_bytes(rec[58..60].try_into().unwrap()) as usize;
                if name_off >= 60 && name_off + name_len <= len && name_len > 0 {
                    let units: Vec<u16> = rec[name_off..name_off + name_len]
                        .as_chunks::<2>()
                        .0
                        .iter()
                        .map(|c| u16::from_le_bytes(*c))
                        .collect();
                    records.push(Raw {
                        key: frn,
                        parent,
                        name: String::from_utf16_lossy(&units),
                        attrs,
                        size: 0,
                        mtime: UNKNOWN_TIME,
                        stat: false,
                    });
                }
            }
            offset += len;
        }
        ctrl.progress.store(records.len() as u64, Ordering::Relaxed);
        if next == input.start_file_reference_number {
            break;
        }
        input.start_file_reference_number = next;
    }
    Ok(records)
}

#[cfg(not(windows))]
fn mft_records(_root: &str, _ctrl: &BuildControl) -> Result<Vec<Raw>> {
    Err(NexusError::Windows("only available on Windows".into()))
}

#[cfg(test)]
mod tests {
    use super::*;

    fn sandbox() -> tempfile::TempDir {
        let dir = tempfile::tempdir().unwrap();
        let p = dir.path();
        fs::create_dir_all(p.join(r"Projects\winhance\src")).unwrap();
        fs::create_dir_all(p.join(r"Photos\2024 Trip")).unwrap();
        fs::write(p.join(r"Projects\winhance\README.md"), "readme").unwrap();
        fs::write(p.join(r"Projects\winhance\src\main.rs"), "fn main() {}").unwrap();
        fs::write(p.join(r"Projects\notes readme.txt"), "x").unwrap();
        fs::write(p.join(r"Photos\2024 Trip\IMG_0001.jpg"), vec![0u8; 2048]).unwrap();
        fs::write(p.join(r"Photos\2024 Trip\IMG_0002.JPG"), vec![0u8; 10]).unwrap();
        fs::write(p.join(r"Photos\Tax Return 2024.pdf"), "pdf").unwrap();
        dir
    }

    fn build(dir: &Path) -> NameIndex {
        NameIndex::build(
            &[dir.to_string_lossy().into_owned()],
            &BuildControl::default(),
        )
        .unwrap()
    }

    fn names(index: &NameIndex, q: &str) -> Vec<String> {
        index
            .search(&Query::parse(q), 100)
            .results
            .into_iter()
            .map(|h| h.name)
            .collect()
    }

    #[test]
    fn parse_rules() {
        let q = Query::parse(r#"tax "return 2024" !draft ext:pdf;.docx *.iso projects\ folder:"#);
        assert_eq!(q.terms.len(), 5);
        assert_eq!(q.terms[1].text, "return 2024");
        assert!(q.terms[2].negate);
        assert_eq!(q.extensions, vec!["pdf", "docx"]);
        assert_eq!(q.terms[3].kind, TermKind::Wildcard);
        assert_eq!(q.terms[4].kind, TermKind::Path);
        assert!(q.folders_only);
    }

    #[test]
    fn wildcards() {
        assert!(wildcard_match("*.jpg", "img_0001.jpg"));
        assert!(wildcard_match("img_????.jpg", "img_0001.jpg"));
        assert!(!wildcard_match("img_???.jpg", "img_0001.jpg"));
        assert!(wildcard_match("*", ""));
        assert!(!wildcard_match("*.png", "a.png.txt"));
        assert!(wildcard_match("*.jpg", "HOLIDAY.JPG"), "case-insensitive");
        assert!(wildcard_match("r?sum?.*", "Résumé.PDF"));
        assert!(contains_ci("Quarterly REPORT.xlsx", "report"));
        assert!(contains_ci("Résumé 2024", "résumé"));
        assert!(!contains_ci("abc", "abcd"));
    }

    #[test]
    fn walk_and_search() {
        let dir = sandbox();
        let index = build(dir.path());
        assert_eq!(index.roots[0].method, "scan");
        assert_eq!(index.file_count(), 6);
        assert_eq!(index.folder_count(), 5);

        assert_eq!(
            names(&index, "readme"),
            vec!["README.md", "notes readme.txt"]
        );
        assert_eq!(names(&index, "tax 2024"), vec!["Tax Return 2024.pdf"]);
        assert_eq!(names(&index, "img_????.jpg").len(), 2);
        assert_eq!(
            names(&index, "ext:jpg"),
            vec!["IMG_0001.jpg", "IMG_0002.JPG"]
        );
        assert_eq!(names(&index, "2024 folder:"), vec!["2024 Trip"]);
        assert_eq!(names(&index, "2024 file:"), vec!["Tax Return 2024.pdf"]);
        assert_eq!(names(&index, r"winhance\ .rs"), vec!["main.rs"]);
        assert_eq!(names(&index, "readme !notes"), vec!["README.md"]);
        assert!(names(&index, "nothing-like-this").is_empty());

        let hit = &index.search(&Query::parse("IMG_0001"), 10).results[0];
        assert_eq!(hit.size, 2048);
        assert!(hit.modified.is_some());
        assert!(hit.path.ends_with(r"Photos\2024 Trip\IMG_0001.jpg"));
        assert!(hit.folder.ends_with(r"Photos\2024 Trip"));
        assert!(!hit.missing);
    }

    #[test]
    fn ranking_exact_then_prefix() {
        let dir = tempfile::tempdir().unwrap();
        for n in ["my report.txt", "report", "report-final.txt", "areport.txt"] {
            fs::write(dir.path().join(n), "x").unwrap();
        }
        let index = build(dir.path());
        assert_eq!(
            names(&index, "report"),
            vec!["report", "report-final.txt", "my report.txt", "areport.txt"]
        );
        let largest = index.search_sorted(&Query::parse("file:"), 3, SortBy::Size);
        assert_eq!(largest.results.len(), 3);
        assert!(largest.results.windows(2).all(|w| w[0].size >= w[1].size));
        let everything = index.search_sorted(&Query::default(), 100, SortBy::Modified);
        assert_eq!(
            everything.total, 4,
            "an empty query sorted by date lists everything"
        );
        let limited = index.search(&Query::parse("report"), 2);
        assert_eq!(limited.total, 4);
        assert_eq!(limited.results.len(), 2);
    }

    #[test]
    fn save_load_round_trip_and_missing() {
        let dir = sandbox();
        let index = build(dir.path());
        let file = dir.path().join("index.bin");
        index.save(&file).unwrap();
        let loaded = NameIndex::load(&file).unwrap();
        assert_eq!(loaded.len(), index.len());
        assert_eq!(loaded.roots[0].path, index.roots[0].path);
        assert_eq!(names(&loaded, "tax"), vec!["Tax Return 2024.pdf"]);

        fs::remove_file(dir.path().join(r"Photos\Tax Return 2024.pdf")).unwrap();
        assert!(loaded.search(&Query::parse("tax"), 10).results[0].missing);

        fs::write(&file, b"garbage!").unwrap();
        assert!(NameIndex::load(&file).is_err());
    }

    #[test]
    fn nested_roots_merged_and_bad_root_rejected() {
        let dir = sandbox();
        let outer = dir.path().to_string_lossy().into_owned();
        let inner = dir.path().join("Photos").to_string_lossy().into_owned();
        let index =
            NameIndex::build(&[inner, outer.clone() + "\\"], &BuildControl::default()).unwrap();
        assert_eq!(index.roots.len(), 1);
        assert_eq!(index.roots[0].path, outer);
        assert!(NameIndex::build(&[outer + r"\no-such-folder"], &BuildControl::default()).is_err());
    }

    #[cfg(windows)]
    #[test]
    fn junctions_are_not_followed() {
        let dir = sandbox();
        let link = dir.path().join("loop");
        let status = std::process::Command::new("cmd")
            .args(["/c", "mklink", "/J"])
            .arg(&link)
            .arg(dir.path())
            .output()
            .unwrap();
        assert!(status.status.success());
        let index = build(dir.path());
        assert_eq!(names(&index, "loop"), vec!["loop"]);
        assert_eq!(
            names(&index, "tax").len(),
            1,
            "the junction must not be walked"
        );
    }

    #[test]
    fn cancel_stops_build() {
        let dir = sandbox();
        let ctrl = BuildControl::default();
        ctrl.cancel.store(true, Ordering::Relaxed);
        assert!(NameIndex::build(&[dir.path().to_string_lossy().into_owned()], &ctrl).is_err());
    }
}
