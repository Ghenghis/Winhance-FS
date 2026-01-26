# Phase 7: Testing & Quality Assurance

> **[Back to Roadmap](PROJECT_ROADMAP.md)** | **Previous:** [Phase 6](PHASE_6_MCP_SERVER.md) | **Next:** [Phase 8](PHASE_8_RELEASE.md)

---

## Phase Overview

| Attribute | Value |
|-----------|-------|
| **Status** | `[ ]` Not Started |
| **Priority** | Critical |
| **Estimated Tasks** | 16 |
| **Dependencies** | Phases 2-6 In Progress |

---

## Objectives

1. Achieve >80% code coverage across all components
2. Create integration tests for cross-component functionality
3. Establish performance benchmarks
4. Conduct security audit
5. Set up automated testing in CI/CD

---

## Testing Strategy

```
+==============================================================================+
|                           TESTING PYRAMID                                     |
+==============================================================================+
|                                                                               |
|                          /\                                                   |
|                         /  \                                                  |
|                        /    \    E2E Tests                                    |
|                       / (5%) \   - Full workflow tests                        |
|                      /--------\  - UI automation                              |
|                     /          \                                              |
|                    /            \ Integration Tests                           |
|                   /    (15%)     \ - API tests                                |
|                  /----------------\ - Service tests                           |
|                 /                  \ - Cross-component                        |
|                /                    \                                         |
|               /      (80%)          \ Unit Tests                              |
|              /                        \ - Functions                           |
|             /                          \ - Classes                            |
|            /____________________________\ - Modules                           |
|                                                                               |
+==============================================================================+
```

---

## Task List

### 7.1 Unit Tests

#### Task 7.1.1: Rust Unit Tests

**Status:** `[ ]` Not Started

**Description:**
Write comprehensive unit tests for the Rust backend.

**Acceptance Criteria:**
- [ ] >80% code coverage
- [ ] All public functions tested
- [ ] Edge cases covered
- [ ] Error handling tested

**Files to Create:**
```
src/nexus-native/src/mft/tests.rs
src/nexus-native/src/search/tests.rs
src/nexus-native/src/index/tests.rs
src/nexus-native/src/forensics/tests.rs
```

**Implementation:**
```rust
// src/mft/tests.rs
#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_mft_entry_creation() {
        let entry = MftEntry {
            name: "test.txt".to_string(),
            size: 1024,
            is_directory: false,
            created: Some(132456789012345678),
            modified: Some(132456789012345678),
            accessed: None,
            mft_record_number: 12345,
            parent_record_number: 5,
        };

        assert_eq!(entry.name, "test.txt");
        assert_eq!(entry.size, 1024);
        assert!(!entry.is_directory);
    }

    #[test]
    fn test_mft_entry_extension() {
        let entry = MftEntry {
            name: "document.pdf".to_string(),
            size: 0,
            is_directory: false,
            ..Default::default()
        };

        assert_eq!(entry.extension(), Some("pdf"));
    }

    #[test]
    fn test_mft_entry_no_extension() {
        let entry = MftEntry {
            name: "README".to_string(),
            ..Default::default()
        };

        assert_eq!(entry.extension(), None);
    }

    #[test]
    fn test_mft_entry_hidden() {
        let entry = MftEntry {
            name: ".gitignore".to_string(),
            ..Default::default()
        };

        assert!(entry.is_hidden());
    }
}

// src/search/tests.rs
#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_simd_search_basic() {
        let searcher = SimdSearcher::new(true);
        let haystack = b"Hello, World! This is a test.";
        let needle = b"World";

        let matches = searcher.find_all(haystack, needle);
        assert_eq!(matches.len(), 1);
        assert_eq!(matches[0], 7);
    }

    #[test]
    fn test_simd_search_no_match() {
        let searcher = SimdSearcher::new(true);
        let haystack = b"Hello, World!";
        let needle = b"xyz";

        let matches = searcher.find_all(haystack, needle);
        assert!(matches.is_empty());
    }

    #[test]
    fn test_simd_search_multiple_matches() {
        let searcher = SimdSearcher::new(true);
        let haystack = b"abcabcabc";
        let needle = b"abc";

        let matches = searcher.find_all(haystack, needle);
        assert_eq!(matches.len(), 3);
    }

    #[test]
    fn test_bloom_filter_contains() {
        let mut bloom = BloomIndex::new(1000);
        bloom.insert("test.txt");
        bloom.insert("document.pdf");

        assert!(bloom.might_contain("test.txt"));
        assert!(bloom.might_contain("document.pdf"));
    }

    #[test]
    fn test_bloom_filter_not_contains() {
        let bloom = BloomIndex::new(1000);
        // Empty filter should not contain anything
        assert!(!bloom.might_contain("nonexistent.txt"));
    }
}

// src/forensics/tests.rs
#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_entropy_low() {
        let data = b"aaaaaaaaaaaaaaaaaaaaaa";
        let entropy = EntropyCalculator::calculate(data);
        assert!(entropy < 1.0);
    }

    #[test]
    fn test_entropy_high() {
        // Random-looking data
        let data: Vec<u8> = (0..255).collect();
        let entropy = EntropyCalculator::calculate(&data);
        assert!(entropy > 7.0);
    }

    #[test]
    fn test_entropy_classification() {
        assert!(matches!(
            EntropyCalculator::classify(3.5),
            EntropyClass::LowEntropy
        ));
        assert!(matches!(
            EntropyCalculator::classify(5.5),
            EntropyClass::MediumEntropy
        ));
        assert!(matches!(
            EntropyCalculator::classify(7.5),
            EntropyClass::HighEntropy
        ));
        assert!(matches!(
            EntropyCalculator::classify(7.95),
            EntropyClass::VeryHighEntropy
        ));
    }
}
```

