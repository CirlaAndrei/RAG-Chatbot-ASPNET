using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RAGChatbot.Core.Interfaces;
using RAGChatbot.Core.Models;

namespace RAGChatbot.Infrastructure.VectorDb;

public class InMemoryVectorDatabase : IVectorDatabase
{
    private readonly ConcurrentDictionary<string, VectorDocument> _documents = new();
    private readonly ILogger<InMemoryVectorDatabase> _logger;

    public InMemoryVectorDatabase(ILogger<InMemoryVectorDatabase> logger)
    {
        _logger = logger;
    }

    public Task StoreEmbeddingAsync(
        string documentId, 
        string chunkId, 
        ReadOnlyMemory<float> embedding, 
        Dictionary<string, object> metadata)
    {
        var document = new VectorDocument
        {
            Id = chunkId,
            DocumentId = documentId,
            Content = metadata.TryGetValue("content", out var content) ? content?.ToString() ?? "" : "",
            Embedding = embedding.ToArray(),
            Metadata = metadata
        };

        _documents[chunkId] = document;
        _logger.LogDebug("Stored embedding for chunk {ChunkId} from document {DocumentId}", chunkId, documentId);
        
        return Task.CompletedTask;
    }

    public Task<IEnumerable<SimilarityResult>> FindSimilarAsync(
        ReadOnlyMemory<float> queryEmbedding, 
        int topK = 5)
    {
        var results = new List<(VectorDocument doc, double score)>();

        foreach (var doc in _documents.Values)
        {
            var score = CosineSimilarity(queryEmbedding.Span, doc.Embedding.AsSpan());
            results.Add((doc, score));
        }

        var topResults = results
            .OrderByDescending(r => r.score)
            .Take(topK)
            .Select(r => r.doc.ToSimilarityResult(r.score))
            .ToList();

        _logger.LogDebug("Found {Count} similar documents", topResults.Count);
        
        return Task.FromResult<IEnumerable<SimilarityResult>>(topResults);
    }

    public Task DeleteDocumentAsync(string documentId)
    {
        var keysToRemove = _documents
            .Where(kvp => kvp.Value.DocumentId == documentId)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _documents.TryRemove(key, out _);
        }

        _logger.LogDebug("Deleted {Count} chunks for document {DocumentId}", keysToRemove.Count, documentId);
        
        return Task.CompletedTask;
    }

    private static double CosineSimilarity(ReadOnlySpan<float> v1, ReadOnlySpan<float> v2)
    {
        if (v1.Length != v2.Length)
            throw new ArgumentException("Vectors must have the same length");

        double dotProduct = 0;
        double mag1 = 0;
        double mag2 = 0;

        for (int i = 0; i < v1.Length; i++)
        {
            dotProduct += v1[i] * v2[i];
            mag1 += v1[i] * v1[i];
            mag2 += v2[i] * v2[i];
        }

        if (mag1 == 0 || mag2 == 0)
            return 0;

        return dotProduct / (Math.Sqrt(mag1) * Math.Sqrt(mag2));
    }
}