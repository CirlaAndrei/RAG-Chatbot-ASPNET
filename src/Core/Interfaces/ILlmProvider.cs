namespace RAGChatbot.Core.Interfaces;

public interface ILlmProvider
{
    Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    string ProviderName { get; }
}