# Winhance-FS AI Agents Documentation

> **Purpose:** Complete guide to AI agent orchestration and automation in Winhance-FS  
> **Last Updated:** 2026-01-26  
> **Version:** 1.0

---

## � Navigation

**Related Documents:**
- [Architecture Documentation](ARCHITECTURE.md)
- [Configuration Guide](CONFIGURATION.md)
- [MCP Server Documentation](docs/mcp/README.md)
- [Python AI Development](docs/development/python.md)
- [Performance Guide](PERFORMANCE.md)

---

## �📋 Overview

Winhance-FS features a sophisticated AI agent orchestration system that leverages multiple AI models and services to provide intelligent file management, organization, and analysis capabilities. The system is built on the Model Context Protocol (MCP) and supports integration with Claude, Windsurf, and other AI platforms.

---

## 🤖 Agent Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    AI Agent Orchestration                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐         │
│  │   Claude    │    │   Windsurf  │    │  Custom AI  │         │
│  │   Agent     │    │   Agent     │    │   Agents    │         │
│  └─────────────┘    └─────────────┘    └─────────────┘         │
│         │                   │                   │             │
│         └───────────────────┼───────────────────┘             │
│                             │                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              MCP Server (nexus_mcp)                     │   │
│  │                                                         │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │   │
│  │  │   Router    │  │  Tool Mgr   │  │  Session    │     │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘     │   │
│  └─────────────────────────────────────────────────────────┘   │
│                             │                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                Agent Services                            │   │
│  │                                                         │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │   │
│  │  │ Classifier  │  │  Organizer  │  │  Analyzer   │     │   │
│  │  │  Agent      │  │  Agent      │  │  Agent      │     │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘     │   │
│  │                                                         │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │   │
│  │  │ Search AI   │  │   Model     │  │ Transaction │     │   │
│  │  │  Agent      │  │  Manager    │  │   Agent     │     │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘     │   │
│  └─────────────────────────────────────────────────────────┘   │
│                             │                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              AI Infrastructure                           │   │
│  │                                                         │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │   │
│  │  │   Models    │  │   Vector    │  │  Knowledge  │     │   │
│  │  │  Registry   │  │   Database  │  │     Base    │     │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘     │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🎭 Core Agents

### 1. File Classifier Agent

```python
class FileClassifierAgent(BaseAgent):
    """Intelligently categorizes files based on content and metadata."""
    
    def __init__(self):
        super().__init__()
        self.categories = [
            "Documents", "Images", "Videos", "Audio",
            "Archives", "Code", "Datasets", "Models",
            "Configuration", "Executables", "Other"
        ]
    
    async def classify_file(self, file_path: Path) -> ClassificationResult:
        """
        Classify a single file using multi-modal analysis.
        
        Args:
            file_path: Path to the file to classify
            
        Returns:
            ClassificationResult with category and confidence
        """
        # Extract features
        features = await self._extract_features(file_path)
        
        # Analyze with AI model
        classification = await self._ai_model.classify(features)
        
        # Apply business rules
        result = self._apply_rules(classification, file_path)
        
        return result
    
    async def batch_classify(self, file_paths: List[Path]) -> List[ClassificationResult]:
        """Classify multiple files in parallel."""
        tasks = [self.classify_file(path) for path in file_paths]
        return await asyncio.gather(*tasks)
```

### 2. Smart Organizer Agent

```python
class SmartOrganizerAgent(BaseAgent):
    """Automatically organizes files based on rules and AI suggestions."""
    
    def __init__(self):
        super().__init__()
        self.rule_engine = RuleEngine()
        self.classifier = FileClassifierAgent()
    
    async def suggest_organization(self, directory: Path) -> OrganizationPlan:
        """
        Analyze a directory and suggest organization improvements.
        
        Returns:
            OrganizationPlan with suggested moves and rules
        """
        # Scan directory
        files = await self._scan_directory(directory)
        
        # Classify files
        classifications = await self.classifier.batch_classify(files)
        
        # Generate organization plan
        plan = await self._generate_plan(files, classifications)
        
        # Validate plan
        validated_plan = await self._validate_plan(plan)
        
        return validated_plan
    
    async def execute_organization(
        self, 
        plan: OrganizationPlan,
        preview_mode: bool = True
    ) -> OrganizationResult:
        """Execute an organization plan with transaction support."""
        if preview_mode:
            return await self._preview_execution(plan)
        
        # Create transaction
        transaction = await self._create_transaction(plan)
        
        try:
            # Execute moves
            result = await self._execute_moves(transaction)
            
            # Update index
            await self._update_search_index(result)
            
            return result
        except Exception as e:
            # Rollback on failure
            await transaction.rollback()
            raise
```

