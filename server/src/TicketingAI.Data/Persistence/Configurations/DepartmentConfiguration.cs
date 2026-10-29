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

        builder.HasData(
            new Department
            {
                Id = 1, Name = "IT",
                Description = "laptops, accounts, access to systems, tools or data, VPN, software, network",
                Contact = "it-help@ticket.com or extension 100"
            },
            new Department
            {
                Id = 2, Name = "HR",
                Description = "leave, benefits, policies, conduct, onboarding",
                Contact = "hr@ticket.com (payroll and salary: payroll@ticket.com)"
            },
            new Department
            {
                Id = 3, Name = "Finance",
                Description = "purchases, payments, expenses, budgets, invoices",
                Contact = "finance@ticket.com"
            });
    }
}
