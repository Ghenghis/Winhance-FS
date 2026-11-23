//! Reads an NTFS volume's Master File Table directly.
//!
//! The MFT is located from the boot sector, its extents are taken from record 0 (`$MFT` itself), and the
//! table is read in large sequential chunks and parsed in parallel. Each in-use record yields every one of
//! its names (hard links included, 8.3 short names skipped) with the parent folder, attributes, size and
//! modified time. Needs Administrator rights (the volume is opened for reading).

use super::name_index::{BuildControl, Raw};
use crate::{NexusError, Result};
use rayon::prelude::*;
use std::sync::atomic::Ordering;

const MASK: u64 = 0x0000_FFFF_FFFF_FFFF;
const FIRST_USER_RECORD: u64 = 16;
const ATTR_DIRECTORY: u32 = 0x10;
const CHUNK: usize = 16 * 1024 * 1024;
const FILETIME_UNIX_DIFF: i64 = 11_644_473_600;

/// Geometry from the NTFS boot sector.
#[derive(Debug, Clone, Copy, PartialEq)]
pub struct Geometry {
    pub bytes_per_cluster: u64,
    pub record_size: usize,
    pub mft_lcn: u64,
}

/// Parses the boot sector.
pub fn parse_boot(sector: &[u8]) -> Result<Geometry> {
    if sector.len() < 512 || &sector[3..11] != b"NTFS    " {
        return Err(NexusError::Windows("not an NTFS volume".into()));
    }
    let bytes_per_sector = u16::from_le_bytes([sector[0x0B], sector[0x0C]]) as u64;
    let spc = sector[0x0D];
    let sectors_per_cluster = if spc > 0x80 {
        1u64 << (256 - spc as u32)
    } else {
        spc as u64
    };
    let bytes_per_cluster = bytes_per_sector * sectors_per_cluster;
    let mft_lcn = u64::from_le_bytes(sector[0x30..0x38].try_into().unwrap());
    let cpr = sector[0x40] as i8;
    let record_size = if cpr < 0 {
        1usize << (-(cpr as i32))
    } else {
        cpr as usize * bytes_per_cluster as usize
    };
    if bytes_per_cluster == 0 || !(512..=65536).contains(&record_size) || record_size % 512 != 0 {
        return Err(NexusError::Windows("unexpected NTFS geometry".into()));
    }
    Ok(Geometry {
        bytes_per_cluster,
        record_size,
        mft_lcn,
    })
}

/// Applies the update-sequence fixups in place. Returns false for a torn or invalid record.
pub fn apply_fixups(record: &mut [u8]) -> bool {
    if record.len() < 48 || &record[0..4] != b"FILE" {
        return false;
    }
    let usa_off = u16::from_le_bytes([record[4], record[5]]) as usize;
    let usa_count = u16::from_le_bytes([record[6], record[7]]) as usize;
    if usa_count < 2
        || (usa_count - 1) * 512 != record.len()
        || usa_off + usa_count * 2 > record.len()
    {
        return false;
    }
    let usn = [record[usa_off], record[usa_off + 1]];
    for i in 1..usa_count {
        let end = i * 512;
        if record[end - 2..end] != usn {
            return false;
        }
        record[end - 2] = record[usa_off + i * 2];
        record[end - 1] = record[usa_off + i * 2 + 1];
    }
    true
}

/// One parsed record.
#[derive(Debug, Default, PartialEq)]
pub struct Parsed {
    pub base: u64,
    pub is_dir: bool,
    /// (parent record, name)
    pub names: Vec<(u64, String)>,
    /// (file attributes, modified unix seconds) from $STANDARD_INFORMATION
    pub info: Option<(u32, i64)>,
    pub size: Option<u64>,
    /// Data runs of the unnamed $DATA attribute (only kept for record 0).
    pub runs: Vec<(u64, u64)>,
}

fn u16_at(b: &[u8], o: usize) -> Option<u16> {
    b.get(o..o + 2).map(|s| u16::from_le_bytes([s[0], s[1]]))
}

fn u32_at(b: &[u8], o: usize) -> Option<u32> {
    b.get(o..o + 4)
        .map(|s| u32::from_le_bytes(s.try_into().unwrap()))
}

fn u64_at(b: &[u8], o: usize) -> Option<u64> {
    b.get(o..o + 8)
        .map(|s| u64::from_le_bytes(s.try_into().unwrap()))
}

fn filetime_to_unix(ft: u64) -> i64 {
    (ft / 10_000_000) as i64 - FILETIME_UNIX_DIFF
}

