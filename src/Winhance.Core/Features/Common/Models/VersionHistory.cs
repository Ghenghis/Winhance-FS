using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Winhance.Core.Features.Common.Models
{
    /// <summary>
    /// Represents a version entry in the release history for rollback capability.
    /// </summary>
    public class VersionHistoryEntry
    {
        public string Version { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public bool IsStable { get; set; } = true;
        public bool IsInstalled { get; set; } = false;
        public long? InstallSize { get; set; }
        public string? Checksum { get; set; }
        
        [JsonIgnore]
        public bool CanRollbackTo => !IsInstalled && IsStable;
        
        [JsonIgnore] 
        public string FormattedReleaseDate => ReleaseDate.ToString("yyyy-MM-dd");
        
        [JsonIgnore]
        public string VersionType => IsStable ? "Stable" : "Beta";
    }

    /// <summary>
    /// Manages version history and rollback operations.
    /// </summary>
    public class VersionHistory
    {
        public List<VersionHistoryEntry> Entries { get; set; } = new();
        public string CurrentVersion { get; set; } = string.Empty;
        public DateTime LastChecked { get; set; }
        
        [JsonIgnore]
        public IEnumerable<VersionHistoryEntry> StableVersions => 
            Entries.Where(e => e.IsStable).OrderByDescending(e => e.ReleaseDate);
        
        [JsonIgnore]
        public IEnumerable<VersionHistoryEntry> AvailableForRollback =>
            Entries.Where(e => e.CanRollbackTo).OrderByDescending(e => e.ReleaseDate);
        
        [JsonIgnore]
        public VersionHistoryEntry? LatestStable => StableVersions.FirstOrDefault();
        
        [JsonIgnore]
        public VersionHistoryEntry? CurrentEntry => 
            Entries.FirstOrDefault(e => e.Version == CurrentVersion);
        
        public void AddOrUpdateEntry(VersionHistoryEntry entry)
        {
            var existing = Entries.FirstOrDefault(e => e.Version == entry.Version);
            if (existing != null)
            {
                Entries.Remove(existing);
            }
            Entries.Add(entry);
            LastChecked = DateTime.Now;
        }
        
        public void SetInstalledVersion(string version)
        {
            foreach (var entry in Entries)
            {
                entry.IsInstalled = entry.Version == version;
            }
            CurrentVersion = version;
        }
        
        public VersionHistoryEntry? GetPreviousVersion()
        {
            var sorted = Entries
                .Where(e => e.IsStable && e.Version != CurrentVersion)
                .OrderByDescending(e => e.ReleaseDate)
                .ToList();
                
            var current = CurrentEntry;
            if (current == null) return sorted.FirstOrDefault();
            
            // Find the version that was released just before current
            return sorted.FirstOrDefault(e => e.ReleaseDate < current.ReleaseDate) ?? sorted.FirstOrDefault();
        }
    }
}
