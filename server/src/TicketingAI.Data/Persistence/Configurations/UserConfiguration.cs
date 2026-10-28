using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketingAI.Data.Entities;

namespace TicketingAI.Data.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.Username).HasMaxLength(50);
        builder.Property(u => u.FullName).HasMaxLength(100);
        builder.Property(u => u.Email).HasMaxLength(256);
        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.HasOne(u => u.Department)
            .WithMany(d => d.Users)
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(SeedUsers());
    }

    // 5 users per department (DepartmentId 1 = IT, 2 = HR, 3 = Finance), password: user
    private static IEnumerable<User> SeedUsers()
    {
        (string First, string Last)[][] namesByDepartment =
        [
            [("Aarav", "Sharma"), ("Bikash", "Thapa"), ("Anita", "Gurung"), ("Dipesh", "Adhikari"), ("Sita", "Rai")],
            [("Prakash", "Karki"), ("Sunita", "Shrestha"), ("Rajesh", "Poudel"), ("Manisha", "Tamang"), ("Nabin", "Khadka")],
            [("Binod", "Bhandari"), ("Kabita", "Magar"), ("Suman", "Basnet"), ("Puja", "Lama"), ("Roshan", "Acharya")]
        ];

        for (var d = 0; d < namesByDepartment.Length; d++)
        {
            for (var n = 0; n < namesByDepartment[d].Length; n++)
            {
                var (first, last) = namesByDepartment[d][n];
                var username = first.ToLowerInvariant();
                yield return new User
                {
                    Id = d * 5 + n + 1,
                    Username = username,
                    FullName = $"{first} {last}",
                    Email = $"{username}@ticket.com",
                    Password = "user",
                    DepartmentId = d + 1
                };
            }
        }
    }
}
