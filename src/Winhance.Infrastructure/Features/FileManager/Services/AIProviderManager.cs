using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Core.Features.FileManager.Models;

namespace Winhance.Infrastructure.Features.FileManager.Services
{
    /// <summary>
    /// Manages AI provider selection, configuration, model discovery and connectivity checks.
    /// Supports MiniMax, DeepSeek (cloud) and LM Studio, Ollama (local/OpenAI-compatible).
    /// API keys are never hard-coded; they are read from environment variables or caller-supplied config.
    /// </summary>
    public sealed class AIProviderManager : IAIProviderManager, IDisposable
    {
        // Single static HttpClient to avoid socket exhaustion
        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        private AIProviderType _activeProviderType = AIProviderType.Ollama;
        private string _apiKey = string.Empty;
        private string _baseUrl = string.Empty;
        private string _selectedModel = string.Empty;

        // -------------------------------------------------------------------------
        // IAIProviderManager implementation
        // -------------------------------------------------------------------------

        /// <inheritdoc/>
        public IAIProviderService? ActiveProvider { get; private set; }

        /// <inheritdoc/>
        public AIProviderType ActiveProviderType
        {
            get => _activeProviderType;
            set
            {
                if (_activeProviderType == value) return;
                _activeProviderType = value;
                // Reset model selection when provider changes
                _selectedModel = string.Empty;
                RaiseProviderChanged();
            }
        }

        /// <inheritdoc/>
        public string ApiKey
        {
            get => _apiKey;
            set
            {
                if (_apiKey == value) return;
                _apiKey = value;
                RaiseProviderChanged();
            }
        }

        /// <inheritdoc/>
        public string BaseUrl
        {
            get => _baseUrl;
            set
            {
                if (_baseUrl == value) return;
                _baseUrl = value;
                RaiseProviderChanged();
            }
        }

        /// <inheritdoc/>
        public string SelectedModel
        {
            get => _selectedModel;
            set
            {
                if (_selectedModel == value) return;
                _selectedModel = value;
                RaiseProviderChanged();
            }
        }

        /// <inheritdoc/>
        public event EventHandler? ProviderChanged;

        // -------------------------------------------------------------------------
        // Model discovery
        // -------------------------------------------------------------------------

        /// <inheritdoc/>
        public async Task<IReadOnlyList<string>> GetAvailableModelsAsync(CancellationToken ct = default)
        {
            try
            {
                return _activeProviderType switch
                {
                    AIProviderType.LmStudio => await FetchOpenAiCompatibleModelsAsync(GetEffectiveBaseUrl(AIProviderType.LmStudio), ct),
                    AIProviderType.Ollama   => await FetchOllamaModelsAsync(GetEffectiveBaseUrl(AIProviderType.Ollama), ct),
                    AIProviderType.MiniMax  => GetStaticModelList(AIProviderType.MiniMax),
                    AIProviderType.DeepSeek => GetStaticModelList(AIProviderType.DeepSeek),
                    _                       => Array.Empty<string>()
                };
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        // -------------------------------------------------------------------------
        // Connectivity test
        // -------------------------------------------------------------------------

        /// <inheritdoc/>
        public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
        {
            try
            {
                return _activeProviderType switch
                {
                    AIProviderType.LmStudio => await PingOpenAiCompatibleAsync(GetEffectiveBaseUrl(AIProviderType.LmStudio), ct),
                    AIProviderType.Ollama   => await PingOllamaAsync(GetEffectiveBaseUrl(AIProviderType.Ollama), ct),
                    AIProviderType.MiniMax  => await PingCloudAsync("https://api.minimax.chat/v1/models", GetEffectiveApiKey(AIProviderType.MiniMax), ct),
                    AIProviderType.DeepSeek => await PingCloudAsync("https://api.deepseek.com/v1/models", GetEffectiveApiKey(AIProviderType.DeepSeek), ct),
                    _                       => false
                };
            }
            catch
            {
                return false;
            }
        }

        // -------------------------------------------------------------------------
        // Private helpers — model lists
        // -------------------------------------------------------------------------

        private async Task<IReadOnlyList<string>> FetchOpenAiCompatibleModelsAsync(string baseUrl, CancellationToken ct)
        {
            var url = baseUrl.TrimEnd('/') + "/v1/models";
            using var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return Array.Empty<string>();

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("data", out var data))
                return Array.Empty<string>();

            var models = new List<string>();
            foreach (var item in data.EnumerateArray())
            {
                if (item.TryGetProperty("id", out var id))
                    models.Add(id.GetString() ?? string.Empty);
            }
            return models.Where(m => !string.IsNullOrEmpty(m)).ToList();
        }

        private async Task<IReadOnlyList<string>> FetchOllamaModelsAsync(string baseUrl, CancellationToken ct)
        {
            var url = baseUrl.TrimEnd('/') + "/api/tags";
            using var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return Array.Empty<string>();

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("models", out var models))
                return Array.Empty<string>();

            var result = new List<string>();
            foreach (var item in models.EnumerateArray())
            {
                if (item.TryGetProperty("name", out var name))
                    result.Add(name.GetString() ?? string.Empty);
            }
            return result.Where(m => !string.IsNullOrEmpty(m)).ToList();
        }

