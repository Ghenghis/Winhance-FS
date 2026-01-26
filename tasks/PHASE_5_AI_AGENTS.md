# Phase 5: AI Agents (nexus-agents)

> **[Back to Roadmap](PROJECT_ROADMAP.md)** | **Previous:** [Phase 4](PHASE_4_UI_THEMING.md) | **Next:** [Phase 6](PHASE_6_MCP_SERVER.md)

---

## Phase Overview

| Attribute | Value |
|-----------|-------|
| **Status** | `[ ]` Not Started |
| **Priority** | High |
| **Estimated Tasks** | 18 |
| **Dependencies** | [Phase 1](PHASE_1_FOUNDATION.md) Complete |

---

## Objectives

1. Create Python agent framework with BaseAgent pattern
2. Implement specialized agents (FileDiscovery, Classification, Organization, Cleanup)
3. Build multi-agent orchestration system
4. Integrate with LLM providers (OpenAI, Anthropic, Local)
5. Enable browser automation with Playwright MCP

---

## Agent Architecture

```
+==============================================================================+
|                           AGENT ARCHITECTURE                                  |
+==============================================================================+
|                                                                               |
|  +---------------------------+         +---------------------------+          |
|  |     Orchestrator          |-------->|      Agent Runtime        |          |
|  |  (Workflow Coordinator)   |         |   (Execution Context)     |          |
|  +---------------------------+         +---------------------------+          |
|              |                                      |                         |
|              v                                      v                         |
|  +---------------------------+         +---------------------------+          |
|  |     Multi-Agent Patterns  |         |     Tool Registry         |          |
|  |  - Sequential             |         |  - File Operations        |          |
|  |  - Concurrent             |         |  - Search                 |          |
|  |  - Group Chat             |         |  - Browser (Playwright)   |          |
|  |  - Handoff                |         |  - LLM Calls              |          |
|  +---------------------------+         +---------------------------+          |
|                                                                               |
|  +-------------------------------------------------------------------------+  |
|  |                         SPECIALIZED AGENTS                               |  |
|  |                                                                          |  |
|  |  +----------------+  +----------------+  +----------------+              |  |
|  |  | FileDiscovery  |  | Classification |  | Organization   |              |  |
|  |  | Agent          |  | Agent          |  | Agent          |              |  |
|  |  +----------------+  +----------------+  +----------------+              |  |
|  |                                                                          |  |
|  |  +----------------+  +----------------+  +----------------+              |  |
|  |  | Cleanup        |  | Browser        |  | Research       |              |  |
|  |  | Agent          |  | Agent          |  | Agent          |              |  |
|  |  +----------------+  +----------------+  +----------------+              |  |
|  +-------------------------------------------------------------------------+  |
|                                                                               |
+==============================================================================+
```

---

## Task List

### 5.1 Agent Framework

#### Task 5.1.1: Create Base Agent Class

**Status:** `[ ]` Not Started

**Description:**
Create the foundational BaseAgent class that all agents inherit from.

**Acceptance Criteria:**
- [ ] `BaseAgent` abstract class with standard interface
- [ ] Tool registration system
- [ ] State management
- [ ] Logging integration
- [ ] Async execution support

**Files to Create:**
```
src/nexus-agents/nexus_agents/core/base_agent.py
```

