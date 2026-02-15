namespace RAGChatbot.Infrastructure.Configuration;

public class VectorDbSettings
{
    public string Provider { get; set; } = "Pinecone";
    public string ApiKey { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string IndexName { get; set; } = "rag-chatbot";
    public int Dimension { get; set; } = 1536; // OpenAI embedding dimension
    public string Metric { get; set; } = "cosine";
}