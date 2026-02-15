using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RAGChatbot.Core.Interfaces;
using RAGChatbot.Core.Services;
using RAGChatbot.Infrastructure.Configuration;
using RAGChatbot.Infrastructure.LLM;
using RAGChatbot.Infrastructure.Services;
using RAGChatbot.Infrastructure.VectorDb;

namespace RAGChatbot.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register settings
        services.Configure<VectorDbSettings>(configuration.GetSection("VectorDatabase"));
        services.Configure<LlmSettings>(configuration.GetSection("LlmSettings"));
        services.Configure<RagSettings>(configuration.GetSection("RagSettings"));

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

        // Register Vector Database (always InMemory for now)
        services.AddSingleton<IVectorDatabase, InMemoryVectorDatabase>();

        // Register RAG Service
        services.AddScoped<IRagService, RagService>();

        return services;
    }
}