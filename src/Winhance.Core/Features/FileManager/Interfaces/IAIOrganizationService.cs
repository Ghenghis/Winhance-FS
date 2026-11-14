using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.FileManager.Models;

namespace Winhance.Core.Features.FileManager.Interfaces
{
    /// <summary>
    /// AI-powered organization service interface
    /// </summary>
    public interface IAIOrganizationService
    {
        /// <summary>
        /// Generate automatic rules based on observed patterns
        /// </summary>
        Task<List<AutoRuleResult>> GenerateAutoRulesAsync(string directoryPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Suggest cleanup actions for a directory
        /// </summary>
        Task<List<CleanupSuggestion>> SuggestCleanupAsync(string directoryPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Categorize files using smart analysis
        /// </summary>
        Task<List<SmartCategoryResult>> CategorizeFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyze observed patterns in file organization
        /// </summary>
        Task<List<ObservedPattern>> AnalyzePatternsAsync(string directoryPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generate organization suggestions based on AI analysis
        /// </summary>
        Task<List<OrganizationSuggestion>> GenerateOrganizationSuggestionsAsync(string directoryPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Create smart folders based on patterns
        /// </summary>
        Task<List<SmartFolder>> CreateSmartFoldersAsync(string directoryPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generate tagging suggestions
        /// </summary>
        Task<List<TaggingSuggestion>> GenerateTaggingSuggestionsAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyze duplicate files and provide recommendations
        /// </summary>
        Task<List<DuplicateGroup>> AnalyzeDuplicatesAsync(string directoryPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generate workflow recommendations
        /// </summary>
        Task<List<WorkflowRecommendation>> GenerateWorkflowRecommendationsAsync(string directoryPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Learn from user actions to improve future suggestions
        /// </summary>
        Task LearnFromUserActionAsync(string action, Dictionary<string, object> context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if AI organization service is available
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Get AI model information
        /// </summary>
        AIModelInfo ModelInfo { get; }
    }
}
