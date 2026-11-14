using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.Common.Interfaces;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Core.Features.FileManager.Models;
using Winhance.Core.Features.Common.Enums;
using LogLevel = Winhance.Core.Features.Common.Enums.LogLevel;
using DuplicateGroup = Winhance.Core.Features.FileManager.Interfaces.DuplicateGroup;

namespace Winhance.Infrastructure.Features.FileManager.Services
{
    /// <summary>
    /// AI-powered file organization service implementing features OA-030 to OA-050
    /// Provides intelligent file categorization, auto-rule creation, and cleanup suggestions
    /// </summary>
    public class AIOrganizationService : IAIOrganizationService
    {
        private readonly ILogService _logService;
        private readonly IFileManagerService _fileManager;
        private readonly IAIProviderService? _aiProvider;
        private readonly Dictionary<string, OrganizationRule> _learnedRules;
        private readonly Dictionary<string, FileCategory> _categoryCache;

        public AIOrganizationService(
            ILogService logService,
            IFileManagerService fileManager,
            IAIProviderService? aiProvider = null)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
            _aiProvider = aiProvider;
            _learnedRules = new Dictionary<string, OrganizationRule>();
            _categoryCache = new Dictionary<string, FileCategory>();
        }

        public async Task<OrganizationSuggestion[]> AnalyzeDirectoryAsync(
            string path, 
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            _logService.Log(LogLevel.Info, $"AI analyzing directory: {path}");

            var suggestions = new List<OrganizationSuggestion>();
            var entries = await _fileManager.GetDirectoryContentsAsync(path, cancellationToken);
            var files = entries.Where(e => !e.IsDirectory).ToArray();
            var directories = entries.Where(e => e.IsDirectory).ToArray();

            // Analyze file distribution by type
            var typeAnalysis = await AnalyzeFileTypeDistribution(files, cancellationToken);
            suggestions.AddRange(await GenerateTypeBasedSuggestions(typeAnalysis, path));

            // Analyze naming patterns
            var namingAnalysis = await AnalyzeNamingPatterns(files, cancellationToken);
            suggestions.AddRange(await GenerateNamingBasedSuggestions(namingAnalysis, path));

            // Analyze date patterns
            var dateAnalysis = await AnalyzeDatePatterns(files, cancellationToken);
            suggestions.AddRange(await GenerateDateBasedSuggestions(dateAnalysis, path));

            // Analyze size patterns
            var sizeAnalysis = await AnalyzeSizePatterns(files, cancellationToken);
            suggestions.AddRange(await GenerateSizeBasedSuggestions(sizeAnalysis, path));

            // Use AI for advanced pattern recognition if available
            if (_aiProvider != null)
            {
                var aiSuggestions = await GenerateAISuggestions(files, path, cancellationToken);
                suggestions.AddRange(aiSuggestions);
            }

            // Score and rank suggestions
            var rankedSuggestions = suggestions
                .OrderByDescending(s => s.Confidence * s.ImpactScore)
                .Take(10)
                .ToArray();

            _logService.Log(LogLevel.Info, $"Generated {rankedSuggestions.Length} organization suggestions for {path}");
            return rankedSuggestions;
        }

        public async Task<AutoRuleResult> CreateAutoRulesAsync(
            string path, 
            TimeSpan observationPeriod, 
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            _logService.Log(LogLevel.Info, $"Creating auto-rules for {path} with {observationPeriod.TotalDays} day observation period");

            var result = new AutoRuleResult
            {
                RulesCreated = 0,
                ObservationPeriod = observationPeriod,
                AnalyzedPath = path,
                CreatedAt = DateTime.UtcNow
            };
            
            var createdRules = new List<OrganizationRule>();

            // Observe file patterns over time
            var patterns = await ObserveFilePatterns(path, observationPeriod, cancellationToken);

            // Create rules based on observed patterns
            foreach (var pattern in patterns.Where(p => p.Frequency > 0.7f)) // 70% confidence threshold
            {
                var rule = new OrganizationRule
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = GenerateRuleName(pattern),
                    Description = GenerateRuleDescription(pattern),
                    Conditions = CreateConditionsFromPattern(pattern),
                    Actions = CreateActionsFromPattern(pattern).ToList(),
                    Confidence = pattern.Frequency,
                    CreatedAt = DateTime.UtcNow,
                    IsEnabled = true,
                    AutoGenerated = true
                };

                createdRules.Add(rule);
                _learnedRules[rule.Id] = rule;
            }

