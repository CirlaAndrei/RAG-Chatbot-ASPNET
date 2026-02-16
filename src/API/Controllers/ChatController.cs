using Microsoft.AspNetCore.Mvc;
using RAGChatbot.Core.Models;
using RAGChatbot.Core.Services;

namespace RAGChatbot.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IRagService _ragService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IRagService ragService,
        ILogger<ChatController> logger)
    {
        _ragService = ragService;
        _logger = logger;
    }

    /// <summary>
    /// Ask a question to the RAG chatbot
    /// </summary>
    [HttpPost("ask")]
    public async Task<ActionResult<RagResponse>> AskQuestion([FromBody] RagQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.Question))
        {
            return BadRequest("Question cannot be empty");
        }

        // Generate a session ID if not provided
        if (string.IsNullOrEmpty(query.SessionId))
        {
            query.SessionId = Guid.NewGuid().ToString();
        }

        var response = await _ragService.AskQuestionAsync(query);
        
        if (!response.Success)
        {
            return StatusCode(500, response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { 
            status = "healthy", 
            timestamp = DateTime.UtcNow,
            service = "RAG Chatbot API"
        });
    }

    /// <summary>
    /// Stream a response (for real-time chat)
    /// </summary>
    [HttpPost("stream")]
    public async Task StreamResponse([FromBody] RagQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.Question))
        {
            Response.StatusCode = 400;
            await Response.WriteAsync("Question cannot be empty");
            return;
        }
        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";
        // Generate a session ID if not provided
        if (string.IsNullOrEmpty(query.SessionId))
        {
            query.SessionId = Guid.NewGuid().ToString();
        }

        try
        {
            var response = await _ragService.AskQuestionAsync(query);
            
            // Send the complete response as server-sent event
            await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(response)}\n\n");
            await Response.Body.FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in stream endpoint");
            await Response.WriteAsync($"data: {{\"error\":\"{ex.Message}\"}}\n\n");
            await Response.Body.FlushAsync();
        }
    }
}