### 3. Content Analyzer Agent

```python
class ContentAnalyzerAgent(BaseAgent):
    """Deep analysis of file content for insights and recommendations."""
    
    async def analyze_document(self, file_path: Path) -> DocumentAnalysis:
        """Analyze document content and extract insights."""
        # Extract text
        text = await self._extract_text(file_path)
        
        # Generate summary
        summary = await self._ai_model.summarize(text)
        
        # Extract entities
        entities = await self._extract_entities(text)
        
        # Analyze sentiment
        sentiment = await self._analyze_sentiment(text)
        
        # Generate tags
        tags = await self._generate_tags(text, entities)
        
        return DocumentAnalysis(
            summary=summary,
            entities=entities,
            sentiment=sentiment,
            tags=tags,
            word_count=len(text.split()),
            reading_time=self._estimate_reading_time(text)
        )
    
    async def analyze_code(self, file_path: Path) -> CodeAnalysis:
        """Analyze source code for quality and metrics."""
        # Parse code
        ast = await self._parse_code(file_path)
        
        # Calculate metrics
        metrics = await self._calculate_metrics(ast)
        
        # Detect issues
        issues = await self._detect_issues(ast)
        
        # Suggest improvements
        suggestions = await self._suggest_improvements(ast, metrics)
        
        return CodeAnalysis(
            metrics=metrics,
            issues=issues,
            suggestions=suggestions,
            complexity_score=metrics.complexity,
            maintainability_index=metrics.maintainability
        )
```

### 4. Search AI Agent

```python
class SearchAIAgent(BaseAgent):
    """Enhanced search with natural language understanding."""
    
    async def semantic_search(
        self, 
        query: str, 
        filters: SearchFilters = None
    ) -> SearchResult:
        """
        Perform semantic search using embeddings.
        
        Args:
            query: Natural language search query
            filters: Optional search filters
            
        Returns:
            SearchResult with ranked files
        """
        # Generate query embedding
        query_embedding = await self._generate_embedding(query)
        
        # Search vector database
        vector_results = await self._vector_db.search(
            query_embedding, 
            limit=100,
            filters=filters
        )
        
        # Re-rank with AI
        reranked = await self._rerank_results(query, vector_results)
        
        # Apply permissions
        filtered = await self._apply_permissions(reranked)
        
        return SearchResult(
            query=query,
            results=filtered,
            total=len(filtered),
            search_time=timer.elapsed
        )
    
    async def natural_language_query(
        self, 
        query: str
    ) -> QueryResult:
        """
        Process natural language queries into structured searches.
        
        Examples:
        - "Find all PDFs larger than 10MB modified last week"
        - "Show me Python files with TODO comments"
        - "Organize my downloads folder"
        """
        # Parse intent
        intent = await self._parse_intent(query)
        
        # Extract entities
        entities = await self._extract_entities(query)
        
        # Generate search plan
        search_plan = await self._generate_search_plan(intent, entities)
        
        # Execute search
        results = await self._execute_search_plan(search_plan)
        
        # Format response
        formatted = await self._format_response(results, intent)
        
        return QueryResult(
            original_query=query,
            intent=intent,
            results=formatted,
            suggestions=await self._generate_suggestions(query, results)
        )
```

---

## 🔧 Agent Configuration

### Agent Registry

```json
{
  "agents": {
    "classifier": {
      "class": "FileClassifierAgent",
      "model": "gpt-4-vision-preview",
      "enabled": true,
      "config": {
        "batch_size": 50,
        "confidence_threshold": 0.8,
        "use_content_analysis": true
      }
    },
    "organizer": {
      "class": "SmartOrganizerAgent",
      "model": "gpt-4-turbo",
      "enabled": true,
      "config": {
        "preview_mode": true,
        "max_moves_per_operation": 1000,
        "require_confirmation": true
      }
    },
    "analyzer": {
      "class": "ContentAnalyzerAgent",
      "model": "gpt-4-turbo",
      "enabled": true,
      "config": {
        "max_file_size": "10MB",
        "extract_metadata": true,
        "generate_summaries": true
      }
    },
    "search": {
      "class": "SearchAIAgent",
      "model": "text-embedding-ada-002",
      "enabled": true,
      "config": {
        "embedding_dimension": 1536,
        "search_limit": 100,
        "rerank_results": true
      }
    }
  }
}
```

