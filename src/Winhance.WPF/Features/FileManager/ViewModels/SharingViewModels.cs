using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Core.Features.FileManager.Models;

namespace Winhance.WPF.Features.FileManager.ViewModels
{
    // Model classes
    public class FileVersion
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FilePath { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public long Size { get; set; }
        public string Checksum { get; set; } = string.Empty;
        public bool IsMajorVersion { get; set; }
        public string? StoragePath { get; set; }
        public int DownloadCount { get; set; }
    }

    // Enums
    public enum DifferenceType
    {
        Addition,
        Deletion,
        Modification,
        Move,
        Permission,
        Metadata
    }
}
