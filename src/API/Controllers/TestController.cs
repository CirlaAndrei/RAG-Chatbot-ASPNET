using Microsoft.AspNetCore.Mvc;
using RAGChatbot.Core.Interfaces;
using RAGChatbot.Core.Models;

namespace RAGChatbot.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly IVectorDatabase _vectorDatabase;
    private readonly ILlmProvider _llmProvider;
    private readonly ILogger<TestController> _logger;

    public TestController(
        IVectorDatabase vectorDatabase,
        ILlmProvider llmProvider,
        ILogger<TestController> logger)
    {
        _vectorDatabase = vectorDatabase;
        _llmProvider = llmProvider;
        _logger = logger;
    }

    /// <summary>
    /// Add sample documents to test the RAG system
    /// </summary>
    [HttpPost("add-sample-data")]
    public async Task<IActionResult> AddSampleData()
    {
        var samples = new[]
        {
            new { 
                DocumentId = "doc1", 
                Content = "ASP.NET Core is a cross-platform, high-performance framework for building modern, cloud-based, internet-connected applications." 
            },
            new { 
                DocumentId = "doc2", 
                Content = "RAG (Retrieval-Augmented Generation) combines information retrieval with language models to generate accurate, context-aware responses." 
            },
            new { 
                DocumentId = "doc3", 
                Content = "Vector databases like Pinecone and Qdrant store embeddings for efficient similarity search in AI applications." 
            },
            new { 
                DocumentId = "doc4", 
                Content = "Clean Architecture separates concerns into layers: Domain, Application, Infrastructure, and Presentation, making code maintainable and testable." 
            }
        };

        foreach (var sample in samples)
        {
            var chunkId = Guid.NewGuid().ToString();
            var embedding = await _llmProvider.GenerateEmbeddingAsync(sample.Content);
            
            var metadata = new Dictionary<string, object>
            {
                ["content"] = sample.Content,
                ["source"] = "sample-data"
            };

            await _vectorDatabase.StoreEmbeddingAsync(
                sample.DocumentId,
                chunkId,
                new ReadOnlyMemory<float>(embedding.ToArray()),
                metadata);
        }

        return Ok(new { message = "Sample data added successfully", count = samples.Length });
    }

    /// <summary>
    /// Get statistics about the vector database
    /// </summary>
    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        // This is a simple placeholder - InMemory vector DB doesn't expose stats directly
        return Ok(new { 
            message = "Vector database is running",
            provider = "InMemory",
            status = "healthy"
        });
    }
}