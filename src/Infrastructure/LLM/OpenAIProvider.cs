using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RAGChatbot.Core.Interfaces;
using RAGChatbot.Infrastructure.Configuration;

namespace RAGChatbot.Infrastructure.LLM;

public class OpenAIProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly LlmSettings _settings;
    private readonly ILogger<OpenAIProvider> _logger;

    public string ProviderName => "OpenAI";

    public OpenAIProvider(
        HttpClient httpClient,
        IOptions<LlmSettings> settings,
        ILogger<OpenAIProvider> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");
    }

    public async Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new
            {
                model = _settings.Model,
                messages = new[] { new { role = "user", content = prompt } },
                max_tokens = _settings.MaxTokens,
                temperature = _settings.Temperature
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            using var jsonDoc = JsonDocument.Parse(responseBody);
            
            var result = jsonDoc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return result ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating completion");
            throw;
        }
    }

    public async Task<IReadOnlyList<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new
            {
                model = _settings.EmbeddingModel,
                input = text
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("embeddings", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            using var jsonDoc = JsonDocument.Parse(responseBody);
            
            var embeddingArray = jsonDoc.RootElement
                .GetProperty("data")[0]
                .GetProperty("embedding")
                .EnumerateArray()
                .Select(x => (float)x.GetDouble())
                .ToArray();

            return embeddingArray;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding");
            throw;
        }
    }
}