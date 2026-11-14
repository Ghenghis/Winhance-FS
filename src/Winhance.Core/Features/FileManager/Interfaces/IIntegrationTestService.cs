using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Winhance.Core.Features.FileManager.Interfaces
{
    /// <summary>
    /// Service for integration testing operations - created by SemFix constraint-based repair
    /// </summary>
    public interface IIntegrationTestService
    {
        /// <summary>
        /// Execute integration test suite
        /// </summary>
        /// <param name="testSuiteName">Name of test suite to execute</param>
        /// <returns>Test execution result</returns>
        Task<TestExecutionResult> ExecuteTestSuiteAsync(string testSuiteName);

        /// <summary>
        /// Execute individual integration test
        /// </summary>
        /// <param name="testName">Name of test to execute</param>
        /// <returns>Test result</returns>
        Task<IntegrationTestResult> ExecuteTestAsync(string testName);

        /// <summary>
        /// Get available test suites
        /// </summary>
        /// <returns>Array of test suite names</returns>
        Task<string[]> GetTestSuitesAsync();

        /// <summary>
        /// Get tests in a specific suite
        /// </summary>
        /// <param name="testSuiteName">Test suite name</param>
        /// <returns>Array of test names</returns>
        Task<string[]> GetTestsInSuiteAsync(string testSuiteName);

        /// <summary>
        /// Set up test environment
        /// </summary>
        /// <param name="environment">Environment configuration</param>
        /// <returns>Task indicating setup completion</returns>
        Task SetupTestEnvironmentAsync(TestEnvironment environment);

        /// <summary>
        /// Tear down test environment
        /// </summary>
        /// <returns>Task indicating teardown completion</returns>
        Task TeardownTestEnvironmentAsync();

        /// <summary>
        /// Generate test report
        /// </summary>
        /// <param name="results">Test results to include in report</param>
        /// <param name="reportPath">Output path for report</param>
        /// <param name="format">Report format</param>
        /// <returns>Task indicating report generation completion</returns>
        Task GenerateTestReportAsync(TestExecutionResult[] results, string reportPath, ReportFormat format = ReportFormat.Html);

        /// <summary>
        /// Validate test environment readiness
        /// </summary>
        /// <returns>Validation result</returns>
        Task<ValidationResult> ValidateEnvironmentAsync();

        /// <summary>
        /// Get test execution history
        /// </summary>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <returns>Historical test execution results</returns>
        Task<TestExecutionResult[]> GetTestHistoryAsync(int maxResults = 50);

        /// <summary>
        /// Event fired when test execution progress updates
        /// </summary>
        event EventHandler<TestProgressEventArgs>? ProgressChanged;

        /// <summary>
        /// Event fired when test completes
        /// </summary>
        event EventHandler<TestCompletedEventArgs>? TestCompleted;
    }

    /// <summary>
    /// Test execution result
    /// </summary>
    public class TestExecutionResult
    {
        /// <summary>
        /// Test suite name
        /// </summary>
        public string TestSuiteName { get; set; } = string.Empty;

        /// <summary>
        /// Execution start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Execution end time
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Total execution duration
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// Individual test results
        /// </summary>
        public List<IntegrationTestResult> TestResults { get; set; } = new();

        /// <summary>
        /// Overall result status
        /// </summary>
        public TestStatus Status { get; set; }

        /// <summary>
        /// Summary statistics
        /// </summary>
        public TestStatistics Statistics { get; set; } = new();

        /// <summary>
        /// Any errors or warnings
        /// </summary>
        public List<string> Messages { get; set; } = new();
    }

    /// <summary>
    /// Integration test result
    /// </summary>
    public class IntegrationTestResult
    {
        /// <summary>
        /// Test name
        /// </summary>
        public string TestName { get; set; } = string.Empty;

        /// <summary>
        /// Test status
        /// </summary>
        public TestStatus Status { get; set; }

        /// <summary>
        /// Execution duration
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Error message if failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Stack trace if failed
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// Test output/logs
        /// </summary>
        public string? Output { get; set; }

        /// <summary>
        /// Test assertions/verifications
        /// </summary>
        public List<TestAssertion> Assertions { get; set; } = new();
    }

    /// <summary>
    /// Test assertion result
    /// </summary>
    public class TestAssertion
    {
        /// <summary>
        /// Assertion description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Whether assertion passed
        /// </summary>
        public bool Passed { get; set; }

        /// <summary>
        /// Expected value
        /// </summary>
        public string? Expected { get; set; }

        /// <summary>
        /// Actual value
        /// </summary>
        public string? Actual { get; set; }
    }

    /// <summary>
    /// Test environment configuration
    /// </summary>
    public class TestEnvironment
    {
        /// <summary>
        /// Environment name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Database connection strings
        /// </summary>
        public Dictionary<string, string> ConnectionStrings { get; set; } = new();

        /// <summary>
        /// Environment variables
        /// </summary>
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

        /// <summary>
        /// Configuration settings
        /// </summary>
        public Dictionary<string, object> Settings { get; set; } = new();

        /// <summary>
        /// Test data files
        /// </summary>
        public string[] TestDataFiles { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Clean up after tests
        /// </summary>
        public bool CleanupAfterTests { get; set; } = true;
    }

    /// <summary>
    /// Environment validation result
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Whether environment is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation messages
        /// </summary>
        public List<ValidationMessage> Messages { get; set; } = new();

        /// <summary>
        /// Missing dependencies
        /// </summary>
        public List<string> MissingDependencies { get; set; } = new();
    }

    /// <summary>
    /// Validation message
    /// </summary>
    public class ValidationMessage
    {
        /// <summary>
        /// Message type
        /// </summary>
        public ValidationMessageType Type { get; set; }

        /// <summary>
        /// Message content
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Component that generated the message
        /// </summary>
        public string Component { get; set; } = string.Empty;
    }

    /// <summary>
    /// Test statistics
    /// </summary>
    public class TestStatistics
    {
        /// <summary>
        /// Total tests executed
        /// </summary>
        public int TotalTests { get; set; }

        /// <summary>
        /// Tests that passed
        /// </summary>
        public int PassedTests { get; set; }

        /// <summary>
        /// Tests that failed
        /// </summary>
        public int FailedTests { get; set; }

        /// <summary>
        /// Tests that were skipped
        /// </summary>
        public int SkippedTests { get; set; }

        /// <summary>
        /// Pass rate percentage
        /// </summary>
        public double PassRate => TotalTests > 0 ? (double)PassedTests / TotalTests * 100 : 0;
    }

    /// <summary>
    /// Test status
    /// </summary>
    public enum TestStatus
    {
        NotRun,
        Running,
        Passed,
        Failed,
        Skipped,
        Inconclusive
    }

    /// <summary>
    /// Report format
    /// </summary>
    public enum ReportFormat
    {
        Html,
        Xml,
        Json,
        Csv
    }

    /// <summary>
    /// Validation message type
    /// </summary>
    public enum ValidationMessageType
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Test progress event arguments
    /// </summary>
    public class TestProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Current test being executed
        /// </summary>
        public string CurrentTest { get; set; } = string.Empty;

        /// <summary>
        /// Tests completed
        /// </summary>
        public int CompletedTests { get; set; }

        /// <summary>
        /// Total tests to execute
        /// </summary>
        public int TotalTests { get; set; }

        /// <summary>
        /// Progress percentage
        /// </summary>
        public int ProgressPercentage => TotalTests > 0 ? CompletedTests * 100 / TotalTests : 0;
    }

    /// <summary>
    /// Test completed event arguments
    /// </summary>
    public class TestCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Test result
        /// </summary>
        public IntegrationTestResult Result { get; set; } = null!;
    }
}
