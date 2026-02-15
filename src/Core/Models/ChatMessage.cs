namespace RAGChatbot.Core.Models;

public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
    public List<SimilarityResult> Sources { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}