**Implementation:**
```python
# base_agent.py
from abc import ABC, abstractmethod
from typing import Any, Dict, List, Optional, Callable
from dataclasses import dataclass, field
from enum import Enum
import logging

class AgentState(Enum):
    IDLE = "idle"
    RUNNING = "running"
    PAUSED = "paused"
    COMPLETED = "completed"
    FAILED = "failed"

@dataclass
class AgentContext:
    """Shared context for agent execution."""
    session_id: str
    workspace_path: str
    variables: Dict[str, Any] = field(default_factory=dict)
    history: List[Dict[str, Any]] = field(default_factory=list)

@dataclass
class AgentResult:
    """Result of agent execution."""
    success: bool
    data: Any = None
    error: Optional[str] = None
    execution_time_ms: float = 0.0

class BaseAgent(ABC):
    """Base class for all Winhance-FS agents."""

    def __init__(self, name: str, description: str):
        self.name = name
        self.description = description
        self.state = AgentState.IDLE
        self.tools: Dict[str, Callable] = {}
        self.logger = logging.getLogger(f"agent.{name}")
        self._context: Optional[AgentContext] = None

    def register_tool(self, name: str, func: Callable, description: str = "") -> None:
        """Register a tool that the agent can use."""
        self.tools[name] = func
        self.logger.debug(f"Registered tool: {name}")

    def set_context(self, context: AgentContext) -> None:
        """Set the execution context."""
        self._context = context

    @abstractmethod
    async def plan(self, task: str) -> List[Dict[str, Any]]:
        """Create an execution plan for the given task."""
        pass

    @abstractmethod
    async def execute(self, task: str, **kwargs) -> AgentResult:
        """Execute the given task."""
        pass

    async def run(self, task: str, context: Optional[AgentContext] = None) -> AgentResult:
        """Run the agent with the given task."""
        if context:
            self.set_context(context)

        self.state = AgentState.RUNNING
        self.logger.info(f"Starting task: {task}")

        try:
            result = await self.execute(task)
            self.state = AgentState.COMPLETED if result.success else AgentState.FAILED
            return result
        except Exception as e:
            self.state = AgentState.FAILED
            self.logger.error(f"Agent failed: {e}")
            return AgentResult(success=False, error=str(e))

    def pause(self) -> None:
        """Pause agent execution."""
        if self.state == AgentState.RUNNING:
            self.state = AgentState.PAUSED
            self.logger.info("Agent paused")

    def resume(self) -> None:
        """Resume agent execution."""
        if self.state == AgentState.PAUSED:
            self.state = AgentState.RUNNING
            self.logger.info("Agent resumed")

    def get_tools_schema(self) -> List[Dict[str, Any]]:
        """Get OpenAI-compatible tools schema."""
        schemas = []
        for name, func in self.tools.items():
            schema = {
                "type": "function",
                "function": {
                    "name": name,
                    "description": func.__doc__ or "",
                    "parameters": self._extract_params(func)
                }
            }
            schemas.append(schema)
        return schemas

    def _extract_params(self, func: Callable) -> Dict[str, Any]:
        """Extract parameter schema from function signature."""
        # Implementation for extracting type hints
        return {"type": "object", "properties": {}}
```