### Model Configuration

```yaml
# models.yaml
models:
  openai:
    api_key: "${OPENAI_API_KEY}"
    base_url: "https://api.openai.com/v1"
    models:
      chat: "gpt-4-turbo"
      vision: "gpt-4-vision-preview"
      embedding: "text-embedding-ada-002"
    
  anthropic:
    api_key: "${ANTHROPIC_API_KEY}"
    base_url: "https://api.anthropic.com"
    models:
      chat: "claude-3-opus-20240229"
    
  local:
    base_url: "http://localhost:8081"
    models:
      chat: "llama-2-7b-chat"
      embedding: "all-MiniLM-L6-v2"

agent_settings:
  default_model: "openai"
  timeout: 30
  max_retries: 3
  rate_limit: 100  # requests per minute
```

---

## 🎯 Agent Workflows

### 1. Smart Organization Workflow

```python
class OrganizationWorkflow:
    """Orchestrates the smart organization process."""
    
    async def execute(self, directory: Path, options: OrgOptions) -> OrgResult:
        # Step 1: Analyze directory
        analysis = await self.analyzer_agent.analyze_directory(directory)
        
        # Step 2: Classify files
        classifications = await self.classifier_agent.batch_classify(
            analysis.files
        )
        
        # Step 3: Generate organization plan
        plan = await self.organizer_agent.suggest_organization(
            directory,
            classifications,
            options.rules
        )
        
        # Step 4: Preview and confirm
        if options.interactive:
            preview = await self._generate_preview(plan)
            confirmed = await self._get_user_confirmation(preview)
            if not confirmed:
                return OrgResult(cancelled=True)
        
        # Step 5: Execute organization
        result = await self.organizer_agent.execute_organization(
            plan,
            preview_mode=options.preview_mode
        )
        
        # Step 6: Update knowledge base
        await self._update_knowledge_base(result)
        
        return result
```

### 2. Content Discovery Workflow

```python
class DiscoveryWorkflow:
    """Discovers and analyzes content across the file system."""
    
    async def discover_content(
        self, 
        scope: DiscoveryScope,
        filters: ContentFilters
    ) -> DiscoveryResult:
        # Step 1: Scan for content
        files = await self._scan_files(scope, filters)
        
        # Step 2: Parallel analysis
        analysis_tasks = []
        for file in files:
            if self._is_document(file):
                analysis_tasks.append(
                    self.analyzer_agent.analyze_document(file)
                )
            elif self._is_code(file):
                analysis_tasks.append(
                    self.analyzer_agent.analyze_code(file)
                )
        
        analyses = await asyncio.gather(*analysis_tasks)
        
        # Step 3: Extract insights
        insights = await self._extract_insights(analyses)
        
        # Step 4: Generate recommendations
        recommendations = await self._generate_recommendations(insights)
        
        # Step 5: Create summary
        summary = await self._create_summary(insights, recommendations)
        
        return DiscoveryResult(
            files_analyzed=len(files),
            insights=insights,
            recommendations=recommendations,
            summary=summary
        )
```

---

## 📊 Agent Performance

### Performance Metrics

```python
class AgentMetrics:
    """Tracks and reports agent performance."""
    
    def __init__(self):
        self.metrics = {
            "classification": {
                "accuracy": 0.95,
                "avg_time_ms": 150,
                "throughput_per_sec": 100
            },
            "organization": {
                "success_rate": 0.98,
                "avg_time_per_1000_files": 45,
                "error_rate": 0.02
            },
            "search": {
                "query_latency_p95": 50,
                "recall_at_10": 0.92,
                "index_size_gb": 2.3
            },
            "analysis": {
                "docs_per_hour": 500,
                "code_analysis_accuracy": 0.88,
                "summary_quality_score": 4.2
            }
        }
    
    async def track_operation(
        self, 
        agent: str, 
        operation: str, 
        duration: float,
        success: bool
    ):
        """Track an operation for metrics."""
        # Update metrics
        # Store in time-series database
        # Trigger alerts if needed
        pass
```

