using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.Common.Interfaces;

namespace Winhance.Infrastructure.Features.Common.Services
{
    /// <summary>
    /// Configuration management service providing persistent settings storage
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly string _configFilePath;
        private readonly Dictionary<string, object> _settings;
        private readonly SemaphoreSlim _fileLock;

        public ConfigurationService()
        {
            var configDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Winhance", "Config");
            
            Directory.CreateDirectory(configDir);
            _configFilePath = Path.Combine(configDir, "settings.json");
            
            _settings = new Dictionary<string, object>();
            _fileLock = new SemaphoreSlim(1, 1);
            
            _ = LoadSettingsAsync();
        }

        public T GetSetting<T>(string key, T defaultValue = default!)
        {
            if (_settings.TryGetValue(key, out var value))
            {
                try
                {
                    if (value is JsonElement jsonElement)
                    {
                        return jsonElement.Deserialize<T>() ?? defaultValue;
                    }
                    return (T)Convert.ChangeType(value, typeof(T)) ?? defaultValue;
                }
                catch
                {
                    return defaultValue;
                }
            }
            
            return defaultValue;
        }

        public async Task SetSettingAsync<T>(string key, T value)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            
            _settings[key] = value!;
            await SaveSettingsAsync();
        }

        public Task<bool> HasSettingAsync(string key)
        {
            return Task.FromResult(_settings.ContainsKey(key));
        }

        public async Task RemoveSettingAsync(string key)
        {
            if (_settings.Remove(key))
            {
                await SaveSettingsAsync();
            }
        }

        public async Task ClearAllSettingsAsync()
        {
            _settings.Clear();
            await SaveSettingsAsync();
        }

        private async Task LoadSettingsAsync()
        {
            await _fileLock.WaitAsync();
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = await File.ReadAllTextAsync(_configFilePath);
                    var loadedSettings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    
                    if (loadedSettings != null)
                    {
                        _settings.Clear();
                        foreach (var kvp in loadedSettings)
                        {
                            _settings[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // If loading fails, start with empty settings
                _settings.Clear();
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private async Task SaveSettingsAsync()
        {
            await _fileLock.WaitAsync();
            try
            {
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                await File.WriteAllTextAsync(_configFilePath, json);
            }
            catch (Exception)
            {
                // Ignore save errors - settings will remain in memory
            }
            finally
            {
                _fileLock.Release();
            }
        }

        // IConfigurationService interface methods
        public async Task ExportConfigurationAsync()
        {
            // Persist all current in-memory settings to the configuration file
            await SaveSettingsAsync();
        }

        public async Task ImportConfigurationAsync()
        {
            // Reload settings from the configuration file into memory
            await LoadSettingsAsync();
        }

        public async Task ImportRecommendedConfigurationAsync()
        {
            // Reset all settings to recommended defaults, then persist
            _settings.Clear();
            _settings["Theme"] = "Dark";
            _settings["ShowHiddenFiles"] = false;
            _settings["ShowSystemFiles"] = false;
            _settings["ConfirmDelete"] = true;
            _settings["DefaultViewMode"] = "Details";
            await SaveSettingsAsync();
        }

        public void Dispose()
        {
            _fileLock?.Dispose();
        }
    }
}
