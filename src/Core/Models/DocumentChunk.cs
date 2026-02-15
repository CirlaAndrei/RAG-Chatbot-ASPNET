namespace RAGChatbot.Core.Models;

public class DocumentChunk
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DocumentId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}