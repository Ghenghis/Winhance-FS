using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Winhance.Core.Features.FileManager.Interfaces;

namespace Winhance.WPF.Features.FileManager.ViewModels
{
    /// <summary>
    /// Selection mode enum.
    /// </summary>
    public enum SelectionMode
    {
        Normal,
        Extended,
        Multiple
    }

    /// <summary>
    /// Selection preset model.
    /// </summary>
    public class SelectionPreset
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string[] Paths { get; set; } = Array.Empty<string>();
        public DateTime CreatedAt { get; set; }
    }
}
