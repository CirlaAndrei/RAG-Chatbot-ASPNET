using Microsoft.AspNetCore.Mvc;
using RAGChatbot.Core.Interfaces;
using System.IO; // Add this for Path

namespace RAGChatbot.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DebugController : ControllerBase
{
    private readonly IVectorDatabase _vectorDatabase;
    private readonly ILogger<DebugController> _logger;

    public DebugController(
        IVectorDatabase vectorDatabase,
        ILogger<DebugController> logger)
    {
        _vectorDatabase = vectorDatabase;
        _logger = logger;
    }

    [HttpGet("all-documents")]
    public async Task<IActionResult> GetAllDocuments()
    {
        try {
            // Create a zero vector to get all documents
            var testEmbedding = new float[1536];
            var allDocs = await _vectorDatabase.FindSimilarAsync(
                new ReadOnlyMemory<float>(testEmbedding), 
                100);
            
            var documentGroups = allDocs
                .GroupBy(d => d.DocumentId)
                .Select(g => new
                {
                    documentId = g.Key,
                    chunkCount = g.Count(),
                    preview = g.First().Content.Length > 100 
                        ? g.First().Content.Substring(0, 100) + "..." 
                        : g.First().Content
                });
            
            return Ok(documentGroups);
        }
        catch (Exception ex)
        {
            return Ok(new { error = ex.Message });
        }
    }

    [HttpGet("inspect-document/{documentId}")]
    public async Task<IActionResult> InspectDocument(string documentId)
    {
        try {
            var testEmbedding = new float[1536];
            var allDocs = await _vectorDatabase.FindSimilarAsync(
                new ReadOnlyMemory<float>(testEmbedding), 
                100);
                
            var documentChunks = allDocs.Where(d => d.DocumentId == documentId).ToList();
            
            return Ok(new
            {
                documentId = documentId,
                chunkCount = documentChunks.Count,
                chunks = documentChunks.Select((c, i) => new
                {
                    chunkIndex = i,
                    content = c.Content,
                    contentLength = c.Content.Length,
                    score = c.Score,
                    metadata = c.Metadata
                })
            });
        }
        catch (Exception ex)
        {
            return Ok(new { error = ex.Message });
        }
    }

