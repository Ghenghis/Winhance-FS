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
    // Enums
    public enum OperationType
    {
        All,
        Copy,
        Move,
        Delete,
        Create,
        Rename,
        Compress,
        Decompress,
        Encrypt,
        Decrypt
    }

}
