# Winhance-FS Configuration Documentation

> **Purpose:** Complete guide to configuring Winhance-FS components and settings  
> **Last Updated:** 2026-01-26  
> **Version:** 1.0

---

## � Navigation

**Related Documents:**
- [Architecture Documentation](ARCHITECTURE.md)
- [Performance Guide](PERFORMANCE.md)
- [Theming Guide](THEMING.md)
- [Security Policy](SECURITY.md)
- [WPF Development Guide](docs/development/wpf.md)

---

## �📋 Overview

Winhance-FS uses a flexible configuration system that allows customization of all aspects of the application, from UI appearance to performance settings. Configuration is stored in JSON format with support for environment-specific overrides.

---

## 🗂️ Configuration Locations

### Windows Configuration Paths

```
%APPDATA%\Winhance-FS\           # User settings
├── config.json                 # Main configuration
├── themes\                      # Custom themes
├── profiles\                    # User profiles
└── plugins\                     # Plugin configs

%LOCALAPPDATA%\Winhance-FS\      # Local data
├── cache\                       # Temporary cache
├── logs\                        # Application logs
├── indexes\                     # Search indexes
└── temp\                        # Temporary files

%PROGRAMDATA%\Winhance-FS\       # System-wide settings
├── default.json                 # Default configuration
├── policies.json                # Enterprise policies
└── licenses\                    # License files
```

### Project Configuration Paths

```
Winhance-FS-Repo\
├── config\                      # Default configs
│   ├── default.json             # Base configuration
│   ├── development.json         # Dev overrides
│   ├── production.json          # Prod overrides
│   └── testing.json             # Test overrides
├── src\nexus_ai\config\         # Python AI config
│   ├── ai_models.json           # AI model settings
│   └── mcp_servers.json         # MCP server config
└── src\nexus_core\config\       # Rust core config
    └── engine.toml               # Rust engine settings
```

---

## ⚙️ Main Configuration

### config.json Structure

```json
{
  "version": "1.0.0",
  "lastModified": "2026-01-26T08:00:00Z",
  "user": {
    "id": "user-uuid",
    "name": "User Name",
    "email": "user@example.com"
  },
  "application": {
    "theme": "Borg Original",
    "language": "en-US",
    "autoStart": false,
    "minimizeToTray": true,
    "checkUpdates": true,
    "telemetry": {
      "enabled": false,
      "level": "basic"
    }
  },
  "performance": {
    "maxMemoryUsage": 2147483648,
    "enableSIMD": true,
    "parallelOperations": 4,
    "cacheSize": 1073741824,
    "indexUpdateInterval": 300
  },
  "ui": {
    "window": {
      "rememberPosition": true,
      "startMaximized": false,
      "alwaysOnTop": false
    },
    "fileManager": {
      "showHiddenFiles": false,
      "showSystemFiles": false,
      "doubleClickAction": "open",
      "confirmDeletions": true,
      "smoothScrolling": true
    },
    "search": {
      "instantSearch": true,
      "searchAsType": true,
      "includeContent": true,
      "maxResults": 1000
    }
  },
  "security": {
    "requireAdmin": false,
    "encryptIndex": false,
    "auditLog": false,
    "allowedPaths": []
  },
  "ai": {
    "provider": "openai",
    "model": "gpt-4-turbo",
    "apiKey": "",
    "maxTokens": 4096,
    "temperature": 0.7,
    "enableClassification": true,
    "enableAnalysis": true
  },
  "mcp": {
    "enabled": true,
    "port": 8080,
    "host": "localhost",
    "authToken": "",
    "allowedTools": ["*"]
  },
  "storage": {
    "defaultPaths": {
      "downloads": "%USERPROFILE%\\Downloads",
      "documents": "%USERPROFILE%\\Documents",
      "desktop": "%USERPROFILE%\\Desktop"
    },
    "excludePatterns": [
      "*.tmp",
      "*.cache",
      "node_modules/*",
      ".git/*"
    ]
  }
}
```

---

## 🎨 Theme Configuration

### Theme Settings