    [HttpGet("search-test")]
    public async Task<IActionResult> SearchTest([FromQuery] string keyword)
    {
        try {
            var testEmbedding = new float[1536];
            var allDocs = await _vectorDatabase.FindSimilarAsync(
                new ReadOnlyMemory<float>(testEmbedding), 
                100);
            
            var matches = allDocs
                .Where(d => d.Content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .Select(d => new
                {
                    preview = d.Content.Length > 100 ? d.Content.Substring(0, 100) + "..." : d.Content,
                    documentId = d.DocumentId,
                    score = d.Score
                });
            
            return Ok(new
            {
                keyword = keyword,
                matchCount = matches.Count(),
                matches = matches
            });
        }
        catch (Exception ex)
        {
            return Ok(new { error = ex.Message });
        }
    }

    [HttpGet("check-pdf-processor")]
    public IActionResult CheckPdfProcessor()
    {
        try {
            return Ok(new { 
                message = "Debug controller is working",
                vectorDbType = _vectorDatabase.GetType().Name,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return Ok(new { error = ex.Message, message = "Error checking processor" });
        }
    }

    [HttpPost("test-pdf-upload")]
    public async Task<IActionResult> TestPdfUpload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        try
        {
            // Just try to read the PDF text without storing it
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;
            
            using var pdf = UglyToad.PdfPig.PdfDocument.Open(stream);
            var allText = new System.Text.StringBuilder();
            var pages = new List<object>();
            
            foreach (var page in pdf.GetPages())
            {
                var pageText = page.Text;
                allText.AppendLine($"Page {page.Number}:");
                allText.AppendLine(pageText);
                allText.AppendLine();
                
                pages.Add(new
                {
                    pageNumber = page.Number,
                    text = pageText,
                    textLength = pageText.Length,
                    words = pageText.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length
                });
            }
            
            return Ok(new
            {
                fileName = file.FileName,
                pageCount = pdf.NumberOfPages,
                totalTextLength = allText.Length,
                pages = pages,
                fullText = allText.ToString()
            });
        }
        catch (Exception ex)
        {
            return Ok(new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpPost("test-text-upload")]
    public async Task<IActionResult> TestTextUpload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        try
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();
            
            return Ok(new
            {
                fileName = file.FileName,
                contentLength = content.Length,
                wordCount = content.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length,
                preview = content.Length > 500 ? content.Substring(0, 500) + "..." : content,
                fullContent = content
            });
        }
        catch (Exception ex)
        {
            return Ok(new { error = ex.Message });
        }
    }
[HttpGet("direct-text-search")]
public IActionResult DirectTextSearch([FromQuery] string text)
{
    try
    {
        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "ragchatbot.db");
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = "SELECT DocumentId, Content FROM VectorDocuments WHERE Content LIKE @text LIMIT 10;";
        command.Parameters.AddWithValue("@text", $"%{text}%");
        
        var reader = command.ExecuteReader();
        var results = new List<object>();
        
        while (reader.Read())
        {
            var content = reader.GetString(1);
            results.Add(new
            {
                documentId = reader.GetString(0),
                preview = content.Length > 200 
                    ? content.Substring(0, 200) + "..." 
                    : content,
                containsName = content.Contains("Andrei") ? "Yes" : "No"
            });
        }
        
        return Ok(new
        {
            searchText = text,
            matchCount = results.Count,
            results = results
        });
    }
    catch (Exception ex)
    {
        return Ok(new { error = ex.Message, stackTrace = ex.StackTrace });
    }
}

[HttpDelete("clear-sample-data")]
public async Task<IActionResult> ClearSampleData()
{
    try
    {
        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "ragchatbot.db");
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
        await connection.OpenAsync();
        
        // Delete documents with doc1, doc2, doc3, doc4 IDs (the sample data)
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM VectorDocuments WHERE DocumentId IN ('doc1', 'doc2', 'doc3', 'doc4');";
        var deleted = await command.ExecuteNonQueryAsync();
        
        return Ok(new { 
            message = $"Deleted {deleted} sample documents",
            remainingDocs = await GetDocumentCount(connection)
        });
    }
    catch (Exception ex)
    {
        return Ok(new { error = ex.Message });
    }
}

private async Task<int> GetDocumentCount(Microsoft.Data.Sqlite.SqliteConnection connection)
{
    var cmd = connection.CreateCommand();
    cmd.CommandText = "SELECT COUNT(*) FROM VectorDocuments;";
    return Convert.ToInt32(await cmd.ExecuteScalarAsync());
}
    // NEW METHOD: Force direct database check
    [HttpGet("force-check")]
    public IActionResult ForceCheck()
    {
        try
        {
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "ragchatbot.db");
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
            connection.Open();
            
            // Check if table exists
            var checkTableCmd = connection.CreateCommand();
            checkTableCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='VectorDocuments';";
            var tableExists = checkTableCmd.ExecuteScalar() != null;
            
            if (!tableExists)
            {
                return Ok(new { 
                    databasePath = dbPath,
                    databaseExists = System.IO.File.Exists(dbPath),
                    tableExists = false,
                    message = "Table does not exist"
                });
            }
            
            // Get count
            var countCmd = connection.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM VectorDocuments;";
            var count = (long)countCmd.ExecuteScalar();
            
            // Get samples
            var sampleCmd = connection.CreateCommand();
            sampleCmd.CommandText = "SELECT DocumentId, Content FROM VectorDocuments LIMIT 5;";
            var reader = sampleCmd.ExecuteReader();
            var samples = new List<object>();
            
            while (reader.Read())
            {
                samples.Add(new
                {
                    documentId = reader.GetString(0),
                    preview = reader.GetString(1).Length > 100 
                        ? reader.GetString(1).Substring(0, 100) + "..." 
                        : reader.GetString(1)
                });
            }
            
            return Ok(new
            {
                databasePath = dbPath,
                databaseExists = System.IO.File.Exists(dbPath),
                tableExists = true,
                totalDocuments = count,
                samples = samples,
                workingDirectory = Directory.GetCurrentDirectory()
            });
        }
        catch (Exception ex)
        {
            return Ok(new { 
                error = ex.Message,
                stackTrace = ex.StackTrace,
                workingDirectory = Directory.GetCurrentDirectory()
            });
        }
    }
}