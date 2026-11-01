using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketingAI.Data.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiModelKeysAndEmbeddingModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedAiModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    BaseUrl = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ApiKeyName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    QueryPrefix = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DocumentPrefix = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedAiModels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedAiModels_Kind_Name",
                table: "SavedAiModels",
                columns: new[] { "Kind", "Name" },
                unique: true);

            // The chat models saved so far become the "chat" ones of the new table (no key name yet: they use ChatApi__ApiKey).
            migrationBuilder.Sql(
                "INSERT INTO SavedAiModels (Kind, Name, BaseUrl, Model, ApiKeyName, QueryPrefix, DocumentPrefix, Notes) " +
                "SELECT 'chat', Name, BaseUrl, Model, '', '', '', Notes FROM SavedChatModels;");

            migrationBuilder.DropTable(
                name: "SavedChatModels");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedAiModels");

            migrationBuilder.CreateTable(
                name: "SavedChatModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BaseUrl = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedChatModels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedChatModels_Name",
                table: "SavedChatModels",
                column: "Name",
                unique: true);
        }
    }
}
