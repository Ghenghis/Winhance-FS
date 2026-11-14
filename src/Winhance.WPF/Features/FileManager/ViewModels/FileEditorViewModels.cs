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
    // Enums
    public enum PreviewType
    {
        Text,
        Image,
        Audio,
        Video,
        PDF,
        Document,
        Spreadsheet,
        Presentation,
        Archive,
        Binary,
        Hex,
        Unknown
    }

    public enum ConversionStatus
    {
        Queued,
        Running,
        Completed,
        Failed,
        Cancelled,
        Paused
    }
}
