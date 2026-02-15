namespace RAGChatbot.Infrastructure.Configuration;

public class RagSettings
{
    public int MaxSources { get; set; } = 5;
    public double MinScore { get; set; } = 0.7;
    public string NoResultsMessage { get; set; } = "I couldn't find any relevant information to answer your question.";
    public string ErrorMessage { get; set; } = "An error occurred while processing your request. Please try again.";
}