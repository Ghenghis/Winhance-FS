using System;
using System.Collections.Generic;

namespace Winhance.WPF.Features.SelfHealing
{
    public class HealingSession
    {
        public string SessionId { get; set; } = string.Empty;
        public string FeatureId { get; set; } = string.Empty;
        public string FeaturePath { get; set; } = string.Empty;
        public string OperationName { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;
        public int StartLine { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public HealingStatus Status { get; set; } = HealingStatus.Pending;
        public Exception? Error { get; set; }
        public bool HealingApplied { get; set; }
        public string HealingType { get; set; } = string.Empty;
        public bool PreventiveFixApplied { get; set; }
        public List<HealingAttempt> HealingAttempts { get; } = new();
    }

    public class HealingAttempt
    {
        public DateTime Timestamp { get; set; }
        public string ErrorType { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Details { get; set; } = string.Empty;
    }

    public enum HealingStatus
    {
        Pending,
        Running,
        Healing,
        Succeeded,
        Failed,
        Completed
    }
}