        private static IReadOnlyList<string> GetStaticModelList(AIProviderType provider)
        {
            // Return well-known default models; user can always type a custom value
            return provider switch
            {
                AIProviderType.MiniMax  => new[] { "MiniMax-Text-01", "abab6.5s-chat", "abab5.5-chat" },
                AIProviderType.DeepSeek => new[] { "deepseek-chat", "deepseek-coder", "deepseek-reasoner" },
                _                       => Array.Empty<string>()
            };
        }

        // -------------------------------------------------------------------------
        // Private helpers — connectivity
        // -------------------------------------------------------------------------

        private async Task<bool> PingOpenAiCompatibleAsync(string baseUrl, CancellationToken ct)
        {
            var url = baseUrl.TrimEnd('/') + "/v1/models";
            using var response = await _http.GetAsync(url, ct);
            return response.IsSuccessStatusCode;
        }

        private async Task<bool> PingOllamaAsync(string baseUrl, CancellationToken ct)
        {
            var url = baseUrl.TrimEnd('/') + "/api/tags";
            using var response = await _http.GetAsync(url, ct);
            return response.IsSuccessStatusCode;
        }

        private async Task<bool> PingCloudAsync(string url, string apiKey, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return false;

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            using var response = await _http.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }

        // -------------------------------------------------------------------------
        // Private helpers — config resolution
        // -------------------------------------------------------------------------

        private string GetEffectiveBaseUrl(AIProviderType provider)
        {
            if (!string.IsNullOrWhiteSpace(_baseUrl)) return _baseUrl;

            // Read from environment variables, then fall back to localhost defaults
            return provider switch
            {
                AIProviderType.LmStudio => Environment.GetEnvironmentVariable("LMSTUDIO_BASE_URL") ?? "http://localhost:1234",
                AIProviderType.Ollama   => Environment.GetEnvironmentVariable("OLLAMA_BASE_URL")   ?? "http://localhost:11434",
                _                       => string.Empty
            };
        }

        private string GetEffectiveApiKey(AIProviderType provider)
        {
            if (!string.IsNullOrWhiteSpace(_apiKey)) return _apiKey;

            return provider switch
            {
                AIProviderType.MiniMax  => Environment.GetEnvironmentVariable("MINIMAX_API_KEY")  ?? string.Empty,
                AIProviderType.DeepSeek => Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY") ?? string.Empty,
                _                       => string.Empty
            };
        }

        // -------------------------------------------------------------------------
        // Event helper
        // -------------------------------------------------------------------------

        private void RaiseProviderChanged() => ProviderChanged?.Invoke(this, EventArgs.Empty);

        // -------------------------------------------------------------------------
        // IDisposable
        // -------------------------------------------------------------------------

        public void Dispose()
        {
            // _http is static/shared — intentionally not disposed here
        }
    }
}
