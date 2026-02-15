namespace RAGChatbot.Core.Interfaces;
using RAGChatbot.Core.Models;

public interface IDocumentProcessor
{
    Task<IEnumerable<DocumentChunk>> ProcessDocumentAsync(Stream documentStream, string fileName, string documentId);
    bool CanProcess(string fileExtension);
}