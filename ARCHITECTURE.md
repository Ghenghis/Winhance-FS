# Winhance-FS Architecture Documentation

> **Purpose:** Complete system architecture overview with diagrams  
> **Last Updated:** 2026-01-26  
> **Version:** 2.0

---

## 📋 Overview

Winhance-FS is a multi-language Windows desktop application for storage intelligence and file forensics. The architecture is designed to leverage the strengths of each technology stack while maintaining clear separation of concerns.

---

## 🏗️ High-Level Architecture

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           Winhance-FS Desktop Application                      │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐            │
│  │   WPF Client    │    │   CLI Tools     │    │  MCP Server     │            │
│  │  (Winhance.WPF) │    │ (nexus_cli)     │    │ (nexus_mcp)     │            │
│  │                 │    │                 │    │                 │            │
│  │ • File Browser  │    │ • Batch Ops     │    │ • AI Integration│            │
│  │ • Visualizations│    │ • Automation    │    │ • Extensibility │            │
│  │ • User Settings │    │ • Scripting     │    │ • API Layer     │            │
│  └─────────────────┘    └─────────────────┘    └─────────────────┘            │
│           │                       │                       │                    │
│           └───────────────────────┼───────────────────────┘                    │
│                                   │                                            │
├─────────────────────────────────────────────────────────────────────────────────┤
│                            Application Layer (C#)                              │
│                                                                                 │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐            │
│  │   Services      │    │   Dialogs       │    │  Navigation     │            │
│  │                 │    │                 │    │                 │            │
│  │ • File Manager  │    │ • Settings      │    │ • View Router   │            │
│  │ • Search        │    │ • Progress      │    │ • State Mgmt    │            │
│  │ • Favorites     │    │ • Conflicts     │    │ • History       │            │
│  └─────────────────┘    └─────────────────┘    └─────────────────┘            │
│                                   │                                            │
├─────────────────────────────────────────────────────────────────────────────────┤
│                            Domain Layer (C#)                                   │
│                                                                                 │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐            │
│  │   Interfaces    │    │    Models       │    │     DTOs        │            │
│  │                 │    │                 │    │                 │            │
│  │ • IFileManager  │    │ • FileEntry     │    │ • SearchQuery   │            │
│  │ • ISearch       │    │ • Directory     │    │ • Operation     │            │
│  │ • IFavorites    │    │ • StorageStats  │    │ • Result        │            │
│  └─────────────────┘    └─────────────────┘    └─────────────────┘            │
└─────────────────────────────────────────────────────────────────────────────────┘
                                   │
┌─────────────────────────────────────────────────────────────────────────────────┐
│                         Infrastructure & Native Layer                           │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐            │
│  │  Windows APIs   │    │   Rust Core     │    │   Python AI     │            │
│  │                 │    │                 │    │                 │            │
│  │ • Registry      │    │ • MFT Reader    │    │ • Classification│            │
│  │ • WinGet        │    │ • USN Journal   │    │ • Analysis      │            │
│  │ • Shell         │    │ • Tantivy       │    │ • Agents        │            │
│  │ • File System   │    │ • SIMD Ops      │    │ • Embeddings    │            │
│  └─────────────────┘    └─────────────────┘    └─────────────────┘            │
│                                   │                                            │
├─────────────────────────────────────────────────────────────────────────────────┤
│                              Data Layer                                        │
│                                                                                 │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐            │
│  │   File System   │    │  Search Index   │    │   Metadata DB   │            │
│  │                 │    │                 │    │                 │            │
│  │ • NTFS Volumes  │    │ • Tantivy Index │    │ • SQLite        │            │
│  │ • MFT Records   │    │ • Full Text     │    │ • File Metadata │            │
│  │ • USN Journal   │    │ • Vector Store  │    │ • Cache         │            │
│  │ • ReFS Support  │    │ • Semantic      │    │ • History       │            │
│  └─────────────────┘    └─────────────────┘    └─────────────────┘            │
└─────────────────────────────────────────────────────────────────────────────────┘
```
```

---

## 🎯 Component Architecture

### WPF Application Layer

```
┌─────────────────────────────────────────────────────────────────┐
│                        Winhance.WPF                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐             │
│  │   Views     │  │ ViewModels  │  │  Controls   │             │
│  │             │  │             │  │             │             │
│  │ • MainView  │  │ • MainVM    │  │ • TreeView  │             │
│  │ • FileView  │  │ • FileVM    │  │ • ListView  │             │
│  │ • SearchView│  │ • SearchVM  │  │ • TreeMap   │             │
│  └─────────────┘  └─────────────┘  └─────────────┘             │
│         │                │                │                    │
│  ┌─────────────────────────────────────────────────────┐       │
│  │              MVVM Infrastructure                   │       │
│  │                                                     │       │
│  │ • CommunityToolkit.Mvvm                             │       │
│  │ • Ioc Container                                      │       │
│  │ • Command Routing                                   │       │
│  │ • Message Bus                                        │       │
│  └─────────────────────────────────────────────────────┘       │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│                      Feature Modules                           │
│                                                                 │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐             │
│  │FileManager  │  │   Search    │  │  Organizer  │             │
│  │             │  │             │  │             │             │
│  │ • DualPane  │  │ • Indexing  │  │ • Smart     │             │
│  │ • Operations│  │ • Filters   │  │ • Rules     │             │
│  │ • Preview   │  │ • History   │  │ • Auto      │             │
│  └─────────────┘  └─────────────┘  └─────────────┘             │
└─────────────────────────────────────────────────────────────────┘
```

### Backend Services Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Backend Services                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────────────────────────────────────────┐       │
│  │                Service Layer                         │       │
│  │                                                     │       │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │       │
│  │  │File Manager │  │   Search    │  │Favorites    │ │       │
│  │  │   Service   │  │   Service   │  │  Service    │ │       │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │       │
│  │         │                │                │        │       │
│  │  ┌─────────────────────────────────────────────┐ │       │
│  │  │            Operation Queue Service          │ │       │
│  │  │  • Background Operations                    │ │       │
│  │  │  • Progress Tracking                        │ │       │
│  │  │  • Conflict Resolution                       │ │       │
│  │  └─────────────────────────────────────────────┘ │       │
│  └─────────────────────────────────────────────────────┘       │
│                                 │                                 │
│  ┌─────────────────────────────────────────────────────┐       │
│  │              Infrastructure Layer                   │       │
│  │                                                     │       │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │       │
│  │  │Registry     │  │   WinGet    │  │  Scheduled  │ │       │
│  │  │Provider     │  │  Provider   │  │   Tasks     │ │       │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │       │
│  │         │                │                │        │       │
│  │  ┌─────────────────────────────────────────────┐ │       │
│  │  │            Windows Services                 │ │       │
│  │  │  • File System Monitoring                   │ │       │
│  │  │  • Volume Management                        │ │       │
│  │  │  • Shell Integration                        │ │       │
│  │  └─────────────────────────────────────────────┘ │       │
│  └─────────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🦀 Rust Backend Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        nexus_core                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────────────────────────────────────────┐       │
│  │                   FFI Bridge                         │       │
│  │                                                     │       │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │       │
│  │  │   Python    │  │    C#       │  │   CLI       │ │       │
│  │  │   Bindings  │  │  Bindings   │  │  Bindings   │ │       │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │       │
│  └─────────────────────────────────────────────────────┘       │
│                                 │                                 │
│  ┌─────────────────────────────────────────────────────┐       │
│  │                 Core Modules                         │       │
│  │                                                     │       │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │       │
│  │  │   MFT       │  │    USN      │  │  Search     │ │       │
│  │  │   Reader    │  │  Monitor    │  │   Engine    │ │       │
│  │  │             │  │             │  │             │ │       │
│  │  │ • Raw I/O   │  │ • Real-time │  │ • Tantivy   │ │       │
│  │  │ • Parsing   │  │ • Events    │  │ • Indexing  │ │       │
│  │  │ • Cache     │  │ • Filtering │  │ • Queries   │ │       │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │       │
│  │         │                │                │        │       │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │       │
│  │  │   Hasher   │  │   Metadata  │  │   Utils     │ │       │
│  │  │             │  │  Extractor  │  │             │ │       │
│  │  │ • xxHash   │  │ • EXIF      │  │ • SIMD      │ │       │
│  │  │ • SHA-256  │  │ • ID3       │  │ • Parallel  │ │       │
│  │  │ • Dedup    │  │ • Streams   │  │ • Cache     │ │       │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │       │
│  └─────────────────────────────────────────────────────┘       │
│                                 │                                 │
│  ┌─────────────────────────────────────────────────────┐       │
│  │               Low-Level Access                       │       │
│  │                                                     │       │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │       │
│  │  │  Volume    │  │   Disk I/O  │  │  Memory     │ │       │
│  │  │  Manager   │  │   Layer     │  │  Manager    │ │       │
│  │  │             │  │             │  │             │ │       │
│  │  │ • Handles   │  │ • Async     │  │ • Arena     │ │       │
│  │  │ • Locking   │  │ • Buffers   │  │ • Pool      │ │       │
│  │  │ • Mounts    │  │ • Mapped    │  │ • Safety    │ │       │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │       │
│  └─────────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🐍 Python AI Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        nexus_ai                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────────────────────────────────────────┐       │
│  │                    MCP Server                        │       │
│  │                                                     │       │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │       │
│  │  │   Tools     │  │   Resources │  │   Prompts   │ │       │
│  │  │             │  │             │  │             │ │       │
│  │  │ • Search    │  │ • Files     │  │ • Analysis  │ │       │
│  │  │ • Organize  │  │ • Metadata  │  │ • Reports   │ │       │
│  │  │ • Analyze   │  │ • Stats     │  │ • Actions   │ │       │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │       │
│  └─────────────────────────────────────────────────────┘       │
│                                 │                                 │
│  ┌─────────────────────────────────────────────────────┐       │
│  │                  AI Services                         │       │
│  │                                                     │       │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │       │
│  │  │ Classifier  │  │  Analyzer   │  │   Agents    │ │       │
│  │  │             │  │             │  │             │ │       │
│  │  │ • ML Models │  │ • Patterns  │  │ • Workflow  │ │       │
│  │  │ • Rules     │  │ • Anomalies │  │ • Tasks     │ │       │
│  │  │ • Learning  │  │ • Reports   │  │ • Automation│ │       │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │       │
│  │         │                │                │        │       │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │       │
│  │  │   Search    │  │   Storage   │  │  Backup     │ │       │
│  │  │  Engine     │  │ Intelligence│  │  System     │ │       │
│  │  │             │  │             │  │             │ │       │
│  │  │ • Semantic  │  │ • Tiering   │  │ • Versioning │ │       │
│  │  │ • Vector    │  │ • Cleanup   │  │ • Dedup     │ │       │
│  │  │ • Hybrid    │  │ • Analysis  │  │ • Restore   │ │       │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │       │
│  └─────────────────────────────────────────────────────┘       │
│                                 │                                 │
│  ┌─────────────────────────────────────────────────────┐       │
│  │                AI Infrastructure                     │       │
│  │                                                     │       │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │       │
│  │  │   Models    │  │   Vector    │  │   Config    │ │       │
│  │  │             │  │    DB       │  │             │ │       │
│  │  │ • Transformers│ │ • Qdrant    │  │ • Settings  │ │       │
│  │  │ • spaCy     │  │ • Chroma    │  │ • Providers │ │       │
│  │  │ • scikit    │  │ • Embeddings│  │ • Models    │ │       │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │       │
│  └─────────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📊 Data Flow Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Data Flow Diagram                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  User Action                                                    │
│     │                                                           │
│     ▼                                                           │
│  ┌─────────────┐    Request    ┌─────────────┐                   │
│  │   WPF UI    │ ────────────► │   Service   │                   │
│  │             │               │   Layer     │                   │
│  └─────────────┘               └─────────────┘                   │
│     ▲                              │                            │
│     │                              ▼                            │
│  ┌─────────────┐    Response   ┌─────────────┐                   │
│  │   View      │ ◄──────────── │ ViewModel   │                   │
│  │   Update    │               │             │                   │
│  └─────────────┘               └─────────────┘                   │
│                                                                 │
│     │                              │                            │
│     │    ┌─────────────────────────┼─────────────────┐          │
│     │    │                         │                 │          │
│     ▼    ▼                         ▼                 ▼          │
│  ┌─────────┐  ┌─────────┐  ┌─────────────┐  ┌─────────────┐   │
│  │ Windows │  │   Rust   │  │   Python    │  │   Storage    │   │
│  │   APIs  │  │Backend  │  │     AI      │  │    Layer     │   │
│  └─────────┘  └─────────┘  └─────────────┘  └─────────────┘   │
│     │          │              │                 │            │
│     └──────────┴──────────────┴─────────────────┘            │
│                   Data Aggregation                              │
│                              │                                    │
│                              ▼                                    │
│  ┌─────────────────────────────────────────────────────┐        │
│  │                  Response Pipeline                  │        │
│  │                                                     │        │
│  │ 1. Collect from all sources                         │        │
│  │ 2. Transform and normalize                          │        │
│  │ 3. Cache results                                   │        │
│  │ 4. Return to UI                                    │        │
│  └─────────────────────────────────────────────────────┘        │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔌 Integration Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Integration Patterns                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────┐    REST/HTTP     ┌─────────────────┐       │
│  │   WPF Client    │ ◄──────────────► │   MCP Server    │       │
│  │                 │                  │                 │       │
│  │ • HTTP Client   │                  │ • FastAPI       │       │
│  │ • JSON/MsgPack  │                  │ • WebSocket     │       │
│  │ • Auth Tokens   │                  │ • SSE Events    │       │
│  └─────────────────┘                  └─────────────────┘       │
│          │                                     │                │
│          │ Native Calls                        │                │
│          ▼                                     ▼                │
│  ┌─────────────────┐    FFI Bridge     ┌─────────────────┐       │
│  │   C# Services   │ ◄──────────────► │   Rust Core     │       │
│  │                 │                  │                 │       │
│  │ • P/Invoke      │                  │ • C ABI         │       │
│  │ • C++/CLI       │                  │ • UniFFI        │       │
│  │ • Shared Memory │                  │ • Pipes         │       │
│  └─────────────────┘                  └─────────────────┘       │
│                                                   │                │
│                                      Python Subprocess          │
│                                                   ▼                │
│  ┌─────────────────┐    Subprocess    ┌─────────────────┐       │
│  │   Rust Core     │ ◄──────────────► │   Python AI     │       │
│  │                 │                  │                 │       │
│  │ • stdin/stdout  │                  │ • FastMCP        │       │
│  │ • Message Pack  │                  │ • Pydantic      │       │
│  │ • Async IPC     │                  │ • asyncio       │       │
│  └─────────────────┘                  └─────────────────┘       │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│                      Event Architecture                          │
│                                                                 │
│  ┌─────────────────┐    Events        ┌─────────────────┐       │
│  │  File System    │ ───────────────► │  Event Bus      │       │
│  │  Monitor        │                  │                 │       │
│  └─────────────────┘                  │ • In-Memory     │       │
│          │                            │ • Persistent    │       │
│          ▼                            │ • Filtered      │       │
│  ┌─────────────────┐                  └─────────────────┘       │
│  │  USN Journal    │                           │                │
│  │  Events         │                           ▼                │
│  └─────────────────┘    ┌─────────────────────────────────┐    │
│                         │        Subscribers            │    │
│                         │                                 │    │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │    │
│  │ UI Updates  │  │ Index Update│  │ AI Analysis │         │    │
│  └─────────────┘  └─────────────┘  └─────────────┘         │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🗄️ Storage Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     Storage Architecture                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────────────────────────────────────────┐       │
│  │                  File System                         │       │
│  │                                                     │       │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │       │
│  │  │   NTFS      │  │    ReFS     │  │   FAT32     │ │       │
│  │  │             │  │             │  │             │ │       │
│  │  │ • MFT       │  │ • Tables    │  │ • FAT       │ │       │
│  │  │ • USN Jrnl  │  │ • Streams   │  │ • Directories│ │       │
│  │  │ • Streams   │  │ • Dedup     │  │ • Attributes │ │       │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │       │
│  └─────────────────────────────────────────────────────┘       │
│                                 │                                 │
│  ┌─────────────────────────────────────────────────────┐       │
│  │                Search Storage                        │       │
│  │                                                     │       │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │       │
│  │  │  Tantivy    │  │   Vector    │  │    Cache    │ │       │
│  │  │   Index     │  │    Store    │  │             │ │       │
│  │  │             │  │             │  │             │ │       │
│  │  │ • Full Text │  │ • Embeddings│  │ • LRU       │ │       │
│  │  │ • Fields    │  │ • HNSW      │  │ • Memory    │ │       │
│  │  │ • Facets    │  │ • Quantized │  │ • Disk      │ │       │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │       │
│  └─────────────────────────────────────────────────────┘       │
│                                 │                                 │
│  ┌─────────────────────────────────────────────────────┐       │
│  │              Metadata Storage                        │       │
│  │                                                     │       │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │       │
│  │  │   SQLite    │  │   Config    │  │    Logs     │ │       │
│  │  │             │  │             │  │             │ │       │
│  │  │ • File Meta │  │ • Settings  │  │ • Rotating  │ │       │
│  │  │ • Indexes   │  │ • Themes    │  │ • Structured│ │       │
│  │  │ • Relations │  │ • History   │  │ • Searchable│ │       │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │       │
│  └─────────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔒 Security Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     Security Architecture                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────────────────────────────────────────┐       │
│  │                 Authentication                       │       │
│  │                                                     │       │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │       │
│  │  │   Windows   │  │   Local     │  │   API       │ │       │
│  │  │   Auth      │  │   Token     │  │   Keys      │ │       │
│  │  │             │  │             │  │             │ │       │
│  │  │ • UAC       │  │ • JWT       │  │ • Managed   │ │       │
│  │  │ • ACLs      │  │ • Refresh   │  │ • Rotation  │ │       │
│  │  │ • Imperson. │  │ • Secure    │  │ • Scope     │ │       │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │       │
│  └─────────────────────────────────────────────────────┘       │
│                                 │                                 │
│  ┌─────────────────────────────────────────────────────┐       │
│  │                  Authorization                        │       │
│  │                                                     │       │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │       │
│  │  │   Policy    │  │   RBAC      │  │   Resource  │ │       │
│  │  │   Engine    │  │             │  │   Access    │ │       │
│  │  │             │  │             │  │             │ │       │
│  │  │ • Rules     │  │ • Roles     │  │ • Files     │ │       │
│  │  │ • Claims    │  │ • Groups    │  │ • APIs      │ │       │
│  │  │ • Context   │  │ • Scopes    │  │ • Admin     │ │       │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │       │
│  └─────────────────────────────────────────────────────┘       │
│                                 │                                 │
│  ┌─────────────────────────────────────────────────────┐       │
│  │                Data Protection                        │       │
│  │                                                     │       │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │       │
│  │  │ Encryption  │  │  Integrity  │  │   Privacy   │ │       │
│  │  │             │  │             │  │             │ │       │
│  │  │ • AES-256   │  │ • HMAC      │  │ • Minimal   │ │       │
│  │  │ • RSA       │  │ • Signatures│  │ • Local     │ │       │
│  │  │ • Keys      │  │ • Checksums │  │ • Consent   │ │       │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │       │
│  └─────────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────────┘
```

---

## ⚡ Performance Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Performance Architecture                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────────────────────────────────────────┐       │
│  │                 Parallelization                       │       │
│  │                                                     │       │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │       │
│  │  │   Thread    │  │   Async     │  │  Parallel   │ │       │
│  │  │    Pool     │  │   Runtime   │  │   Libraries │ │       │
│  │  │             │  │             │  │             │ │       │
│  │  │ • Work Steal│  │ • tokio     │  │ • Rayon     │ │       │
│  │  │ • IOCP      │  │ • async/await│ │ • SIMD      │ │       │
│  │  │ • Affinity  │  │ • Futures   │  │ • GPU       │ │       │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │       │
│  └─────────────────────────────────────────────────────┘       │
│                                 │                                 │
│  ┌─────────────────────────────────────────────────────┐       │
│  │                  Caching Strategy                       │       │
│  │                                                     │       │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │       │
│  │  │    Memory   │  │     Disk    │  │ Distributed │ │       │
│  │  │    Cache    │  │    Cache    │  │    Cache    │ │       │
│  │  │             │  │             │  │             │ │       │
│  │  │ • LRU       │  │ • MMap      │  │ • Redis     │ │       │
│  │  │ • TTL       │  │ • Persistent│  │ • Shared    │ │       │
│  │  │ • Size Lim  │  │ • Compression│ │ • Invalidation│ │       │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │       │
│  └─────────────────────────────────────────────────────┘       │
│                                 │                                 │
│  ┌─────────────────────────────────────────────────────┐       │
│  │                Optimization                         │       │
│  │                                                     │       │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │       │
│  │  │   Data      │  │   Algorithms│  │   I/O       │ │       │
│  │  │ Structures │  │             │  │             │ │       │
│  │  │             │  │             │  │             │ │       │
│  │  │ • B-Trees   │  │ • O(1) Lookup│ │ • Zero Copy │ │       │
│  │  │ • Bloom     │  │ • Indexing  │ │ • Batching  │ │       │
│  │  │ • Skip List │  │ • Memoization│ │ • Prefetch  │ │       │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │       │
│  └─────────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔄 Deployment Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Deployment Architecture                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────────────────────────────────────────┐       │
│  │                  Installation                         │       │
│  │                                                     │       │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │       │
│  │  │   MSI       │  │   Portable  │  │   Store     │ │       │
│  │  │ Installer   │  │   Package   │  │   App       │ │       │
│  │  │             │  │             │  │             │ │       │
│  │  │ • Custom UI │  │ • ZIP       │  │ • MSIX      │ │       │
│  │  │ • Prereqs   │  │ • Self-Ext  │  │ • Auto-Update│ │       │
│  │  │ • Services  │  │ • No Admin  │  │ • Licensing │ │       │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │       │
│  └─────────────────────────────────────────────────────┘       │
│                                 │                                 │
│  ┌─────────────────────────────────────────────────────┐       │
│  │                 Runtime Layout                        │       │
│  │                                                     │       │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │       │
│  │  │   Main      │  │   Backend   │  │   Support   │ │       │
│  │  │   Process   │  │   Processes │  │   Files     │ │       │
│  │  │             │  │             │  │             │ │       │
│  │  │ • WPF App   │  │ • Rust DLL  │  │ • Config    │ │       │
│  │  │ • UI Thread │  │ • Python    │  │ • Logs      │ │       │
│  │  │ • Services  │  │ • MCP       │  │ • Cache     │ │       │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │       │
│  └─────────────────────────────────────────────────────┘       │
│                                 │                                 │
│  ┌─────────────────────────────────────────────────────┐       │
│  │                Update System                          │       │
│  │                                                     │       │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │       │
│  │  │   Auto      │  │   Delta     │  │   Rollback  │ │       │
│  │  │   Update    │  │   Updates   │  │   Support   │ │       │
│  │  │             │  │             │  │             │ │       │
│  │  │ • Check     │  │ • Binary    │  │ • Backup    │ │       │
│  │  │ • Download  │  │ • Patches   │  │ • Restore   │ │       │
│  │  │ • Install   │  │ • Verify    │  │ • Safe Mode │ │       │
│  │  └─────────────┘  └─────────────┘  └─────────────┘ │       │
│  └─────────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📐 Design Principles

### 1. **Separation of Concerns**
```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   UI Layer  │    │ Logic Layer │    │  Data Layer │
│             │    │             │    │             │
│ • Views     │    │ • Services  │    │ • Storage   │
│ • Events    │    │ • Business  │    │ • Persistence│
│ • Commands  │    │ • Rules     │    │ • Access    │
└─────────────┘    └─────────────┘    └─────────────┘
```

### 2. **Dependency Inversion**
```
┌─────────────┐
│   Client    │
│             │
│ Depends on │
└─────┬───────┘
      │
      ▼
┌─────────────┐    ┌─────────────┐
│  Interface  │◄───│ Implementation│
│             │    │             │
│ • Abstraction│    │ • Concrete   │
│ • Contract  │    │ • Details    │
│ • Testable  │    │ • Optimized  │
└─────────────┘    └─────────────┘
```

### 3. **Event-Driven Architecture**
```
┌─────────────┐    Events    ┌─────────────┐
│   Producer  │ ───────────► │  Consumer   │
│             │              │             │
│ • Emit      │              │ • Listen    │
│ • Async     │              │ • React     │
│ • Loosely   │              │ • Decoupled │
└─────────────┘              └─────────────┘
```

---

## 🔧 Technology Mapping

| Layer              | Technology        | Purpose               |
| ------------------ | ----------------- | --------------------- |
| **Presentation**   | WPF / .NET 9      | Modern Windows UI     |
| **Application**    | C# / MVVM         | Business logic        |
| **Domain**         | C# / Interfaces   | Core abstractions     |
| **Infrastructure** | C# / Windows APIs | System integration    |
| **Performance**    | Rust              | High-performance ops  |
| **AI/ML**          | Python            | Intelligence features |
| **Data**           | SQLite / Tantivy  | Storage & search      |
| **Communication**  | FastAPI / FFI     | Inter-process         |

---

## 📚 References

- [Component Details](../components/)
- [API Documentation](../api/)
- [Deployment Guide](../deployment/)
- [Performance Guide](../performance/)

---

*This architecture document is a living representation of the Winhance-FS system. It evolves as the project grows and improves.*
}

impl MftReader {
    pub fn new(volume: &str) -> Result<Self, MftError> {
        // Initialize MFT reader
    }
    
    pub fn enumerate_files(&self) -> impl Iterator<Item = FileRecord> {
        // Enumerate MFT records
    }
    
    pub fn read_record(&self, record_number: u64) -> Result<FileRecord, MftError> {
        // Read specific MFT record
    }
}
```

#### FFI Bridge
```rust
#[repr(C)]
pub struct FileRecordFFI {
    pub name: *const c_char,
    pub size: u64,
    pub created: u64,
    pub modified: u64,
    pub attributes: u32,
}

#[no_mangle]
pub extern "C" fn winhance_get_files(
    path: *const c_char,
    callback: extern "C" fn(*const FileRecordFFI),
) -> bool {
    // FFI implementation
}
```

### AI Layer (Python)

#### MCP Server
```python
class WinhanceMCPServer:
    def __init__(self):
        self.tools = {
            "nexus_search": self.search_files,
            "nexus_organize": self.organize_files,
            "nexus_space": self.analyze_space,
            "nexus_rollback": self.rollback_operations,
        }
    
    async def search_files(self, args: dict) -> dict:
        """Search files with advanced filters"""
        query = args.get("query", "")
        filters = args.get("filters", {})
        # Implementation
        return {"results": results}
```

---

## 🔄 Data Flow Architecture

### File Operation Flow
```
User Action → ViewModel → Service → Infrastructure → Rust Engine
    ↓              ↓         ↓              ↓           ↓
UI Update ← Command ← Result ← Win32 API ← System Call
```

### Search Flow
```
Search Query → ViewModel → SearchService → Tantivy (Rust) → Results
      ↓              ↓           ↓              ↓              ↓
UI Update ← Binding ← Results ← Deserialization ← Matches
```

### Sync Flow
```
Sync Trigger → SyncService → FileMonitor → ConflictDetection → Resolution
      ↓           ↓           ↓              ↓              ↓
UI Update ← Progress ← Events ← FileChanges ← Actions
```

---

## 🎨 UI Architecture

### MVVM Pattern Implementation

#### View-ViewModel Binding
```xml
<!-- View: DualPaneBrowserView.xaml -->
<Grid>
    <ListBox ItemsSource="{Binding LeftPane.Files}"
             SelectedItem="{Binding LeftPane.SelectedItem}">
        <ListBox.ItemTemplate>
            <DataTemplate>
                <StackPanel Orientation="Horizontal">
                    <Image Source="{Binding Icon}" />
                    <TextBlock Text="{Binding Name}" />
                </StackPanel>
            </DataTemplate>
        </ListBox.ItemTemplate>
    </ListBox>
</Grid>
```

```csharp
// ViewModel: DualPaneBrowserViewModel.cs
public partial class DualPaneBrowserViewModel : BaseViewModel
{
    [ObservableProperty]
    private PaneViewModel _leftPane = new();
    
    [ObservableProperty]
    private PaneViewModel _rightPane = new();
    
    [RelayCommand]
    private async Task CopyToRightAsync()
    {
        if (LeftPane.SelectedItem != null)
        {
            await CopyAsync(LeftPane.SelectedItem.FullPath, RightPane.CurrentPath);
            await RefreshPanesAsync();
        }
    }
}
```

### Command Pattern
```csharp
public class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    
    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }
    
    public async Task ExecuteAsync()
    {
        if (CanExecute(null))
        {
            await _execute();
        }
    }
}
```

---

## 🔌 Plugin Architecture

### Plugin Interface
```csharp
public interface IPlugin
{
    string Name { get; }
    string Version { get; }
    string Description { get; }
    
    Task InitializeAsync(IPluginContext context);
    Task ExecuteAsync(IPluginCommand command);
    void Dispose();
}
```

### Plugin Manager
```csharp
public class PluginManager
{
    private readonly Dictionary<string, IPlugin> _plugins = new();
    
    public async Task LoadPluginAsync(string pluginPath)
    {
        var assembly = Assembly.LoadFrom(pluginPath);
        var pluginTypes = assembly.GetTypes()
            .Where(t => typeof(IPlugin).IsAssignableFrom(t));
        
        foreach (var type in pluginTypes)
        {
            var plugin = (IPlugin)Activator.CreateInstance(type);
            await plugin.InitializeAsync(_context);
            _plugins[plugin.Name] = plugin;
        }
    }
}
```

---

## 📊 Performance Architecture

### Caching Strategy
```csharp
public class CacheManager : ICacheManager
{
    private readonly IMemoryCache _cache;
    private readonly IDistributedCache _distributedCache;
    
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiry = null)
    {
        // Try local cache first
        if (_cache.TryGetValue(key, out T value))
        {
            return value;
        }
        
        // Try distributed cache
        var serialized = await _distributedCache.GetAsync(key);
        if (serialized != null)
        {
            value = JsonSerializer.Deserialize<T>(serialized);
            _cache.Set(key, value, expiry ?? TimeSpan.FromMinutes(5));
            return value;
        }
        
        // Create and cache
        value = await factory();
        _cache.Set(key, value, expiry ?? TimeSpan.FromMinutes(5));
        await _distributedCache.SetAsync(key, 
            JsonSerializer.SerializeToUtf8Bytes(value));
        
        return value;
    }
}
```

### Virtualization
```xml
<!-- Virtualizing ListBox for large datasets -->
<ListBox VirtualizingPanel.IsVirtualizing="True"
         VirtualizingPanel.VirtualizationMode="Recycling"
         VirtualizingPanel.IsContainerVirtualizable="True"
         ScrollViewer.IsDeferredScrollingEnabled="True">
```

---

## 🔒 Security Architecture

### Permission Model
```csharp
public class PermissionManager : IPermissionManager
{
    public async Task<bool> HasPermissionAsync(string operation, string resource)
    {
        var user = await _userService.GetCurrentUserAsync();
        var permissions = await _userService.GetPermissionsAsync(user.Id);
        
        return permissions.Any(p => 
            p.Operation == operation && 
            p.Resource == resource &&
            p.IsGranted);
    }
}
```

### Secure File Operations
```csharp
public class SecureFileOperations : IFileOperations
{
    public async Task CopyAsync(string source, string destination)
    {
        // Verify permissions
        if (!await _permissionManager.HasPermissionAsync("copy", source))
        {
            throw new UnauthorizedAccessException("Insufficient permissions");
        }
        
        // Validate paths
        _pathValidator.Validate(source);
        _pathValidator.Validate(destination);
        
        // Perform operation with audit
        await _auditService.LogAsync("copy", source, destination);
        await _fileSystem.CopyAsync(source, destination);
    }
}
```

---

## 🧪 Testing Architecture

### Unit Testing
```csharp
[TestFixture]
public class FileManagerTests
{
    private Mock<IFileSystemService> _mockFileSystem;
    private FileManagerService _fileManager;
    
    [SetUp]
    public void Setup()
    {
        _mockFileSystem = new Mock<IFileSystemService>();
        _fileManager = new FileManagerService(_mockFileSystem.Object);
    }
    
    [Test]
    public async Task CopyFile_ShouldCallFileSystem_WithCorrectParameters()
    {
        // Arrange
        var source = "C:\\test.txt";
        var destination = "C:\\backup\\test.txt";
        
        // Act
        await _fileManager.CopyAsync(source, destination);
        
        // Assert
        _mockFileSystem.Verify(x => x.CopyAsync(source, destination), Times.Once);
    }
}
```

### Integration Testing
```csharp
[TestFixture]
public class SearchIntegrationTests
{
    private TestHost _host;
    private ISearchService _searchService;
    
    [SetUp]
    public void Setup()
    {
        _host = new TestHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSearchServices();
                services.AddTestFileSystem();
            })
            .Build();
        
        _searchService = _host.Services.GetRequiredService<ISearchService>();
    }
    
    [Test]
    public async Task SearchFiles_ShouldReturnResults_FromRealFileSystem()
    {
        // Test with real file system
    }
}
```

---

## 📈 Monitoring & Diagnostics

### Performance Monitoring
```csharp
public class PerformanceMonitor : IPerformanceMonitor
{
    private readonly IMetrics _metrics;
    
    public async Task<T> MeasureAsync<T>(string operationName, Func<Task<T>> operation)
    {
        using var timer = _metrics.StartTimer(operationName);
        try
        {
            var result = await operation();
            _metrics.Increment($"{operationName}_success");
            return result;
        }
        catch
        {
            _metrics.Increment($"{operationName}_error");
            throw;
        }
    }
}
```

### Health Checks
```csharp
public class HealthCheckService : IHealthCheckService
{
    public async Task<HealthReport> CheckHealthAsync()
    {
        var report = new HealthReport();
        
        // Check file system
        report.Add("FileSystem", await CheckFileSystemAsync());
        
        // Check Rust engine
        report.Add("RustEngine", await CheckRustEngineAsync());
        
        // Check AI services
        report.Add("AIServices", await CheckAIServicesAsync());
        
        return report;
    }
}
```

---

## 🚀 Deployment Architecture

### Configuration Management
```csharp
public class ConfigurationService : IConfigurationService
{
    public AppConfiguration GetConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{Environment}.json")
            .AddEnvironmentVariables("WINHANCE_")
            .AddUserSecrets<Program>();
        
        return builder.Build().Get<AppConfiguration>();
    }
}
```

### Service Registration
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWinhanceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Core services
        services.AddSingleton<IFileManagerService, FileManagerService>();
        services.AddSingleton<ISearchService, SearchService>();
        
        // Infrastructure
        services.AddSingleton<IFileSystemService, WindowsFileSystemService>();
        services.AddSingleton<IRegistryService, WindowsRegistryService>();
        
        // Rust integration
        services.AddSingleton<IRustEngine, RustEngine>();
        
        // AI services
        services.AddMcpServer();
        
        return services;
    }
}
```

---

## 📝 Architecture Decisions

### Why Rust for Performance-Critical Code?
- **Memory Safety**: No null pointer exceptions or buffer overflows
- **Performance**: Zero-cost abstractions and LLVM optimizations
- **Concurrency**: Fearless concurrency with ownership model
- **FFI**: Excellent C interoperability for integration

### Why WPF for Desktop UI?
- **Native Windows Integration**: Full access to Windows APIs
- **Mature Framework**: Stable and well-documented
- **MVVM Support**: Excellent data binding and MVVM patterns
- **Customization**: Extensive styling and templating capabilities

### Why Python for AI Layer?
- **ML Ecosystem**: Rich ecosystem of AI/ML libraries
- **Flexibility**: Rapid prototyping and development
- **Integration**: Easy integration with AI models
- **MCP Support**: Native support for Model Context Protocol

### Why Multi-Language Architecture?
- **Right Tool for the Job**: Each language excels in its domain
- **Performance**: Native code where it matters most
- **Maintainability**: Clear separation of concerns
- **Future-Proof**: Easy to replace individual components

---

## 🔮 Future Architecture Considerations

### Cloud Integration
- **Storage Providers**: Support for cloud storage APIs
- **Sync Engine**: Multi-cloud synchronization
- **Offline Mode**: Intelligent caching and offline support

### Advanced AI Features
- **Local Models**: On-device AI processing
- **Semantic Search**: Vector embeddings and similarity search
- **Predictive Analytics**: Usage pattern analysis

### Performance Enhancements
- **GPU Acceleration**: CUDA/OpenCL for parallel processing
- **Distributed Processing**: Multi-machine file operations
- **Edge Computing**: Processing closer to data

---

## � Navigation

**Related Documents:**
- [Project Roadmap Hub](docs/roadmap/PROJECT-ROADMAP-HUB.md)
- [Task System Features](docs/roadmap/TASK-SYSTEM-FEATURES.md)
- [Code Quality Standards](docs/roadmap/quality/code-standards.md)
- [Testing Strategy](docs/roadmap/quality/testing-strategy.md)
- [Release Checklist](docs/roadmap/release/release-checklist.md)

---

## �📚 References

### Design Patterns
- [MVVM Pattern](https://docs.microsoft.com/en-us/archive/msdn-magazine/2009/february/patterns-wpf-apps-with-the-model-view-viewmodel-design-pattern)
- [Repository Pattern](https://martinfowler.com/eaaCatalog/repository.html)
- [CQRS Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)

### Architecture Frameworks
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Hexagonal Architecture](https://netflixtechblog.com/ready-for-changes-with-hexagonal-architecture-b31c3cc719a5)
- [Microservices Architecture](https://microservices.io/)

### Technology Documentation
- [.NET 9 Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [Rust Book](https://doc.rust-lang.org/book/)
- [WPF Documentation](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)

---

*This architecture document is continuously updated as the system evolves. For the most current information, refer to the source code and inline documentation.*
