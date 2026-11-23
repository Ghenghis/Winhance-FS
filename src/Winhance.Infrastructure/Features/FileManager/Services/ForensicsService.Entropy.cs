using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.Common.Enums;
using Winhance.Core.Features.FileManager.Interfaces;

namespace Winhance.Infrastructure.Features.FileManager.Services;

/// <summary>Content inspection: real file type from its header, Shannon entropy, and what that says about the file.</summary>
public sealed partial class ForensicsService
{
    private sealed record Signature(string Type, int Offset, byte[] Magic, string[] Extensions);

    private static readonly string[] ProgramExtensions =
    {
        "exe", "dll", "sys", "scr", "com", "ocx", "cpl", "drv", "efi", "mui", "msstyles", "node", "pyd", "winmd", "ax",
        "acm", "tsp", "ime", "mun", "fon", "rll", "tlb", "olb", "api", "xll", "wll", "pyz", "bin", "dat", "tmp", "old", "bak",
    };

    private static readonly Signature[] Signatures =
    {
        new("Windows program", 0, "MZ"u8.ToArray(), ProgramExtensions),
        new("ZIP-based (zip, Office, jar...)", 0, new byte[] { 0x50, 0x4B, 0x03, 0x04 }, new[] { "zip", "docx", "xlsx", "pptx", "docm", "xlsm", "pptm", "jar", "apk", "odt", "ods", "odp", "epub", "vsix", "nupkg", "xpi", "appx", "msix", "appxbundle", "msixbundle", "whl", "kmz", "3mf", "aar", "ipa", "crx", "war", "ear", "xps", "oxps", "sketch", "pbix", "vsdx", "mpp" }),
        new("PDF document", 0, "%PDF"u8.ToArray(), new[] { "pdf", "ai" }),
        new("PNG image", 0, new byte[] { 0x89, 0x50, 0x4E, 0x47 }, new[] { "png", "apng" }),
        new("JPEG image", 0, new byte[] { 0xFF, 0xD8, 0xFF }, new[] { "jpg", "jpeg", "jfif", "jpe" }),
        new("GIF image", 0, "GIF8"u8.ToArray(), new[] { "gif" }),
        new("7-Zip archive", 0, new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C }, new[] { "7z" }),
        new("RAR archive", 0, "Rar!"u8.ToArray(), new[] { "rar" }),
        new("GZip archive", 0, new byte[] { 0x1F, 0x8B }, new[] { "gz", "tgz", "gzip" }),
        new("BZip2 archive", 0, "BZh"u8.ToArray(), new[] { "bz2", "tbz2" }),
        new("XZ archive", 0, new byte[] { 0xFD, 0x37, 0x7A, 0x58, 0x5A, 0x00 }, new[] { "xz", "txz" }),
        new("Cabinet archive", 0, "MSCF"u8.ToArray(), new[] { "cab", "msu" }),
        new("Office 97-2003 / MSI (OLE)", 0, new byte[] { 0xD0, 0xCF, 0x11, 0xE0 }, new[] { "doc", "xls", "ppt", "msi", "msp", "msg", "vsd", "pub", "mst", "db", "dot", "xlt", "pot", "suo", "thumbs" }),
        new("Linux program (ELF)", 0, new byte[] { 0x7F, 0x45, 0x4C, 0x46 }, new[] { "so", "elf", "o", "bin" }),
        new("RIFF media (wav, avi, webp)", 0, "RIFF"u8.ToArray(), new[] { "wav", "avi", "webp", "ani", "rmi", "cdr" }),
        new("MP3 audio", 0, "ID3"u8.ToArray(), new[] { "mp3" }),
        new("MP4 / QuickTime media", 4, "ftyp"u8.ToArray(), new[] { "mp4", "m4a", "m4v", "mov", "heic", "heif", "avif", "3gp", "3g2", "f4v", "m4b" }),
        new("Ogg media", 0, "OggS"u8.ToArray(), new[] { "ogg", "oga", "ogv", "opus" }),
        new("FLAC audio", 0, "fLaC"u8.ToArray(), new[] { "flac" }),
        new("Matroska / WebM media", 0, new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }, new[] { "mkv", "webm", "mka", "mk3d" }),
        new("SQLite database", 0, "SQLite format 3"u8.ToArray(), new[] { "sqlite", "sqlite3", "db", "db3" }),
        new("RTF document", 0, "{\\rtf"u8.ToArray(), new[] { "rtf", "doc" }),
        new("Windows shortcut", 0, new byte[] { 0x4C, 0x00, 0x00, 0x00, 0x01, 0x14, 0x02, 0x00 }, new[] { "lnk" }),
        new("Icon", 0, new byte[] { 0x00, 0x00, 0x01, 0x00 }, new[] { "ico" }),
        new("TIFF image", 0, new byte[] { 0x49, 0x49, 0x2A, 0x00 }, new[] { "tif", "tiff", "dng", "nef", "cr2", "arw" }),
        new("ISO disc image", 0x8001, "CD001"u8.ToArray(), new[] { "iso" }),
    };

    /// <summary>Extensions a user would open without thinking: a program wearing one of these is a disguise.</summary>
    private static readonly HashSet<string> HarmlessLooking = new(StringComparer.OrdinalIgnoreCase)
    {
        "txt", "pdf", "doc", "docx", "xls", "xlsx", "ppt", "pptx", "rtf", "csv", "odt", "jpg", "jpeg", "png", "gif", "bmp",
        "webp", "heic", "mp3", "mp4", "avi", "mkv", "mov", "wav", "flac", "zip", "rar", "7z", "html", "htm", "log", "md",
        "json", "xml", "ini", "cfg", "iso", "svg", "eml",
    };

    /// <summary>Plain-text formats: their content is never close to random.</summary>
    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "txt", "csv", "log", "md", "xml", "json", "html", "htm", "ini", "cfg", "conf", "yaml", "yml", "toml", "sql", "ps1",
        "psm1", "py", "js", "ts", "cs", "c", "cpp", "h", "hpp", "java", "go", "rs", "rb", "php", "bat", "cmd", "reg",
        "svg", "css", "tex", "srt", "vbs", "sh", "eml", "properties", "gitignore",
    };

    /// <summary>Extensions ransomware families append to files they encrypted.</summary>
    private static readonly HashSet<string> RansomExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "locky", "zepto", "odin", "thor", "aesir", "cerber", "cerber2", "cerber3", "wncry", "wnry", "wcry", "crypt",
        "crypted", "cryptolocker", "encrypted", "enc", "locked", "crinf", "r5a", "xrnt", "xtbl", "crypz", "cryp1",
        "vvv", "ecc", "ezz", "exx", "zzz", "micro", "ttt", "mp3crypt", "kraken", "darkness", "nochance", "oshit", "kkk",
        "gws", "hush", "lockbit", "conti", "ryk", "ryuk", "basta", "akira", "royal", "phobos", "eking", "makop", "djvu",
        "dharma", "wallet", "arena", "onion",
    };

    /// <summary>Shannon entropy in bits per byte (0 = constant, 8 = indistinguishable from random).</summary>
    public static double CalculateEntropy(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return 0;
        }

        Span<long> counts = stackalloc long[256];
        foreach (var b in data)
        {
            counts[b]++;
        }

        double entropy = 0, length = data.Length;
        foreach (var c in counts)
        {
            if (c > 0)
            {
                var p = c / length;
                entropy -= p * Math.Log2(p);
            }
        }

        return entropy;
    }

    public async Task<FileInspection> InspectFileAsync(string path, EntropyScanOptions? options = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        options ??= new EntropyScanOptions();
        var info = new FileInfo(path);
        var sample = await ReadSampleAsync(info, options.SampleBytes, ct);
        var header = await ReadHeaderAsync(info, ct);
        return Judge(info.FullName, info.Length, sample, header, options.HighEntropyThreshold);
    }

    public Task<EntropyScanResult> ScanEntropyAsync(string path, EntropyScanOptions options, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(options);
        return Task.Run(
            async () =>
            {
                var root = Path.GetFullPath(path);
                IEnumerable<string> files;
                if (File.Exists(root))
                {
                    files = new[] { root };
                }
                else if (Directory.Exists(root))
                {
                    files = Directory.EnumerateFiles(root, "*", new EnumerationOptions
                    {
                        RecurseSubdirectories = options.Recursive,
                        IgnoreInaccessible = true,
                        AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.Offline,
                    });
                }
                else
                {
                    return new EntropyScanResult(Array.Empty<FileInspection>(), 0, 0, $"'{root}' does not exist.");
                }

                var found = new List<FileInspection>();
                int scanned = 0, skipped = 0;
                foreach (var file in files)
                {
                    ct.ThrowIfCancellationRequested();
                    if (scanned >= options.MaxFiles)
                    {
                        break;
                    }

                    try
                    {
                        var result = await InspectFileAsync(file, options, ct);
                        scanned++;
                        if (!options.OnlySuspicious || result.Verdict > FileVerdict.Compressed)
                        {
                            found.Add(result);
                        }
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        skipped++;
                    }

                    if ((scanned + skipped) % 200 == 0)
                    {
                        progress?.Report($"Inspected {scanned:N0} files, {found.Count(f => f.Verdict > FileVerdict.Compressed):N0} suspicious so far...");
                    }
                }

                _log?.Log(LogLevel.Info, $"Content scan of {root}: {scanned} files, {found.Count} reported, {skipped} skipped");
                var ordered = found.OrderByDescending(f => f.Verdict).ThenByDescending(f => f.Entropy).ToList();
                return new EntropyScanResult(ordered, scanned, skipped);
            },
            ct);
    }

    internal static FileInspection Judge(string path, long size, ReadOnlySpan<byte> sample, ReadOnlySpan<byte> header, double threshold)
    {
        var ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        var entropy = Math.Round(CalculateEntropy(sample), 3);
        var detected = Detect(header);
        var high = entropy >= threshold && sample.Length >= 512;

        if (size == 0)
        {
            return new FileInspection(path, size, 0, ext, null, FileVerdict.Normal, "Empty file.");
        }

        if (detected?.Type == "Windows program" && HarmlessLooking.Contains(ext))
        {
            return new FileInspection(path, size, entropy, ext, detected.Type, FileVerdict.DisguisedExecutable, $"This is a Windows program pretending to be a .{ext} file. Do not open it; scan it with your antivirus.");
        }

        if (RansomExtensions.Contains(ext) && high)
        {
            return new FileInspection(path, size, entropy, ext, detected?.Type, FileVerdict.LikelyEncrypted, $"Random-looking content with an extension (.{ext}) that ransomware appends to encrypted files.");
        }

        var expected = Signatures.Where(s => s.Extensions.Contains(ext)).ToList();
        if (detected == null && high)
        {
            if (TextExtensions.Contains(ext))
            {
                return new FileInspection(path, size, entropy, ext, null, FileVerdict.LikelyEncrypted, $"A .{ext} file should be readable text, but its content is random-looking ({entropy:0.00} bits/byte): it is probably encrypted.");
            }

            if (expected.Count > 0 && !expected.Any(s => s.Type == "Windows program"))
            {
                return new FileInspection(path, size, entropy, ext, null, FileVerdict.LikelyEncrypted, $"A real .{ext} file starts with a {expected[0].Type} header; this one has none and random-looking content - typical of ransomware encryption.");
            }

            return new FileInspection(path, size, entropy, ext, null, FileVerdict.Compressed, "Compressed or encrypted data of an unknown type.");
        }

        if (detected != null && expected.Count > 0 && !detected.Extensions.Contains(ext) && detected.Type != "Windows program")
        {
            return new FileInspection(path, size, entropy, ext, detected.Type, FileVerdict.ExtensionMismatch, $"Named .{ext} but the content is a {detected.Type}.");
        }

        if (detected == null && TextExtensions.Contains(ext) && sample.Length >= 512 && LooksBinary(sample))
        {
            return new FileInspection(path, size, entropy, ext, null, FileVerdict.ExtensionMismatch, $"Named .{ext} but the content is binary, not text.");
        }

        return new FileInspection(path, size, entropy, ext, detected?.Type, high ? FileVerdict.Compressed : FileVerdict.Normal, high ? "Compressed content (normal for this type)." : "Looks normal.");
    }

    private static Signature? Detect(ReadOnlySpan<byte> header)
    {
        foreach (var sig in Signatures)
        {
            if (header.Length >= sig.Offset + sig.Magic.Length && header.Slice(sig.Offset, sig.Magic.Length).SequenceEqual(sig.Magic))
            {
                if (sig.Type == "Windows program" && !HasPeHeader(header))
                {
                    continue; // text that merely starts with "MZ"
                }

                return sig;
            }
        }

        // MP3 frames without an ID3 tag.
        if (header.Length >= 2 && header[0] == 0xFF && (header[1] & 0xE0) == 0xE0 && header[1] != 0xFF && header[1] != 0xD8)
        {
            return Signatures.First(s => s.Type == "MP3 audio");
        }

        return null;
    }

    private static bool HasPeHeader(ReadOnlySpan<byte> header)
    {
        if (header.Length < 0x40)
        {
            return false;
        }

        var peOffset = BitConverter.ToInt32(header.Slice(0x3C, 4));
        return peOffset > 0 && peOffset <= header.Length - 4 && header.Slice(peOffset, 4).SequenceEqual(new byte[] { 0x50, 0x45, 0, 0 });
    }

    private static bool LooksBinary(ReadOnlySpan<byte> sample)
    {
        // UTF-16 text has many zero bytes but a BOM; treat zero bytes without a BOM as binary.
        if (sample.Length >= 2 && ((sample[0] == 0xFF && sample[1] == 0xFE) || (sample[0] == 0xFE && sample[1] == 0xFF)))
        {
            return false;
        }

        var zeros = 0;
        foreach (var b in sample[..Math.Min(sample.Length, 8192)])
        {
            if (b == 0)
            {
                zeros++;
            }
        }

        return zeros > 16;
    }

    private static async Task<byte[]> ReadHeaderAsync(FileInfo info, CancellationToken ct)
    {
        var length = (int)Math.Min(info.Length, 0x8010);
        var buffer = new byte[length];
        await using var fs = new FileStream(LongPath(info.FullName), FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, true);
        var read = await fs.ReadAtLeastAsync(buffer, length, throwOnEndOfStream: false, ct);
        return read == length ? buffer : buffer[..read];
    }

    private static async Task<byte[]> ReadSampleAsync(FileInfo info, int sampleBytes, CancellationToken ct)
    {
        sampleBytes = Math.Max(4096, sampleBytes);
        await using var fs = new FileStream(LongPath(info.FullName), FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 65536, true);
        if (info.Length <= sampleBytes)
        {
            var all = new byte[info.Length];
            var n = await fs.ReadAtLeastAsync(all, all.Length, throwOnEndOfStream: false, ct);
            return n == all.Length ? all : all[..n];
        }

        // Start, middle and end: ransomware often encrypts only part of large files.
        var chunk = sampleBytes / 3;
        var buffer = new byte[chunk * 3];
        var total = 0;
        foreach (var offset in new[] { 0L, (info.Length / 2) - (chunk / 2), info.Length - chunk })
        {
            fs.Seek(offset, SeekOrigin.Begin);
            total += await fs.ReadAtLeastAsync(buffer.AsMemory(total, chunk), chunk, throwOnEndOfStream: false, ct);
        }

        return total == buffer.Length ? buffer : buffer[..total];
    }
}
