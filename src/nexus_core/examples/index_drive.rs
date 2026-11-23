//! Indexes the given roots and runs a few searches: `cargo run --release --example index_drive -- C:\ "query"`.
use nexus_core::search::name_index::{BuildControl, NameIndex, Query};

fn main() {
    let mut args: Vec<String> = std::env::args().skip(1).collect();
    let query = args.pop().unwrap_or_else(|| "readme".into());
    let index = NameIndex::build(&args, &BuildControl::default()).expect("build");
    println!(
        "{} entries ({} files, {} folders) in {} ms: {:?}",
        index.len(),
        index.file_count(),
        index.folder_count(),
        index.build_ms,
        index.roots
    );
    let file = std::env::temp_dir().join("wfs-index-test.bin");
    let t = std::time::Instant::now();
    index.save(&file).unwrap();
    println!(
        "saved {} MB in {} ms",
        std::fs::metadata(&file).unwrap().len() / 1_048_576,
        t.elapsed().as_millis()
    );
    let t = std::time::Instant::now();
    let loaded = NameIndex::load(&file).unwrap();
    println!("loaded in {} ms", t.elapsed().as_millis());
    for q in [
        query.as_str(),
        "*.iso",
        "ext:dll kernel",
        r"windows\system32\ notepad",
    ] {
        let out = loaded.search(&Query::parse(q), 5);
        println!(
            "'{q}': {} matches in {} ms; top: {:?}",
            out.total,
            out.ms,
            out.results
                .iter()
                .map(|h| (&h.path, h.size))
                .collect::<Vec<_>>()
        );
    }
    let _ = std::fs::remove_file(file);
}
