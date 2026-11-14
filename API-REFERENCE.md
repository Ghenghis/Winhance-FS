# Winhance-FS API Reference

## Overview

This document provides a comprehensive reference for all APIs exposed by Winhance-FS, including the REST API, WebSocket API, and MCP Server endpoints.

---

## 🌐 REST API

### Base URL
```
https://localhost:7243/api/v1
```

### Authentication
All API requests require an API key or JWT token:
```http
Authorization: Bearer <jwt-token>
X-API-Key: <api-key>
```

### Common Response Format
```json
{
  "success": true,
  "data": {},
  "error": null,
  "timestamp": "2026-01-24T18:45:00Z"
}
```

---

## 📁 File Manager API

### Get Directory Contents
```http
GET /api/v1/files/list?path={path}&includeHidden={bool}
```

**Parameters:**
- `path` (string, required): Directory path
- `includeHidden` (boolean, optional): Include hidden files

**Response:**
```json
{
  "success": true,
  "data": {
    "path": "C:\\Users\\Admin\\Documents",
    "files": [
      {
        "name": "Report.docx",
        "fullPath": "C:\\Users\\Admin\\Documents\\Report.docx",
        "size": 24576,
        "createdDate": "2026-01-15T10:30:00Z",
        "modifiedDate": "2026-01-20T14:45:00Z",
        "attributes": "Archive",
        "isDirectory": false,
        "icon": "📄"
      }
    ],
    "totalFiles": 42,
    "totalSize": 1048576
  }
}
```

### Copy File/Directory
```http
POST /api/v1/files/copy
```

**Request Body:**
```json
{
  "source": "C:\\Source\\File.txt",
  "destination": "C:\\Destination\\File.txt",
  "overwrite": false,
  "preserveTimestamps": true
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "operationId": "op-123456",
    "status": "InProgress",
    "progress": 0
  }
}
```

### Move File/Directory
```http
POST /api/v1/files/move
```

**Request Body:**
```json
{
  "source": "C:\\Source\\File.txt",
  "destination": "C:\\Destination\\File.txt"
}
```

### Delete File/Directory
```http
DELETE /api/v1/files/delete?path={path}&permanent={bool}
```

**Parameters:**
- `path` (string, required): Path to delete
- `permanent` (boolean, optional): Skip recycle bin

### Create Directory
```http
POST /api/v1/files/directory
```

**Request Body:**
```json
{
  "path": "C:\\NewFolder",
  "name": "MyNewFolder"
}
```

### Get File Properties
```http
GET /api/v1/files/properties?path={path}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "name": "Document.pdf",
    "fullPath": "C:\\Documents\\Document.pdf",
    "size": 524288,
    "createdDate": "2026-01-10T09:00:00Z",
    "modifiedDate": "2026-01-15T16:30:00Z",
    "accessedDate": "2026-01-24T10:15:00Z",
    "attributes": ["Archive", "ReadOnly"],
    "checksum": {
      "md5": "d41d8cd98f00b204e9800998ecf8427e",
      "sha256": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
    },
    "metadata": {
      "author": "John Doe",
      "title": "Important Document",
      "subject": "Project Report"
    }
  }
}
```

---

## 🔍 Search API

### Search Files
```http
GET /api/v1/search?query={query}&path={path}&type={type}&size={size}&date={date}
```

**Parameters:**
- `query` (string, required): Search query
- `path` (string, optional): Search path (default: all drives)
- `type` (string, optional): File type filter
- `size` (string, optional): Size range (e.g., ">10MB")
- `date` (string, optional): Date range (e.g., "lastweek")

**Response:**
```json
{
  "success": true,
  "data": {
    "query": "*.pdf",
    "totalResults": 15,
    "results": [
      {
        "name": "AnnualReport.pdf",
        "fullPath": "C:\\Reports\\AnnualReport.pdf",
        "size": 2097152,
        "modifiedDate": "2026-01-20T11:00:00Z",
        "relevanceScore": 0.95,
        "snippet": "This is the annual financial report for 2026...",
        "matches": [
          {
            "field": "content",
            "text": "annual report",
            "positions": [10, 156]
          }
        ]
      }
    ],
    "facets": {
      "fileTypes": {
        "pdf": 15,
        "docx": 8,
        "xlsx": 5
      },
      "sizeRanges": {
        "<1MB": 12,
        "1-10MB": 10,
        ">10MB": 6
      }
    }
  }
}
```

