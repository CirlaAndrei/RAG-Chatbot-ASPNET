using Microsoft.EntityFrameworkCore;
using RAGChatbot.Infrastructure.Data.Models;

namespace RAGChatbot.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<VectorDocumentEntity> VectorDocuments { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VectorDocumentEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => new { e.DocumentId, e.ChunkIndex });
            
            // Store the embedding as JSON for SQLite
            entity.Property(e => e.Embedding)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<float[]>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? Array.Empty<float>()
                );
        });
    }
}