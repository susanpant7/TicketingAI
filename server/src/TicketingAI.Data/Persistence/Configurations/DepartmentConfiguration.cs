using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketingAI.Data.Entities;

namespace TicketingAI.Data.Persistence.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.Property(d => d.Name).HasMaxLength(100);
        builder.Property(d => d.Description).HasMaxLength(500);
        builder.Property(d => d.Contact).HasMaxLength(300);
        builder.HasIndex(d => d.Name).IsUnique();
    }
}
