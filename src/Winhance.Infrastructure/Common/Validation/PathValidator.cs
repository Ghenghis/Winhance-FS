using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Winhance.Infrastructure.Common.Validation
{
    /// <summary>
    /// Provides comprehensive path validation and sanitization
    /// </summary>
    public static class PathValidator
    {
        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
        private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars();
        private static readonly string[] ReservedNames = {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        private static readonly Regex UnsafePathPattern = new(@"\.\.[\\/]|[\\/]\.\.[\\/]|[\\/]\.\.$", 
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex WindowsPathPattern = new(@"^[a-zA-Z]:\\", RegexOptions.Compiled);
        private static readonly Regex UncPathPattern = new(@"^\\\\[^\\]+\\[^\\]+", RegexOptions.Compiled);

        /// <summary>
        /// Validates if a path is safe and well-formed
        /// </summary>
        /// <param name="path">Path to validate</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidatePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return ValidationResult.Invalid("Path cannot be null or empty");
            }

            // Check for path traversal attempts
            if (UnsafePathPattern.IsMatch(path))
            {
                return ValidationResult.Invalid("Path contains unsafe traversal patterns");
            }

            // Check for invalid characters
            if (path.IndexOfAny(InvalidPathChars) >= 0)
            {
                return ValidationResult.Invalid("Path contains invalid characters");
            }

            // Check path length
            if (path.Length > 260) // MAX_PATH on Windows
            {
                return ValidationResult.Invalid("Path exceeds maximum length (260 characters)");
            }

            // Check for reserved device names in path components
            var pathParts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            foreach (var part in pathParts)
            {
                if (IsReservedName(part))
                {
                    return ValidationResult.Invalid($"Path contains reserved device name: {part}");
                }
            }

            // Validate path format
            try
            {
                var fullPath = Path.GetFullPath(path);
                
                // Check if it's a valid Windows or UNC path
                if (!WindowsPathPattern.IsMatch(fullPath) && !UncPathPattern.IsMatch(fullPath))
                {
                    return ValidationResult.Invalid("Invalid path format");
                }

                return ValidationResult.Valid(fullPath);
            }
            catch (Exception ex)
            {
                return ValidationResult.Invalid($"Invalid path format: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates if a filename is safe and well-formed
        /// </summary>
        /// <param name="fileName">Filename to validate</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateFileName(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return ValidationResult.Invalid("Filename cannot be null or empty");
            }

            // Check for invalid characters
            if (fileName.IndexOfAny(InvalidFileNameChars) >= 0)
            {
                return ValidationResult.Invalid("Filename contains invalid characters");
            }

            // Check for reserved names
            if (IsReservedName(fileName))
            {
                return ValidationResult.Invalid($"Filename is a reserved device name: {fileName}");
            }

            // Check filename length
            if (fileName.Length > 255)
            {
                return ValidationResult.Invalid("Filename exceeds maximum length (255 characters)");
            }

            // Check for leading/trailing dots and spaces
            if (fileName.StartsWith('.') || fileName.EndsWith('.') || 
                fileName.StartsWith(' ') || fileName.EndsWith(' '))
            {
                return ValidationResult.Invalid("Filename cannot start or end with dots or spaces");
            }

            return ValidationResult.Valid(fileName);
        }

        /// <summary>
        /// Sanitizes a path by removing or replacing invalid characters
        /// </summary>
        /// <param name="path">Path to sanitize</param>
        /// <param name="replacement">Character to replace invalid characters with</param>
        /// <returns>Sanitized path</returns>
        public static string SanitizePath(string path, char replacement = '_')
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            var sanitized = path;

            // Replace invalid path characters
            foreach (var invalidChar in InvalidPathChars)
            {
                sanitized = sanitized.Replace(invalidChar, replacement);
            }

            // Remove path traversal patterns
            sanitized = UnsafePathPattern.Replace(sanitized, replacement.ToString());

            // Truncate if too long
            if (sanitized.Length > 260)
            {
                sanitized = sanitized.Substring(0, 260);
            }

            return sanitized;
        }

        /// <summary>
        /// Sanitizes a filename by removing or replacing invalid characters
        /// </summary>
        /// <param name="fileName">Filename to sanitize</param>
        /// <param name="replacement">Character to replace invalid characters with</param>
        /// <returns>Sanitized filename</returns>
        public static string SanitizeFileName(string fileName, char replacement = '_')
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return string.Empty;
            }

            var sanitized = fileName.Trim();

            // Replace invalid filename characters
            foreach (var invalidChar in InvalidFileNameChars)
            {
                sanitized = sanitized.Replace(invalidChar, replacement);
            }

            // Handle reserved names
            if (IsReservedName(sanitized))
            {
                sanitized = $"{replacement}{sanitized}";
            }

            // Remove leading/trailing dots and spaces
            sanitized = sanitized.Trim(' ', '.');

            // Ensure not empty after sanitization
            if (string.IsNullOrEmpty(sanitized))
            {
                sanitized = "file";
            }

            // Truncate if too long
            if (sanitized.Length > 255)
            {
                var extension = Path.GetExtension(sanitized);
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized);
                var maxNameLength = 255 - extension.Length;
                
                if (maxNameLength > 0)
                {
                    sanitized = nameWithoutExtension.Substring(0, Math.Min(nameWithoutExtension.Length, maxNameLength)) + extension;
                }
                else
                {
                    sanitized = sanitized.Substring(0, 255);
                }
            }

            return sanitized;
        }

        /// <summary>
        /// Checks if a directory exists and is accessible
        /// </summary>
        /// <param name="path">Directory path to check</param>
        /// <returns>True if directory exists and is accessible</returns>
        public static bool IsValidDirectory(string path)
        {
            try
            {
                var validation = ValidatePath(path);
                if (!validation.IsValid)
                {
                    return false;
                }

                return Directory.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a file exists and is accessible
        /// </summary>
        /// <param name="path">File path to check</param>
        /// <returns>True if file exists and is accessible</returns>
        public static bool IsValidFile(string path)
        {
            try
            {
                var validation = ValidatePath(path);
                if (!validation.IsValid)
                {
                    return false;
                }

                return File.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a safe, unique filename in the specified directory
        /// </summary>
        /// <param name="directory">Target directory</param>
        /// <param name="baseName">Base filename</param>
        /// <param name="extension">File extension (optional)</param>
        /// <returns>Unique filename</returns>
        public static string GetUniqueFileName(string directory, string baseName, string? extension = null)
        {
            var sanitizedBase = SanitizeFileName(baseName);
            var sanitizedDir = SanitizePath(directory);
            
            if (!string.IsNullOrEmpty(extension) && !extension.StartsWith('.'))
            {
                extension = "." + extension;
            }

            var fileName = sanitizedBase + extension;
            var fullPath = Path.Combine(sanitizedDir, fileName);

            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            {
                return fileName;
            }

            // Generate unique name with counter
            var counter = 1;
            do
            {
                fileName = $"{sanitizedBase} ({counter}){extension}";
                fullPath = Path.Combine(sanitizedDir, fileName);
                counter++;
            } while ((File.Exists(fullPath) || Directory.Exists(fullPath)) && counter < 10000);

            return fileName;
        }

        /// <summary>
        /// Checks if the current user has write access to a directory
        /// </summary>
        /// <param name="directoryPath">Directory path to check</param>
        /// <returns>True if user has write access</returns>
        public static bool HasWriteAccess(string directoryPath)
        {
            try
            {
                var validation = ValidatePath(directoryPath);
                if (!validation.IsValid || !Directory.Exists(directoryPath))
                {
                    return false;
                }

                // Try to create a temporary file
                var tempFile = Path.Combine(directoryPath, Path.GetRandomFileName());
                using (var fs = File.Create(tempFile, 1, FileOptions.DeleteOnClose))
                {
                    // File created successfully
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Normalizes a path by resolving relative components and ensuring consistent separators
        /// </summary>
        /// <param name="path">Path to normalize</param>
        /// <returns>Normalized path</returns>
        public static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            try
            {
                // Get full path to resolve relative components
                var fullPath = Path.GetFullPath(path);
                
                // Ensure consistent directory separators
                return fullPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }
            catch
            {
                // If Path.GetFullPath fails, just normalize separators
                return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }
        }

        private static bool IsReservedName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            // Check base name (without extension)
            var baseName = Path.GetFileNameWithoutExtension(name).ToUpperInvariant();
            return ReservedNames.Contains(baseName);
        }
    }

    /// <summary>
    /// Represents the result of a validation operation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; private set; }
        public string? ErrorMessage { get; private set; }
        public string? SanitizedValue { get; private set; }

        private ValidationResult(bool isValid, string? errorMessage, string? sanitizedValue)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
            SanitizedValue = sanitizedValue;
        }

        public static ValidationResult Valid(string sanitizedValue)
        {
            return new ValidationResult(true, null, sanitizedValue);
        }

        public static ValidationResult Invalid(string errorMessage)
        {
            return new ValidationResult(false, errorMessage, null);
        }
    }
}
