using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketingAI.Data.Entities;

namespace TicketingAI.Data.Persistence.Configurations;

public class SavedAiModelConfiguration : IEntityTypeConfiguration<SavedAiModel>
{
    public void Configure(EntityTypeBuilder<SavedAiModel> builder)
    {
        builder.Property(m => m.Kind).HasMaxLength(20);
        builder.Property(m => m.Name).HasMaxLength(100);
        builder.Property(m => m.BaseUrl).HasMaxLength(200);
        builder.Property(m => m.Model).HasMaxLength(200);
        builder.Property(m => m.ApiKeyName).HasMaxLength(100);
        builder.Property(m => m.QueryPrefix).HasMaxLength(100);
        builder.Property(m => m.DocumentPrefix).HasMaxLength(100);
        builder.Property(m => m.Notes).HasMaxLength(500);
        builder.HasIndex(m => new { m.Kind, m.Name }).IsUnique();
    }
}
