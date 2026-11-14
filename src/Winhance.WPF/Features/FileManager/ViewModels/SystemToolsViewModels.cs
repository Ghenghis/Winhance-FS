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
    // Supporting classes
    public class NetworkConnection
    {
        public string Protocol { get; set; } = string.Empty;
        public string LocalAddress { get; set; } = string.Empty;
        public string RemoteAddress { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
    }

    // Enums
    public enum DriveType
    {
        Unknown,
        NoRootDirectory,
        Removable,
        Fixed,
        Network,
        CDRom,
        Ram
    }

}
