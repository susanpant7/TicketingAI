using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TicketingAI.Data.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "DepartmentId", "Email", "FullName", "Password", "Username" },
                values: new object[,]
                {
                    { 1, 1, "it1@ticketingai.local", "IT User 1", "user", "it1" },
                    { 2, 1, "it2@ticketingai.local", "IT User 2", "user", "it2" },
                    { 3, 1, "it3@ticketingai.local", "IT User 3", "user", "it3" },
                    { 4, 1, "it4@ticketingai.local", "IT User 4", "user", "it4" },
                    { 5, 1, "it5@ticketingai.local", "IT User 5", "user", "it5" },
                    { 6, 2, "hr1@ticketingai.local", "HR User 1", "user", "hr1" },
                    { 7, 2, "hr2@ticketingai.local", "HR User 2", "user", "hr2" },
                    { 8, 2, "hr3@ticketingai.local", "HR User 3", "user", "hr3" },
                    { 9, 2, "hr4@ticketingai.local", "HR User 4", "user", "hr4" },
                    { 10, 2, "hr5@ticketingai.local", "HR User 5", "user", "hr5" },
                    { 11, 3, "finance1@ticketingai.local", "Finance User 1", "user", "finance1" },
                    { 12, 3, "finance2@ticketingai.local", "Finance User 2", "user", "finance2" },
                    { 13, 3, "finance3@ticketingai.local", "Finance User 3", "user", "finance3" },
                    { 14, 3, "finance4@ticketingai.local", "Finance User 4", "user", "finance4" },
                    { 15, 3, "finance5@ticketingai.local", "Finance User 5", "user", "finance5" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 15);
        }
    }
}
