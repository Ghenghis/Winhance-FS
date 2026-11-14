using System;
using System.IO;

namespace Winhance.WPF.Features.FileManager.ViewModels
{
    /// <summary>
    /// Scoping rules applied to search results. Indexed search is not limited by the search paths and the
    /// file-system search always recurses, so results are filtered to the folder the user searched.
    /// </summary>
    public static class SearchResultScope
    {
        /// <summary>
        /// Returns true when <paramref name="resultPath"/> lies inside <paramref name="searchRoot"/>
        /// (directly inside it when <paramref name="recursive"/> is false).
        /// </summary>
        public static bool IsWithinScope(string? resultPath, string searchRoot, bool recursive)
        {
            if (string.IsNullOrEmpty(resultPath) || string.IsNullOrEmpty(searchRoot))
            {
                return false;
            }

            var root = searchRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (recursive)
            {
                return resultPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            }

            var parent = Path.GetDirectoryName(resultPath)?
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Equals(parent, root, StringComparison.OrdinalIgnoreCase);
        }
    }
}
