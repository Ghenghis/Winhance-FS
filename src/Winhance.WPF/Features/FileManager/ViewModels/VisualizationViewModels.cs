using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Core.Features.FileManager.Models;
using CoreSpaceAnalyzerService = Winhance.Core.Features.FileManager.Interfaces.ISpaceAnalyzerService;

namespace Winhance.WPF.Features.FileManager.ViewModels
{
    // Model classes
    public class DiskUsageChartItem
    {
        public string Label { get; set; } = string.Empty;
        public double Value { get; set; }
        public double Percentage { get; set; }
        public string Color { get; set; } = "#0078D4";
        public string Description { get; set; } = string.Empty;
    }

    public class FileTypeItem
    {
        public string Extension { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long TotalSize { get; set; }
        public int FileCount { get; set; }
        public double SizePercentage { get; set; }
        public string Color { get; set; } = "#0078D4";
        public string Icon { get; set; } = "📄";
    }

    public class TimelineItem
    {
        public DateTime Date { get; set; }
        public int FileCount { get; set; }
        public long TotalSize { get; set; }
        public int FilesCreated { get; set; }
        public int FilesModified { get; set; }
        public int FilesDeleted { get; set; }
    }

    public class NetworkDrive
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Server { get; set; } = string.Empty;
        public long TotalSpace { get; set; }
        public long FreeSpace { get; set; }
        public bool IsConnected { get; set; }
        public DateTime? LastConnected { get; set; }
    }

    // Enums
    public enum TimelineGranularity
    {
        Hourly,
        Daily,
        Weekly,
        Monthly
    }

}
