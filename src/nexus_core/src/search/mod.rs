//! Search: the instant file-name index (`name_index`) and a Tantivy full-text engine.

mod mft_raw;
pub mod name_index;
#[cfg(feature = "fulltext")]
mod tantivy_engine;

pub use name_index::{NameIndex, Query as NameQuery};
#[cfg(feature = "fulltext")]
pub use tantivy_engine::{SearchEngine, SearchQuery, SearchResult};
