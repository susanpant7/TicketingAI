using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketingAI.Data.Entities;

namespace TicketingAI.Data.Persistence.Configurations;

public class StoredChatMessageConfiguration : IEntityTypeConfiguration<StoredChatMessage>
{
    public void Configure(EntityTypeBuilder<StoredChatMessage> builder)
    {
        builder.ToTable("ChatMessages");
        builder.Property(m => m.Role).HasMaxLength(20);
        builder.Property(m => m.Content).HasMaxLength(8000);
        builder.Property(m => m.Department).HasMaxLength(100);

        // A deleted user takes the chat with it.
        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.UserId, m.Id });
    }
}
