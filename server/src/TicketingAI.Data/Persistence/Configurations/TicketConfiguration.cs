using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketingAI.Data.Entities;

namespace TicketingAI.Data.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.Property(t => t.Title).HasMaxLength(200);
        builder.Property(t => t.Description).HasMaxLength(4000);

        // Users with tickets cannot be deleted.
        builder.HasOne(t => t.ReportedBy)
            .WithMany()
            .HasForeignKey(t => t.ReportedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.AssignedTo)
            .WithMany()
            .HasForeignKey(t => t.AssignedToId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
