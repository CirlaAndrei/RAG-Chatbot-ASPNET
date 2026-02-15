using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RAGChatbot.Core.Interfaces;
using RAGChatbot.Infrastructure.Configuration;
using RAGChatbot.Infrastructure.VectorDb;

namespace RAGChatbot.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVectorDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        // Register settings
        services.Configure<VectorDbSettings>(configuration.GetSection("VectorDatabase"));

        // For now, always use InMemory until we properly set up Pinecone
        services.AddSingleton<IVectorDatabase, InMemoryVectorDatabase>();
        
        return services;
    }
}