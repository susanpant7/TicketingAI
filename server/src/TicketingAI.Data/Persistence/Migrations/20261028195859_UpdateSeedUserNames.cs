using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketingAI.Data.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSeedUserNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "alex@ticket.com", "Alex Morgan", "alex" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "ben@ticket.com", "Ben Carter", "ben" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "chloe@ticket.com", "Chloe Bennett", "chloe" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "david@ticket.com", "David Kim", "david" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "emma@ticket.com", "Emma Davis", "emma" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "fiona@ticket.com", "Fiona Walsh", "fiona" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "george@ticket.com", "George Patel", "george" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "hannah@ticket.com", "Hannah Lee", "hannah" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "ian@ticket.com", "Ian Brooks", "ian" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "julia@ticket.com", "Julia Sanchez", "julia" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "kevin@ticket.com", "Kevin Turner", "kevin" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "laura@ticket.com", "Laura Nguyen", "laura" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "mike@ticket.com", "Mike Roberts", "mike" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "nina@ticket.com", "Nina Cooper", "nina" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "oscar@ticket.com", "Oscar Reed", "oscar" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "it1@ticketingai.local", "IT User 1", "it1" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "it2@ticketingai.local", "IT User 2", "it2" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "it3@ticketingai.local", "IT User 3", "it3" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "it4@ticketingai.local", "IT User 4", "it4" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "it5@ticketingai.local", "IT User 5", "it5" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "hr1@ticketingai.local", "HR User 1", "hr1" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "hr2@ticketingai.local", "HR User 2", "hr2" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "hr3@ticketingai.local", "HR User 3", "hr3" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "hr4@ticketingai.local", "HR User 4", "hr4" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "hr5@ticketingai.local", "HR User 5", "hr5" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "finance1@ticketingai.local", "Finance User 1", "finance1" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "finance2@ticketingai.local", "Finance User 2", "finance2" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "finance3@ticketingai.local", "Finance User 3", "finance3" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "finance4@ticketingai.local", "Finance User 4", "finance4" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "finance5@ticketingai.local", "Finance User 5", "finance5" });
        }
    }
}