### Advanced Search
```http
POST /api/v1/search/advanced
```

**Request Body:**
```json
{
  "query": {
    "operator": "AND",
    "conditions": [
      {
        "field": "name",
        "operator": "contains",
        "value": "report"
      },
      {
        "field": "size",
        "operator": ">",
        "value": "1MB"
      },
      {
        "field": "modified",
        "operator": ">=",
        "value": "2026-01-01"
      }
    ]
  },
  "options": {
    "includeContent": true,
    "maxResults": 100,
    "sortBy": "relevance",
    "sortOrder": "desc"
  }
}
```

### Search Suggestions
```http
GET /api/v1/search/suggestions?q={partial}&limit={limit}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "suggestions": [
      "Annual Report 2026",
      "Financial Report Q1",
      "Project Status Report"
    ]
  }
}
```

---

## 📦 Archive API

### Create Archive
```http
POST /api/v1/archive/create
```

**Request Body:**
```json
{
  "sourcePaths": ["C:\\Documents\\Folder1", "C:\\Documents\\File.txt"],
  "destination": "C:\\Archives\\backup.zip",
  "format": "zip",
  "compressionLevel": 5,
  "password": "optional-password",
  "splitSize": 0
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "archiveId": "arc-789012",
    "path": "C:\\Archives\\backup.zip",
    "size": 10485760,
    "fileCount": 150,
    "compressionRatio": 0.65
  }
}
```

### Extract Archive
```http
POST /api/v1/archive/extract
```

**Request Body:**
```json
{
  "archivePath": "C:\\Archives\\backup.zip",
  "destination": "C:\\Extracted\\",
  "password": "optional-password",
  "files": ["specific/file.txt"] // optional, null for all
}
```

### List Archive Contents
```http
GET /api/v1/archive/list?path={path}&password={password}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "format": "zip",
    "totalFiles": 150,
    "totalSize": 10485760,
    "isEncrypted": false,
    "files": [
      {
        "name": "document.pdf",
        "path": "folder/document.pdf",
        "size": 524288,
        "compressedSize": 342453,
        "modifiedDate": "2026-01-15T10:30:00Z",
        "crc32": "12345678"
      }
    ]
  }
}
```

---

## 🔄 Synchronization API

### Create Sync Profile
```http
POST /api/v1/sync/profile
```

**Request Body:**
```json
{
  "name": "Documents Backup",
  "source": "C:\\Users\\Admin\\Documents",
  "destination": "D:\\Backup\\Documents",
  "mode": "Mirror",
  "options": {
    "includeSubfolders": true,
    "preserveTimestamps": true,
    "excludePatterns": ["*.tmp", "*.log"],
    "conflictResolution": "KeepSource"
  }
}
```

### Start Synchronization
```http
POST /api/v1/sync/start/{profileId}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "syncId": "sync-345678",
    "status": "Running",
    "startTime": "2026-01-24T18:45:00Z",
    "progress": {
      "filesProcessed": 45,
      "totalFiles": 100,
      "bytesTransferred": 104857600,
      "totalBytes": 209715200
    }
  }
}
```

### Get Sync Status
```http
GET /api/v1/sync/status/{syncId}
```

### List Sync Profiles
```http
GET /api/v1/sync/profiles
```

---

## 📊 Space Analysis API

### Get Disk Usage
```http
GET /api/v1/space/disk/{drive}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "drive": "C:",
    "totalSpace": 107374182400,
    "freeSpace": 53687091200,
    "usedSpace": 53687091200,
    "usagePercentage": 50.0,
    "fileSystem": "NTFS",
    "label": "Windows",
    "driveType": "Fixed"
  }
}
```

### Analyze Folder Size
```http
POST /api/v1/space/analyze
```

**Request Body:**
```json
{
  "path": "C:\\Users\\Admin\\Documents",
  "depth": 3,
  "includeHidden": false,
  "groupBy": "extension"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "totalSize": 1073741824,
    "totalFiles": 1250,
    "totalFolders": 85,
    "analysis": [
      {
        "name": "PDF Documents",
        "path": "C:\\Users\\Admin\\Documents\\PDFs",
        "size": 536870912,
        "fileCount": 250,
        "percentage": 50.0,
        "subItems": [...]
      }
    ],
    "fileTypes": [
      {
        "extension": ".pdf",
        "count": 250,
        "totalSize": 536870912,
        "percentage": 50.0
      }
    ]
  }
}
```

