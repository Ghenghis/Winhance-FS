"""
Search files - nexus search command
"""

import os
import re
import time
from datetime import datetime
from pathlib import Path

import humanize
import typer
from rich.console import Console
from rich.progress import BarColumn, Progress, SpinnerColumn, TaskProgressColumn, TextColumn
from rich.table import Table

app = typer.Typer(help="Search for files and content")
console = Console()


class SearchEngine:
    """Fast search engine with multiple search modes"""

    def __init__(self):
        self.results = []
        self.total_files = 0
        self.searched_files = 0

    def search_by_name(
        self,
        pattern: str,
        search_path: Path,
        recursive: bool = True,
        case_sensitive: bool = False,
        use_regex: bool = False,
    ):
        """Search files by name pattern"""
        self.results = []
        self.total_files = 0

        # Compile regex if needed
        if use_regex:
            flags = 0 if case_sensitive else re.IGNORECASE
            try:
                regex_pattern = re.compile(pattern, flags)
            except re.error as e:
                console.print(f"[red]Invalid regex pattern: {e}[/red]")
                return

        # Walk directory tree
        with Progress(
            SpinnerColumn(),
            TextColumn("[progress.description]{task.description}"),
            BarColumn(),
            TaskProgressColumn(),
            console=console,
        ) as progress:

            task = progress.add_task("Searching...", total=None)

            if recursive:
                for root, _dirs, files in os.walk(search_path):
                    self.total_files += len(files)

                    # Search files
                    for file in files:
                        self.searched_files += 1

                        if use_regex:
                            if regex_pattern.search(file):
                                self.results.append(Path(root) / file)
                        else:
                            if case_sensitive:
                                if pattern in file:
                                    self.results.append(Path(root) / file)
                            else:
                                if pattern.lower() in file.lower():
                                    self.results.append(Path(root) / file)

                        # Update progress periodically
                        if self.searched_files % 100 == 0:
                            progress.update(
                                task, description=f"Searching... {self.searched_files} files"
                            )
            else:
                items = list(search_path.iterdir())
                self.total_files = len(items)

                for item in items:
                    if item.is_file():
                        self.searched_files += 1

                        if use_regex:
                            if regex_pattern.search(item.name):
                                self.results.append(item)
                        else:
                            if case_sensitive:
                                if pattern in item.name:
                                    self.results.append(item)
                            else:
                                if pattern.lower() in item.name.lower():
                                    self.results.append(item)

                        progress.update(task, advance=1)

            progress.stop()

    def search_by_content(
        self,
        pattern: str,
        search_path: Path,
        file_types: list[str] = None,
        case_sensitive: bool = False,
        use_regex: bool = False,
        max_results: int = 100,
    ):
        """Search file contents"""
        self.results = []

        # Default to text files
        if not file_types:
            file_types = [".txt", ".py", ".js", ".html", ".css", ".json", ".xml", ".md", ".log"]

        # Compile regex
        flags = 0 if case_sensitive else re.IGNORECASE
        if use_regex:
            try:
                regex_pattern = re.compile(pattern, flags)
            except re.error as e:
                console.print(f"[red]Invalid regex pattern: {e}[/red]")
                return
        else:
            regex_pattern = re.compile(re.escape(pattern), flags)

        with Progress(
            SpinnerColumn(), TextColumn("[progress.description]{task.description}"), console=console
        ) as progress:

            task = progress.add_task("Searching content...", total=None)

            for root, _dirs, files in os.walk(search_path):
                for file in files:
                    file_path = Path(root) / file

                    # Check file type
                    if file_path.suffix.lower() not in file_types:
                        continue

                    # Skip binary files
                    if self.is_binary_file(file_path):
                        continue

                    try:
                        # Search in file
                        with open(file_path, encoding="utf-8", errors="ignore") as f:
                            for line_num, line in enumerate(f, 1):
                                if regex_pattern.search(line):
                                    self.results.append(
                                        {
                                            "file": file_path,
                                            "line": line_num,
                                            "content": line.strip(),
                                        }
                                    )
                                    break

                        if len(self.results) >= max_results:
                            break

                    except (UnicodeDecodeError, PermissionError):
                        continue

                if len(self.results) >= max_results:
                    break

                progress.update(
                    task, description=f"Searching content... {len(self.results)} matches"
                )

            progress.stop()

    def is_binary_file(self, file_path: Path) -> bool:
        """Check if file is binary"""
        try:
            with open(file_path, "rb") as f:
                chunk = f.read(1024)
                return b"\0" in chunk
        except OSError:
            return True


