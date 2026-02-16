using RAGChatbot.Core.Interfaces;
using RAGChatbot.Core.Models;
using System.Text;
using Microsoft.Extensions.Logging;


namespace RAGChatbot.Infrastructure.DocumentProcessors;

public class TextDocumentProcessor : IDocumentProcessor
{
    private readonly ILogger<TextDocumentProcessor> _logger;

    public TextDocumentProcessor(ILogger<TextDocumentProcessor> logger)
    {
        _logger = logger;
    }

    public bool CanProcess(string fileExtension)
    {
        return fileExtension.Equals(".txt", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IEnumerable<DocumentChunk>> ProcessDocumentAsync(
        Stream documentStream, 
        string fileName, 
        string documentId)
    {
        var chunks = new List<DocumentChunk>();
        
        using var reader = new StreamReader(documentStream);
        var content = await reader.ReadToEndAsync();
        
        // Simple chunking - split by paragraphs and combine to reasonable size
        var paragraphs = content.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        var currentChunk = new StringBuilder();
        var chunkIndex = 0;
        
        foreach (var paragraph in paragraphs)
        {
            if (currentChunk.Length + paragraph.Length > 1000) // ~1000 chars per chunk
            {
                if (currentChunk.Length > 0)
                {
                    chunks.Add(CreateChunk(documentId, currentChunk.ToString(), chunkIndex++, fileName));
                    currentChunk.Clear();
                }
            }
            
            if (currentChunk.Length > 0)
                currentChunk.AppendLine();
                
            currentChunk.Append(paragraph);
        }
        
        // Add the last chunk
        if (currentChunk.Length > 0)
        {
            chunks.Add(CreateChunk(documentId, currentChunk.ToString(), chunkIndex++, fileName));
        }
        
        _logger.LogInformation("Processed text file {FileName} into {Count} chunks", fileName, chunks.Count);
        return chunks;
    }
    
    private DocumentChunk CreateChunk(string documentId, string content, int index, string fileName)
    {
        return new DocumentChunk
        {
            DocumentId = documentId,
            Content = content,
            ChunkIndex = index,
            Metadata = new Dictionary<string, object>
            {
                ["fileName"] = fileName,
                ["chunkIndex"] = index,
                ["content"] = content,
                ["source"] = "upload"
            }
        };
    }
}