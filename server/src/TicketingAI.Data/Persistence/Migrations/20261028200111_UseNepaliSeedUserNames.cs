using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketingAI.Data.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UseNepaliSeedUserNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "aarav@ticket.com", "Aarav Sharma", "aarav" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "bikash@ticket.com", "Bikash Thapa", "bikash" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "anita@ticket.com", "Anita Gurung", "anita" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "dipesh@ticket.com", "Dipesh Adhikari", "dipesh" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "sita@ticket.com", "Sita Rai", "sita" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "prakash@ticket.com", "Prakash Karki", "prakash" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "sunita@ticket.com", "Sunita Shrestha", "sunita" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "rajesh@ticket.com", "Rajesh Poudel", "rajesh" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "manisha@ticket.com", "Manisha Tamang", "manisha" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "nabin@ticket.com", "Nabin Khadka", "nabin" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "binod@ticket.com", "Binod Bhandari", "binod" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "kabita@ticket.com", "Kabita Magar", "kabita" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "suman@ticket.com", "Suman Basnet", "suman" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "puja@ticket.com", "Puja Lama", "puja" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "Email", "FullName", "Username" },
                values: new object[] { "roshan@ticket.com", "Roshan Acharya", "roshan" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
    }
}