/// Decodes a non-resident attribute's run list into (lcn, cluster count) extents. Sparse runs are skipped.
pub fn decode_runs(mut data: &[u8]) -> Vec<(u64, u64)> {
    let mut runs = Vec::new();
    let mut lcn: i64 = 0;
    while let Some(&header) = data.first() {
        if header == 0 {
            break;
        }
        let len_size = (header & 0x0F) as usize;
        let off_size = (header >> 4) as usize;
        if len_size == 0 || len_size > 8 || off_size > 8 || data.len() < 1 + len_size + off_size {
            break;
        }
        let mut length = 0u64;
        for i in 0..len_size {
            length |= (data[1 + i] as u64) << (8 * i);
        }
        if off_size > 0 {
            let mut offset = 0i64;
            for i in 0..off_size {
                offset |= (data[1 + len_size + i] as i64) << (8 * i);
            }
            let shift = 64 - 8 * off_size as u32;
            offset = (offset << shift) >> shift; // sign-extend
            lcn += offset;
            if lcn < 0 {
                break;
            }
            runs.push((lcn as u64, length));
        }
        data = &data[1 + len_size + off_size..];
    }
    runs
}

/// Parses a record whose fixups were applied. Returns None when the record is not in use.
pub fn parse_record(rec: &[u8], keep_runs: bool) -> Option<Parsed> {
    let flags = u16_at(rec, 0x16)?;
    if flags & 0x01 == 0 {
        return None;
    }
    let mut out = Parsed {
        base: u64_at(rec, 0x20)? & MASK,
        is_dir: flags & 0x02 != 0,
        ..Default::default()
    };
    let mut off = u16_at(rec, 0x14)? as usize;
    let used = (u32_at(rec, 0x18)? as usize).min(rec.len());
    while off + 16 <= used {
        let kind = u32_at(rec, off)?;
        if kind == 0xFFFF_FFFF {
            break;
        }
        let len = u32_at(rec, off + 4)? as usize;
        if len < 16 || off + len > used {
            break;
        }
        let attr = &rec[off..off + len];
        let non_resident = attr[8] != 0;
        let name_len = attr[9];
        if !non_resident {
            let content_len = u32_at(attr, 0x10)? as usize;
            let content_off = u16_at(attr, 0x14)? as usize;
            let content = attr.get(content_off..content_off + content_len);
            match (kind, content) {
                (0x10, Some(c)) if c.len() >= 0x24 => {
                    out.info = Some((u32_at(c, 0x20)?, filetime_to_unix(u64_at(c, 0x08)?)));
                }
                (0x30, Some(c)) if c.len() >= 0x42 => {
                    let n = c[0x40] as usize;
                    let namespace = c[0x41];
                    if namespace != 2 {
                        if let Some(raw) = c.get(0x42..0x42 + n * 2) {
                            let units: Vec<u16> = raw
                                .as_chunks::<2>()
                                .0
                                .iter()
                                .map(|p| u16::from_le_bytes(*p))
                                .collect();
                            out.names
                                .push((u64_at(c, 0)? & MASK, String::from_utf16_lossy(&units)));
                        }
                    }
                }
                (0x80, Some(_)) if name_len == 0 => out.size = Some(content_len as u64),
                _ => {}
            }
        } else if kind == 0x80 && name_len == 0 && u64_at(attr, 0x10)? == 0 {
            out.size = Some(u64_at(attr, 0x30)?);
            if keep_runs {
                let runs_off = u16_at(attr, 0x20)? as usize;
                out.runs = decode_runs(attr.get(runs_off..)?);
            }
        }
        off += len;
    }
    Some(out)
}

#[cfg(windows)]
fn open_volume(root: &str) -> Result<std::fs::File> {
    use std::os::windows::fs::OpenOptionsExt;
    std::fs::OpenOptions::new()
        .read(true)
        .share_mode(7)
        .open(format!(r"\\.\{}", &root[..2]))
        .map_err(|_| {
            NexusError::PermissionDenied(
                "reading the drive table needs Administrator rights".into(),
            )
        })
}

#[cfg(windows)]
fn read_at(file: &std::fs::File, buf: &mut [u8], offset: u64) -> Result<()> {
    use std::os::windows::fs::FileExt;
    let mut done = 0;
    while done < buf.len() {
        let n = file.seek_read(&mut buf[done..], offset + done as u64)?;
        if n == 0 {
            return Err(NexusError::Windows("unexpected end of volume".into()));
        }
        done += n;
    }
    Ok(())
}