```json
{
  "theme": {
    "name": "Borg Original",
    "custom": {
      "primary": "#00FF41",
      "secondary": "#00CC33",
      "background": "#0A0A0A",
      "surface": "#1A1A1A",
      "text": "#00FF41",
      "textSecondary": "#00CC33",
      "accent": "#00FF88",
      "error": "#FF0040",
      "warning": "#FFAA00",
      "success": "#00FF88"
    },
    "animations": {
      "enabled": true,
      "duration": 300,
      "easing": "cubic-bezier"
    },
    "fonts": {
      "primary": "Consolas",
      "secondary": "Segoe UI",
      "monospace": "Cascadia Code",
      "sizes": {
        "small": 12,
        "medium": 14,
        "large": 16,
        "xlarge": 18
      }
    }
  }
}
```

---

## ⚡ Performance Configuration

### Engine Settings (engine.toml)

```toml
[engine]
# MFT Reader Configuration
[mft]
enable_caching = true
cache_size = "256MB"
parallel_records = 1000
use_simd = true

# Search Engine Configuration
[search]
index_path = "%LOCALAPPDATA%/Winhance-FS/indexes"
max_results = 10000
enable_fuzzy = true
fuzzy_threshold = 0.8

# Content Hashing
[hashing]
algorithm = "xxhash64"
parallel_files = 8
chunk_size = "64KB"

# USN Journal Monitoring
[usn]
enabled = true
buffer_size = "1MB"
batch_size = 1000
```

### Performance Tiers

```json
{
  "performanceProfiles": {
    "minimal": {
      "maxMemoryUsage": 536870912,
      "parallelOperations": 2,
      "cacheSize": 268435456,
      "enableSIMD": false
    },
    "balanced": {
      "maxMemoryUsage": 2147483648,
      "parallelOperations": 4,
      "cacheSize": 1073741824,
      "enableSIMD": true
    },
    "maximum": {
      "maxMemoryUsage": 8589934592,
      "parallelOperations": 8,
      "cacheSize": 4294967296,
      "enableSIMD": true
    }
  }
}
```

---

## 🤖 AI Configuration

### AI Models (ai_models.json)

```json
{
  "providers": {
    "openai": {
      "apiBase": "https://api.openai.com/v1",
      "models": {
        "gpt-4-turbo": {
          "maxTokens": 128000,
          "costPerToken": 0.00001
        },
        "gpt-3.5-turbo": {
          "maxTokens": 16384,
          "costPerToken": 0.000001
        }
      }
    },
    "anthropic": {
      "apiBase": "https://api.anthropic.com",
      "models": {
        "claude-3-opus": {
          "maxTokens": 200000,
          "costPerToken": 0.000015
        }
      }
    },
    "local": {
      "apiBase": "http://localhost:8081",
      "models": {
        "llama-2-7b": {
          "maxTokens": 4096,
          "costPerToken": 0
        }
      }
    }
  },
  "classification": {
    "enabled": true,
    "model": "gpt-3.5-turbo",
    "categories": [
      "Documents",
      "Images",
      "Videos",
      "Audio",
      "Archives",
      "Code",
      "Datasets",
      "Models"
    ]
  },
  "analysis": {
    "enabled": true,
    "model": "gpt-4-turbo",
    "maxFileSize": "10MB",
    "supportedFormats": [
      ".txt", ".md", ".py", ".js", ".json",
      ".pdf", ".docx", ".xlsx"
    ]
  }
}
```

---

## 🔌 MCP Configuration

### MCP Servers (mcp_servers.json)

```json
{
  "servers": {
    "winhance-fs": {
      "command": "python",
      "args": ["-m", "nexus_mcp"],
      "env": {
        "PYTHONPATH": "%PROJECT_ROOT%/src",
        "LOG_LEVEL": "INFO"
      },
      "tools": [
        "search_files",
        "organize_files",
        "analyze_storage",
        "classify_files",
        "get_file_info",
        "create_symlink",
        "move_files",
        "duplicate_finder"
      ]
    }
  },
  "clients": {
    "claude": {
      "timeout": 30000,
      "maxRetries": 3
    },
    "windsurf": {
      "timeout": 60000,
      "maxRetries": 5
    }
  }
}
```

---

## 🔒 Security Configuration

### Security Settings

