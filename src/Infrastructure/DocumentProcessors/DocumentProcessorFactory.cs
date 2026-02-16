using Microsoft.Extensions.Logging;
using RAGChatbot.Core.Interfaces;

namespace RAGChatbot.Infrastructure.DocumentProcessors;

public interface IDocumentProcessorFactory
{
    IDocumentProcessor? GetProcessor(string fileName);
}

public class DocumentProcessorFactory : IDocumentProcessorFactory
{
    private readonly IEnumerable<IDocumentProcessor> _processors;
    private readonly ILogger<DocumentProcessorFactory> _logger;

    public DocumentProcessorFactory(
        IEnumerable<IDocumentProcessor> processors,
        ILogger<DocumentProcessorFactory> logger)
    {
        _processors = processors;
        _logger = logger;
    }

    public IDocumentProcessor? GetProcessor(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        
        var processor = _processors.FirstOrDefault(p => p.CanProcess(extension));
        
        if (processor == null)
        {
            _logger.LogWarning("No processor found for file extension {Extension}", extension);
        }
        
        return processor;
    }
}