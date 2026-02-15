namespace RAGChatbot.Core.Models;

public class RagQuery
{
    public string Question { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public int MaxSources { get; set; } = 5;
    public double MinScore { get; set; } = 0.7;
    public Dictionary<string, object> Filter { get; set; } = new();
}