### Optimization Strategies

```python
class AgentOptimizer:
    """Optimizes agent performance and resource usage."""
    
    async def optimize_classification(self):
        """Optimize classification agent."""
        # Batch similar files
        # Cache model predictions
        # Use ensemble models
        # Implement early termination
        pass
    
    async def optimize_search(self):
        """Optimize search agent."""
        # Pre-compute embeddings
        # Use approximate nearest neighbor
        # Implement query caching
        # Optimize vector indexing
        pass
    
    async def optimize_organization(self):
        """Optimize organization agent."""
        # Parallel file operations
        # Use transaction batching
        # Implement incremental updates
        # Optimize rule evaluation
        pass
```

---

## 🔗 Integration Points

### 1. MCP Server Integration

```python
# MCP Server Tools
@mcp_tool
async def classify_files(paths: List[str]) -> List[ClassificationResult]:
    """Classify files using AI."""
    agent = FileClassifierAgent()
    file_paths = [Path(p) for p in paths]
    return await agent.batch_classify(file_paths)

@mcp_tool
async def organize_directory(
    path: str, 
    rules: Dict[str, Any] = None
) -> OrganizationResult:
    """Organize directory smartly."""
    agent = SmartOrganizerAgent()
    directory = Path(path)
    plan = await agent.suggest_organization(directory)
    return await agent.execute_organization(plan)

@mcp_tool
async def search_files(query: str, filters: Dict = None) -> SearchResult:
    """Search files with natural language."""
    agent = SearchAIAgent()
    search_filters = SearchFilters(**filters or {})
    return await agent.semantic_search(query, search_filters)
```

### 2. WPF Integration

```csharp
// Agent Service in C#
public class AgentService : IAgentService
{
    private readonly IMcpClient _mcpClient;
    
    public async Task<ClassificationResult[]> ClassifyFilesAsync(string[] paths)
    {
        var result = await _mcpClient.CallToolAsync(
            "classify_files", 
            new { paths }
        );
        return JsonSerializer.Deserialize<ClassificationResult[]>(result);
    }
    
    public async Task<OrganizationPlan> SuggestOrganizationAsync(string path)
    {
        var result = await _mcpClient.CallToolAsync(
            "suggest_organization", 
            new { path }
        );
        return JsonSerializer.Deserialize<OrganizationPlan>(result);
    }
}
```

---

## 🛡️ Security and Privacy

### Data Protection

```python
class AgentSecurityManager:
    """Manages security and privacy for AI agents."""
    
    def __init__(self):
        self.encryption_key = self._load_encryption_key()
        self.access_control = AccessControlManager()
    
    async def protect_sensitive_data(self, data: Any) -> ProtectedData:
        """Encrypt sensitive data before processing."""
        # Identify sensitive information
        sensitive = await self._identify_sensitive_data(data)
        
        # Encrypt if needed
        if sensitive:
            encrypted = await self._encrypt(data, self.encryption_key)
            return ProtectedData(
                data=encrypted,
                is_encrypted=True,
                access_level="restricted"
            )
        
        return ProtectedData(data=data, is_encrypted=False)
    
    async def verify_access(self, user: User, resource: Resource) -> bool:
        """Verify user has access to resource."""
        return await self.access_control.check_access(user, resource)
    
    async def audit_agent_action(
        self, 
        agent: str, 
        action: str, 
        user: User
    ):
        """Log all agent actions for audit."""
        await self._log_action({
            "timestamp": datetime.utcnow(),
            "agent": agent,
            "action": action,
            "user": user.id,
            "ip_address": self._get_client_ip()
        })
```

### Privacy Controls

```yaml
# privacy.yaml
privacy_settings:
  data_retention:
    classifications: 90  # days
    analyses: 365
    search_history: 30
    
  data_minimization:
    strip_metadata: true
    anonymize_paths: true
    hash_file_contents: true
    
  user_controls:
    opt_out_of_ai: true
    local_processing_only: false
    delete_on_request: true
    
  compliance:
    gdpr: true
    ccpa: true
    hipaa: false
```

