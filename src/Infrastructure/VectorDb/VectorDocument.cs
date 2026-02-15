using RAGChatbot.Core.Models;

namespace RAGChatbot.Infrastructure.VectorDb;

public class VectorDocument
{
    public string Id { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    public SimilarityResult ToSimilarityResult(double score)
    {
        return new SimilarityResult
        {
            DocumentId = DocumentId,
            ChunkId = Id,
            Content = Content,
            Score = score,
            Metadata = Metadata
        };
    }
}