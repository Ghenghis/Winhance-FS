using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Winhance.Infrastructure.Common.Validation
{
    /// <summary>
    /// Provides comprehensive input validation and sanitization for user inputs
    /// </summary>
    public static class InputValidator
    {
        private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex AlphaNumericPattern = new(@"^[a-zA-Z0-9]+$", RegexOptions.Compiled);
        private static readonly Regex NumericPattern = new(@"^[0-9]+$", RegexOptions.Compiled);
        private static readonly Regex HexPattern = new(@"^[0-9a-fA-F]+$", RegexOptions.Compiled);
        
        private static readonly char[] DangerousChars = { '<', '>', '"', '\'', '&', '\0' };
        private static readonly string[] SqlKeywords = {
            "SELECT", "INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER", "EXEC", "EXECUTE",
            "UNION", "SCRIPT", "JAVASCRIPT", "VBSCRIPT", "ONLOAD", "ONERROR", "EVAL"
        };

        /// <summary>
        /// Validates and sanitizes a search query
        /// </summary>
        /// <param name="query">Search query to validate</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateSearchQuery(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return ValidationResult.Invalid("Search query cannot be empty");
            }

            if (query.Length > 1000)
            {
                return ValidationResult.Invalid("Search query too long (maximum 1000 characters)");
            }

            // Check for dangerous patterns
            var sanitized = SanitizeInput(query);
            
            // Check for potential injection attempts
            if (ContainsSqlInjectionPatterns(sanitized))
            {
                return ValidationResult.Invalid("Search query contains potentially dangerous patterns");
            }

            return ValidationResult.Valid(sanitized);
        }

        /// <summary>
        /// Validates a file extension
        /// </summary>
        /// <param name="extension">File extension to validate</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateFileExtension(string? extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return ValidationResult.Valid("");
            }

            var normalized = extension.Trim().ToLowerInvariant();
            if (normalized.StartsWith('.'))
            {
                normalized = normalized.Substring(1);
            }

            if (normalized.Length > 10)
            {
                return ValidationResult.Invalid("File extension too long (maximum 10 characters)");
            }

            if (!AlphaNumericPattern.IsMatch(normalized))
            {
                return ValidationResult.Invalid("File extension contains invalid characters");
            }

            return ValidationResult.Valid("." + normalized);
        }

        /// <summary>
        /// Validates a file size value
        /// </summary>
        /// <param name="sizeText">Size text to validate</param>
        /// <returns>Validation result with parsed size in bytes</returns>
        public static ValidationResult<long> ValidateFileSize(string? sizeText)
        {
            if (string.IsNullOrWhiteSpace(sizeText))
            {
                return ValidationResult<long>.Invalid("File size cannot be empty");
            }

            try
            {
                var trimmed = sizeText.Trim().ToUpperInvariant();
                long multiplier = 1;
                var numberPart = trimmed;

                // Handle size suffixes
                if (trimmed.EndsWith("KB", StringComparison.OrdinalIgnoreCase))
                {
                    multiplier = 1024;
                    numberPart = trimmed.Substring(0, trimmed.Length - 2).Trim();
                }
                else if (trimmed.EndsWith("MB", StringComparison.OrdinalIgnoreCase))
                {
                    multiplier = 1024 * 1024;
                    numberPart = trimmed.Substring(0, trimmed.Length - 2).Trim();
                }
                else if (trimmed.EndsWith("GB", StringComparison.OrdinalIgnoreCase))
                {
                    multiplier = 1024L * 1024 * 1024;
                    numberPart = trimmed.Substring(0, trimmed.Length - 2).Trim();
                }
                else if (trimmed.EndsWith("TB", StringComparison.OrdinalIgnoreCase))
                {
                    multiplier = 1024L * 1024 * 1024 * 1024;
                    numberPart = trimmed.Substring(0, trimmed.Length - 2).Trim();
                }
                else if (trimmed.EndsWith("B", StringComparison.OrdinalIgnoreCase))
                {
                    numberPart = trimmed.Substring(0, trimmed.Length - 1).Trim();
                }

                if (!double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                {
                    return ValidationResult<long>.Invalid("Invalid file size format");
                }

                if (number < 0)
                {
                    return ValidationResult<long>.Invalid("File size cannot be negative");
                }

                var bytes = (long)(number * multiplier);
                
                // Check for reasonable limits (1 PB max)
                if (bytes > 1024L * 1024 * 1024 * 1024 * 1024)
                {
                    return ValidationResult<long>.Invalid("File size too large");
                }

                return ValidationResult<long>.Valid(bytes);
            }
            catch
            {
                return ValidationResult<long>.Invalid("Invalid file size format");
            }
        }

        /// <summary>
        /// Validates a date string
        /// </summary>
        /// <param name="dateText">Date text to validate</param>
        /// <returns>Validation result with parsed date</returns>
        public static ValidationResult<DateTime> ValidateDate(string? dateText)
        {
            if (string.IsNullOrWhiteSpace(dateText))
            {
                return ValidationResult<DateTime>.Invalid("Date cannot be empty");
            }

            if (DateTime.TryParse(dateText, CultureInfo.CurrentCulture, DateTimeStyles.None, out var date))
            {
                // Check reasonable date range (1900 - 2100)
                if (date.Year < 1900 || date.Year > 2100)
                {
                    return ValidationResult<DateTime>.Invalid("Date is outside valid range (1900-2100)");
                }

                return ValidationResult<DateTime>.Valid(date);
            }

            return ValidationResult<DateTime>.Invalid("Invalid date format");
        }

        /// <summary>
        /// Validates a numeric value within specified range
        /// </summary>
        /// <param name="valueText">Value text to validate</param>
        /// <param name="min">Minimum allowed value</param>
        /// <param name="max">Maximum allowed value</param>
        /// <returns>Validation result with parsed value</returns>
        public static ValidationResult<int> ValidateNumericRange(string? valueText, int min, int max)
        {
            if (string.IsNullOrWhiteSpace(valueText))
            {
                return ValidationResult<int>.Invalid("Value cannot be empty");
            }

            if (!int.TryParse(valueText, out var value))
            {
                return ValidationResult<int>.Invalid("Invalid numeric format");
            }

            if (value < min || value > max)
            {
                return ValidationResult<int>.Invalid($"Value must be between {min} and {max}");
            }

            return ValidationResult<int>.Valid(value);
        }

        /// <summary>
        /// Validates an email address
        /// </summary>
        /// <param name="email">Email address to validate</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return ValidationResult.Invalid("Email address cannot be empty");
            }

            if (email.Length > 254) // RFC 5321 limit
            {
                return ValidationResult.Invalid("Email address too long");
            }

            if (!EmailPattern.IsMatch(email))
            {
                return ValidationResult.Invalid("Invalid email format");
            }

            var sanitized = SanitizeInput(email);
            return ValidationResult.Valid(sanitized);
        }

        /// <summary>
        /// Validates a password strength
        /// </summary>
        /// <param name="password">Password to validate</param>
        /// <returns>Validation result with strength assessment</returns>
        public static ValidationResult<PasswordStrength> ValidatePassword(string? password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return ValidationResult<PasswordStrength>.Invalid("Password cannot be empty");
            }

            if (password.Length < 8)
            {
                return ValidationResult<PasswordStrength>.Invalid("Password must be at least 8 characters long");
            }

            if (password.Length > 128)
            {
                return ValidationResult<PasswordStrength>.Invalid("Password too long (maximum 128 characters)");
            }

            var strength = CalculatePasswordStrength(password);
            
            if (strength == PasswordStrength.VeryWeak)
            {
                return ValidationResult<PasswordStrength>.Invalid("Password is too weak");
            }

            return ValidationResult<PasswordStrength>.Valid(strength);
        }

        /// <summary>
        /// Validates a hexadecimal string
        /// </summary>
        /// <param name="hexString">Hex string to validate</param>
        /// <param name="exactLength">Expected exact length (optional)</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateHexString(string? hexString, int? exactLength = null)
        {
            if (string.IsNullOrWhiteSpace(hexString))
            {
                return ValidationResult.Invalid("Hex string cannot be empty");
            }

            var cleaned = hexString.Replace(" ", "").Replace("-", "");

            if (!HexPattern.IsMatch(cleaned))
            {
                return ValidationResult.Invalid("Invalid hexadecimal format");
            }

            if (exactLength.HasValue && cleaned.Length != exactLength.Value)
            {
                return ValidationResult.Invalid($"Hex string must be exactly {exactLength} characters");
            }

            return ValidationResult.Valid(cleaned.ToUpperInvariant());
        }

        /// <summary>
        /// Sanitizes input text by removing dangerous characters
        /// </summary>
        /// <param name="input">Input text to sanitize</param>
        /// <returns>Sanitized text</returns>
        public static string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            var sanitized = input;

            // Remove dangerous characters
            foreach (var dangerousChar in DangerousChars)
            {
                sanitized = sanitized.Replace(dangerousChar, ' ');
            }

            // Remove control characters except tab, newline, and carriage return
            sanitized = new string(sanitized.Where(c => 
                !char.IsControl(c) || c == '\t' || c == '\n' || c == '\r').ToArray());

            // Normalize whitespace
            sanitized = Regex.Replace(sanitized, @"\s+", " ").Trim();

            return sanitized;
        }

        /// <summary>
        /// Validates a collection of strings
        /// </summary>
        /// <param name="items">Items to validate</param>
        /// <param name="maxItems">Maximum number of items allowed</param>
        /// <param name="maxItemLength">Maximum length per item</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateStringCollection(IEnumerable<string>? items, int maxItems, int maxItemLength)
        {
            if (items == null)
            {
                return ValidationResult.Invalid("Collection cannot be null");
            }

            var itemList = items.ToList();

            if (itemList.Count > maxItems)
            {
                return ValidationResult.Invalid($"Too many items (maximum {maxItems})");
            }

            var sanitizedItems = new List<string>();
            foreach (var item in itemList)
            {
                if (string.IsNullOrWhiteSpace(item))
                {
                    continue; // Skip empty items
                }

                if (item.Length > maxItemLength)
                {
                    return ValidationResult.Invalid($"Item too long (maximum {maxItemLength} characters)");
                }

                var sanitized = SanitizeInput(item);
                sanitizedItems.Add(sanitized);
            }

            return ValidationResult.Valid(string.Join(";", sanitizedItems));
        }

        private static bool ContainsSqlInjectionPatterns(string input)
        {
            var upperInput = input.ToUpperInvariant();

            // Check for SQL keywords
            foreach (var keyword in SqlKeywords)
            {
                if (upperInput.Contains(keyword))
                {
                    return true;
                }
            }

            // Check for common injection patterns
            var injectionPatterns = new[]
            {
                "'", "\"", "/*", "*/", "--", ";", "xp_", "sp_", "@@", "char(", "nchar(", "varchar(", "nvarchar(",
                "exec(", "execute(", "cast(", "convert(", "substring(", "ascii(", "len(", "rtrim(", "ltrim("
            };

            foreach (var pattern in injectionPatterns)
            {
                if (upperInput.Contains(pattern))
                {
                    return true;
                }
            }

            return false;
        }

        private static PasswordStrength CalculatePasswordStrength(string password)
        {
            int score = 0;

            // Length scoring
            if (password.Length >= 8) score += 1;
            if (password.Length >= 12) score += 1;
            if (password.Length >= 16) score += 1;

            // Character type scoring
            if (password.Any(char.IsLower)) score += 1;
            if (password.Any(char.IsUpper)) score += 1;
            if (password.Any(char.IsDigit)) score += 1;
            if (password.Any(c => !char.IsLetterOrDigit(c))) score += 1;

            // Complexity scoring
            if (HasRepeatingPatterns(password)) score -= 1;
            if (HasSequentialPatterns(password)) score -= 1;
            if (IsCommonPassword(password)) score -= 2;

            return score switch
            {
                <= 2 => PasswordStrength.VeryWeak,
                3 => PasswordStrength.Weak,
                4 => PasswordStrength.Medium,
                5 => PasswordStrength.Strong,
                >= 6 => PasswordStrength.VeryStrong,
            };
        }

        private static bool HasRepeatingPatterns(string password)
        {
            for (int i = 0; i < password.Length - 2; i++)
            {
                if (password[i] == password[i + 1] && password[i] == password[i + 2])
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasSequentialPatterns(string password)
        {
            var sequences = new[] { "123", "234", "345", "456", "567", "678", "789", "abc", "bcd", "cde", "def" };
            var upperPassword = password.ToUpperInvariant();
            return sequences.Any(seq => upperPassword.Contains(seq) || upperPassword.Contains(seq.ToUpperInvariant()));
        }

        private static bool IsCommonPassword(string password)
        {
            var commonPasswords = new[]
            {
                "password", "123456", "123456789", "qwerty", "abc123", "password123",
                "admin", "letmein", "welcome", "monkey", "dragon", "master"
            };
            return commonPasswords.Contains(password.ToLowerInvariant());
        }
    }

    /// <summary>
    /// Generic validation result with typed value
    /// </summary>
    /// <typeparam name="T">Type of the validated value</typeparam>
    public class ValidationResult<T>
    {
        public bool IsValid { get; private set; }
        public string? ErrorMessage { get; private set; }
        public T? Value { get; private set; }

        private ValidationResult(bool isValid, string? errorMessage, T? value)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
            Value = value;
        }

        public static ValidationResult<T> Valid(T value)
        {
            return new ValidationResult<T>(true, null, value);
        }

        public static ValidationResult<T> Invalid(string errorMessage)
        {
            return new ValidationResult<T>(false, errorMessage, default);
        }
    }

    /// <summary>
    /// Password strength enumeration
    /// </summary>
    public enum PasswordStrength
    {
        VeryWeak,
        Weak,
        Medium,
        Strong,
        VeryStrong
    }
}