**Next Task:** [7.1.2](#task-712-c-unit-tests)

---

#### Task 7.1.2: C# Unit Tests

**Status:** `[ ]` Not Started

**Description:**
Write unit tests for C# services and models.

**Acceptance Criteria:**
- [ ] >80% code coverage
- [ ] Service logic tested
- [ ] Model validation tested
- [ ] Mock dependencies properly

**Files to Create:**
```
tests/Winhance.Core.Tests/
tests/Winhance.Infrastructure.Tests/
```

**Implementation:**
```csharp
// FileCategorizer.Tests.cs
namespace Winhance.Infrastructure.Tests.Features.Storage;

public class FileCategorizerTests
{
    private readonly FileCategorizer _categorizer;

    public FileCategorizerTests()
    {
        _categorizer = new FileCategorizer();
    }

    [Theory]
    [InlineData("document.pdf", FileCategory.Document)]
    [InlineData("image.jpg", FileCategory.Image)]
    [InlineData("video.mp4", FileCategory.Video)]
    [InlineData("model.gguf", FileCategory.AIModel)]
    [InlineData("script.ps1", FileCategory.Executable)]
    public void Categorize_ReturnsCorrectCategory(string fileName, FileCategory expected)
    {
        var result = _categorizer.Categorize(fileName);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("README")]
    [InlineData("Makefile")]
    [InlineData("file.xyz")]
    public void Categorize_UnknownExtension_ReturnsUnknown(string fileName)
    {
        var result = _categorizer.Categorize(fileName);
        Assert.Equal(FileCategory.Unknown, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Categorize_EmptyOrNull_ReturnsUnknown(string fileName)
    {
        var result = _categorizer.Categorize(fileName);
        Assert.Equal(FileCategory.Unknown, result);
    }
}

// StorageIntelligenceService.Tests.cs
namespace Winhance.Infrastructure.Tests.Features.Storage;

public class StorageIntelligenceServiceTests
{
    private readonly Mock<INexusNative> _mockNative;
    private readonly Mock<ILogger<StorageIntelligenceService>> _mockLogger;
    private readonly Mock<IFileCategorizer> _mockCategorizer;
    private readonly StorageIntelligenceService _service;

    public StorageIntelligenceServiceTests()
    {
        _mockNative = new Mock<INexusNative>();
        _mockLogger = new Mock<ILogger<StorageIntelligenceService>>();
        _mockCategorizer = new Mock<IFileCategorizer>();

        _service = new StorageIntelligenceService(
            _mockNative.Object,
            _mockLogger.Object,
            _mockCategorizer.Object);
    }

    [Fact]
    public async Task ScanDriveAsync_ValidDrive_ReturnsSuccess()
    {
        // Arrange
        var expectedResult = new ScanResult
        {
            Entries = new List<FileEntry>(),
            FileCount = 100,
            TotalSize = 1024000
        };

        _mockNative.Setup(x => x.ScanDriveAsync(
            It.IsAny<string>(),
            It.IsAny<ScanOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<ScanResult>.Succeeded(expectedResult));

        // Act
        var result = await _service.ScanDriveAsync("C");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(100, result.Data!.FileCount);
    }

    [Fact]
    public async Task ScanDriveAsync_NativeError_ReturnsFailure()
    {
        // Arrange
        _mockNative.Setup(x => x.ScanDriveAsync(
            It.IsAny<string>(),
            It.IsAny<ScanOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<ScanResult>.Failed("Access denied"));

        // Act
        var result = await _service.ScanDriveAsync("C");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Access denied", result.ErrorMessage);
    }

    [Fact]
    public async Task SearchAsync_ValidQuery_ReturnsResults()
    {
        // Arrange
        var expectedResults = new List<SearchResult>
        {
            new() { Path = "C:\\test.txt", Name = "test.txt", Score = 1.0f }
        };

        _mockNative.Setup(x => x.SearchAsync(
            It.IsAny<string>(),
            It.IsAny<SearchOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<IReadOnlyList<SearchResult>>.Succeeded(expectedResults));

        // Act
        var result = await _service.SearchAsync("test");

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data!);
    }
}
```

**Dependencies:** [7.1.1](#task-711-rust-unit-tests)

**Next Task:** [7.1.3](#task-713-python-unit-tests)

---

#### Task 7.1.3: Python Unit Tests

**Status:** `[ ]` Not Started

**Description:**
Write unit tests for Python agents.

**Acceptance Criteria:**
- [ ] >80% code coverage
- [ ] Agent logic tested
- [ ] Mocked LLM calls
- [ ] Async tests working

**Files to Create:**
```
src/nexus-agents/tests/test_base_agent.py
src/nexus-agents/tests/test_file_discovery.py
src/nexus-agents/tests/test_orchestrator.py
```

**Implementation:**
```python
# test_base_agent.py
import pytest
from unittest.mock import Mock, AsyncMock
from nexus_agents.core.base_agent import BaseAgent, AgentState, AgentResult

class MockAgent(BaseAgent):
    """Mock agent for testing."""

    async def plan(self, task: str):
        return [{"action": "test", "input": task}]

    async def execute(self, task: str, **kwargs):
        return AgentResult(success=True, data={"task": task})


@pytest.fixture
def agent():
    return MockAgent("test_agent", "Test agent")


class TestBaseAgent:
    def test_agent_initialization(self, agent):
        assert agent.name == "test_agent"
        assert agent.description == "Test agent"
        assert agent.state == AgentState.IDLE

    def test_register_tool(self, agent):
        def test_tool():
            pass

        agent.register_tool("test_tool", test_tool, "A test tool")
        assert "test_tool" in agent.tools

    @pytest.mark.asyncio
    async def test_run_success(self, agent):
        result = await agent.run("test task")

        assert result.success
        assert agent.state == AgentState.COMPLETED

    @pytest.mark.asyncio
    async def test_run_sets_state(self, agent):
        assert agent.state == AgentState.IDLE

        await agent.run("test task")

        assert agent.state == AgentState.COMPLETED

    def test_pause_resume(self, agent):
        agent.state = AgentState.RUNNING

        agent.pause()
        assert agent.state == AgentState.PAUSED

        agent.resume()
        assert agent.state == AgentState.RUNNING


# test_file_discovery.py
import pytest
from pathlib import Path
from nexus_agents.agents.file_discovery import FileDiscoveryAgent


@pytest.fixture
def agent():
    return FileDiscoveryAgent()


@pytest.fixture
def temp_directory(tmp_path):
    # Create test files
    (tmp_path / "file1.txt").write_text("content1")
    (tmp_path / "file2.pdf").write_text("content2")
    (tmp_path / "subdir").mkdir()
    (tmp_path / "subdir" / "file3.txt").write_text("content3")
    return tmp_path


class TestFileDiscoveryAgent:
    @pytest.mark.asyncio
    async def test_list_files(self, agent, temp_directory):
        files = await agent.list_files(str(temp_directory))

        assert len(files) == 3
        names = [f["name"] for f in files]
        assert "file1.txt" in names
        assert "file2.pdf" in names

    @pytest.mark.asyncio
    async def test_list_files_non_recursive(self, agent, temp_directory):
        files = await agent.list_files(str(temp_directory), recursive=False)

        assert len(files) == 2
        names = [f["name"] for f in files]
        assert "file3.txt" not in names

    @pytest.mark.asyncio
    async def test_search_files(self, agent, temp_directory):
        results = await agent.search_files(str(temp_directory), "*.txt")

        assert len(results) == 2

    @pytest.mark.asyncio
    async def test_find_large_files(self, agent, temp_directory):
        # Create a large file
        large_file = temp_directory / "large.bin"
        large_file.write_bytes(b"x" * (200 * 1024 * 1024))  # 200 MB

        results = await agent.find_large_files(str(temp_directory), min_size_mb=100)

        assert len(results) == 1
        assert results[0]["name"] == "large.bin"


# test_orchestrator.py
import pytest
from nexus_agents.core.orchestrator import (
    Orchestrator,
    Workflow,
    WorkflowStep,
    OrchestrationPattern
)
from nexus_agents.core.runtime import AgentRuntime, RuntimeConfig


@pytest.fixture
def runtime():
    config = RuntimeConfig(llm_provider="mock")
    return AgentRuntime(config)


@pytest.fixture
def orchestrator(runtime):
    return Orchestrator(runtime)


class TestOrchestrator:
    @pytest.mark.asyncio
    async def test_sequential_workflow(self, orchestrator, runtime):
        # Register mock agents
        mock_agent = MockAgent("agent1", "Mock agent 1")
        runtime.register_agent(mock_agent)

        workflow = Workflow(
            name="test_workflow",
            steps=[
                WorkflowStep(agent_name="agent1", task="Task 1")
            ],
            pattern=OrchestrationPattern.SEQUENTIAL
        )

        results = await orchestrator.run_workflow(workflow)

        assert "agent1" in results
        assert results["agent1"].success
```

**Dependencies:** [7.1.2](#task-712-c-unit-tests)

**Next Task:** [7.2.1](#task-721-create-integration-tests)

---

### 7.2 Integration Tests

#### Task 7.2.1: Create Integration Tests

**Status:** `[ ]` Not Started

**Description:**
Create integration tests for cross-component functionality.

**Acceptance Criteria:**
- [ ] Rust-to-C# integration tested
- [ ] Python agent-to-native tested
- [ ] MCP server tested end-to-end

**Files to Create:**
```
tests/Integration/
tests/Integration/RustCSharpIntegrationTests.cs
tests/Integration/McpServerTests.py
```

**Dependencies:** [7.1.3](#task-713-python-unit-tests)

**Next Task:** [7.3.1](#task-731-create-performance-benchmarks)

---

### 7.3 Performance Testing

#### Task 7.3.1: Create Performance Benchmarks

**Status:** `[ ]` Not Started

**Description:**
Create benchmarks to verify performance targets.

**Acceptance Criteria:**
- [ ] MFT scan benchmark < 1 sec
- [ ] Search latency < 5 ms
- [ ] Memory usage tracked
- [ ] CI performance tracking

**Files to Create:**
```
src/nexus-native/benches/
benchmarks/benchmark_results.md
```

**Implementation:**
```rust
// benches/mft_bench.rs
use criterion::{criterion_group, criterion_main, Criterion, BenchmarkId};
use nexus_native::mft::MftScanner;

fn bench_mft_scan(c: &mut Criterion) {
    let scanner = MftScanner::new('C');

    c.bench_function("mft_scan_c_drive", |b| {
        b.iter(|| {
            let _ = scanner.scan();
        })
    });
}

fn bench_search(c: &mut Criterion) {
    let searcher = SimdSearcher::new(true);
    let haystack = "a".repeat(1_000_000);

    let mut group = c.benchmark_group("simd_search");

    for pattern_len in [3, 10, 50, 100].iter() {
        let pattern = "x".repeat(*pattern_len);
        group.bench_with_input(
            BenchmarkId::new("pattern_len", pattern_len),
            pattern_len,
            |b, _| {
                b.iter(|| {
                    searcher.find_all(haystack.as_bytes(), pattern.as_bytes())
                })
            }
        );
    }

    group.finish();
}

fn bench_bloom_filter(c: &mut Criterion) {
    let mut bloom = BloomIndex::new(1_000_000);

    // Insert 1M items
    for i in 0..1_000_000 {
        bloom.insert(&format!("file_{}.txt", i));
    }

    c.bench_function("bloom_positive_lookup", |b| {
        b.iter(|| bloom.might_contain("file_500000.txt"))
    });

    c.bench_function("bloom_negative_lookup", |b| {
        b.iter(|| bloom.might_contain("nonexistent_file.txt"))
    });
}

criterion_group!(benches, bench_mft_scan, bench_search, bench_bloom_filter);
criterion_main!(benches);
```

**Dependencies:** [7.2.1](#task-721-create-integration-tests)

**Next Task:** [7.4.1](#task-741-conduct-security-audit)

---

### 7.4 Security Testing

#### Task 7.4.1: Conduct Security Audit

**Status:** `[ ]` Not Started

**Description:**
Audit code for security vulnerabilities.

**Acceptance Criteria:**
- [ ] Path traversal prevention verified
- [ ] Input validation tested
- [ ] Privilege escalation checked
- [ ] OWASP guidelines followed

**Checklist:**
```markdown
## Security Audit Checklist

### Input Validation
- [ ] All user inputs validated
- [ ] Path validation prevents traversal
- [ ] SQL injection not applicable (no SQL)
- [ ] Command injection prevented

### Authentication & Authorization
- [ ] No hardcoded credentials
- [ ] API keys not exposed
- [ ] Admin operations require elevation

### Data Protection
- [ ] Sensitive data not logged
- [ ] Temp files securely deleted
- [ ] Memory cleared after use

### File Operations
- [ ] Protected paths enforced
- [ ] Symlink attacks prevented
- [ ] Race conditions avoided

### MCP Server Security
- [ ] Rate limiting implemented
- [ ] Input size limits
- [ ] Timeout handling
```

**Dependencies:** [7.3.1](#task-731-create-performance-benchmarks)

---

## Phase Completion Checklist

- [ ] All 7.1.x tasks complete (Unit Tests)
- [ ] All 7.2.x tasks complete (Integration Tests)
- [ ] All 7.3.x tasks complete (Performance Testing)
- [ ] All 7.4.x tasks complete (Security Testing)
- [ ] Code coverage >80%
- [ ] All tests passing in CI
- [ ] Performance targets met
- [ ] Security audit passed

---

## Running Tests

```bash
# Rust tests
cd src/nexus-native
cargo test
cargo test --release  # For benchmark accuracy

# C# tests
dotnet test Winhance.sln

# Python tests
cd src/nexus-agents
pytest tests/ -v --cov=nexus_agents

# Benchmarks
cargo bench --manifest-path src/nexus-native/Cargo.toml
```

---

**[Back to Roadmap](PROJECT_ROADMAP.md)** | **Previous:** [Phase 6](PHASE_6_MCP_SERVER.md) | **Next:** [Phase 8](PHASE_8_RELEASE.md)
