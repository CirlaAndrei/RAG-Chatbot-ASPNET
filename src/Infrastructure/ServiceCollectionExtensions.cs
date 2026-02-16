using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RAGChatbot.Core.Interfaces;
using RAGChatbot.Core.Services;
using RAGChatbot.Infrastructure.Configuration;
using RAGChatbot.Infrastructure.Data;
using RAGChatbot.Infrastructure.DocumentProcessors;
using RAGChatbot.Infrastructure.LLM;
using RAGChatbot.Infrastructure.Services;
using RAGChatbot.Infrastructure.VectorDb;
using System.IO;

namespace RAGChatbot.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register settings
        services.Configure<VectorDbSettings>(configuration.GetSection("VectorDatabase"));
        services.Configure<LlmSettings>(configuration.GetSection("LlmSettings"));
        services.Configure<RagSettings>(configuration.GetSection("RagSettings"));

        // Register Database
        services.AddDbContext<AppDbContext>(options =>
        {
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "ragchatbot.db");
            options.UseSqlite($"Data Source={dbPath}");
        });
                
        // Register LLM Provider
        services.AddHttpClient();
        
        var llmProvider = configuration["LlmSettings:Provider"]?.ToLower() ?? "ollama";
        
        switch (llmProvider)
        {
            case "openai":
                services.AddHttpClient<ILlmProvider, OpenAIProvider>();
                break;
            case "ollama":
            default:
                services.AddHttpClient<ILlmProvider, OllamaProvider>();
                break;
        }

        // Register Vector Database (using SQLite now)
        services.AddScoped<IVectorDatabase, SqliteVectorDatabase>();

        // Register Document Processors
        services.AddScoped<IDocumentProcessor, TextDocumentProcessor>();
        services.AddScoped<IDocumentProcessor, PdfDocumentProcessor>();
        services.AddScoped<IDocumentProcessorFactory, DocumentProcessorFactory>();

        // Register RAG Service
        services.AddScoped<IRagService, RagService>();

        return services;
    }
}