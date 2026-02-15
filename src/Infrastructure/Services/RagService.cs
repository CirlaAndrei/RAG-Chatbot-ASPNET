using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RAGChatbot.Core.Interfaces;
using RAGChatbot.Core.Models;
using RAGChatbot.Core.Services;
using RAGChatbot.Infrastructure.Configuration;

namespace RAGChatbot.Infrastructure.Services;

public class RagService : IRagService
{
    private readonly ILlmProvider _llmProvider;
    private readonly IVectorDatabase _vectorDatabase;
    private readonly ILogger<RagService> _logger;
    private readonly RagSettings _settings;

    public RagService(
        ILlmProvider llmProvider,
        IVectorDatabase vectorDatabase,
        IOptions<RagSettings> settings,
        ILogger<RagService> logger)
    {
        _llmProvider = llmProvider;
        _vectorDatabase = vectorDatabase;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<RagResponse> AskQuestionAsync(RagQuery query, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new RagResponse
        {
            SessionId = query.SessionId
        };

        try
        {
            _logger.LogInformation("Processing question: {Question}", query.Question);

            // Step 1: Generate embedding for the question
            _logger.LogDebug("Generating embedding for question");
            var questionEmbedding = await _llmProvider.GenerateEmbeddingAsync(query.Question, cancellationToken);
            
            // Step 2: Find similar documents
            _logger.LogDebug("Searching for similar documents");
            var similarDocs = await _vectorDatabase.FindSimilarAsync(
                new ReadOnlyMemory<float>(questionEmbedding.ToArray()), 
                query.MaxSources);
            
            // Filter by minimum score
            var relevantDocs = similarDocs
                .Where(d => d.Score >= query.MinScore)
                .ToList();

            _logger.LogInformation("Found {Count} relevant documents with score >= {MinScore}", 
                relevantDocs.Count, query.MinScore);

            if (!relevantDocs.Any())
            {
                response.Answer = _settings.NoResultsMessage;
                response.Sources = new List<SimilarityResult>();
                return response;
            }

            // Step 3: Generate answer with sources
            var answer = await GenerateAnswerWithSourcesAsync(query.Question, relevantDocs, cancellationToken);
            
            response.Answer = answer;
            response.Sources = relevantDocs;
            response.Success = true;

            // Estimate tokens (rough approximation)
            response.TokensUsed = (query.Question.Length / 4) + (answer.Length / 4) + 
                                 (relevantDocs.Sum(d => d.Content.Length) / 4);

            _logger.LogInformation("Successfully generated answer in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing question: {Question}", query.Question);
            response.Success = false;
            response.ErrorMessage = ex.Message;
            response.Answer = _settings.ErrorMessage;
        }

        response.ProcessingTime = stopwatch.Elapsed;
        return response;
    }

    public async Task<string> GenerateAnswerWithSourcesAsync(
        string question, 
        List<SimilarityResult> sources, 
        CancellationToken cancellationToken = default)
    {
        // Build context from sources
        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("Based on the following information:\n");
        
        for (int i = 0; i < sources.Count; i++)
        {
            var source = sources[i];
            contextBuilder.AppendLine($"[Source {i + 1}]");
            contextBuilder.AppendLine(source.Content);
            contextBuilder.AppendLine();
        }

        contextBuilder.AppendLine($"Question: {question}");
        contextBuilder.AppendLine("\nPlease provide a comprehensive answer based ONLY on the information above. " +
                                 "If the information doesn't contain the answer, say so. " +
                                 "When referencing information, cite the source number like [Source 1].");

        var prompt = contextBuilder.ToString();
        
        var answer = await _llmProvider.GenerateCompletionAsync(prompt, cancellationToken);
        
        return answer;
    }
}