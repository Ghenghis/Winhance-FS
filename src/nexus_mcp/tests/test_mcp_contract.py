from __future__ import annotations

import sys
from pathlib import Path

import pytest
from mcp import ClientSession
from mcp.client.stdio import StdioServerParameters, stdio_client


REPO_ROOT = Path(__file__).resolve().parents[3]
SRC_ROOT = REPO_ROOT / "src"


def server_parameters() -> StdioServerParameters:
    return StdioServerParameters(
        command=sys.executable,
        args=["-m", "nexus_mcp"],
        env={"PYTHONPATH": str(SRC_ROOT)},
    )


async def call_tool(name: str, arguments: dict[str, object]) -> dict[str, object]:
    async with stdio_client(server_parameters()) as (read_stream, write_stream):
        async with ClientSession(read_stream, write_stream) as session:
            await session.initialize()
            result = await session.call_tool(name, arguments)

    assert len(result.content) == 1
    content = result.content[0]
    assert content.type == "text"

    import json

    payload = json.loads(content.text)
    assert isinstance(payload, dict)
    return payload


@pytest.mark.anyio
async def test_list_tools_contract() -> None:
    async with stdio_client(server_parameters()) as (read_stream, write_stream):
        async with ClientSession(read_stream, write_stream) as session:
            await session.initialize()
            tools = await session.list_tools()

    tool_names = {tool.name for tool in tools.tools}
    assert tool_names == {
        "nexus_search",
        "nexus_index",
        "nexus_organize",
        "nexus_rollback",
        "nexus_space",
        "nexus_models",
        "nexus_similar",
    }


@pytest.mark.anyio
async def test_nexus_search_contract() -> None:
    payload = await call_tool("nexus_search", {"query": "*.rs", "type": "glob", "limit": 3})

    assert payload["query"] == "*.rs"
    assert payload["type"] == "glob"
    assert payload["limit"] == 3
    assert isinstance(payload["results"], list)


@pytest.mark.anyio
async def test_nexus_index_contract() -> None:
    payload = await call_tool("nexus_index", {"path": "Z:\\not-allowed", "deep": False})

    assert "error" in payload
    assert "Invalid path" in payload["error"] or "Indexer not available" in payload["error"]


@pytest.mark.anyio
async def test_nexus_organize_contract() -> None:
    payload = await call_tool(
        "nexus_organize",
        {"path": str(Path.home()), "strategy": "type", "dry_run": True},
    )

    assert payload["path"] == str(Path.home())
    assert payload["strategy"] == "type"
    assert payload["dry_run"] is True
    assert isinstance(payload["suggestions"], list)


@pytest.mark.anyio
async def test_nexus_rollback_contract() -> None:
    payload = await call_tool("nexus_rollback", {})

    assert "error" in payload
    assert payload["error"] == "Specify transaction_id or hours" or "not available" in payload["error"]


@pytest.mark.anyio
async def test_nexus_space_contract() -> None:
    payload = await call_tool("nexus_space", {"path": "Z:\\not-allowed"})

    assert "error" in payload
    assert "Invalid path" in payload["error"]


@pytest.mark.anyio
async def test_nexus_models_contract() -> None:
    payload = await call_tool("nexus_models", {"action": "scan"})

    if "error" in payload:
        assert "not available" in payload["error"]
    else:
        assert payload["action"] == "scan"
        assert isinstance(payload["summary"], dict)


@pytest.mark.anyio
async def test_nexus_similar_contract() -> None:
    payload = await call_tool("nexus_similar", {"file_path": "C:\\Users\\missing.txt", "limit": 2})

    assert payload["file_path"] == "C:\\Users\\missing.txt"
    assert payload["limit"] == 2
    assert isinstance(payload["similar_files"], list)