```json
{
  "security": {
    "authentication": {
      "enabled": false,
      "method": "windows",
      "requirePassword": false,
      "sessionTimeout": 3600
    },
    "authorization": {
      "roles": ["user", "admin"],
      "permissions": {
        "user": ["read", "write", "delete"],
        "admin": ["*"]
      }
    },
    "encryption": {
      "algorithm": "AES-256-GCM",
      "keyDerivation": "PBKDF2",
      "iterations": 100000
    },
    "audit": {
      "enabled": false,
      "logLevel": "info",
      "retention": 30
    },
    "sandbox": {
      "enabled": false,
      "restrictedPaths": [
        "C:\\Windows\\System32",
        "C:\\Program Files"
      ]
    }
  }
}
```

---

## 📁 Storage Configuration

### Path Configuration

```json
{
  "storage": {
    "paths": {
      "user": {
        "home": "%USERPROFILE%",
        "documents": "%USERPROFILE%\\Documents",
        "downloads": "%USERPROFILE%\\Downloads",
        "desktop": "%USERPROFILE%\\Desktop",
        "pictures": "%USERPROFILE%\\Pictures",
        "videos": "%USERPROFILE%\\Videos",
        "music": "%USERPROFILE%\\Music"
      },
      "system": {
        "temp": "%TEMP%",
        "programData": "%PROGRAMDATA%\\Winhance-FS",
        "logs": "%LOCALAPPDATA%\\Winhance-FS\\Logs",
        "cache": "%LOCALAPPDATA%\\Winhance-FS\\Cache"
      },
      "custom": {}
    },
    "monitoring": {
      "enabled": true,
      "paths": ["%USERPROFILE%"],
      "excludeHidden": true,
      "excludeSystem": true,
      "realTime": true
    }
  }
}
```

---

## 🔧 Environment-Specific Configs

### Development (development.json)

```json
{
  "application": {
    "logLevel": "debug",
    "showDebugConsole": true,
    "hotReload": true
  },
  "performance": {
    "maxMemoryUsage": 4294967296,
    "enableProfiling": true
  },
  "ai": {
    "provider": "local",
    "model": "llama-2-7b"
  },
  "features": {
    "experimental": true,
    "debugTools": true,
    "mockData": true
  }
}
```

### Production (production.json)

```json
{
  "application": {
    "logLevel": "info",
    "telemetry": {
      "enabled": true,
      "level": "basic"
    }
  },
  "security": {
    "requireAdmin": true,
    "auditLog": true
  },
  "performance": {
    "optimizeFor": "speed"
  },
  "features": {
    "experimental": false,
    "debugTools": false
  }
}
```

---

## 📝 Configuration API

### Programmatic Configuration

```csharp
// Configuration Manager
public class ConfigurationManager
{
    private readonly IConfiguration _config;
    private readonly IOptionsMonitor<AppSettings> _options;

    public async Task UpdateSettingAsync<T>(string key, T value)
    {
        // Update in-memory configuration
        _config[key] = value?.ToString();
        
        // Persist to file
        await SaveConfigurationAsync();
        
        // Notify subscribers
        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(key, value));
    }

    public T GetValue<T>(string key, T defaultValue = default)
    {
        return _config.GetValue(key, defaultValue);
    }

    public async Task ResetToDefaultsAsync()
    {
        var defaults = LoadDefaultConfiguration();
        await ApplyConfigurationAsync(defaults);
    }
}
```

### Configuration Validation

```csharp
public class ConfigurationValidator
{
    public ValidationResult Validate(Configuration config)
    {
        var result = new ValidationResult();
        
        // Validate performance settings
        if (config.Performance.MaxMemoryUsage < 536870912)
        {
            result.AddError("Max memory usage must be at least 512MB");
        }
        
        // Validate paths
        foreach (var path in config.Storage.Paths.Custom.Values)
        {
            if (!Directory.Exists(path))
            {
                result.AddWarning($"Custom path does not exist: {path}");
            }
        }
        
        // Validate AI settings
        if (config.AI.Enabled && string.IsNullOrEmpty(config.AI.ApiKey))
        {
            result.AddError("API key is required when AI is enabled");
        }
        
        return result;
    }
}
```

---

## 🔄 Configuration Migration

### Version Migration

