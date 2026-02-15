using RAGChatbot.Core.Models;

namespace RAGChatbot.Core.Interfaces;

public interface IVectorDatabase
{
    Task StoreEmbeddingAsync(string documentId, string chunkId, ReadOnlyMemory<float> embedding, Dictionary<string, object> metadata);
    Task<IEnumerable<SimilarityResult>> FindSimilarAsync(ReadOnlyMemory<float> queryEmbedding, int topK = 5);
    Task DeleteDocumentAsync(string documentId);
}