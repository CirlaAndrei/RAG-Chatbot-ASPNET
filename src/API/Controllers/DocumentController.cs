using Microsoft.AspNetCore.Mvc;
using RAGChatbot.Core.Interfaces;
using RAGChatbot.Infrastructure.DocumentProcessors;

namespace RAGChatbot.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentController : ControllerBase
{
    private readonly IDocumentProcessorFactory _processorFactory;
    private readonly IVectorDatabase _vectorDatabase;
    private readonly ILlmProvider _llmProvider;
    private readonly ILogger<DocumentController> _logger;

    public DocumentController(
        IDocumentProcessorFactory processorFactory,
        IVectorDatabase vectorDatabase,
        ILlmProvider llmProvider,
        ILogger<DocumentController> logger)
    {
        _processorFactory = processorFactory;
        _vectorDatabase = vectorDatabase;
        _llmProvider = llmProvider;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        _logger.LogInformation("=== STARTING UPLOAD ===");
        _logger.LogInformation("File: {FileName}, Size: {FileSize} bytes", file.FileName, file.Length);

        try
        {
            _logger.LogInformation("Getting processor for file: {FileName}", file.FileName);
            var processor = _processorFactory.GetProcessor(file.FileName);
            
            if (processor == null)
            {
                _logger.LogWarning("No processor found for file type: {FileType}", Path.GetExtension(file.FileName));
                return BadRequest($"Unsupported file type: {Path.GetExtension(file.FileName)}");
            }

            _logger.LogInformation("Using processor: {ProcessorType}", processor.GetType().Name);

            var documentId = Guid.NewGuid().ToString();
            _logger.LogInformation("Generated documentId: {DocumentId}", documentId);
            
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;
            _logger.LogInformation("File copied to memory stream, size: {StreamSize} bytes", stream.Length);
            
            // Process the document into chunks
            _logger.LogInformation("Starting document processing...");
            var chunks = await processor.ProcessDocumentAsync(stream, file.FileName, documentId);
            var chunksList = chunks.ToList();
            _logger.LogInformation("Document processed into {ChunkCount} chunks", chunksList.Count);
            
            if (!chunksList.Any())
            {
                _logger.LogWarning("No chunks were generated from document {FileName}", file.FileName);
                return Ok(new
                {
                    documentId = documentId,
                    fileName = file.FileName,
                    chunksProcessed = 0,
                    message = $"No text content could be extracted from {file.FileName}"
                });
            }

            // Log first chunk preview
            _logger.LogInformation("First chunk preview: {Preview}", 
                chunksList.First().Content.Length > 100 
                    ? chunksList.First().Content.Substring(0, 100) + "..." 
                    : chunksList.First().Content);
            
            // Generate embeddings and store each chunk
            var processedChunks = 0;
            foreach (var chunk in chunksList)
            {
                _logger.LogInformation("Processing chunk {ProcessedChunks}/{TotalChunks}, length: {Length} chars", 
                    processedChunks + 1, chunksList.Count, chunk.Content.Length);
                
                try
                {
                    _logger.LogInformation("Generating embedding for chunk {ChunkIndex}", chunk.ChunkIndex);
                    var embedding = await _llmProvider.GenerateEmbeddingAsync(chunk.Content);
                    _logger.LogInformation("Embedding generated, size: {EmbeddingSize} dimensions", embedding.Count);
                    
                    _logger.LogInformation("Storing embedding in vector database");
                    await _vectorDatabase.StoreEmbeddingAsync(
                        documentId,
                        chunk.Id,
                        new ReadOnlyMemory<float>(embedding.ToArray()),
                        chunk.Metadata);
                    
                    processedChunks++;
                    _logger.LogInformation("Successfully stored chunk {ProcessedChunks}", processedChunks);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process chunk {ChunkIndex}", chunk.ChunkIndex);
                    throw;
                }
            }
            
            _logger.LogInformation("=== UPLOAD COMPLETE: {ProcessedChunks} chunks stored for document {DocumentId} ===", 
                processedChunks, documentId);
            
            return Ok(new
            {
                documentId = documentId,
                fileName = file.FileName,
                chunksProcessed = processedChunks,
                message = $"Successfully processed {processedChunks} chunks from {file.FileName}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== UPLOAD FAILED: Error uploading document {FileName} ===", file.FileName);
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpGet("supported-formats")]
    public IActionResult GetSupportedFormats()
    {
        return Ok(new
        {
            formats = new[] { ".txt", ".pdf" },
            description = "Supported file formats for document upload"
        });
    }

    [HttpDelete("{documentId}")]
    public async Task<IActionResult> DeleteDocument(string documentId)
    {
        try
        {
            _logger.LogInformation("Deleting document {DocumentId}", documentId);
            await _vectorDatabase.DeleteDocumentAsync(documentId);
            _logger.LogInformation("Document {DocumentId} deleted successfully", documentId);
            return Ok(new { message = $"Document {documentId} deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}