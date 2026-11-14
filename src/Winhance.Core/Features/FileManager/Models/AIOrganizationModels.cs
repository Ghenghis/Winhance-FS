using System;
using System.Collections.Generic;

namespace Winhance.Core.Features.FileManager.Models
{
    /// <summary>
    /// Observed file organization pattern
    /// </summary>
    public class ObservedPattern
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public PatternType Type { get; set; }
        public string PatternType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<PatternRule> Rules { get; set; } = new();
        public List<string> Examples { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public double Confidence { get; set; }
        public float Frequency { get; set; }
        public DateTime FirstObserved { get; set; }
        public DateTime LastObserved { get; set; }
        public List<string> ExamplePaths { get; set; } = new();
    }

    /// <summary>
    /// Pattern rule definition
    /// </summary>
    public class PatternRule
    {
        public string Condition { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public double Weight { get; set; } = 1.0;
        public Dictionary<string, string> Parameters { get; set; } = new();
    }

    /// <summary>
    /// AI organization suggestion
    /// </summary>
    public class OrganizationSuggestion
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SourcePath { get; set; } = string.Empty;
        public string TargetPath { get; set; } = string.Empty;
        public SuggestionType Type { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public double ImpactScore { get; set; }
        public int Priority { get; set; }
        public int EstimatedTimeMinutes { get; set; }
        public List<string> Tags { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public SuggestionStatus Status { get; set; } = SuggestionStatus.Pending;
    }

    /// <summary>
    /// Organization type enumeration
    /// </summary>
    public enum OrganizationType
    {
        FileGrouping,
        FolderStructure,
        DuplicateRemoval,
        CategoryBasedOrganization,
        DateBasedOrganization,
        SizeBasedOrganization,
        ByFileType,
        ByNamingPattern,
        ByDate,
        BySize,
        AIGenerated,
        Custom
    }

    /// <summary>
    /// Smart folder definition
    /// </summary>
    public class SmartFolder
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public List<SmartFolderRule> Rules { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public int FileCount { get; set; }
    }

    /// <summary>
    /// Smart folder rule
    /// </summary>
    public class SmartFolderRule
    {
        public string Field { get; set; } = string.Empty;
        public RuleOperator Operator { get; set; }
        public string Value { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Auto-tagging suggestion
    /// </summary>
    public class TaggingSuggestion
    {
        public string FilePath { get; set; } = string.Empty;
        public List<string> SuggestedTags { get; set; } = new();
        public double Confidence { get; set; }
        public string Source { get; set; } = string.Empty; // "AI", "Pattern", "User"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Duplicate group from AI analysis
    /// </summary>
    public class DuplicateGroup
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public List<DuplicateItem> Items { get; set; } = new();
        public DuplicateType Type { get; set; }
        public double Confidence { get; set; }
        public long TotalSize { get; set; }
        public long PotentialSavings { get; set; }
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        
        // Additional properties for compatibility with OrganizerService
        public string Key { get; set; } = string.Empty;
        public List<DuplicateItem> Files { get; set; } = new();
        public long WastedSize { get; set; }
        public int FileCount { get; set; }
    }

    /// <summary>
    /// Duplicate item information
    /// </summary>
    public class DuplicateItem
    {
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Hash { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public bool IsRecommendedForDeletion { get; set; }
        public string? RecommendationReason { get; set; }
    }

    /// <summary>
    /// Workflow recommendation from AI
    /// </summary>
    public class WorkflowRecommendation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<AIWorkflowStep> Steps { get; set; } = new();
        public WorkflowTrigger Trigger { get; set; } = new();
        public double Confidence { get; set; }
        public int EstimatedTimeSavings { get; set; } // in minutes
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// AI workflow step definition
    /// </summary>
    public class AIWorkflowStep
    {
        public int Order { get; set; }
        public string Action { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public string? Condition { get; set; }
    }

    /// <summary>
    /// Workflow trigger definition
    /// </summary>
    public class WorkflowTrigger
    {
        public TriggerType Type { get; set; }
        public string Pattern { get; set; } = string.Empty;
        public Dictionary<string, object> Configuration { get; set; } = new();
    }

    /// <summary>
    /// Pattern type enumeration
    /// </summary>
    public enum PatternType
    {
        Naming,
        Organization,
        Usage,
        Temporal,
        FileType,
        Size
    }

    /// <summary>
    /// Suggestion type enumeration
    /// </summary>
    public enum SuggestionType
    {
        Move,
        Rename,
        Delete,
        Archive,
        Tag,
        Duplicate
    }

    /// <summary>
    /// Suggestion status enumeration
    /// </summary>
    public enum SuggestionStatus
    {
        Pending,
        Accepted,
        Rejected,
        Applied,
        Failed
    }

    /// <summary>
    /// Rule operator enumeration
    /// </summary>
    public enum RuleOperator
    {
        Equals,
        NotEquals,
        Contains,
        NotContains,
        StartsWith,
        EndsWith,
        GreaterThan,
        LessThan,
        Matches,
        InRange
    }

    /// <summary>
    /// Duplicate type enumeration
    /// </summary>
    public enum DuplicateType
    {
        Exact,
        Similar,
        NameOnly,
        ContentSimilar
    }

}