### Find Largest Files
```http
GET /api/v1/space/largest?path={path}&count={count}&minSize={size}
```

---

## 🎨 Visualization API

### Generate TreeMap Data
```http
POST /api/v1/visualization/treemap
```

**Request Body:**
```json
{
  "path": "C:\\Users\\Admin\\Documents",
  "maxDepth": 3,
  "colorScheme": "byType",
  "minSize": 1048576
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "name": "Documents",
    "path": "C:\\Users\\Admin\\Documents",
    "size": 1073741824,
    "children": [
      {
        "name": "PDFs",
        "path": "C:\\Users\\Admin\\Documents\\PDFs",
        "size": 536870912,
        "color": "#FF6B6B",
        "children": [...]
      }
    ]
  }
}
```

### Get Timeline Data
```http
GET /api/v1/visualization/timeline?path={path}&granularity={granularity}&start={date}&end={date}
```

---

## 🔌 WebSocket API

### Connection
```
ws://localhost:7243/ws
```

### Authentication
Send authentication message on connect:
```json
{
  "type": "auth",
  "token": "jwt-token"
}
```

### Real-time File Operations
Subscribe to file operation updates:
```json
{
  "type": "subscribe",
  "channel": "file-operations",
  "path": "C:\\Users\\Admin\\Documents"
}
```

### Message Format
```json
{
  "type": "event",
  "channel": "file-operations",
  "event": "file-created",
  "data": {
    "path": "C:\\Users\\Admin\\Documents\\newfile.txt",
    "size": 1024,
    "timestamp": "2026-01-24T18:45:00Z"
  }
}
```

### Available Channels
- `file-operations`: Real-time file changes
- `search-progress`: Search operation progress
- `sync-status`: Synchronization updates
- `space-analysis`: Analysis progress

---

## 🤖 MCP Server API

### Available Tools

#### nexus_search
Search files with advanced filters.

**Parameters:**
```json
{
  "query": "string",
  "path": "string (optional)",
  "filters": {
    "fileType": "string",
    "size": "string",
    "date": "string",
    "content": "boolean"
  },
  "maxResults": "number (default: 100)"
}
```

**Returns:**
```json
{
  "results": [
    {
      "name": "file.txt",
      "path": "C:\\path\\to\\file.txt",
      "size": 1024,
      "modified": "2026-01-24T18:45:00Z",
      "relevance": 0.95
    }
  ],
  "total": 42
}
```

#### nexus_organize
Get AI-driven organization advice.

**Parameters:**
```json
{
  "path": "string",
  "strategy": "type|date|project|size|custom",
  "customRules": ["string"] // optional
}
```

**Returns:**
```json
{
  "suggestions": [
    {
      "action": "move",
      "source": "C:\\Downloads\\file.pdf",
      "destination": "C:\\Documents\\PDFs\\file.pdf",
      "reason": "PDF files should be in Documents/PDFs"
    }
  ],
  "confidence": 0.87
}
```

#### nexus_space
Perform disk space analysis.

**Parameters:**
```json
{
  "path": "string (optional, default: all drives)",
  "analysis": "overview|detailed|largest|duplicates"
}
```

**Returns:**
```json
{
  "summary": {
    "totalSpace": 107374182400,
    "usedSpace": 53687091200,
    "freeSpace": 53687091200,
    "usagePercentage": 50.0
  },
  "breakdown": [
    {
      "category": "Documents",
      "size": 10737418240,
      "percentage": 10.0
    }
  ]
}
```

#### nexus_index
Build or update file indices.

**Parameters:**
```json
{
  "path": "string (optional)",
  "force": "boolean (default: false)",
  "contentIndexing": "boolean (default: true)"
}
```

**Returns:**
```json
{
  "indexId": "idx-123456",
  "status": "building",
  "estimatedFiles": 50000,
  "progress": 0
}
```

#### nexus_rollback
Undo recent file operations.

**Parameters:**
```json
{
  "operationId": "string",
  "confirm": "boolean"
}
```

**Returns:**
```json
{
  "success": true,
  "rollbackId": "rb-789012",
  "filesRestored": 15,
  "filesDeleted": 5
}
```

