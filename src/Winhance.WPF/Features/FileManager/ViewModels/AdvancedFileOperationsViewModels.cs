using System.IO;
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
    public class FileOperation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public OperationStatus Status { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class ComparisonResult
    {
        public string LeftFile { get; set; } = string.Empty;
        public string RightFile { get; set; } = string.Empty;
        public bool AreEqual { get; set; }
        public List<string> Differences { get; set; } = new();
        public DateTime ComparisonTime { get; set; }
    }

    public class FileProperties
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public DateTime Accessed { get; set; }
        public FileAttributes Attributes { get; set; }
        public string? MD5 { get; set; }
        public string? SHA1 { get; set; }
        public string? SHA256 { get; set; }
        public string? Version { get; set; }
        public string? Company { get; set; }
        public string? Description { get; set; }
    }

    // Enums
    public enum ComparisonMode
    {
        Content,
        Size,
        Checksum,
        Attributes,
        Timestamps
    }

    public enum OperationStatus
    {
        Pending,
        Running,
        Completed,
        Failed
    }
}