/// Reads every in-use record of the volume `root` (e.g. `C:\`).
#[cfg(windows)]
pub fn read_volume(root: &str, ctrl: &BuildControl) -> Result<Vec<Raw>> {
    let file = open_volume(root)?;
    let mut boot = vec![0u8; 4096];
    read_at(&file, &mut boot, 0)?;
    let geo = parse_boot(&boot)?;
    let rs = geo.record_size;

    // Record 0 describes the MFT's own extents. Read a whole cluster so the read stays aligned.
    let mut first = vec![0u8; (rs as u64).max(geo.bytes_per_cluster) as usize];
    read_at(&file, &mut first, geo.mft_lcn * geo.bytes_per_cluster)?;
    let mut rec0 = first[..rs].to_vec();
    if !apply_fixups(&mut rec0) {
        return Err(NexusError::Windows(
            "the drive table header is damaged".into(),
        ));
    }
    let mft = parse_record(&rec0, true)
        .ok_or_else(|| NexusError::Windows("$MFT record not in use".into()))?;
    let total_bytes = mft.size.unwrap_or(0);
    let run_bytes: u64 = mft.runs.iter().map(|r| r.1 * geo.bytes_per_cluster).sum();
    if total_bytes == 0 || run_bytes < total_bytes {
        return Err(NexusError::Windows(
            "the drive table is split across an attribute list".into(),
        ));
    }

    let total_records = (total_bytes / rs as u64) as usize;
    let mut info: Vec<(u32, i64)> = vec![(0, i64::MIN); total_records];
    let mut sizes: Vec<u64> = vec![0; total_records];
    let mut dirs: Vec<bool> = vec![false; total_records];
    let mut names: Vec<(u64, u64, String)> = Vec::with_capacity(total_records);

    let mut buf = vec![0u8; CHUNK];
    let mut frn: u64 = 0;
    let mut remaining = total_bytes;
    'runs: for &(lcn, clusters) in &mft.runs {
        let mut pos = lcn * geo.bytes_per_cluster;
        let mut left = (clusters * geo.bytes_per_cluster).min(remaining);
        while left > 0 {
            if ctrl.cancel.load(Ordering::Relaxed) {
                return Err(NexusError::Index("Cancelled".into()));
            }
            let n = (left as usize).min(CHUNK);
            let n = n - n % rs;
            if n == 0 {
                break;
            }
            read_at(&file, &mut buf[..n], pos)?;
            let start = frn;
            let parsed: Vec<(u64, Parsed)> = buf[..n]
                .par_chunks_mut(rs)
                .enumerate()
                .filter_map(|(i, rec)| {
                    let id = start + i as u64;
                    if id < FIRST_USER_RECORD && id != 5 {
                        return None;
                    }
                    if !apply_fixups(rec) {
                        return None;
                    }
                    parse_record(rec, false).map(|p| (id, p))
                })
                .collect();
            for (id, p) in parsed {
                let owner = if p.base != 0 { p.base } else { id } as usize;
                if owner >= total_records {
                    continue;
                }
                if p.base == 0 {
                    dirs[owner] = p.is_dir;
                    if let Some(i) = p.info {
                        info[owner] = i;
                    }
                }
                if let Some(s) = p.size {
                    sizes[owner] = s;
                }
                for (parent, name) in p.names {
                    names.push((owner as u64, parent, name));
                }
            }
            frn += (n / rs) as u64;
            pos += n as u64;
            left -= n as u64;
            remaining -= n as u64;
            ctrl.progress.store(names.len() as u64, Ordering::Relaxed);
            if remaining == 0 {
                break 'runs;
            }
        }
    }

    Ok(names
        .into_iter()
        .map(|(id, parent, name)| {
            let i = id as usize;
            let (mut attrs, mtime) = info[i];
            if dirs[i] {
                attrs |= ATTR_DIRECTORY;
            }
            Raw {
                key: id,
                parent,
                name,
                attrs,
                size: if dirs[i] { 0 } else { sizes[i] },
                mtime,
                stat: true,
            }
        })
        .collect())
}

