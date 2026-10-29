using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketingAI.Data.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentDescriptionAndContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Contact",
                table: "Departments",
                type: "TEXT",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Departments",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Contact", "Description" },
                values: new object[] { "it-help@ticket.com or extension 100", "laptops, accounts, access to systems, tools or data, VPN, software, network" });

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Contact", "Description" },
                values: new object[] { "hr@ticket.com (payroll and salary: payroll@ticket.com)", "leave, benefits, policies, conduct, onboarding" });

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Contact", "Description" },
                values: new object[] { "finance@ticket.com", "purchases, payments, expenses, budgets, invoices" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Contact",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Departments");
        }
    }
}
