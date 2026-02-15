namespace RAGChatbot.Core.Models;

public class RagResponse
{
    public string Answer { get; set; } = string.Empty;
    public List<SimilarityResult> Sources { get; set; } = new();
    public string SessionId { get; set; } = string.Empty;
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public int TokensUsed { get; set; }
}