**Next Task:** [5.1.2](#task-512-create-agent-runtime)

---

#### Task 5.1.2: Create Agent Runtime

**Status:** `[ ]` Not Started

**Description:**
Create the runtime environment for executing agents.

**Acceptance Criteria:**
- [ ] Manages agent lifecycle
- [ ] Handles tool execution
- [ ] Provides LLM integration
- [ ] Manages state persistence

**Files to Create:**
```
src/nexus-agents/nexus_agents/core/runtime.py
```

**Implementation:**
```python
# runtime.py
from typing import Any, Dict, List, Optional, Type
from dataclasses import dataclass
import asyncio
import json

from .base_agent import BaseAgent, AgentContext, AgentResult

@dataclass
class RuntimeConfig:
    """Configuration for agent runtime."""
    llm_provider: str = "openai"  # openai, anthropic, local
    llm_model: str = "gpt-4-turbo"
    max_iterations: int = 10
    timeout_seconds: float = 300.0
    enable_browser: bool = False
    workspace_path: str = "."

class AgentRuntime:
    """Runtime environment for executing agents."""

    def __init__(self, config: RuntimeConfig):
        self.config = config
        self.agents: Dict[str, BaseAgent] = {}
        self.context: Optional[AgentContext] = None
        self._llm_client = None

    def register_agent(self, agent: BaseAgent) -> None:
        """Register an agent with the runtime."""
        self.agents[agent.name] = agent

    async def initialize(self) -> None:
        """Initialize the runtime."""
        # Initialize LLM client
        if self.config.llm_provider == "openai":
            from openai import AsyncOpenAI
            self._llm_client = AsyncOpenAI()
        elif self.config.llm_provider == "anthropic":
            from anthropic import AsyncAnthropic
            self._llm_client = AsyncAnthropic()
        # Local models handled differently

        # Create shared context
        import uuid
        self.context = AgentContext(
            session_id=str(uuid.uuid4()),
            workspace_path=self.config.workspace_path
        )

    async def run_agent(
        self,
        agent_name: str,
        task: str,
        **kwargs
    ) -> AgentResult:
        """Run a specific agent with a task."""
        if agent_name not in self.agents:
            return AgentResult(
                success=False,
                error=f"Agent not found: {agent_name}"
            )

        agent = self.agents[agent_name]
        return await agent.run(task, context=self.context)

    async def call_llm(
        self,
        messages: List[Dict[str, str]],
        tools: Optional[List[Dict]] = None
    ) -> Dict[str, Any]:
        """Call the LLM with messages and optional tools."""
        if self.config.llm_provider == "openai":
            response = await self._llm_client.chat.completions.create(
                model=self.config.llm_model,
                messages=messages,
                tools=tools
            )
            return response.model_dump()
        elif self.config.llm_provider == "anthropic":
            # Anthropic API format
            response = await self._llm_client.messages.create(
                model=self.config.llm_model,
                messages=messages,
                tools=tools
            )
            return response.model_dump()
        else:
            raise ValueError(f"Unknown LLM provider: {self.config.llm_provider}")

    async def execute_tool(
        self,
        agent: BaseAgent,
        tool_name: str,
        arguments: Dict[str, Any]
    ) -> Any:
        """Execute a tool registered with an agent."""
        if tool_name not in agent.tools:
            raise ValueError(f"Tool not found: {tool_name}")

        tool_func = agent.tools[tool_name]
        if asyncio.iscoroutinefunction(tool_func):
            return await tool_func(**arguments)
        else:
            return tool_func(**arguments)

    async def close(self) -> None:
        """Clean up runtime resources."""
        if hasattr(self._llm_client, 'close'):
            await self._llm_client.close()
```

**Dependencies:** [5.1.1](#task-511-create-base-agent-class)

**Next Task:** [5.1.3](#task-513-create-orchestrator)

---

#### Task 5.1.3: Create Orchestrator

**Status:** `[ ]` Not Started

**Description:**
Create the multi-agent orchestrator for coordinating complex workflows.

**Acceptance Criteria:**
- [ ] Sequential execution pattern
- [ ] Concurrent execution pattern
- [ ] Group chat pattern
- [ ] Handoff pattern
- [ ] Error handling and recovery

**Files to Create:**
```
src/nexus-agents/nexus_agents/core/orchestrator.py
```

**Implementation:**
```python
# orchestrator.py
from typing import Any, Dict, List, Optional
from dataclasses import dataclass
from enum import Enum
import asyncio

from .base_agent import BaseAgent, AgentResult
from .runtime import AgentRuntime

class OrchestrationPattern(Enum):
    SEQUENTIAL = "sequential"
    CONCURRENT = "concurrent"
    GROUP_CHAT = "group_chat"
    HANDOFF = "handoff"

@dataclass
class WorkflowStep:
    """A single step in a workflow."""
    agent_name: str
    task: str
    depends_on: List[str] = None
    condition: Optional[str] = None

@dataclass
class Workflow:
    """A complete workflow definition."""
    name: str
    steps: List[WorkflowStep]
    pattern: OrchestrationPattern = OrchestrationPattern.SEQUENTIAL

class Orchestrator:
    """Coordinates multiple agents for complex workflows."""

    def __init__(self, runtime: AgentRuntime):
        self.runtime = runtime
        self.results: Dict[str, AgentResult] = {}

    async def run_workflow(self, workflow: Workflow) -> Dict[str, AgentResult]:
        """Execute a complete workflow."""
        self.results.clear()

        if workflow.pattern == OrchestrationPattern.SEQUENTIAL:
            return await self._run_sequential(workflow)
        elif workflow.pattern == OrchestrationPattern.CONCURRENT:
            return await self._run_concurrent(workflow)
        elif workflow.pattern == OrchestrationPattern.GROUP_CHAT:
            return await self._run_group_chat(workflow)
        elif workflow.pattern == OrchestrationPattern.HANDOFF:
            return await self._run_handoff(workflow)
        else:
            raise ValueError(f"Unknown pattern: {workflow.pattern}")

    async def _run_sequential(self, workflow: Workflow) -> Dict[str, AgentResult]:
        """Run steps one after another."""
        for step in workflow.steps:
            if step.condition and not self._evaluate_condition(step.condition):
                continue

            task = self._interpolate_task(step.task)
            result = await self.runtime.run_agent(step.agent_name, task)
            self.results[step.agent_name] = result

            if not result.success:
                break

        return self.results

    async def _run_concurrent(self, workflow: Workflow) -> Dict[str, AgentResult]:
        """Run independent steps in parallel."""
        tasks = []
        for step in workflow.steps:
            if step.depends_on:
                # Wait for dependencies
                for dep in step.depends_on:
                    if dep in self.results:
                        continue

            task = self._interpolate_task(step.task)
            tasks.append(
                self._run_step(step.agent_name, task)
            )

        results = await asyncio.gather(*tasks, return_exceptions=True)

        for step, result in zip(workflow.steps, results):
            if isinstance(result, Exception):
                self.results[step.agent_name] = AgentResult(
                    success=False,
                    error=str(result)
                )
            else:
                self.results[step.agent_name] = result

        return self.results

    async def _run_group_chat(self, workflow: Workflow) -> Dict[str, AgentResult]:
        """Run agents in a collaborative chat pattern."""
        messages = []
        max_rounds = 10

        for round_num in range(max_rounds):
            for step in workflow.steps:
                agent = self.runtime.agents[step.agent_name]

                # Add context from previous messages
                context_task = f"{step.task}\n\nPrevious discussion:\n"
                for msg in messages[-5:]:  # Last 5 messages
                    context_task += f"{msg['agent']}: {msg['content']}\n"

                result = await self.runtime.run_agent(step.agent_name, context_task)
                self.results[f"{step.agent_name}_{round_num}"] = result

                if result.success and result.data:
                    messages.append({
                        "agent": step.agent_name,
                        "content": str(result.data)
                    })

                # Check for consensus or completion
                if self._check_completion(messages):
                    return self.results

        return self.results

    async def _run_handoff(self, workflow: Workflow) -> Dict[str, AgentResult]:
        """Run with explicit handoffs between agents."""
        current_step_idx = 0
        context_data = {}

        while current_step_idx < len(workflow.steps):
            step = workflow.steps[current_step_idx]

            # Prepare task with handoff context
            task = self._interpolate_task(step.task)
            if context_data:
                task += f"\n\nHandoff context: {context_data}"

            result = await self.runtime.run_agent(step.agent_name, task)
            self.results[step.agent_name] = result

            if not result.success:
                break

            # Check for handoff instruction
            if isinstance(result.data, dict) and "handoff_to" in result.data:
                target_agent = result.data["handoff_to"]
                context_data = result.data.get("context", {})

                # Find next step with target agent
                for i, s in enumerate(workflow.steps[current_step_idx + 1:]):
                    if s.agent_name == target_agent:
                        current_step_idx = current_step_idx + 1 + i
                        break
            else:
                current_step_idx += 1
                context_data = result.data if isinstance(result.data, dict) else {}

        return self.results

    async def _run_step(self, agent_name: str, task: str) -> AgentResult:
        """Run a single workflow step."""
        return await self.runtime.run_agent(agent_name, task)

    def _interpolate_task(self, task: str) -> str:
        """Replace placeholders in task with results from previous steps."""
        for agent_name, result in self.results.items():
            placeholder = f"{{{agent_name}.data}}"
            if placeholder in task and result.data:
                task = task.replace(placeholder, str(result.data))
        return task

    def _evaluate_condition(self, condition: str) -> bool:
        """Evaluate a condition string."""
        # Simple evaluation - expand as needed
        return eval(condition, {"results": self.results})

    def _check_completion(self, messages: List[Dict]) -> bool:
        """Check if group chat has reached completion."""
        if not messages:
            return False

        # Check for explicit completion markers
        last_message = messages[-1]["content"].lower()
        completion_markers = ["consensus reached", "task complete", "done", "finished"]
        return any(marker in last_message for marker in completion_markers)
```

**Dependencies:** [5.1.2](#task-512-create-agent-runtime)

**Next Task:** [5.2.1](#task-521-implement-filediscoveryagent)

---

### 5.2 Specialized Agents

#### Task 5.2.1: Implement FileDiscoveryAgent

**Status:** `[ ]` Not Started

**Description:**
Create agent for discovering and analyzing files.

**Acceptance Criteria:**
- [ ] Discovers files matching patterns
- [ ] Analyzes file metadata
- [ ] Integrates with Rust backend
- [ ] Reports findings

**Files to Create:**
```
src/nexus-agents/nexus_agents/agents/file_discovery.py
```

**Implementation:**
```python
# file_discovery.py
from typing import Any, Dict, List
from pathlib import Path
import os

from ..core.base_agent import BaseAgent, AgentResult

class FileDiscoveryAgent(BaseAgent):
    """Agent for discovering and analyzing files."""

    def __init__(self):
        super().__init__(
            name="file_discovery",
            description="Discovers files matching patterns and analyzes metadata"
        )
        self._register_tools()

    def _register_tools(self):
        self.register_tool("list_files", self.list_files)
        self.register_tool("search_files", self.search_files)
        self.register_tool("get_file_info", self.get_file_info)
        self.register_tool("find_large_files", self.find_large_files)
        self.register_tool("find_duplicates", self.find_duplicates)

    async def plan(self, task: str) -> List[Dict[str, Any]]:
        """Create plan for file discovery task."""
        return [
            {"action": "analyze_task", "input": task},
            {"action": "determine_scope", "input": task},
            {"action": "execute_discovery", "input": task},
            {"action": "compile_results", "input": task}
        ]

    async def execute(self, task: str, **kwargs) -> AgentResult:
        """Execute file discovery task."""
        try:
            # Analyze task to determine what to discover
            if "large files" in task.lower():
                results = await self.find_large_files(
                    path=kwargs.get("path", "."),
                    min_size_mb=kwargs.get("min_size_mb", 100)
                )
            elif "duplicate" in task.lower():
                results = await self.find_duplicates(
                    path=kwargs.get("path", ".")
                )
            else:
                results = await self.search_files(
                    path=kwargs.get("path", "."),
                    pattern=kwargs.get("pattern", "*")
                )

            return AgentResult(success=True, data=results)
        except Exception as e:
            return AgentResult(success=False, error=str(e))

    async def list_files(
        self,
        path: str,
        recursive: bool = True,
        include_hidden: bool = False
    ) -> List[Dict[str, Any]]:
        """List files in a directory."""
        files = []
        root = Path(path)

        if recursive:
            iterator = root.rglob("*")
        else:
            iterator = root.glob("*")

        for p in iterator:
            if p.is_file():
                if not include_hidden and p.name.startswith("."):
                    continue

                stat = p.stat()
                files.append({
                    "path": str(p),
                    "name": p.name,
                    "size": stat.st_size,
                    "modified": stat.st_mtime,
                    "extension": p.suffix
                })

        return files

    async def search_files(
        self,
        path: str,
        pattern: str,
        content_search: str = None
    ) -> List[Dict[str, Any]]:
        """Search for files matching pattern."""
        results = []
        root = Path(path)

        for p in root.rglob(pattern):
            if p.is_file():
                match_info = {
                    "path": str(p),
                    "name": p.name,
                    "size": p.stat().st_size
                }

                if content_search:
                    try:
                        content = p.read_text()
                        if content_search.lower() in content.lower():
                            match_info["content_match"] = True
                            results.append(match_info)
                    except:
                        pass
                else:
                    results.append(match_info)

        return results

    async def get_file_info(self, path: str) -> Dict[str, Any]:
        """Get detailed information about a file."""
        p = Path(path)
        stat = p.stat()

        return {
            "path": str(p),
            "name": p.name,
            "size": stat.st_size,
            "created": stat.st_ctime,
            "modified": stat.st_mtime,
            "accessed": stat.st_atime,
            "extension": p.suffix,
            "is_readonly": not os.access(path, os.W_OK)
        }

    async def find_large_files(
        self,
        path: str,
        min_size_mb: float = 100
    ) -> List[Dict[str, Any]]:
        """Find files larger than specified size."""
        min_size_bytes = min_size_mb * 1024 * 1024
        large_files = []

        for p in Path(path).rglob("*"):
            if p.is_file():
                size = p.stat().st_size
                if size >= min_size_bytes:
                    large_files.append({
                        "path": str(p),
                        "name": p.name,
                        "size": size,
                        "size_mb": round(size / (1024 * 1024), 2)
                    })

        return sorted(large_files, key=lambda x: x["size"], reverse=True)

    async def find_duplicates(
        self,
        path: str,
        method: str = "hash"
    ) -> List[Dict[str, Any]]:
        """Find duplicate files."""
        import hashlib
        from collections import defaultdict

        hash_map = defaultdict(list)

        for p in Path(path).rglob("*"):
            if p.is_file():
                try:
                    with open(p, "rb") as f:
                        file_hash = hashlib.md5(f.read()).hexdigest()
                    hash_map[file_hash].append({
                        "path": str(p),
                        "name": p.name,
                        "size": p.stat().st_size
                    })
                except:
                    pass

        # Return only groups with duplicates
        return [
            {"hash": h, "files": files, "count": len(files)}
            for h, files in hash_map.items()
            if len(files) > 1
        ]
```

**Dependencies:** [5.1.3](#task-513-create-orchestrator)

**Next Task:** [5.2.2](#task-522-implement-classificationagent)

---

#### Task 5.2.2: Implement ClassificationAgent

**Status:** `[ ]` Not Started

**Description:**
Create agent for classifying files using LLM.

**Acceptance Criteria:**
- [ ] Classifies files by category
- [ ] Uses LLM for ambiguous cases
- [ ] Supports custom classification rules
- [ ] Batch processing support

**Files to Create:**
```
src/nexus-agents/nexus_agents/agents/classification.py
```

**Dependencies:** [5.2.1](#task-521-implement-filediscoveryagent)

**Next Task:** [5.2.3](#task-523-implement-organizationagent)

---

#### Task 5.2.3: Implement OrganizationAgent

**Status:** `[ ]` Not Started

**Description:**
Create agent for organizing files into folders.

**Acceptance Criteria:**
- [ ] Creates folder structures
- [ ] Moves/copies files safely
- [ ] Supports undo operations
- [ ] Transaction logging

**Files to Create:**
```
src/nexus-agents/nexus_agents/agents/organization.py
```

**Dependencies:** [5.2.2](#task-522-implement-classificationagent)

**Next Task:** [5.2.4](#task-524-implement-cleanupagent)

---

#### Task 5.2.4: Implement CleanupAgent

**Status:** `[ ]` Not Started

**Description:**
Create agent for cleaning up files safely.

**Acceptance Criteria:**
- [ ] Identifies safe-to-delete files
- [ ] Requires confirmation
- [ ] Supports recycle bin
- [ ] Detailed reporting

**Files to Create:**
```
src/nexus-agents/nexus_agents/agents/cleanup.py
```

**Dependencies:** [5.2.3](#task-523-implement-organizationagent)

**Next Task:** [5.3.1](#task-531-create-llm-provider-interface)

---

### 5.3 LLM Integration

#### Task 5.3.1: Create LLM Provider Interface

**Status:** `[ ]` Not Started

**Description:**
Create abstraction layer for LLM providers.

**Acceptance Criteria:**
- [ ] OpenAI support
- [ ] Anthropic support
- [ ] Local model support (Ollama)
- [ ] Streaming responses

**Files to Create:**
```
src/nexus-agents/nexus_agents/llm/provider.py
src/nexus-agents/nexus_agents/llm/openai_provider.py
src/nexus-agents/nexus_agents/llm/anthropic_provider.py
src/nexus-agents/nexus_agents/llm/local_provider.py
```

**Dependencies:** [5.2.4](#task-524-implement-cleanupagent)

**Next Task:** [5.4.1](#task-541-integrate-playwright-mcp)

---

### 5.4 Browser Automation

#### Task 5.4.1: Integrate Playwright MCP

**Status:** `[ ]` Not Started

**Description:**
Integrate Playwright for browser automation.

**Acceptance Criteria:**
- [ ] Browser launch/control
- [ ] Page navigation
- [ ] Element interaction
- [ ] Screenshot capture
- [ ] MCP tool exposure

**Files to Create:**
```
src/nexus-agents/nexus_agents/browser/playwright_tools.py
```

**Implementation:**
```python
# playwright_tools.py
from typing import Any, Dict, Optional
from playwright.async_api import async_playwright, Browser, Page

class PlaywrightTools:
    """Browser automation tools using Playwright."""

    def __init__(self):
        self._playwright = None
        self._browser: Optional[Browser] = None
        self._page: Optional[Page] = None

    async def initialize(self, headless: bool = True) -> None:
        """Initialize the browser."""
        self._playwright = await async_playwright().start()
        self._browser = await self._playwright.chromium.launch(headless=headless)
        self._page = await self._browser.new_page()

    async def close(self) -> None:
        """Close the browser."""
        if self._browser:
            await self._browser.close()
        if self._playwright:
            await self._playwright.stop()

    async def navigate(self, url: str) -> Dict[str, Any]:
        """Navigate to a URL."""
        response = await self._page.goto(url)
        return {
            "url": self._page.url,
            "status": response.status if response else None,
            "title": await self._page.title()
        }

    async def click(self, selector: str) -> Dict[str, Any]:
        """Click an element."""
        await self._page.click(selector)
        return {"clicked": selector}

    async def fill(self, selector: str, value: str) -> Dict[str, Any]:
        """Fill a form field."""
        await self._page.fill(selector, value)
        return {"filled": selector, "value": value}

    async def get_text(self, selector: str) -> str:
        """Get text content of an element."""
        return await self._page.text_content(selector)

    async def screenshot(self, path: str = None) -> bytes:
        """Take a screenshot."""
        return await self._page.screenshot(path=path)

    async def get_accessibility_tree(self) -> Dict[str, Any]:
        """Get the accessibility tree for the page."""
        snapshot = await self._page.accessibility.snapshot()
        return snapshot

    async def execute_script(self, script: str) -> Any:
        """Execute JavaScript on the page."""
        return await self._page.evaluate(script)
```

**Dependencies:** [5.3.1](#task-531-create-llm-provider-interface)

---

## Phase Completion Checklist

- [ ] All 5.1.x tasks complete (Agent Framework)
- [ ] All 5.2.x tasks complete (Specialized Agents)
- [ ] All 5.3.x tasks complete (LLM Integration)
- [ ] All 5.4.x tasks complete (Browser Automation)
- [ ] `pytest` passes all tests
- [ ] Agents can be invoked from CLI
- [ ] Multi-agent workflows execute correctly

---

**[Back to Roadmap](PROJECT_ROADMAP.md)** | **Previous:** [Phase 4](PHASE_4_UI_THEMING.md) | **Next:** [Phase 6](PHASE_6_MCP_SERVER.md)
