using System.Text;
using Microsoft.Extensions.Logging;
using RAGChatbot.Core.Interfaces;
using RAGChatbot.Core.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace RAGChatbot.Infrastructure.DocumentProcessors;

public class PdfDocumentProcessor : IDocumentProcessor
{
    private readonly ILogger<PdfDocumentProcessor> _logger;

    public PdfDocumentProcessor(ILogger<PdfDocumentProcessor> logger)
    {
        _logger = logger;
    }

    public bool CanProcess(string fileExtension)
    {
        return fileExtension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IEnumerable<DocumentChunk>> ProcessDocumentAsync(
        Stream documentStream, 
        string fileName, 
        string documentId)
    {
        var chunks = new List<DocumentChunk>();
        
        // Process PDF using PdfPig (synchronous, so wrap in Task.Run)
        await Task.Run(() =>
        {
            using var pdf = PdfDocument.Open(documentStream);
            var currentChunk = new StringBuilder();
            var chunkIndex = 0;
            var pageCount = 1;
            
            foreach (var page in pdf.GetPages())
            {
                var pageText = page.Text;
                
                // Split page text into paragraphs
                var paragraphs = pageText.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var paragraph in paragraphs)
                {
                    if (currentChunk.Length + paragraph.Length > 1000) // ~1000 chars per chunk
                    {
                        if (currentChunk.Length > 0)
                        {
                            chunks.Add(CreateChunk(documentId, currentChunk.ToString(), chunkIndex++, fileName, pageCount));
                            currentChunk.Clear();
                        }
                    }
                    
                    if (currentChunk.Length > 0)
                        currentChunk.AppendLine();
                        
                    currentChunk.Append(paragraph);
                }
                
                pageCount++;
            }
            
            // Add the last chunk
            if (currentChunk.Length > 0)
            {
                chunks.Add(CreateChunk(documentId, currentChunk.ToString(), chunkIndex++, fileName, pageCount - 1));
            }
        });
        
        _logger.LogInformation("Processed PDF file {FileName} into {Count} chunks", fileName, chunks.Count);
        return chunks;
    }
    
    private DocumentChunk CreateChunk(string documentId, string content, int index, string fileName, int pageNumber = 1)
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
                ["pageNumber"] = pageNumber,
                ["content"] = content,
                ["source"] = "upload"
            }
        };
    }
}