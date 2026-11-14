using System.Security.AccessControl;
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
    public enum ShareType
    {
        None,
        Read,
        Write,
        FullControl
    }
}
