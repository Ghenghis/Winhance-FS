//! JSON-over-FFI API for the name index, used by the Winhance-FS app (C#) and the MCP server.
//!
//! Every function takes and returns UTF-8 JSON. Returned strings are owned by Rust and must be released with
//! `nexus_free_string`. Functions never unwind across the boundary: a panic becomes `{"ok":false,...}`.

use crate::search::name_index::{BuildControl, NameIndex, Query, SortBy};
use once_cell::sync::Lazy;
use parking_lot::RwLock;
use serde::Deserialize;
use serde_json::{json, Value};
use std::ffi::{CStr, CString};
use std::os::raw::c_char;
use std::path::Path;
use std::sync::atomic::Ordering;
use std::sync::Arc;

static INDEX: Lazy<RwLock<Option<Arc<NameIndex>>>> = Lazy::new(|| RwLock::new(None));
static CONTROL: Lazy<RwLock<Arc<BuildControl>>> =
    Lazy::new(|| RwLock::new(Arc::new(BuildControl::default())));

fn reply(value: Value) -> *mut c_char {
    let text = value.to_string().replace('\0', "");
    CString::new(text)
        .map(CString::into_raw)
        .unwrap_or(std::ptr::null_mut())
}

fn failure(message: impl Into<String>) -> Value {
    json!({ "ok": false, "error": message.into() })
}

fn guarded(f: impl FnOnce() -> Value + std::panic::UnwindSafe) -> *mut c_char {
    match std::panic::catch_unwind(f) {
        Ok(v) => reply(v),
        Err(_) => reply(failure("Internal error in the search engine")),
    }
}

fn read_json<'a, T: Deserialize<'a>>(text: &'a str) -> Result<T, Value> {
    serde_json::from_str(text).map_err(|e| failure(format!("Bad request: {e}")))
}

unsafe fn input<'a>(ptr: *const c_char) -> Result<&'a str, Value> {
    if ptr.is_null() {
        return Err(failure("Missing request"));
    }
    CStr::from_ptr(ptr)
        .to_str()
        .map_err(|_| failure("Request is not UTF-8"))
}

fn info(index: &NameIndex) -> Value {
    json!({
        "ok": true,
        "entries": index.len(),
        "files": index.file_count(),
        "folders": index.folder_count(),
        "built": index.built,
        "ms": index.build_ms,
        "roots": index.roots,
    })
}

/// Engine version, e.g. `{"ok":true,"version":"1.5.0"}`.
#[no_mangle]
pub extern "C" fn nexus_engine_version() -> *mut c_char {
    reply(json!({ "ok": true, "version": env!("CARGO_PKG_VERSION"), "format": "WFSIDX01" }))
}

#[derive(Deserialize)]
struct BuildRequest {
    roots: Vec<String>,
    #[serde(default)]
    save_to: Option<String>,
}

/// Builds an index. Request: `{"roots":["C:\\","D:\\Data"],"save_to":"...\\index.bin"}`.
/// Replaces the current index only when the build completes. Reply: index info, or `cancelled:true`.
///
/// # Safety
/// `request` must be a valid NUL-terminated UTF-8 string.
#[no_mangle]
pub unsafe extern "C" fn nexus_index_build(request: *const c_char) -> *mut c_char {
    let text = match input(request) {
        Ok(t) => t.to_owned(),
        Err(e) => return reply(e),
    };
    guarded(move || {
        let req: BuildRequest = match read_json(&text) {
            Ok(r) => r,
            Err(e) => return e,
        };
        let ctrl = Arc::new(BuildControl::default());
        *CONTROL.write() = ctrl.clone();
        let index = match NameIndex::build(&req.roots, &ctrl) {
            Ok(i) => i,
            Err(e) if ctrl.cancel.load(Ordering::Relaxed) => {
                return json!({ "ok": false, "cancelled": true, "error": e.to_string() })
            }
            Err(e) => return failure(e.to_string()),
        };
        let mut out = info(&index);
        if let Some(file) = req.save_to.filter(|f| !f.is_empty()) {
            if let Err(e) = index.save(Path::new(&file)) {
                out["saveError"] = json!(e.to_string());
            }
        }
        *INDEX.write() = Some(Arc::new(index));
        out
    })
}

/// Cancels a running build.
#[no_mangle]
pub extern "C" fn nexus_index_cancel() {
    CONTROL.read().cancel.store(true, Ordering::Relaxed);
}

/// Entries found so far by a running build.
#[no_mangle]
pub extern "C" fn nexus_index_progress() -> u64 {
    CONTROL.read().progress.load(Ordering::Relaxed)
}

