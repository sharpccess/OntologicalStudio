using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OntologicalStudio.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHydrationLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HydrationLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PromptUsed = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderUsed = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RawResponse = table.Column<string>(type: "TEXT", nullable: false),
                    AppliedFields = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HydrationLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HydrationLog_Entity_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Entity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HydrationLog_EntityId",
                table: "HydrationLog",
                column: "EntityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HydrationLog");
        }
    }
}
