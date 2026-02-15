namespace RAGChatbot.Infrastructure.Configuration;

public class LlmSettings
{
    public string Provider { get; set; } = "OpenAI";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-3.5-turbo";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    public int MaxTokens { get; set; } = 2000;
    public double Temperature { get; set; } = 0.7;
}