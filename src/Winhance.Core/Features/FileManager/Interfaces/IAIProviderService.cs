using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.FileManager.Models;

namespace Winhance.Core.Features.FileManager.Interfaces
{
    /// <summary>
    /// Identifies the AI backend provider to use
    /// </summary>
    public enum AIProviderType
    {
        MiniMax,
        DeepSeek,
        LmStudio,
        Ollama
    }

    /// <summary>
    /// Manages AI provider selection, configuration, and lifecycle
    /// </summary>
    public interface IAIProviderManager
    {
        /// <summary>Gets the currently active provider service, or null if unconfigured.</summary>
        IAIProviderService? ActiveProvider { get; }

        /// <summary>Gets or sets the active provider type.</summary>
        AIProviderType ActiveProviderType { get; set; }

        /// <summary>API key for cloud providers (MiniMax, DeepSeek). Never store in source code.</summary>
        string ApiKey { get; set; }

        /// <summary>Base URL for local providers (LM Studio, Ollama).</summary>
        string BaseUrl { get; set; }

        /// <summary>Currently selected model identifier.</summary>
        string SelectedModel { get; set; }

        /// <summary>Returns the list of models available for the active provider.</summary>
        Task<IReadOnlyList<string>> GetAvailableModelsAsync(CancellationToken ct = default);

        /// <summary>Returns true if the provider is reachable and authenticated.</summary>
        Task<bool> TestConnectionAsync(CancellationToken ct = default);

        /// <summary>Raised when the active provider or its configuration changes.</summary>
        event EventHandler? ProviderChanged;
    }


    /// <summary>
    /// AI provider service for intelligent file operations and analysis
    /// </summary>
    public interface IAIProviderService
    {
        /// <summary>
        /// Analyze text content and extract insights
        /// </summary>
        Task<ContentAnalysisResult> AnalyzeContentAsync(string content, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generate organization suggestions based on file patterns
        /// </summary>
        Task<List<OrganizationSuggestion>> GenerateOrganizationSuggestionsAsync(
            IEnumerable<string> filePaths, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Categorize files using AI analysis
        /// </summary>
        Task<SmartCategoryResult> CategorizeFileAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generate smart tags for files
        /// </summary>
        Task<List<TaggingSuggestion>> GenerateTagsAsync(
            IEnumerable<string> filePaths, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyze naming patterns in directories
        /// </summary>
        Task<List<NamingPattern>> AnalyzeNamingPatternsAsync(
            string directoryPath, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generate cleanup suggestions
        /// </summary>
        Task<List<CleanupSuggestion>> GenerateCleanupSuggestionsAsync(
            string directoryPath, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Predict file organization based on user behavior
        /// </summary>
        Task<List<WorkflowRecommendation>> PredictWorkflowsAsync(
            string directoryPath, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generate a response from AI for a given prompt
        /// </summary>
        Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if AI provider is available and ready
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Get the current AI model information
        /// </summary>
        AIModelInfo ModelInfo { get; }
    }

    /// <summary>
    /// Content analysis result from AI
    /// </summary>
    public class ContentAnalysisResult
    {
        public string Summary { get; set; } = string.Empty;
        public List<string> KeyPhrases { get; set; } = new();
        public List<string> Entities { get; set; } = new();
        public Dictionary<string, double> Topics { get; set; } = new();
        public ContentCategory Category { get; set; }
        public double Confidence { get; set; }
        public string Language { get; set; } = "en";
    }

    /// <summary>
    /// AI model information
    /// </summary>
    public class AIModelInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public List<string> Capabilities { get; set; } = new();
        public DateTime LastUpdated { get; set; }
        public bool IsOnline { get; set; }
    }

    /// <summary>
    /// Content category enumeration
    /// </summary>
    public enum ContentCategory
    {
        Document,
        Code,
        Data,
        Media,
        Archive,
        System,
        Personal,
        Business,
        Academic,
        Other
    }
}