#[cfg(not(windows))]
pub fn read_volume(_root: &str, _ctrl: &BuildControl) -> Result<Vec<Raw>> {
    Err(NexusError::Windows("only available on Windows".into()))
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn decodes_runs_with_negative_offsets() {
        // 0x21: 1-byte length, 2-byte offset. Run 1: 0x18 clusters at 0x5634; run 2: 0x10 clusters at -0x10 rel.
        let data = [0x21, 0x18, 0x34, 0x56, 0x21, 0x10, 0xF0, 0xFF, 0x00];
        assert_eq!(decode_runs(&data), vec![(0x5634, 0x18), (0x5624, 0x10)]);
        // sparse run (offset size 0) is skipped
        assert_eq!(
            decode_runs(&[0x01, 0x05, 0x11, 0x02, 0x10, 0x00]),
            vec![(0x10, 0x02)]
        );
    }

    #[test]
    fn boot_sector_geometry() {
        let mut s = vec![0u8; 512];
        s[3..11].copy_from_slice(b"NTFS    ");
        s[0x0B..0x0D].copy_from_slice(&512u16.to_le_bytes());
        s[0x0D] = 8;
        s[0x30..0x38].copy_from_slice(&0xC0000u64.to_le_bytes());
        s[0x40] = 0xF6; // -10 => 1024-byte records
        assert_eq!(
            parse_boot(&s).unwrap(),
            Geometry {
                bytes_per_cluster: 4096,
                record_size: 1024,
                mft_lcn: 0xC0000
            }
        );
        s[3] = b'X';
        assert!(parse_boot(&s).is_err());
    }

    /// Builds a 1024-byte record with fixups, $STANDARD_INFORMATION, two $FILE_NAMEs (Win32 + DOS) and $DATA.
    fn record(name: &str, parent: u64, size: u64) -> Vec<u8> {
        let mut r = vec![0u8; 1024];
        r[0..4].copy_from_slice(b"FILE");
        r[4..6].copy_from_slice(&0x30u16.to_le_bytes());
        r[6..8].copy_from_slice(&3u16.to_le_bytes());
        r[0x14..0x16].copy_from_slice(&0x38u16.to_le_bytes());
        r[0x16..0x18].copy_from_slice(&1u16.to_le_bytes());
        let mut off = 0x38;
        let mut attr = |r: &mut Vec<u8>, kind: u32, content: Vec<u8>| {
            let len = (0x18 + content.len() + 7) & !7;
            r[off..off + 4].copy_from_slice(&kind.to_le_bytes());
            r[off + 4..off + 8].copy_from_slice(&(len as u32).to_le_bytes());
            r[off + 0x10..off + 0x14].copy_from_slice(&(content.len() as u32).to_le_bytes());
            r[off + 0x14..off + 0x16].copy_from_slice(&0x18u16.to_le_bytes());
            r[off + 0x18..off + 0x18 + content.len()].copy_from_slice(&content);
            off += len;
        };
        let mut si = vec![0u8; 0x48];
        let ft: u64 = (1_700_000_000 + FILETIME_UNIX_DIFF as u64) * 10_000_000;
        si[8..16].copy_from_slice(&ft.to_le_bytes());
        si[0x20..0x24].copy_from_slice(&0x20u32.to_le_bytes());
        attr(&mut r, 0x10, si);
        for (n, ns) in [(name, 1u8), ("SHORT~1.TXT", 2u8)] {
            let units: Vec<u16> = n.encode_utf16().collect();
            let mut fname = vec![0u8; 0x42 + units.len() * 2];
            fname[0..8].copy_from_slice(&(parent | (3u64 << 48)).to_le_bytes());
            fname[0x40] = units.len() as u8;
            fname[0x41] = ns;
            for (i, u) in units.iter().enumerate() {
                fname[0x42 + i * 2..0x44 + i * 2].copy_from_slice(&u.to_le_bytes());
            }
            attr(&mut r, 0x30, fname);
        }
        attr(&mut r, 0x80, vec![7u8; size as usize]);
        r[off..off + 4].copy_from_slice(&0xFFFF_FFFFu32.to_le_bytes());
        r[0x18..0x1C].copy_from_slice(&((off + 8) as u32).to_le_bytes());
        // update sequence: value 0xABCD stored at the end of each 512-byte block
        r[0x30..0x32].copy_from_slice(&[0xCD, 0xAB]);
        for i in 1..3 {
            let end = i * 512;
            r[0x30 + i * 2] = r[end - 2];
            r[0x31 + i * 2] = r[end - 1];
            r[end - 2] = 0xCD;
            r[end - 1] = 0xAB;
        }
        r
    }

    #[test]
    fn parses_record_names_size_and_time() {
        let mut r = record("Résumé 2024.txt", 1234, 42);
        assert!(apply_fixups(&mut r));
        let p = parse_record(&r, false).unwrap();
        assert_eq!(
            p.names,
            vec![(1234, "Résumé 2024.txt".to_string())],
            "DOS 8.3 name skipped"
        );
        assert_eq!(p.size, Some(42));
        assert_eq!(p.info, Some((0x20, 1_700_000_000)));
        assert!(!p.is_dir);
    }

    #[test]
    fn torn_or_unused_records_are_rejected() {
        let mut torn = record("a.txt", 5, 1);
        torn[1022] = 0; // breaks the update sequence of block 2
        assert!(!apply_fixups(&mut torn));
        let mut unused = record("a.txt", 5, 1);
        unused[0x16] = 0;
        assert!(apply_fixups(&mut unused));
        assert!(parse_record(&unused, false).is_none());
        assert!(!apply_fixups(&mut vec![0u8; 1024]));
    }
}
