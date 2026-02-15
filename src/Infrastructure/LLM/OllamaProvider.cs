using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RAGChatbot.Core.Interfaces;
using RAGChatbot.Infrastructure.Configuration;

namespace RAGChatbot.Infrastructure.LLM;

public class OllamaProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly LlmSettings _settings;
    private readonly ILogger<OllamaProvider> _logger;

    public string ProviderName => "Ollama";

    public OllamaProvider(
        HttpClient httpClient,
        IOptions<LlmSettings> settings,
        ILogger<OllamaProvider> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri("http://localhost:11434/api/");
    }

    public async Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new
            {
                model = _settings.Model ?? "llama2",
                prompt = prompt,
                stream = false
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("generate", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            using var jsonDoc = JsonDocument.Parse(responseBody);
            
            return jsonDoc.RootElement.GetProperty("response").GetString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating completion with Ollama");
            throw;
        }
    }

    public async Task<IReadOnlyList<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new
            {
                model = "nomic-embed-text", // or all-minilm
                prompt = text
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
                .GetProperty("embedding")
                .EnumerateArray()
                .Select(x => (float)x.GetDouble())
                .ToArray();

            return embeddingArray;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding with Ollama");
            throw;
        }
    }
}