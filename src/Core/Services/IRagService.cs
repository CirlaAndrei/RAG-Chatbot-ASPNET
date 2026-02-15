using RAGChatbot.Core.Models;

namespace RAGChatbot.Core.Services;

public interface IRagService
{
    Task<RagResponse> AskQuestionAsync(RagQuery query, CancellationToken cancellationToken = default);
    Task<string> GenerateAnswerWithSourcesAsync(string question, List<SimilarityResult> sources, CancellationToken cancellationToken = default);
}