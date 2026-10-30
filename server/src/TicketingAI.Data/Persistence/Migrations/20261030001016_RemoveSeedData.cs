using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TicketingAI.Data.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The default departments and users are no longer seeded; an admin loads them from the app.
            // A new database gets the rows from the earlier migrations, so they are removed here. A database that already has
            // tickets is left alone, because the tickets refer to those users.
            migrationBuilder.Sql(
                "DELETE FROM Users WHERE Id BETWEEN 1 AND 15 AND NOT EXISTS (SELECT 1 FROM Tickets);");
            migrationBuilder.Sql(
                "DELETE FROM Departments WHERE Id IN (1, 2, 3) AND NOT EXISTS (SELECT 1 FROM Users WHERE Users.DepartmentId = Departments.Id);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
