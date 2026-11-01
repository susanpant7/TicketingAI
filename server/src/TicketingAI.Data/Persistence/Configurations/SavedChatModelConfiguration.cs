using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketingAI.Data.Entities;

namespace TicketingAI.Data.Persistence.Configurations;

public class SavedChatModelConfiguration : IEntityTypeConfiguration<SavedChatModel>
{
    public void Configure(EntityTypeBuilder<SavedChatModel> builder)
    {
        builder.Property(m => m.Name).HasMaxLength(100);
        builder.Property(m => m.BaseUrl).HasMaxLength(200);
        builder.Property(m => m.Model).HasMaxLength(200);
        builder.Property(m => m.Notes).HasMaxLength(500);
        builder.HasIndex(m => m.Name).IsUnique();
    }
}