```csharp
public class ConfigurationMigrator
{
    public async Task MigrateAsync(string fromVersion, string toVersion)
    {
        var config = await LoadConfigurationAsync();
        
        // Migrate from 0.9 to 1.0
        if (fromVersion == "0.9" && toVersion == "1.0")
        {
            // Add new AI section
            config.AI = new AIConfiguration
            {
                Provider = "openai",
                Model = "gpt-3.5-turbo"
            };
            
            // Remove deprecated settings
            config.LegacySettings = null;
        }
        
        await SaveConfigurationAsync(config);
    }
}
```

---

## 🎛️ Configuration UI

### Settings Window Structure

```xml
<!-- SettingsWindow.xaml -->
<TabControl>
    <!-- General Settings -->
    <TabItem Header="General">
        <StackPanel>
            <CheckBox Content="Start with Windows" 
                      IsChecked="{Binding AutoStart}"/>
            <CheckBox Content="Minimize to tray" 
                      IsChecked="{Binding MinimizeToTray}"/>
            <ComboBox SelectedItem="{Binding Language}">
                <ComboBoxItem>English</ComboBoxItem>
                <ComboBoxItem>中文</ComboBoxItem>
                <ComboBoxItem>日本語</ComboBoxItem>
            </ComboBox>
        </StackPanel>
    </TabItem>
    
    <!-- Performance Settings -->
    <TabItem Header="Performance">
        <StackPanel>
            <Label Content="Performance Profile"/>
            <ComboBox SelectedItem="{Binding PerformanceProfile}">
                <ComboBoxItem>Minimal</ComboBoxItem>
                <ComboBoxItem>Balanced</ComboBoxItem>
                <ComboBoxItem>Maximum</ComboBoxItem>
            </ComboBox>
            
            <Slider Minimum="512" Maximum="8192" 
                    Value="{Binding MaxMemoryMB}"
                    TickFrequency="512"/>
            <TextBlock Text="{Binding MaxMemoryMB, StringFormat='Memory: {0} MB'}"/>
        </StackPanel>
    </TabItem>
    
    <!-- AI Settings -->
    <TabItem Header="AI & ML">
        <StackPanel>
            <CheckBox Content="Enable AI features" 
                      IsChecked="{Binding AI.Enabled}"/>
            <ComboBox SelectedItem="{Binding AI.Provider}"
                      IsEnabled="{Binding AI.Enabled}">
                <ComboBoxItem>OpenAI</ComboBoxItem>
                <ComboBoxItem>Anthropic</ComboBoxItem>
                <ComboBoxItem>Local</ComboBoxItem>
            </ComboBox>
            
            <TextBox Text="{Binding AI.ApiKey}"
                     materialDesign:HintAssist.Hint="API Key"
                     IsEnabled="{Binding AI.Enabled}"/>
        </StackPanel>
    </TabItem>
</TabControl>
```

---

## 📊 Configuration Templates

### Power User Template

```json
{
  "name": "Power User",
  "description": "Optimized for advanced users",
  "config": {
    "performance": {
      "profile": "maximum",
      "enableSIMD": true,
      "parallelOperations": 8
    },
    "ui": {
      "showHiddenFiles": true,
      "showSystemFiles": true,
      "confirmDeletions": false
    },
    "features": {
      "experimental": true,
      "debugTools": true
    }
  }
}
```

### Minimal Resource Template

```json
{
  "name": "Minimal",
  "description": "Low resource usage configuration",
  "config": {
    "performance": {
      "profile": "minimal",
      "enableSIMD": false,
      "parallelOperations": 2
    },
    "ui": {
      "animations": false,
      "smoothScrolling": false
    },
    "features": {
      "ai": false,
      "realTimeMonitoring": false
    }
  }
}
```

---

## 🔗 Navigation

**Related Documents:**
- [Architecture Documentation](ARCHITECTURE.md)
- [Performance Guide](PERFORMANCE.md)
- [Theming Guide](THEMING.md)
- [Security Policy](SECURITY.md)

---

## 📚 References

### Configuration Standards
- [JSON Configuration](https://json.org/)
- [TOML Configuration](https://toml.io/)
- [.NET Configuration](https://docs.microsoft.com/en-us/dotnet/core/extensions/configuration)

### Best Practices
- [12-Factor App - Config](https://12factor.net/config)
- [Configuration Management](https://docs.microsoft.com/en-us/azure/azure-app-configuration/)

---

*Configure Winhance-FS your way! The flexible configuration system adapts to your needs.* ⚙️
