using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TicketingAI.Data.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false),
                    DepartmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "IT" },
                    { 2, "HR" },
                    { 3, "Finance" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "DepartmentId", "Email", "FullName", "Password", "Username" },
                values: new object[,]
                {
                    { 1, 1, "aarav@ticket.com", "Aarav Sharma", "user", "aarav" },
                    { 2, 1, "bikash@ticket.com", "Bikash Thapa", "user", "bikash" },
                    { 3, 1, "anita@ticket.com", "Anita Gurung", "user", "anita" },
                    { 4, 1, "dipesh@ticket.com", "Dipesh Adhikari", "user", "dipesh" },
                    { 5, 1, "sita@ticket.com", "Sita Rai", "user", "sita" },
                    { 6, 2, "prakash@ticket.com", "Prakash Karki", "user", "prakash" },
                    { 7, 2, "sunita@ticket.com", "Sunita Shrestha", "user", "sunita" },
                    { 8, 2, "rajesh@ticket.com", "Rajesh Poudel", "user", "rajesh" },
                    { 9, 2, "manisha@ticket.com", "Manisha Tamang", "user", "manisha" },
                    { 10, 2, "nabin@ticket.com", "Nabin Khadka", "user", "nabin" },
                    { 11, 3, "binod@ticket.com", "Binod Bhandari", "user", "binod" },
                    { 12, 3, "kabita@ticket.com", "Kabita Magar", "user", "kabita" },
                    { 13, 3, "suman@ticket.com", "Suman Basnet", "user", "suman" },
                    { 14, 3, "puja@ticket.com", "Puja Lama", "user", "puja" },
                    { 15, 3, "roshan@ticket.com", "Roshan Acharya", "user", "roshan" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Name",
                table: "Departments",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId",
                table: "Users",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
