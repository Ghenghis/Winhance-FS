"""
List directory contents - nexus ls command
"""

import typer
import os
import stat
from pathlib import Path
from datetime import datetime
from typing import Optional, List
from rich.console import Console
from rich.table import Table
from rich.tree import Tree
from rich import print as rprint
import humanize

app = typer.Typer(help="List directory contents")
console = Console()


def format_permissions(mode: int) -> str:
    """Format file permissions in rwx format"""
    perms = []
    perms.append('d' if stat.S_ISDIR(mode) else '-')
    
    for who in ("USR", "GRP", "OTH"):
        for what in ("R", "W", "X"):
            perm = getattr(stat, f"S_I{what}{who}")
            perms.append('r' if mode & perm else '-')
    
    return ''.join(perms)


def get_file_icon(path: Path) -> str:
    """Get icon for file type"""
    if path.is_dir():
        return "📁"
    
    ext = path.suffix.lower()
    icons = {
        '.txt': '📄',
        '.pdf': '📕',
        '.doc': '📘',
        '.docx': '📘',
        '.xls': '📗',
        '.xlsx': '📗',
        '.ppt': '📙',
        '.pptx': '📙',
        '.jpg': '🖼️',
        '.jpeg': '🖼️',
        '.png': '🖼️',
        '.gif': '🖼️',
        '.mp3': '🎵',
        '.mp4': '🎬',
        .zip': '📦',
        '.rar': '📦',
        '.7z': '📦',
        '.exe': '⚙️',
        '.dll': '⚙️',
        '.py': '🐍',
        '.js': '📜',
        '.html': '🌐',
        '.css': '🎨',
    }
    
    return icons.get(ext, '📄')


@app.callback(invoke_without_command=True)
def ls_cmd(
    ctx: typer.Context,
    path: str = typer.Argument(".", help="Directory to list"),
    long: bool = typer.Option(False, "-l", "--long", help="Long format"),
    all: bool = typer.Option(False, "-a", "--all", help="Show hidden files"),
    sort: str = typer.Option("name", "-s", "--sort", help="Sort by: name, size, date, type"),
    reverse: bool = typer.Option(False, "-r", "--reverse", help="Reverse sort order"),
    human: bool = typer.Option(True, "-h", "--human", help="Human readable sizes"),
    tree: bool = typer.Option(False, "-t", "--tree", help="Tree view"),
    depth: int = typer.Option(1, "-d", "--depth", help="Tree depth"),
    color: bool = typer.Option(True, "--color/--no-color", help="Color output")
):
    """List directory contents"""
    
    # Resolve path
    target_path = Path(path).resolve()
    
    if not target_path.exists():
        console.print(f"[red]Error: '{path}' does not exist[/red]")
        raise typer.Exit(1)
    
    if not target_path.is_dir():
        # If it's a file, just show its info
        if long:
            show_file_info(target_path, human)
        else:
            console.print(target_path.name)
        return
    
    # Get directory contents
    try:
        items = list(target_path.iterdir())
    except PermissionError:
        console.print(f"[red]Error: Permission denied[/red]")
        raise typer.Exit(1)
    
    # Filter hidden files unless -a is used
    if not all:
        items = [item for item in items if not item.name.startswith('.')]
    
    # Sort items
    reverse_sort = -1 if reverse else 1
    
    if sort == "name":
        items.sort(key=lambda x: x.name.lower(), reverse=reverse < 0)
    elif sort == "size":
        items.sort(key=lambda x: (x.is_file(), x.stat().st_size if x.is_file() else 0), reverse=reverse < 0)
    elif sort == "date":
        items.sort(key=lambda x: x.stat().st_mtime, reverse=reverse < 0)
    elif sort == "type":
        items.sort(key=lambda x: (x.is_file(), x.suffix.lower()), reverse=reverse < 0)
    
    # Display results
    if tree:
        show_tree_view(target_path, items, depth, all, sort, reverse)
    elif long:
        show_long_view(items, human)
    else:
        show_simple_view(items, color)