---

## 📈 Monitoring and Observability

### Agent Health Monitoring

```python
class AgentMonitor:
    """Monitors agent health and performance."""
    
    def __init__(self):
        self.health_checks = {
            "classifier": self._check_classifier_health,
            "organizer": self._check_organizer_health,
            "analyzer": self._check_analyzer_health,
            "search": self._check_search_health
        }
    
    async def check_all_agents(self) -> HealthStatus:
        """Check health of all agents."""
        status = HealthStatus()
        
        for agent, check_func in self.health_checks.items():
            try:
                agent_status = await check_func()
                status.agents[agent] = agent_status
            except Exception as e:
                status.agents[agent] = AgentStatus(
                    healthy=False,
                    error=str(e)
                )
        
        status.overall_healthy = all(
            a.healthy for a in status.agents.values()
        )
        
        return status
    
    async def _check_classifier_health(self) -> AgentStatus:
        """Check classifier agent health."""
        # Test with sample file
        test_file = Path("test/sample.txt")
        start = time.time()
        
        try:
            result = await self.classifier.classify_file(test_file)
            duration = time.time() - start
            
            return AgentStatus(
                healthy=True,
                response_time=duration,
                last_check=datetime.utcnow()
            )
        except Exception as e:
            return AgentStatus(healthy=False, error=str(e))
```

### Performance Dashboard

```python
class AgentDashboard:
    """Provides real-time performance metrics."""
    
    async def get_metrics(self) -> DashboardMetrics:
        """Get current metrics for dashboard."""
        return DashboardMetrics(
            active_agents=await self._count_active_agents(),
            requests_per_minute=await self._get_rpm(),
            avg_response_time=await self._get_avg_response_time(),
            error_rate=await self._get_error_rate(),
            queue_sizes=await self._get_queue_sizes(),
            resource_usage=await self._get_resource_usage()
        )
    
    async def get_agent_details(self, agent_name: str) -> AgentDetails:
        """Get detailed metrics for specific agent."""
        return AgentDetails(
            name=agent_name,
            status=await self._get_agent_status(agent_name),
            current_load=await self._get_agent_load(agent_name),
            processing_queue=await self._get_queue_status(agent_name),
            recent_errors=await self._get_recent_errors(agent_name),
            performance_history=await self._get_performance_history(agent_name)
        )
```

---

## 🔧 Troubleshooting

### Common Issues

1. **Agent Not Responding**
   ```python
   # Check agent status
   status = await monitor.check_agent("classifier")
   if not status.healthy:
       # Restart agent
       await agent_manager.restart_agent("classifier")
   ```

2. **High Latency**
   ```python
   # Check queue sizes
   queues = await monitor.get_queue_sizes()
   if queues["classifier"] > 1000:
       # Scale up or add workers
       await agent_manager.scale_agent("classifier", workers=4)
   ```

3. **Incorrect Classifications**
   ```python
   # Check model version
   model_info = await agent_manager.get_model_info("classifier")
   if model_info.version != "latest":
       await agent_manager.update_model("classifier", "latest")
   ```

### Debug Mode

```python
# Enable debug logging
import logging
logging.getLogger("nexus_ai").setLevel(logging.DEBUG)

# Trace agent execution
with AgentTracer("classify_file") as tracer:
    result = await classifier.classify_file(test_file)
    tracer.log_result(result)
```

---

## 🔗 Navigation

**Related Documents:**
- [Architecture Documentation](ARCHITECTURE.md)
- [Configuration Guide](CONFIGURATION.md)
- [MCP Server Documentation](docs/mcp/README.md)
- [Python AI Development](docs/development/python.md)

---

## 📚 References

### AI/ML Resources
- [OpenAI API Documentation](https://platform.openai.com/docs)
- [Anthropic Claude API](https://docs.anthropic.com/claude/reference)
- [Model Context Protocol](https://modelcontextprotocol.io/)
- [LangChain Documentation](https://python.langchain.com/)

### Agent Frameworks
- [CrewAI](https://www.crewai.com/)
- [AutoGen](https://github.com/microsoft/autogen)
- [Swarm](https://github.com/openai/swarm)

---

*Intelligent automation at your fingertips! The AI agent system makes file management effortless.* 🤖