---

## 📈 Metrics API

### Get Performance Metrics
```http
GET /api/v1/metrics/performance
```

**Response:**
```json
{
  "success": true,
  "data": {
    "operations": {
      "copy": {
        "count": 1250,
        "averageTime": "2.5s",
        "successRate": 99.2
      },
      "search": {
        "count": 3400,
        "averageTime": "0.8s",
        "successRate": 100.0
      }
    },
    "system": {
      "cpuUsage": 15.5,
      "memoryUsage": 512,
      "diskIO": {
        "readSpeed": 250000000,
        "writeSpeed": 180000000
      }
    }
  }
}
```

### Get Usage Statistics
```http
GET /api/v1/metrics/usage?period={period}
```

---

## 🔧 Configuration API

### Get Settings
```http
GET /api/v1/settings
```

### Update Settings
```http
PUT /api/v1/settings
```

**Request Body:**
```json
{
  "general": {
    "defaultLocation": "C:\\Users\\Admin\\Documents",
    "showHiddenFiles": false,
    "theme": "dark"
  },
  "fileOperations": {
    "confirmDeletions": true,
    "preserveTimestamps": true,
    "maxConcurrentOperations": 3
  },
  "search": {
    "includeContent": true,
    "maxResults": 1000,
    "indexingEnabled": true
  }
}
```

---

## 🚨 Error Codes

| Code | Description             | HTTP Status |
| ---- | ----------------------- | ----------- |
| 1001 | Invalid path            | 400         |
| 1002 | File not found          | 404         |
| 1003 | Access denied           | 403         |
| 1004 | Insufficient disk space | 507         |
| 1005 | File in use             | 409         |
| 1006 | Invalid archive format  | 400         |
| 1007 | Sync profile not found  | 404         |
| 1008 | Operation in progress   | 409         |
| 1009 | Invalid search query    | 400         |
| 1010 | Index not built         | 425         |

---

## 📝 Rate Limits

| Endpoint           | Limit         | Window   |
| ------------------ | ------------- | -------- |
| Search             | 100 requests  | 1 minute |
| File Operations    | 50 requests   | 1 minute |
| Archive Operations | 20 requests   | 1 minute |
| All Others         | 1000 requests | 1 hour   |

---

## 🔐 Security

### API Keys
Generate API keys in the settings UI or via API:
```http
POST /api/v1/auth/apikey
```

### Permissions
- `read`: Read file system access
- `write`: Modify file system
- `delete`: Delete files/folders
- `admin`: Full access

### CORS
Allowed origins for API access:
- `https://localhost:7243`
- `https://winhance-fs.local`
- Custom domains configured in settings

---

## 📚 SDK Examples

### C# SDK
```csharp
var client = new WinhanceClient("https://localhost:7243", apiKey);

// Search files
var results = await client.SearchAsync("*.pdf", new SearchOptions
{
    Path = "C:\\Documents",
    IncludeContent = true
});

// Copy file
var operation = await client.CopyAsync("source.txt", "backup.txt");

// Monitor progress
operation.ProgressChanged += (s, e) => 
    Console.WriteLine($"Progress: {e.Percentage}%");
```

### Python SDK
```python
from winhance_sdk import WinhanceClient

client = WinhanceClient(api_key="your-key")

# Search files
results = client.search("*.pdf", path="C:/Documents")

# Organize files
suggestions = client.organize("C:/Downloads", strategy="type")

# Apply suggestions
for suggestion in suggestions:
    client.move(suggestion['source'], suggestion['destination'])
```

### JavaScript SDK
```javascript
import { WinhanceClient } from 'winhance-sdk';

const client = new WinhanceClient({ apiKey: 'your-key' });

// Search files
const results = await client.search('*.pdf', {
  path: 'C:\\Documents',
  includeContent: true
});

// WebSocket connection
const ws = client.connectWebSocket();
ws.subscribe('file-operations', (event) => {
  console.log('File changed:', event.data);
});
```

---

## 📄 OpenAPI Specification

The complete OpenAPI 3.0 specification is available at:
```
https://localhost:7243/api/v1/openapi.json
```

Interactive API documentation (Swagger UI):
```
https://localhost:7243/swagger
```

---

*This API reference is continuously updated. For the latest changes, check the version history in the repository.*
