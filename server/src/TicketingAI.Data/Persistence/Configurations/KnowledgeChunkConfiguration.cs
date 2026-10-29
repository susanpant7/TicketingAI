using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketingAI.Data.Entities;

namespace TicketingAI.Data.Persistence.Configurations;

public class KnowledgeChunkConfiguration : IEntityTypeConfiguration<KnowledgeChunk>
{
    public void Configure(EntityTypeBuilder<KnowledgeChunk> builder)
    {
        builder.Property(c => c.DocumentTitle).HasMaxLength(200);
        builder.Property(c => c.Category).HasMaxLength(50);
        builder.Property(c => c.Heading).HasMaxLength(200);
        builder.Property(c => c.SourceFile).HasMaxLength(260);
        builder.Property(c => c.ContentHash).HasMaxLength(64);

        builder.HasIndex(c => c.Category);
        builder.HasIndex(c => new { c.SourceFile, c.Heading }).IsUnique();
    }
}
