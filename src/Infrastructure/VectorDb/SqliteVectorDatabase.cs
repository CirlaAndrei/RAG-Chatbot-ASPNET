using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RAGChatbot.Core.Interfaces;
using RAGChatbot.Core.Models;
using RAGChatbot.Infrastructure.Data;
using RAGChatbot.Infrastructure.Data.Models;

namespace RAGChatbot.Infrastructure.VectorDb;

public class SqliteVectorDatabase : IVectorDatabase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<SqliteVectorDatabase> _logger;

    public SqliteVectorDatabase(
        AppDbContext dbContext,
        ILogger<SqliteVectorDatabase> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task StoreEmbeddingAsync(
        string documentId,
        string chunkId,
        ReadOnlyMemory<float> embedding,
        Dictionary<string, object> metadata)
    {
        var entity = new VectorDocumentEntity
        {
            Id = Guid.NewGuid().ToString(),
            DocumentId = documentId,
            ChunkId = chunkId,
            ChunkIndex = metadata.TryGetValue("chunkIndex", out var index) ? Convert.ToInt32(index) : 0,
            Content = metadata.TryGetValue("content", out var content) ? content?.ToString() ?? "" : "",
            Embedding = embedding.ToArray(),
            MetadataJson = JsonSerializer.Serialize(metadata),
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.VectorDocuments.AddAsync(entity);
        
        // Log embedding sample for debugging
        _logger.LogDebug("Storing embedding for chunk {ChunkId}, first 5 values: [{F1:F4}, {F2:F4}, {F3:F4}, {F4:F4}, {F5:F4}]", 
            chunkId, 
            embedding.Span[0], embedding.Span[1], embedding.Span[2], embedding.Span[3], embedding.Span[4]);
        
        // Force save changes with explicit transaction
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            _logger.LogInformation("Successfully committed chunk {ChunkId} to database", chunkId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to commit chunk {ChunkId}", chunkId);
            throw;
        }
    }

    public async Task<IEnumerable<SimilarityResult>> FindSimilarAsync(
        ReadOnlyMemory<float> queryEmbedding,
        int topK = 5)
    {
        _logger.LogInformation("=== SEMANTIC SEARCH STARTING ===");
        
        var allDocs = await _dbContext.VectorDocuments.ToListAsync();
        _logger.LogInformation("Total documents in database: {Count}", allDocs.Count);
        
        var query = queryEmbedding.ToArray();
        _logger.LogDebug("Query embedding first 5 values: [{F1:F4}, {F2:F4}, {F3:F4}, {F4:F4}, {F5:F4}]", 
            query[0], query[1], query[2], query[3], query[4]);
        
        // Calculate scores for ALL documents
        var allScores = new List<(VectorDocumentEntity Document, double Score)>();
        foreach (var doc in allDocs)
        {
            var score = CosineSimilarity(query, doc.Embedding);
            allScores.Add((doc, score));
        }
        
        allScores = allScores.OrderByDescending(x => x.Score).ToList();
        
        // Log ALL scores to see what's happening
        _logger.LogInformation("=== ALL DOCUMENT SCORES (Top 20) ===");
        for (int i = 0; i < Math.Min(20, allScores.Count); i++)
        {
            var item = allScores[i];
            var preview = item.Document.Content.Length > 100 
                ? item.Document.Content.Substring(0, 100) + "..." 
                : item.Document.Content;
            
            // Highlight documents that might be your CV (contain Andrei)
            var isCV = item.Document.Content.Contains("Andrei") || 
                      item.Document.Content.Contains("Cirla") || 
                      item.Document.DocumentId.Length > 10; // CV docs have long GUIDs
            
            var marker = isCV ? "*** CV DOCUMENT ***" : "";
            
            _logger.LogInformation("Score: {Score:F4} {Marker} | DocId: {DocId} | Preview: {Preview}", 
                item.Score, marker, item.Document.DocumentId, preview);
        }
        
        // Take topK for results
        var topResults = allScores
            .Take(topK)
            .Select(x => new SimilarityResult
            {
                DocumentId = x.Document.DocumentId,
                ChunkId = x.Document.ChunkId,
                Content = x.Document.Content,
                Score = x.Score,
                Metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(x.Document.MetadataJson) 
                    ?? new Dictionary<string, object>()
            })
            .ToList();

        _logger.LogInformation("=== SEMANTIC SEARCH COMPLETE: Returning top {Count} results ===", topResults.Count);
        
        // Log the scores that will be returned
        foreach (var r in topResults)
        {
            _logger.LogInformation("Returning - Score: {Score:F4}, DocId: {DocId}, Preview: {Preview}", 
                r.Score, 
                r.DocumentId,
                r.Content.Length > 100 ? r.Content.Substring(0, 100) + "..." : r.Content);
        }

        return topResults;
    }

    public async Task DeleteDocumentAsync(string documentId)
    {
        var documents = _dbContext.VectorDocuments.Where(d => d.DocumentId == documentId);
        _dbContext.VectorDocuments.RemoveRange(documents);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogDebug("Deleted vectors for document {DocumentId}", documentId);
    }

    private double CosineSimilarity(float[] v1, float[] v2)
    {
        if (v1.Length != v2.Length)
        {
            _logger.LogWarning("Cosine similarity called with mismatched vector lengths: {Len1} vs {Len2}", 
                v1.Length, v2.Length);
            return 0;
        }

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

        var similarity = dotProduct / (Math.Sqrt(mag1) * Math.Sqrt(mag2));
        
        // Log very low similarities for debugging
        if (similarity < 0.1 && similarity > -0.1)
        {
            _logger.LogDebug("Very low similarity detected: {Similarity:F6}", similarity);
        }
        
        return similarity;
    }
}