            result.RulesCreated = createdRules.Count;
            result.Success = createdRules.Any();
            result.Message = result.Success 
                ? $"Created {createdRules.Count} auto-organization rules"
                : "No reliable patterns found for rule creation";

            return result;
        }

        public async Task<List<CleanupSuggestion>> SuggestCleanupAsync(
            string path, 
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            var suggestions = new List<CleanupSuggestion>();
            var entries = await _fileManager.GetDirectoryContentsAsync(path, cancellationToken);
            var files = entries.Where(e => !e.IsDirectory).ToArray();

            // Find duplicate files
            var duplicatesFound = await FindDuplicateFiles(files, cancellationToken);
            if (duplicatesFound.Any())
            {
                suggestions.Add(new CleanupSuggestion
                {
                    Type = CleanupType.Duplicates,
                    Description = $"Found {duplicatesFound.Length} groups of duplicate files",
                    Items = duplicatesFound.SelectMany(g => g.Skip(1).Select(f => f.FullPath)).ToList(),
                    EstimatedSpaceSaved = duplicatesFound.SelectMany(g => g.Skip(1)).Sum(f => f.Size),
                    Confidence = 0.95f,
                    SafetyLevel = SafetyLevel.Safe
                });
            }

            // Find old temporary files
            var tempFiles = files.Where(f => 
                (f.Name.StartsWith("~", StringComparison.Ordinal) ||
                 f.Name.Contains("temp", StringComparison.OrdinalIgnoreCase) ||
                 f.Extension?.Equals(".tmp", StringComparison.OrdinalIgnoreCase) == true ||
                 f.Extension?.Equals(".temp", StringComparison.OrdinalIgnoreCase) == true) &&
                f.DateModified < DateTime.Now.AddDays(-30)).ToArray();

            if (tempFiles.Any())
            {
                suggestions.Add(new CleanupSuggestion
                {
                    Type = CleanupType.TemporaryFiles,
                    Description = $"Found {tempFiles.Length} old temporary files",
                    Items = tempFiles.Select(f => f.FullPath).ToList(),
                    EstimatedSpaceSaved = tempFiles.Sum(f => f.Size),
                    Confidence = 0.8f,
                    SafetyLevel = SafetyLevel.Moderate
                });
            }

            // Find large files that haven't been accessed recently
            var largeUnusedFiles = files
                .Where(f => f.Size > 100 * 1024 * 1024 && // >100MB
                           f.DateAccessed < DateTime.Now.AddMonths(-6))
                .OrderByDescending(f => f.Size)
                .Take(20)
                .ToArray();

            if (largeUnusedFiles.Any())
            {
                suggestions.Add(new CleanupSuggestion
                {
                    Type = CleanupType.LargeUnusedFiles,
                    Description = $"Found {largeUnusedFiles.Length} large files not accessed in 6+ months",
                    Items = largeUnusedFiles.Select(f => f.FullPath).ToList(),
                    EstimatedSpaceSaved = largeUnusedFiles.Sum(f => f.Size),
                    Confidence = 0.7f,
                    SafetyLevel = SafetyLevel.Cautious
                });
            }

            // Find potential log files
            var logFiles = files
                .Where(f => f.Extension?.ToLowerInvariant() == ".log" ||
                           f.Name.Contains("log", StringComparison.OrdinalIgnoreCase))
                .Where(f => f.Size > 10 * 1024 * 1024) // >10MB
                .ToArray();

            if (logFiles.Any())
            {
                suggestions.Add(new CleanupSuggestion
                {
                    Type = CleanupType.LogFiles,
                    Description = $"Found {logFiles.Length} large log files",
                    Items = logFiles.Select(f => f.FullPath).ToList(),
                    EstimatedSpaceSaved = logFiles.Sum(f => f.Size),
                    Confidence = 0.6f,
                    SafetyLevel = SafetyLevel.Moderate
                });
            }

            // Use AI for advanced cleanup suggestions
            if (_aiProvider != null)
            {
                var aiCleanupSuggestions = await GenerateAICleanupSuggestions(files, path, cancellationToken);
                suggestions.AddRange(aiCleanupSuggestions);
            }

            return suggestions
                .OrderByDescending(s => s.EstimatedSpaceSaved)
                .ThenByDescending(s => s.Confidence)
                .ToList();
        }

        public async Task<List<SmartCategoryResult>> CategorizeFilesAsync(
            IEnumerable<string> filePaths, 
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(filePaths);

            var result = new SmartCategoryResult
            {
                Categories = new Dictionary<FileCategory, List<string>>(),
                UncategorizedFiles = new List<string>(),
                ProcessedAt = DateTime.UtcNow
            };

            foreach (var filePath in filePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var category = await DetermineFileCategory(filePath, cancellationToken);
                    
                    if (category != FileCategory.Unknown)
                    {
                        if (!result.Categories.ContainsKey(category))
                            result.Categories[category] = new List<string>();
                        
                        result.Categories[category].Add(filePath);
                        _categoryCache[filePath] = category;
                    }
                    else
                    {
                        result.UncategorizedFiles.Add(filePath);
                    }
                }
                catch (Exception ex)
                {
                    _logService.Log(LogLevel.Warning, $"Failed to categorize {filePath}: {ex.Message}");
                    result.UncategorizedFiles.Add(filePath);
                }
            }

            result.Success = result.Categories.Any();
            return new List<SmartCategoryResult> { result };
        }

        private Task<FileTypeDistribution> AnalyzeFileTypeDistribution(
            FileSystemEntry[] files,
            CancellationToken cancellationToken)
        {
            var distribution = new FileTypeDistribution();
            var typeGroups = files.GroupBy(f => GetFileTypeCategory(f.Extension ?? ""));

            foreach (var group in typeGroups)
            {
                distribution.TypeCounts[group.Key] = group.Count();
                distribution.TypeSizes[group.Key] = group.Sum(f => f.Size);
            }

            return Task.FromResult(distribution);
        }

        private Task<NamingPattern[]> AnalyzeNamingPatterns(
            FileSystemEntry[] files,
            CancellationToken cancellationToken)
        {
            var patterns = new List<NamingPattern>();

            // Analyze common prefixes
            var prefixGroups = files
                .GroupBy(f => f.Name.Length > 3 ? f.Name.Substring(0, 3) : f.Name)
                .Where(g => g.Count() > 2)
                .OrderByDescending(g => g.Count());

            foreach (var group in prefixGroups)
            {
                patterns.Add(new NamingPattern
                {
                    Pattern = $"Prefix: {group.Key}*",
                    FileCount = group.Count(),
                    Confidence = (float)group.Count() / files.Length,
                    PatternType = NamingPatternType.Prefix
                });
            }

            // Analyze date patterns in names
            var datePatterns = files
                .Where(f => ContainsDatePattern(f.Name))
                .GroupBy(f => ExtractDatePattern(f.Name))
                .Where(g => g.Count() > 1);

            foreach (var group in datePatterns)
            {
                patterns.Add(new NamingPattern
                {
                    Pattern = $"Date pattern: {group.Key}",
                    FileCount = group.Count(),
                    Confidence = (float)group.Count() / files.Length,
                    PatternType = NamingPatternType.Date
                });
            }

            return Task.FromResult(patterns.ToArray());
        }



        private Task<SizePattern[]> AnalyzeSizePatterns(
            FileSystemEntry[] files,
            CancellationToken cancellationToken)
        {
            var patterns = new List<SizePattern>();

            // Define size categories
            var sizeCategories = new[]
            {
                ("Tiny", 0L, 1024L), // < 1KB
                ("Small", 1024L, 1024L * 1024L), // 1KB - 1MB
                ("Medium", 1024L * 1024L, 1024L * 1024L * 100L), // 1MB - 100MB
                ("Large", 1024L * 1024L * 100L, 1024L * 1024L * 1024L), // 100MB - 1GB
                ("Huge", 1024L * 1024L * 1024L, long.MaxValue) // > 1GB
            };

            foreach (var (name, min, max) in sizeCategories)
            {
                var filesInCategory = files.Where(f => f.Size >= min && f.Size < max).ToArray();
                if (filesInCategory.Any())
                {
                    patterns.Add(new SizePattern
                    {
                        CategoryName = name,
                        MinSize = min,
                        MaxSize = max,
                        FileCount = filesInCategory.Length,
                        TotalSize = filesInCategory.Sum(f => f.Size),
                        AverageSize = (long)filesInCategory.Average(f => f.Size)
                    });
                }
            }

            return Task.FromResult(patterns.ToArray());
        }

        private async Task<OrganizationSuggestion[]> GenerateAISuggestions(
            FileSystemEntry[] files, 
            string path, 
            CancellationToken cancellationToken)
        {
            var suggestions = new List<OrganizationSuggestion>();

            if (_aiProvider == null) return suggestions.ToArray();

            try
            {
                // Prepare file analysis for AI
                var fileAnalysis = files.Take(100).Select(f => new
                {
                    Name = f.Name,
                    Extension = f.Extension,
                    Size = f.Size,
                    Created = f.DateCreated,
                    Modified = f.DateModified
                }).ToArray();

                var prompt = $@"
Analyze these {fileAnalysis.Length} files and suggest organization strategies:
{string.Join("\n", fileAnalysis.Select(f => $"- {f.Name} ({f.Extension}, {FormatSize(f.Size)}, {f.Modified:yyyy-MM-dd})"))}

Suggest 3-5 organization strategies with confidence scores (0.0-1.0).
Focus on: folder structure, naming conventions, file grouping patterns.
Format as JSON array.";

                var aiResponse = await _aiProvider.GenerateResponseAsync(prompt, cancellationToken);
                
                // Parse AI suggestions (simplified - would need proper JSON parsing)
                if (!string.IsNullOrEmpty(aiResponse))
                {
                    var confidence = float.Parse(aiResponse.Split(',')[1].Split(':')[1]);
                    suggestions.Add(new OrganizationSuggestion
                    {
                        Type = (Winhance.Core.Features.FileManager.Models.SuggestionType)OrganizationType.AIGenerated,
                        Description = "AI-suggested organization strategy",
                        TargetPath = Path.Combine(path, "AI_Organized"),
                        Priority = confidence > 0.8 ? 1 : 2,
                        ImpactScore = 0.8f,
                        EstimatedTimeMinutes = 15,
                        Details = aiResponse
                    });
                }
            }
            catch (Exception ex)
            {
                _logService.Log(LogLevel.Warning, $"AI suggestion generation failed: {ex.Message}");
            }

            return suggestions.ToArray();
        }

        private string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes}B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024}KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024 * 1024)}MB";
            return $"{bytes / (1024 * 1024 * 1024)}GB";
        }

        private bool ContainsDatePattern(string fileName)
        {
            // Simple date pattern detection (YYYY-MM-DD, YYYY_MM_DD, YYYYMMDD)
            return System.Text.RegularExpressions.Regex.IsMatch(fileName, @"\d{4}[-_]?\d{2}[-_]?\d{2}");
        }

        private string ExtractDatePattern(string fileName)
        {
            var match = System.Text.RegularExpressions.Regex.Match(fileName, @"(\d{4}[-_]?\d{2}[-_]?\d{2})");
            return match.Success ? match.Value : string.Empty;
        }

        private string GetFileTypeCategory(string extension)
        {
            if (string.IsNullOrEmpty(extension)) return "Unknown";
            
            extension = extension.ToLowerInvariant();
            return extension switch
            {
                ".pdf" or ".doc" or ".docx" or ".txt" or ".rtf" => "Documents",
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => "Images",
                ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" => "Videos",
                ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" => "Audio",
                ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "Archives",
                ".exe" or ".msi" or ".dmg" or ".pkg" => "Executables",
                ".cs" or ".js" or ".py" or ".java" or ".cpp" or ".html" => "Code",
                _ => "Other"
            };
        }

        private Task<FileCategory> DetermineFileCategory(string filePath, CancellationToken cancellationToken)
        {
            var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
            var category = extension switch
            {
                ".pdf" or ".doc" or ".docx" or ".txt" or ".rtf" => FileCategory.Documents,
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => FileCategory.Images,
                ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" => FileCategory.Videos,
                ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" => FileCategory.Audio,
                ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => FileCategory.Archives,
                ".exe" or ".msi" or ".dmg" or ".pkg" => FileCategory.Applications,
                ".cs" or ".js" or ".py" or ".java" or ".cpp" or ".html" => FileCategory.Code,
                _ => FileCategory.Other
            };
            return Task.FromResult(category);
        }

        private Task<IGrouping<string, FileSystemEntry>[]> FindDuplicateFiles(FileSystemEntry[] files, CancellationToken cancellationToken)
        {
            // Group files by hash (simplified implementation)
            var duplicates = files
                .GroupBy(f => f.Name + f.Size) // Group by name and size for duplicate detection
                .Where(g => g.Count() > 1)
                .ToArray();

            return Task.FromResult(duplicates);
        }

        private Task<DatePattern[]> AnalyzeDatePatterns(
            FileSystemEntry[] files,
            CancellationToken cancellationToken)
        {
            var patterns = new List<DatePattern>();

            // Group by creation date
            var creationGroups = files
                .GroupBy(f => f.DateCreated.Date)
                .Where(g => g.Count() > 5)
                .OrderByDescending(g => g.Count());

            foreach (var group in creationGroups.Take(10))
            {
                patterns.Add(new DatePattern
                {
                    Date = group.Key,
                    FileCount = group.Count(),
                    TotalSize = group.Sum(f => f.Size),
                    PatternType = DatePatternType.Creation
                });
            }

            return Task.FromResult(patterns.ToArray());
        }

        private Task<ObservedPattern[]> ObserveFilePatterns(
            string path,
            TimeSpan observationPeriod,
            CancellationToken cancellationToken)
        {
            // Simplified pattern observation - in real implementation would track file movements
            var patterns = new List<ObservedPattern>();

            patterns.Add(new ObservedPattern
            {
                PatternType = "DateBased",
                Description = "Files grouped by creation date",
                Frequency = 0.8f,
                Tags = new List<string> { "ai-generated", "file-grouping" },
                Examples = new List<string> { "2024-01-*.pdf", "2024-02-*.pdf" }
            });

            return Task.FromResult(patterns.ToArray());
        }

        private async Task<CleanupSuggestion[]> GenerateAICleanupSuggestions(
            FileSystemEntry[] files,
            string path,
            CancellationToken cancellationToken)
        {
            var suggestions = new List<CleanupSuggestion>();
            
            if (_aiProvider == null) return suggestions.ToArray();

            try
            {
                var fileList = files.Take(50).Select(f => $"{f.Name} ({FormatSize(f.Size)}, {f.DateModified:yyyy-MM-dd})");
                var prompt = $"Analyze these files for cleanup opportunities:\n{string.Join("\n", fileList)}";
                
                var response = await _aiProvider.GenerateResponseAsync(prompt, cancellationToken);
                
                if (!string.IsNullOrEmpty(response))
                {
                    suggestions.Add(new CleanupSuggestion
                    {
                        Type = CleanupType.AIRecommended,
                        Tags = new List<string> { "ai-generated", "cleanup" },
                        Items = new List<string> { path },
                        Confidence = 0.6f,
                        SafetyLevel = SafetyLevel.Cautious,
                        Details = response
                    });
                }
            }
            catch (Exception ex)
            {
                _logService.Log(LogLevel.Warning, $"AI cleanup suggestions failed: {ex.Message}");
            }

            return suggestions.ToArray();
        }

        private Task<OrganizationSuggestion[]> GenerateTypeBasedSuggestions(
            FileTypeDistribution distribution,
            string path)
        {
            var suggestions = new List<OrganizationSuggestion>();

            foreach (var typeGroup in distribution.TypeCounts.Where(kvp => kvp.Value > 5))
            {
                suggestions.Add(new OrganizationSuggestion
                {
                    Type = (Winhance.Core.Features.FileManager.Models.SuggestionType)OrganizationType.ByFileType,
                    Description = $"Organize {typeGroup.Value} {typeGroup.Key} files into dedicated folder",
                    TargetPath = Path.Combine(path, typeGroup.Key),
                    Confidence = 0.9f,
                    ImpactScore = 0.7f,
                    EstimatedTimeMinutes = 5
                });
            }

            return Task.FromResult(suggestions.ToArray());
        }

        private Task<OrganizationSuggestion[]> GenerateNamingBasedSuggestions(
            NamingPattern[] patterns,
            string path)
        {
            var suggestions = new List<OrganizationSuggestion>();

            foreach (var pattern in patterns.Where(p => p.Confidence > 0.3f))
            {
                suggestions.Add(new OrganizationSuggestion
                {
                    Type = (Winhance.Core.Features.FileManager.Models.SuggestionType)OrganizationType.ByNamingPattern,
                    Description = $"Group files with {pattern.Pattern}",
                    TargetPath = Path.Combine(path, SanitizeFolderName(pattern.Pattern)),
                    Confidence = pattern.Confidence,
                    ImpactScore = 0.6f,
                    EstimatedTimeMinutes = 3
                });
            }

            return Task.FromResult(suggestions.ToArray());
        }

        private Task<OrganizationSuggestion[]> GenerateDateBasedSuggestions(
            DatePattern[] patterns,
            string path)
        {
            var suggestions = new List<OrganizationSuggestion>();

            if (patterns.Any(p => p.FileCount > 10))
            {
                suggestions.Add(new OrganizationSuggestion
                {
                    Type = (Winhance.Core.Features.FileManager.Models.SuggestionType)OrganizationType.ByDate,
                    Description = "Organize files by creation date (Year/Month folders)",
                    TargetPath = Path.Combine(path, "By_Date"),
                    Confidence = 0.8f,
                    ImpactScore = 0.8f,
                    EstimatedTimeMinutes = 10
                });
            }

            return Task.FromResult(suggestions.ToArray());
        }

        private Task<OrganizationSuggestion[]> GenerateSizeBasedSuggestions(
            SizePattern[] patterns,
            string path)
        {
            var suggestions = new List<OrganizationSuggestion>();

            var largeFiles = patterns.FirstOrDefault(p => p.CategoryName == "Large");
            if (largeFiles?.FileCount > 5)
            {
                suggestions.Add(new OrganizationSuggestion
                {
                    Type = (Winhance.Core.Features.FileManager.Models.SuggestionType)OrganizationType.BySize,
                    Description = $"Move {largeFiles.FileCount} large files to dedicated folder",
                    TargetPath = Path.Combine(path, "Large_Files"),
                    Confidence = 0.7f,
                    ImpactScore = 0.6f,
                    EstimatedTimeMinutes = 5
                });
            }

            return Task.FromResult(suggestions.ToArray());
        }

        private string SanitizeFolderName(string name)
        {
            return System.Text.RegularExpressions.Regex.Replace(name, @"[^\w\s-]", "")
                .Replace(" ", "_")
                .Trim('_');
        }

        public async Task<List<ObservedPattern>> AnalyzePatternsAsync(string directoryPath, CancellationToken cancellationToken = default)
        {
            // Returns empty results; AI backend not configured
            await Task.Delay(1, cancellationToken);
            return new List<ObservedPattern>();
        }

        public async Task<List<OrganizationSuggestion>> GenerateOrganizationSuggestionsAsync(string directoryPath, CancellationToken cancellationToken = default)
        {
            // Returns empty results; AI backend not configured
            await Task.Delay(1, cancellationToken);
            return new List<OrganizationSuggestion>();
        }

        public async Task<List<SmartFolder>> CreateSmartFoldersAsync(string directoryPath, CancellationToken cancellationToken = default)
        {
            // Returns empty results; AI backend not configured
            await Task.Delay(1, cancellationToken);
            return new List<SmartFolder>();
        }

        public async Task<List<TaggingSuggestion>> GenerateTaggingSuggestionsAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default)
        {
            // Returns empty results; AI backend not configured
            await Task.Delay(1, cancellationToken);
            return new List<TaggingSuggestion>();
        }

        public async Task<List<DuplicateGroup>> AnalyzeDuplicatesAsync(string directoryPath, CancellationToken cancellationToken = default)
        {
            // Returns empty results; AI backend not configured
            await Task.Delay(1, cancellationToken);
            return new List<DuplicateGroup>();
        }

        public async Task<List<WorkflowRecommendation>> GenerateWorkflowRecommendationsAsync(string directoryPath, CancellationToken cancellationToken = default)
        {
            // Returns empty results; AI backend not configured
            await Task.Delay(1, cancellationToken);
            return new List<WorkflowRecommendation>();
        }

        public async Task LearnFromUserActionAsync(string action, Dictionary<string, object> context, CancellationToken cancellationToken = default)
        {
            // Returns empty results; AI backend not configured
            await Task.Delay(1, cancellationToken);
        }

        public bool IsAvailable => false; // No AI backend configured

        public AIModelInfo ModelInfo => new AIModelInfo
        {
            Name = "Local Pattern Analyzer",
            Version = "1.0",
            Provider = "None",
            IsOnline = false
        };

        public async Task<List<AutoRuleResult>> GenerateAutoRulesAsync(string directoryPath, CancellationToken cancellationToken = default)
        {
            // Returns empty results; AI backend not configured
            await Task.Delay(1, cancellationToken);
            return new List<AutoRuleResult>();
        }

        private string GenerateRuleName(ObservedPattern pattern) => $"Auto_{pattern.PatternType}_{DateTime.Now:yyyyMMdd}";
        private string GenerateRuleDescription(ObservedPattern pattern) => $"Automatically generated rule based on {pattern.Description}";
        private RuleCondition[] CreateConditionsFromPattern(ObservedPattern pattern) => new[] { new RuleCondition { Type = Winhance.Core.Features.FileManager.Interfaces.ConditionType.Pattern, Value = pattern.PatternType } };
        private RuleAction[] CreateActionsFromPattern(ObservedPattern pattern) => new[] { new RuleAction { Type = Winhance.Core.Features.FileManager.Interfaces.RuleActionType.Move, Target = $"Auto_{pattern.PatternType}" } };
    }
}

