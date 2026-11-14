using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Winhance.Core.Features.FileManager.Interfaces;
using CoreFavoriteItem = Winhance.Core.Features.FileManager.Interfaces.FavoriteItem;

namespace Winhance.WPF.Features.FileManager.ViewModels
{
    /// <summary>
    /// ViewModel for a favorite group.
    /// </summary>
    public partial class FavoriteGroupViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _id = string.Empty;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _color = "#FF5722";

        [ObservableProperty]
        private bool _isExpanded;

        [ObservableProperty]
        private int _favoriteCount;

        [ObservableProperty]
        private ObservableCollection<FavoriteItem> _items = new();

        public string ColorBrush => Color;
    }
}