@app.callback(invoke_without_command=True)
def search_cmd(
    ctx: typer.Context,
    pattern: str = typer.Argument(..., help="Search pattern"),
    path: str = typer.Option(".", "-p", "--path", help="Search path"),
    recursive: bool = typer.Option(True, "-r", "--recursive", help="Search recursively"),
    case_sensitive: bool = typer.Option(False, "-c", "--case", help="Case sensitive search"),
    regex: bool = typer.Option(False, "--regex", help="Use regex pattern"),
    type: str | None = typer.Option(None, "-t", "--type", help="File type filter"),
    size_min: str | None = typer.Option(None, "--min-size", help="Minimum file size"),
    size_max: str | None = typer.Option(None, "--max-size", help="Maximum file size"),
    content: bool = typer.Option(False, "--content", help="Search file contents"),
    limit: int = typer.Option(100, "-l", "--limit", help="Max results"),
    output: str | None = typer.Option(None, "-o", "--output", help="Output to file"),
):
    """Search for files"""

    search_path = Path(path).resolve()

    if not search_path.exists():
        console.print(f"[red]Error: Path '{path}' does not exist[/red]")
        raise typer.Exit(1)

    # Create search engine
    engine = SearchEngine()

    # Start timing
    start_time = time.time()

    # Perform search
    if content:
        # Content search
        file_types = [type] if type else None
        engine.search_by_content(
            pattern,
            search_path,
            file_types=file_types,
            case_sensitive=case_sensitive,
            use_regex=regex,
            max_results=limit,
        )
    else:
        # Filename search
        engine.search_by_name(
            pattern,
            search_path,
            recursive=recursive,
            case_sensitive=case_sensitive,
            use_regex=regex,
        )

        # Apply size filters
        if size_min or size_max:
            engine.results = filter_by_size(engine.results, size_min, size_max)

    # Calculate duration
    duration = time.time() - start_time

    # Display results
    if content:
        display_content_results(engine.results, pattern, duration, output)
    else:
        display_file_results(engine.results, pattern, duration, output)


def filter_by_size(files: list[Path], min_size: str | None, max_size: str | None) -> list[Path]:
    """Filter files by size"""

    def parse_size(size_str: str) -> int:
        """Parse size string like '10MB', '1GB'"""
        size_str = size_str.upper()
        if size_str.endswith("KB"):
            return int(size_str[:-2]) * 1024
        elif size_str.endswith("MB"):
            return int(size_str[:-2]) * 1024 * 1024
        elif size_str.endswith("GB"):
            return int(size_str[:-2]) * 1024 * 1024 * 1024
        else:
            return int(size_str)

    min_bytes = parse_size(min_size) if min_size else 0
    max_bytes = parse_size(max_size) if max_size else float("inf")

    filtered = []
    for file in files:
        try:
            size = file.stat().st_size
            if min_bytes <= size <= max_bytes:
                filtered.append(file)
        except OSError:
            continue

    return filtered


def display_file_results(results: list[Path], pattern: str, duration: float, output: str | None):
    """Display filename search results"""

    if output:
        # Save to file
        with open(output, "w") as f:
            for result in results:
                f.write(str(result) + "\n")
        console.print(f"[green]Results saved to {output}[/green]")
        return

    if not results:
        console.print(f"[yellow]No files found matching '{pattern}'[/yellow]")
        return

    # Create results table
    table = Table(title=f"Search Results for '{pattern}'")
    table.add_column("File", style="cyan")
    table.add_column("Size", justify="right", style="yellow")
    table.add_column("Modified", style="green")
    table.add_column("Path", style="white")

    for result in results[:50]:  # Limit display to 50
        try:
            stat = result.stat()
            size = humanize.naturalsize(stat.st_size)
            modified = datetime.fromtimestamp(stat.st_mtime).strftime("%Y-%m-%d %H:%M")
            table.add_row(result.name, size, modified, str(result.parent))
        except OSError:
            table.add_row(result.name, "-", "-", str(result.parent))

    console.print(table)

    # Summary
    console.print(f"\n[green]Found {len(results)} files in {duration:.2f} seconds[/green]")
    if len(results) > 50:
        console.print("[yellow]Showing first 50 results[/yellow]")


def display_content_results(results: list[dict], pattern: str, duration: float, output: str | None):
    """Display content search results"""

    if output:
        # Save to file
        with open(output, "w") as f:
            for result in results:
                f.write(f"{result['file']}:{result['line']}: {result['content']}\n")
        console.print(f"[green]Results saved to {output}[/green]")
        return

    if not results:
        console.print(f"[yellow]No content found matching '{pattern}'[/yellow]")
        return

    # Create results table
    table = Table(title=f"Content Search Results for '{pattern}'")
    table.add_column("File", style="cyan")
    table.add_column("Line", justify="right", style="yellow")
    table.add_column("Content", style="white")

    for result in results[:50]:  # Limit display to 50
        table.add_row(
            str(result["file"]),
            str(result["line"]),
            result["content"][:100] + "..." if len(result["content"]) > 100 else result["content"],
        )

    console.print(table)

    # Summary
    console.print(f"\n[green]Found {len(results)} matches in {duration:.2f} seconds[/green]")
    if len(results) > 50:
        console.print("[yellow]Showing first 50 results[/yellow]")