def show_simple_view(items: List[Path], color: bool):
    """Show simple list view"""
    # Calculate column widths
    terminal_width = console.width
    max_name_len = max(len(item.name) for item in items) if items else 0
    columns = max(1, terminal_width // (max_name_len + 2))
    
    # Group into columns
    rows = []
    for i in range(0, len(items), columns):
        row = items[i:i + columns]
        rows.append(row)
    
    # Print rows
    for row in rows:
        line_parts = []
        for item in row:
            name = item.name
            if color:
                if item.is_dir():
                    name = f"[blue]{name}[/blue]"
                elif item.is_file() and os.access(item, os.X_OK):
                    name = f"[green]{name}[/green]"
            line_parts.append(f"{name:<{max_name_len}}")
        console.print("  ".join(line_parts))


def show_long_view(items: List[Path], human: bool):
    """Show detailed list view"""
    table = Table(show_header=False, box=None)
    table.add_column("permissions", style="cyan")
    table.add_column("links", justify="right", style="white")
    table.add_column("owner", style="white")
    table.add_column("group", style="white")
    table.add_column("size", justify="right", style="yellow")
    table.add_column("date", style="green")
    table.add_column("name", style="white")
    
    for item in items:
        try:
            stat_info = item.stat()
            
            # Permissions
            perms = format_permissions(stat_info.st_mode)
            
            # Number of links
            links = str(stat_info.st_nlink)
            
            # Owner and group (on Windows, use placeholders)
            owner = "user"  # TODO: Get actual owner on Windows
            group = "group"  # TODO: Get actual group on Windows
            
            # Size
            if item.is_file():
                size = humanize.naturalsize(stat_info.st_size) if human else str(stat_info.st_size)
            else:
                size = "-"
            
            # Date
            date_str = datetime.fromtimestamp(stat_info.st_mtime).strftime("%Y-%m-%d %H:%M")
            
            # Name with color
            name = item.name
            if item.is_dir():
                name = f"[blue]{name}[/blue]"
            elif item.is_file() and os.access(item, os.X_OK):
                name = f"[green]{name}[/green]"
            
            table.add_row(perms, links, owner, group, size, date_str, name)
            
        except OSError as e:
            console.print(f"[red]Error reading {item.name}: {e}[/red]")
    
    console.print(table)


def show_tree_view(root_path: Path, items: List[Path], max_depth: int, show_hidden: bool, sort_by: str, reverse: bool):
    """Show tree view"""
    tree = Tree(f"[bold blue]{root_path.name}[/bold blue]")
    
    def add_tree_items(parent, path: Path, depth: int):
        if depth >= max_depth:
            return
        
        try:
            children = list(path.iterdir())
        except PermissionError:
            return
        
        # Filter and sort
        if not show_hidden:
            children = [c for c in children if not c.name.startswith('.')]
        
        # Sort directories first, then files
        children.sort(key=lambda x: (not x.is_dir(), x.name.lower()))
        
        for child in children:
            if child.is_dir():
                branch = parent.add(f"[blue]{get_file_icon(child)} {child.name}[/blue]")
                add_tree_items(branch, child, depth + 1)
            else:
                parent.add(f"{get_file_icon(child)} {child.name}")
    
    # Add items to tree
    for item in sorted(items, key=lambda x: (not x.is_dir(), x.name.lower())):
        if item.is_dir():
            branch = tree.add(f"[blue]{get_file_icon(item)} {item.name}[/blue]")
            if max_depth > 1:
                add_tree_items(branch, item, 1)
        else:
            tree.add(f"{get_file_icon(item)} {item.name}")
    
    console.print(tree)


def show_file_info(path: Path, human: bool):
    """Show detailed info for a single file"""
    stat_info = path.stat()
    
    table = Table(box=None)
    table.add_column("Property", style="cyan")
    table.add_column("Value", style="white")
    
    table.add_row("File", path.name)
    table.add_row("Path", str(path))
    table.add_row("Size", humanize.naturalsize(stat_info.st_size) if human else str(stat_info.st_size))
    table.add_row("Modified", datetime.fromtimestamp(stat_info.st_mtime).strftime("%Y-%m-%d %H:%M:%S"))
    table.add_row("Created", datetime.fromtimestamp(stat_info.st_ctime).strftime("%Y-%m-%d %H:%M:%S"))
    table.add_row("Permissions", format_permissions(stat_info.st_mode))
    
    console.print(table)