/// Loads a saved index. Request: path to the index file (plain string, not JSON).
///
/// # Safety
/// `path` must be a valid NUL-terminated UTF-8 string.
#[no_mangle]
pub unsafe extern "C" fn nexus_index_load(path: *const c_char) -> *mut c_char {
    let path = match input(path) {
        Ok(t) => t.to_owned(),
        Err(e) => return reply(e),
    };
    guarded(move || match NameIndex::load(Path::new(&path)) {
        Ok(index) => {
            let out = info(&index);
            *INDEX.write() = Some(Arc::new(index));
            out
        }
        Err(e) => failure(e.to_string()),
    })
}

/// Information about the loaded index, or `{"ok":false}` when none is loaded.
#[no_mangle]
pub extern "C" fn nexus_index_info() -> *mut c_char {
    guarded(|| match INDEX.read().as_ref() {
        Some(index) => info(index),
        None => failure("No index yet"),
    })
}

/// Unloads the index from memory.
#[no_mangle]
pub extern "C" fn nexus_index_unload() {
    *INDEX.write() = None;
}

#[derive(Deserialize)]
struct SearchRequest {
    query: String,
    #[serde(default = "default_limit")]
    limit: usize,
    /// "relevance" (default), "size" (largest first) or "modified" (newest first)
    #[serde(default)]
    sort: Option<String>,
}

fn default_limit() -> usize {
    200
}

/// Searches the loaded index. Request: `{"query":"tax 2024 ext:pdf","limit":200,"sort":"relevance"}`
/// (`sort` may also be `size` or `modified`).
/// Reply: `{"ok":true,"total":N,"ms":..,"results":[{path,name,folder,isDir,size,modified,missing}]}`.
///
/// # Safety
/// `request` must be a valid NUL-terminated UTF-8 string.
#[no_mangle]
pub unsafe extern "C" fn nexus_index_search(request: *const c_char) -> *mut c_char {
    let text = match input(request) {
        Ok(t) => t.to_owned(),
        Err(e) => return reply(e),
    };
    guarded(move || {
        let req: SearchRequest = match read_json(&text) {
            Ok(r) => r,
            Err(e) => return e,
        };
        let Some(index) = INDEX.read().clone() else {
            return failure("No index yet");
        };
        let sort = SortBy::parse(req.sort.as_deref().unwrap_or(""));
        let outcome = index.search_sorted(&Query::parse(&req.query), req.limit, sort);
        let mut out = serde_json::to_value(outcome).unwrap_or_else(|_| json!({}));
        out["ok"] = json!(true);
        out
    })
}

#[cfg(test)]
mod tests {
    use super::*;

    fn call(f: unsafe extern "C" fn(*const c_char) -> *mut c_char, arg: &str) -> Value {
        let c = CString::new(arg).unwrap();
        let ptr = unsafe { f(c.as_ptr()) };
        let text = unsafe { CStr::from_ptr(ptr) }.to_str().unwrap().to_owned();
        crate::ffi::nexus_free_string(ptr);
        serde_json::from_str(&text).unwrap()
    }

    #[test]
    fn build_search_load_via_ffi() {
        let dir = tempfile::tempdir().unwrap();
        std::fs::write(dir.path().join("Quarterly Report.xlsx"), "xxxx").unwrap();
        std::fs::write(dir.path().join("notes.txt"), "x").unwrap();
        let file = dir.path().join("idx").join("index.bin");
        let req = json!({ "roots": [dir.path()], "save_to": file }).to_string();
        let built = call(nexus_index_build, &req);
        assert_eq!(built["ok"], true, "{built}");
        assert_eq!(built["files"], 2);
        assert_eq!(built["roots"][0]["method"], "scan");

        let found = call(nexus_index_search, r#"{"query":"report"}"#);
        assert_eq!(found["total"], 1);
        assert_eq!(found["results"][0]["name"], "Quarterly Report.xlsx");
        assert_eq!(found["results"][0]["isDir"], false);

        nexus_index_unload();
        assert_eq!(
            call(nexus_index_search, r#"{"query":"report"}"#)["ok"],
            false
        );
        let loaded = call(nexus_index_load, file.to_str().unwrap());
        assert_eq!(loaded["entries"], built["entries"]);
        assert_eq!(call(nexus_index_search, r#"{"query":"notes"}"#)["total"], 1);

        let big = call(
            nexus_index_search,
            r#"{"query":"","sort":"size","limit":1}"#,
        );
        assert_eq!(big["total"], 2);
        assert_eq!(big["results"][0]["name"], "Quarterly Report.xlsx");

        assert_eq!(call(nexus_index_search, "not json")["ok"], false);
        assert_eq!(
            call(nexus_index_build, r#"{"roots":["Z:\\no\\such"]}"#)["ok"],
            false
        );
    }
}
